using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls
{
    /// <summary>
    /// Floating toast notification that appears at the bottom-right of a parent form.
    /// Usage: ToastHost.Show(this, "Customer saved", ToastKind.Success);
    /// </summary>
    public enum ToastKind { Success, Error, Info }

    public class ToastHost : Panel
    {
        private System.Windows.Forms.Timer _lifeTimer = null!;
        private System.Windows.Forms.Timer _fadeTimer = null!;
        private readonly Label _icon = null!;
        private readonly Label _message = null!;
        private readonly Color _accent;
        private readonly int _originalHeight;

        private ToastHost(string text, ToastKind kind, int durationMs)
        {
            (var bg, _accent, var iconChar) = kind switch
            {
                ToastKind.Success => (Color.FromArgb(0xE6, 0xF4, 0xEA), AppTheme.SuccessText, "✓"),
                ToastKind.Error => (Color.FromArgb(0xFB, 0xEB, 0xEB), AppTheme.DangerText, "!"),
                _ => (AppTheme.InfoBg, AppTheme.InfoText, "i")
            };

            Size = new Size(340, 52);
            _originalHeight = Height;
            BackColor = AppTheme.Surface;

            _icon = new Label
            {
                Text = iconChar,
                Font = new Font("Bahnschrift SemiBold", 13F),
                ForeColor = _accent,
                BackColor = bg,
                Size = new Size(32, 32),
                Location = new Point(12, 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _icon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(bg);
                e.Graphics.FillEllipse(brush, 0, 0, _icon.Width - 1, _icon.Height - 1);
                TextRenderer.DrawText(e.Graphics, _icon.Text, _icon.Font,
                    new Rectangle(0, 0, _icon.Width, _icon.Height), _accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            Controls.Add(_icon);

            _message = new Label
            {
                Text = text,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.Transparent,
                Location = new Point(54, 6),
                Size = new Size(274, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            Controls.Add(_message);

            // Left accent bar + rounded border
            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(_accent);
                e.Graphics.FillRectangle(brush, 0, 8, 3, Math.Max(0, Height - 16));
                using var pen = new Pen(AppTheme.Line, 1);
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 10);
                e.Graphics.DrawPath(pen, path);
            };

            // Auto-dismiss: hold for durationMs, then collapse height
            _lifeTimer = new System.Windows.Forms.Timer { Interval = durationMs };
            _lifeTimer.Tick += (_, _) =>
            {
                _lifeTimer.Stop();
                _fadeTimer.Start();
            };

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeTimer.Tick += (_, _) =>
            {
                // Collapse from bottom up until height is ~0
                int newHeight = Height - 4;
                if (newHeight <= 4)
                {
                    _fadeTimer.Stop();
                    FadeOutComplete?.Invoke(this, EventArgs.Empty);
                    return;
                }
                // Keep the top edge anchored
                Top += 4;
                Height = newHeight;
            };

            _lifeTimer.Start();
        }

        public event EventHandler? FadeOutComplete;

        public static void Show(Control anchor, string message, ToastKind kind = ToastKind.Success, int durationMs = 3000)
        {
            // Find top-most Form to host
            var host = anchor as Form ?? anchor.FindForm();
            if (host == null) return;

            var toast = new ToastHost(message, kind, durationMs);

            // Position bottom-right with 24px margin
            var hostRect = host.ClientRectangle;
            int x = hostRect.Width - toast.Width - 24;
            int y = hostRect.Height - toast.Height - 24;
            toast.Location = new Point(x, y);
            toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Slide-up initial position
            toast.Top = y + 20;
            host.Controls.Add(toast);
            toast.BringToFront();

            // Animate slide in
            var slideTimer = new System.Windows.Forms.Timer { Interval = 15 };
            slideTimer.Tick += (_, _) =>
            {
                if (toast.Top <= y) { slideTimer.Stop(); return; }
                toast.Top -= 2;
            };
            slideTimer.Start();

            toast.FadeOutComplete += (_, _) =>
            {
                host.Controls.Remove(toast);
                toast.Dispose();
            };
        }
    }
}