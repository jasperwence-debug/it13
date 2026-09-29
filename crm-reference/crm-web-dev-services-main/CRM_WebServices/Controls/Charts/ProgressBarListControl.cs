using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls.Charts
{
    /// <summary>
    /// Stacked progress bars — one row per item, showing a 0..1 value.
    /// Perfect for per-rep conversion rates or completion percentages.
    /// </summary>
    public class ProgressBarListControl : Control
    {
        public class Item
        {
            public string Label { get; set; } = "";
            public string Subtitle { get; set; } = "";
            public double Value { get; set; }        // 0.0 to 1.0
            public Color Color { get; set; } = AppTheme.Signal;
        }

        private readonly List<Item> _items = new();
        private string _title = "";
        private string _emptyText = "No data available";

        public ProgressBarListControl()
        {
            DoubleBuffered = true;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBodySmall;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetTitle(string t) { _title = t; Invalidate(); }
        public void SetEmptyText(string t) { _emptyText = t; Invalidate(); }

        public void SetData(IEnumerable<Item> items)
        {
            _items.Clear();
            _items.AddRange(items);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var bg = new SolidBrush(AppTheme.Surface))
                g.FillRectangle(bg, ClientRectangle);
            using (var pen = new Pen(AppTheme.Line, 1))
            using (var path = UiRadiusHelper.CreateRoundedPath(
                new Rectangle(0, 0, Width - 1, Height - 1), 12))
                g.DrawPath(pen, path);

            int top = 16;
            if (!string.IsNullOrEmpty(_title))
            {
                using var tf = new Font("Bahnschrift SemiBold", 11F);
                using var tb = new SolidBrush(AppTheme.Ink);
                g.DrawString(_title, tf, tb, 20, top);
                top += 28;
            }

            if (_items.Count == 0)
            {
                using var ef = new Font("Segoe UI Variable Text", 10F, FontStyle.Italic);
                using var eb = new SolidBrush(AppTheme.TextMuted);
                var sz = g.MeasureString(_emptyText, ef);
                g.DrawString(_emptyText, ef, eb,
                    (Width - sz.Width) / 2, (Height - sz.Height) / 2);
                return;
            }

            int leftX = 20;
            int rightX = Width - 20;
            int barH = 10;
            int rowH = 52;
            int y = top + 8;

            using var labelFont = new Font("Segoe UI Variable Text", 9.5F);
            using var labelBrush = new SolidBrush(AppTheme.Ink);
            using var subFont = new Font("Segoe UI Variable Text", 8F);
            using var subBrush = new SolidBrush(AppTheme.Trace);
            using var valueFont = new Font("Bahnschrift SemiBold", 10F);
            using var valueBrush = new SolidBrush(AppTheme.TextPrimary);

            foreach (var item in _items)
            {
                if (y + rowH > Height) break;

                // Label
                g.DrawString(item.Label, labelFont, labelBrush, leftX, y);

                // Subtitle
                if (!string.IsNullOrEmpty(item.Subtitle))
                    g.DrawString(item.Subtitle, subFont, subBrush, leftX, y + 16);

                // Value (right-aligned)
                string pct = $"{item.Value * 100:0.#}%";
                var vsz = g.MeasureString(pct, valueFont);
                g.DrawString(pct, valueFont, valueBrush,
                    rightX - vsz.Width, y);

                // Track
                int barY = y + 34;
                using (var trackBrush = new SolidBrush(AppTheme.NeutralBg))
                using (var trackPath = UiRadiusHelper.CreateRoundedPath(
                    new Rectangle(leftX, barY, rightX - leftX, barH), barH / 2))
                    g.FillPath(trackBrush, trackPath);

                // Filled portion
                int filledW = (int)((rightX - leftX) * Math.Clamp(item.Value, 0, 1));
                if (filledW > 0)
                {
                    using var fillBrush = new SolidBrush(item.Color);
                    using var fillPath = UiRadiusHelper.CreateRoundedPath(
                        new Rectangle(leftX, barY, filledW, barH), barH / 2);
                    g.FillPath(fillBrush, fillPath);
                }

                y += rowH;
            }
        }
    }
}