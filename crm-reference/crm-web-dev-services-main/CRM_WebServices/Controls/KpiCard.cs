using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls
{
    /// <summary>
    /// A clickable KPI tile. Raises <see cref="KpiClicked"/> when the user
    /// taps the card. Hover feedback highlights the border + lifts the accent.
    /// </summary>
    public class KpiCard : Panel
    {
        private readonly Label _lblValue;
        private readonly Label _lblTitle;
        private readonly Color _accent;
        private readonly string _icon;

        private bool _isHovered;

        public string FilterKey { get; }

        /// <summary>Raised when the user clicks anywhere on the card.</summary>
        public event EventHandler? KpiClicked;

        public KpiCard(string title, string filterKey, Color accentColor, string icon = "•")
        {
            FilterKey = filterKey;
            _accent = accentColor;
            _icon = icon;

            Size = new Size(230, 110);
            BackColor = AppTheme.Surface;
            Cursor = Cursors.Hand;

            _lblTitle = new Label
            {
                Text = title,
                Font = AppTheme.FontKpiLabel,
                ForeColor = AppTheme.Trace,
                Location = new Point(20, 18),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            _lblValue = new Label
            {
                Text = "0",
                Font = AppTheme.FontKpiNumber,
                ForeColor = AppTheme.Ink,
                Location = new Point(16, 42),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            Controls.Add(_lblTitle);
            Controls.Add(_lblValue);

            // Bubble child clicks up to the card itself
            _lblTitle.Click += (_, _) => RaiseClick();
            _lblValue.Click += (_, _) => RaiseClick();
            Click += (_, _) => RaiseClick();

            // Hover state
            MouseEnter += (_, _) => SetHover(true);
            MouseLeave += (_, _) => SetHover(false);
            _lblTitle.MouseEnter += (_, _) => SetHover(true);
            _lblTitle.MouseLeave += (_, _) => SetHover(false);
            _lblValue.MouseEnter += (_, _) => SetHover(true);
            _lblValue.MouseLeave += (_, _) => SetHover(false);

            Paint += KpiCard_Paint;
        }

        public void SetValue(int value) => _lblValue.Text = value.ToString();

        private void SetHover(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            BackColor = hovered ? Color.FromArgb(0xFA, 0xFB, 0xFE) : AppTheme.Surface;
            Invalidate();
        }

        private void RaiseClick()
        {
            KpiClicked?.Invoke(this, EventArgs.Empty);
        }

        private void KpiCard_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Card outline — hairline normally, accent when hovered
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = UiRadiusHelper.CreateRoundedPath(rect, 12))
            using (var pen = new Pen(_isHovered ? _accent : AppTheme.Line, _isHovered ? 1.5f : 1f))
            {
                g.DrawPath(pen, path);
            }

            // Left accent bar
            using (var brush = new SolidBrush(_accent))
            {
                int barH = _isHovered ? Height - 16 : Height - 24;
                int barY = _isHovered ? 8 : 12;
                g.FillRectangle(brush, 0, barY, 3, barH);
            }

            // Icon (top-right)
            int bs = 28, bx = Width - bs - 16, by = 16;
            if (bx > 80)
            {
                using var iconFont = new Font("Segoe UI Emoji", 12F);
                using var iconBrush = new SolidBrush(_accent);
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_icon, iconFont, iconBrush, new RectangleF(bx, by, bs, bs), sf);
            }
        }
    }
}