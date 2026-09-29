using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Helpers
{
    public static class UiRadiusHelper
    {
        public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(bounds); return path; }
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Draw a card outline (flat, no shadow).</summary>
        public static void StyleCard(Panel panel, int radius = 12, Color? borderColor = null)
        {
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = CreateRoundedPath(rect, radius);
                using var pen = new Pen(borderColor ?? AppTheme.Line, 1f);
                g.DrawPath(pen, path);
            };
        }

        /// <summary>Paints the button as a solid rounded rectangle with a label on top.</summary>
        public static void StyleButton(Button btn, int radius = 8)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using var path = CreateRoundedPath(rect, radius);
                using var brush = new SolidBrush(btn.BackColor);
                g.FillPath(brush, path);

                TextRenderer.DrawText(
                    g, btn.Text, btn.Font, rect, btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        public static void AttachHoverFeedback(Button btn, Color normalColor, Color hoverColor)
        {
            btn.MouseEnter += (_, _) => { btn.BackColor = hoverColor; btn.Invalidate(); };
            btn.MouseLeave += (_, _) => { btn.BackColor = normalColor; btn.Invalidate(); };
        }

        /// <summary>Rounded pill (fully round on the ends). Works for Button and Label.</summary>
        public static void ApplyPillShape(Control control)
        {
            if (control is Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
            }

            control.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int radius = control.Height / 2;
                var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);

                using var path = CreateRoundedPath(rect, radius);
                using var brush = new SolidBrush(control.BackColor);
                g.FillPath(brush, path);

                if (!string.IsNullOrEmpty(control.Text))
                {
                    TextRenderer.DrawText(
                        g, control.Text, control.Font, rect, control.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
            control.Invalidate();
        }

        /// <summary>Draws a circular avatar behind a label's text.</summary>
        public static void MakeCircularAvatar(Label label, Color? fillColor = null)
        {
            label.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int d = Math.Min(label.Width, label.Height) - 1;
                using var brush = new SolidBrush(fillColor ?? label.BackColor);
                g.FillEllipse(brush, 0, 0, d, d);

                TextRenderer.DrawText(
                    g, label.Text, label.Font, new Rectangle(0, 0, d, d), label.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        public static void SetPadding(Control control, int left, int right)
        {
            control.Padding = new Padding(left, control.Padding.Top, right, control.Padding.Bottom);
        }

        public static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
        }
    }
}