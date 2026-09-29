using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls.Charts
{
    /// <summary>
    /// Simple vertical bar chart. Pass a list of (label, value) pairs.
    /// </summary>
    public class BarChartControl : Control
    {
        public class Bar
        {
            public string Label { get; set; } = "";
            public double Value { get; set; }
            public Color Color { get; set; } = AppTheme.Signal;
        }

        private readonly List<Bar> _bars = new();
        private string _title = "";
        private string _emptyText = "No data available";

        public BarChartControl()
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

        public void SetData(IEnumerable<Bar> bars)
        {
            _bars.Clear();
            _bars.AddRange(bars);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background
            using (var bg = new SolidBrush(AppTheme.Surface))
                g.FillRectangle(bg, ClientRectangle);

            // Card border
            using (var pen = new Pen(AppTheme.Line, 1))
            using (var path = UiRadiusHelper.CreateRoundedPath(
                new Rectangle(0, 0, Width - 1, Height - 1), 12))
                g.DrawPath(pen, path);

            // Title
            int top = 16;
            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font("Bahnschrift SemiBold", 11F);
                using var titleBrush = new SolidBrush(AppTheme.Ink);
                g.DrawString(_title, titleFont, titleBrush, 20, top);
                top += 28;
            }

            if (_bars.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI Variable Text", 10F, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(AppTheme.TextMuted);
                var sz = g.MeasureString(_emptyText, emptyFont);
                g.DrawString(_emptyText, emptyFont, emptyBrush,
                    (Width - sz.Width) / 2,
                    (Height - sz.Height) / 2);
                return;
            }

            // Chart area
            int leftMargin = 52;
            int rightMargin = 20;
            int bottomMargin = 40;
            int topMargin = top + 8;
            int chartW = Width - leftMargin - rightMargin;
            int chartH = Height - topMargin - bottomMargin;

            if (chartW <= 20 || chartH <= 20) return;

            double maxValue = _bars.Max(b => b.Value);
            if (maxValue <= 0) maxValue = 1;

            // Y-axis gridlines (5 lines)
            using (var gridPen = new Pen(AppTheme.Line, 1) { DashStyle = DashStyle.Dot })
            using (var labelFont = new Font("Segoe UI Variable Text", 8F))
            using (var labelBrush = new SolidBrush(AppTheme.TextMuted))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = topMargin + chartH - (int)(chartH * i / 4.0);
                    g.DrawLine(gridPen, leftMargin, y, leftMargin + chartW, y);

                    double val = maxValue * i / 4.0;
                    string lbl = FormatValue(val);
                    var sz = g.MeasureString(lbl, labelFont);
                    g.DrawString(lbl, labelFont, labelBrush,
                        leftMargin - sz.Width - 6, y - sz.Height / 2);
                }
            }

            // Bars
            int n = _bars.Count;
            int gap = 8;
            int barW = Math.Max(8, (chartW - gap * (n - 1)) / n);

            using (var labelFont = new Font("Segoe UI Variable Text", 8F))
            using (var labelBrush = new SolidBrush(AppTheme.Trace))
            using (var valueFont = new Font("Bahnschrift SemiBold", 9F))
            using (var valueBrush = new SolidBrush(AppTheme.Ink))
            {
                for (int i = 0; i < n; i++)
                {
                    var bar = _bars[i];
                    int x = leftMargin + i * (barW + gap);
                    int barH = (int)(chartH * (bar.Value / maxValue));
                    int y = topMargin + chartH - barH;

                    // Bar
                    using (var brush = new SolidBrush(bar.Color))
                    using (var path = UiRadiusHelper.CreateRoundedPath(
                        new Rectangle(x, y, barW, barH), 4))
                        g.FillPath(brush, path);

                    // Value above bar
                    string valLbl = FormatValue(bar.Value);
                    var valSz = g.MeasureString(valLbl, valueFont);
                    g.DrawString(valLbl, valueFont, valueBrush,
                        x + (barW - valSz.Width) / 2, y - valSz.Height - 2);

                    // Label below bar
                    var lblSz = g.MeasureString(bar.Label, labelFont);
                    float lblX = x + (barW - lblSz.Width) / 2;
                    float lblY = topMargin + chartH + 8;
                    g.DrawString(bar.Label, labelFont, labelBrush, lblX, lblY);
                }
            }
        }

        private static string FormatValue(double v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000:0.#}M";
            if (v >= 1_000) return $"{v / 1_000:0.#}k";
            if (v == Math.Floor(v)) return ((int)v).ToString();
            return v.ToString("0.##");
        }
    }
}