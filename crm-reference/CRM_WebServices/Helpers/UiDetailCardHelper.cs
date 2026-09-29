using System.Drawing;
using System.Windows.Forms;

namespace CRM.winforms.Helpers
{
    /// <summary>Helper for building detail-card layouts (used in editor forms & detail views).</summary>
    public static class UiDetailCardHelper
    {
        public static Panel CreateCardHeader(string title, string? badge = null)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(0, 6),
                AutoSize = true
            };
            panel.Controls.Add(lbl);

            if (!string.IsNullOrWhiteSpace(badge))
            {
                var badgeLbl = new Label
                {
                    Text = badge,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = AppTheme.InfoText,
                    BackColor = AppTheme.InfoBg,
                    Padding = new Padding(8, 3, 8, 3),
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                badgeLbl.Location = new Point(panel.Width - badgeLbl.PreferredWidth - 4, 8);
                panel.Controls.Add(badgeLbl);
            }

            return panel;
        }

        public static Panel CreateDivider()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = AppTheme.Border,
                Margin = new Padding(0, 6, 0, 10)
            };
        }

        /// <summary>A two-column key/value row for detail cards.</summary>
        public static Panel CreateKeyValueRow(
            string leftKey, string leftValue,
            string? rightKey = null, string? rightValue = null)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = string.IsNullOrWhiteSpace(rightKey) ? 42 : 56,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 4)
            };

            panel.Controls.Add(MakeKVColumn(leftKey, leftValue, 0));
            if (!string.IsNullOrWhiteSpace(rightKey))
                panel.Controls.Add(MakeKVColumn(rightKey, rightValue ?? "", panel.Width / 2));
            return panel;
        }

        private static Panel MakeKVColumn(string key, string value, int x)
        {
            var col = new Panel
            {
                Location = new Point(x, 0),
                Size = new Size(300, 52),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblKey = new Label
            {
                Text = key.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(0, 0),
                AutoSize = true
            };

            var lblVal = new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(0, 18),
                AutoSize = false,
                Size = new Size(290, 24),
                AutoEllipsis = true
            };

            col.Controls.Add(lblKey);
            col.Controls.Add(lblVal);
            return col;
        }

        /// <summary>Creates a status pill label.</summary>
        public static Label CreatePillBadge(string text, Color bg, Color fg, Color border)
        {
            var lbl = new Label
            {
                Text = text.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true
            };
            UiRadiusHelper.ApplyPillShape(lbl);

            lbl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int radius = lbl.Height / 2;
                var rect = new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, radius);
                using var brush = new SolidBrush(lbl.BackColor);
                e.Graphics.FillPath(brush, path);
                using var pen = new Pen(border, 1);
                e.Graphics.DrawPath(pen, path);
            };
            return lbl;
        }

        /// <summary>Color tuple for a status string.</summary>
        public static (Color bg, Color fg, Color border) GetStatusColors(string? status)
        {
            var s = status?.ToLowerInvariant() ?? "";
            if (s.Contains("won") || s.Contains("active") || s.Contains("completed") || s.Contains("closed"))
                return (AppTheme.SuccessBg, AppTheme.SuccessText, Color.FromArgb(187, 247, 208));
            if (s.Contains("lost") || s.Contains("inactive") || s.Contains("cancelled"))
                return (AppTheme.DangerBg, AppTheme.DangerText, Color.FromArgb(252, 165, 165));
            if (s.Contains("new") || s.Contains("pending"))
                return (AppTheme.InfoBg, AppTheme.InfoText, Color.FromArgb(191, 219, 254));
            if (s.Contains("qualified") || s.Contains("proposal") || s.Contains("negotiation"))
                return (AppTheme.WarningBg, AppTheme.WarningText, Color.FromArgb(253, 230, 138));
            return (Color.FromArgb(241, 245, 249), AppTheme.TextSecondary, AppTheme.Border);
        }
    }
}