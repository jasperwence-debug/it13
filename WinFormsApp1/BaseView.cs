using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms
{
    /// <summary>
    /// Base class for all CRM views (UserControls).
    ///
    /// Provides:
    ///   - WS_EX_COMPOSITED to eliminate text ghosting and repaint artifacts.
    ///   - OptimizedDoubleBuffer + AllPaintingInWmPaint for flicker-free rendering.
    ///   - Theme.Background as the default BackColor.
    ///   - Theme.BodyFont as the default inherited Font.
    ///   - ApplyCardStyle(Panel) helper to stamp the standard card look.
    ///   - ShowToast(string, bool) for consistent non-intrusive feedback.
    ///
    /// Usage:
    ///   public class DashboardView : BaseView { ... }
    /// </summary>
    public class BaseView : UserControl
    {
        // ----------------------------------------------------------------
        // Toast overlay (floating, top-right)
        // ----------------------------------------------------------------
        private Panel _pnlToast = null!;
        private Label _lblToast = null!;
        private System.Windows.Forms.Timer _toastTimer = null!;

        // ----------------------------------------------------------------
        // WS_EX_COMPOSITED — prevents ghosting in layered WinForms controls
        // ----------------------------------------------------------------
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        public BaseView()
        {
            // ---- Anti-flicker ----
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            UpdateStyles();

            // ---- Default appearance from Theme ----
            BackColor = Theme.Background;
            Font = Theme.BodyFont;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;

            BuildToast();
        }

        // ================================================================
        // VIEW-LEVEL PERMISSIONS ARCHITECTURE
        // Override in derived views to lock down UI elements per role.
        // ================================================================
        /// <summary>
        /// Applies role-based permissions and view-level restrictions for the current user.
        /// Override in derived views to enforce view-only or partial states (e.g. disable Save/Delete buttons).
        /// </summary>
        /// <param name="userRole">The role string of the active user.</param>
        public virtual void ApplyViewPermissions(string userRole)
        {
        }

        // ================================================================
        // HELPER: ApplyCardStyle
        // Stamps the standard white-card look onto any panel.
        // Call once per panel, typically right after construction.
        // ================================================================
        protected static void ApplyCardStyle(Panel panel)
        {
            panel.BackColor = Theme.Surface;
            panel.Paint += (s, e) =>
            {
                e.Graphics.Clear(panel.BackColor);
                using var pen = new System.Drawing.Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
        }

        // ================================================================
        // HELPER: ApplySectionHeaderStyle
        // Renders a 64px header block with title + subtitle inside a card.
        // ================================================================
        protected static Panel CreateSectionHeader(string title, string subtitle)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 0, 0, 6)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.SubHeaderFont,
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Location = new Point(0, 4),
                AutoSize = true
            };
            pnl.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = subtitle,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                Location = new Point(0, lblTitle.PreferredHeight + 10),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 10)
            };
            pnl.Controls.Add(lblSub);

            return pnl;
        }

        // ================================================================
        // TOAST NOTIFICATION
        // Displays a compact floating notification in the top-right corner.
        //   isSuccess = true  → green (success)
        //   isSuccess = false → red (error/warning)
        // ================================================================
        private void BuildToast()
        {
            _pnlToast = new Panel
            {
                Size = new Size(380, 48),
                BackColor = Theme.Success,
                Visible = false,
                Padding = new Padding(16, 0, 16, 0)
            };
            _pnlToast.Paint += (s, e) =>
            {
                e.Graphics.Clear(_pnlToast.BackColor);
                using var pen = new System.Drawing.Pen(Color.FromArgb(0, 0, 0, 30), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlToast.Width - 1, _pnlToast.Height - 1);
            };

            _lblToast = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Theme.Success,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlToast.Controls.Add(_lblToast);
            Controls.Add(_pnlToast);

            _toastTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _toastTimer.Tick += (s, e) =>
            {
                _toastTimer.Stop();
                _pnlToast.Visible = false;
            };

            // Keep toast in top-right when the view resizes
            Resize += (s, e) => PositionToast();
        }

        private void PositionToast()
        {
            if (_pnlToast != null)
            {
                _pnlToast.Location = new Point(
                    Math.Max(10, Width - _pnlToast.Width - 28),
                    16);
            }
        }

        /// <summary>
        /// Shows a timed non-intrusive toast notification.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="isSuccess">
        ///   <c>true</c> for a green success toast;
        ///   <c>false</c> for a red error toast.
        /// </param>
        protected void ShowToast(string message, bool isSuccess)
        {
            _toastTimer.Stop();
            var bg = isSuccess ? Theme.Success : Theme.Danger;
            _pnlToast.BackColor = bg;
            _lblToast.BackColor = bg;
            _lblToast.Text = (isSuccess ? "✓  " : "⚠  ") + message;
            PositionToast();
            _pnlToast.Visible = true;
            _pnlToast.BringToFront();
            _toastTimer.Start();
        }
    }
}
