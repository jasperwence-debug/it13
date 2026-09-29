using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls.Charts
{
    /// <summary>
    /// Horizontal bar chart. Pass a list of (label, value, optional color).
    /// Best for ranked lists like pipeline stages.
    /// </summary>
    public class HorizontalBarChartControl : Control
    {
        public class Row
        {
            public string Label { get; set; } = "";
            public double Value { get; set; }
            public Color Color { get; set; } = AppTheme.Signal;
        }

        private readonly List<Row> _rows = new();
        private string _title = "";
        private string _emptyText = "No data available";

        public HorizontalBarChartControl()
        {
            DoubleBuffered = true;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBodySmall;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetTitle(string title) { _title = title; Invalidate(); }
        public void SetEmptyText(string text) { _emptyText = text; Invalidate(); }

        public void SetData(IEnumerable<Row> rows)
        {
            _rows.Clear();
            _rows.AddRange(rows);
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
                using var titleFont = new Font("Bahnschrift SemiBold", 11F);
                using var titleBrush = new SolidBrush(AppTheme.Ink);
                g.DrawString(_title, titleFont, titleBrush, 20, top);
                top += 28;
            }

            if (_rows.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI Variable Text", 10F, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(AppTheme.TextMuted);
                var sz = g.MeasureString(_emptyText, emptyFont);
                g.DrawString(_emptyText, emptyFont, emptyBrush,
                    (Width - sz.Width) / 2,
                    (Height - sz.Height) / 2);
                return;
            }

            int leftLabel = 20;
            int labelWidth = 130;
            int rightMargin = 90;
            int chartLeft = leftLabel + labelWidth + 10;
            int chartW = Width - chartLeft - rightMargin;
            int rowH = 32;
            int gap = 8;
            int y = top + 8;

            if (chartW < 20) return;
            double maxValue = _rows.Max(r => r.Value);
            if (maxValue <= 0) maxValue = 1;

            using var labelFont = new Font("Segoe UI Variable Text", 9F);
            using var labelBrush = new SolidBrush(AppTheme.Ink);
            using var valueFont = new Font("Bahnschrift SemiBold", 9.5F);
            using var valueBrush = new SolidBrush(AppTheme.TextPrimary);

            foreach (var row in _rows)
            {
                // Label (left)
                var lblSz = g.MeasureString(row.Label, labelFont);
                g.DrawString(row.Label, labelFont, labelBrush,
                    leftLabel, y + (rowH - lblSz.Height) / 2);

                // Track background
                using (var trackBrush = new SolidBrush(AppTheme.NeutralBg))
                using (var trackPath = UiRadiusHelper.CreateRoundedPath(
                    new Rectangle(chartLeft, y + 6, chartW, rowH - 12), 4))
                    g.FillPath(trackBrush, trackPath);

                // Filled bar
                int barW = (int)(chartW * (row.Value / maxValue));
                if (barW > 0)
                {
                    using var barBrush = new SolidBrush(row.Color);
                    using var barPath = UiRadiusHelper.CreateRoundedPath(
                        new Rectangle(chartLeft, y + 6, barW, rowH - 12), 4);
                    g.FillPath(barBrush, barPath);
                }

                // Value (right)
                string v = FormatValue(row.Value);
                var vSz = g.MeasureString(v, valueFont);
                g.DrawString(v, valueFont, valueBrush,
                    chartLeft + chartW + 10, y + (rowH - vSz.Height) / 2);

                y += rowH + gap;
            }
        }

        private static string FormatValue(double v)
        {
            if (v >= 1_000_000) return $"₱{v / 1_000_000:0.#}M";
            if (v >= 1_000) return $"₱{v / 1_000:0.#}k";
            if (v == Math.Floor(v)) return ((int)v).ToString();
            return v.ToString("0.##");
        }
    }
}