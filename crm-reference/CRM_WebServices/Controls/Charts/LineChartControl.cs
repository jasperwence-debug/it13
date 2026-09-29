using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls.Charts
{
    /// <summary>
    /// Line chart with dots. Pass a list of (label, value) pairs.
    /// </summary>
    public class LineChartControl : Control
    {
        public class Point2
        {
            public string Label { get; set; } = "";
            public double Value { get; set; }
        }

        private readonly List<Point2> _points = new();
        private string _title = "";
        private string _emptyText = "No data available";
        private Color _lineColor = AppTheme.Signal;

        public LineChartControl()
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
        public void SetLineColor(Color c) { _lineColor = c; Invalidate(); }

        public void SetData(IEnumerable<Point2> pts)
        {
            _points.Clear();
            _points.AddRange(pts);
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

            if (_points.Count == 0)
            {
                using var ef = new Font("Segoe UI Variable Text", 10F, FontStyle.Italic);
                using var eb = new SolidBrush(AppTheme.TextMuted);
                var sz = g.MeasureString(_emptyText, ef);
                g.DrawString(_emptyText, ef, eb,
                    (Width - sz.Width) / 2, (Height - sz.Height) / 2);
                return;
            }

            int leftMargin = 52;
            int rightMargin = 24;
            int bottomMargin = 40;
            int topMargin = top + 8;
            int chartW = Width - leftMargin - rightMargin;
            int chartH = Height - topMargin - bottomMargin;

            if (chartW <= 20 || chartH <= 20) return;

            double max = _points.Max(p => p.Value);
            double min = 0;   // baseline always 0 for trend clarity
            if (max <= 0) max = 1;

            // Gridlines
            using (var gridPen = new Pen(AppTheme.Line, 1) { DashStyle = DashStyle.Dot })
            using (var lf = new Font("Segoe UI Variable Text", 8F))
            using (var lb = new SolidBrush(AppTheme.TextMuted))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = topMargin + chartH - (int)(chartH * i / 4.0);
                    g.DrawLine(gridPen, leftMargin, y, leftMargin + chartW, y);
                    double val = min + (max - min) * i / 4.0;
                    string lbl = FormatValue(val);
                    var sz = g.MeasureString(lbl, lf);
                    g.DrawString(lbl, lf, lb, leftMargin - sz.Width - 6, y - sz.Height / 2);
                }
            }

            int n = _points.Count;
            var coords = new System.Drawing.PointF[n];
            for (int i = 0; i < n; i++)
            {
                float x = leftMargin + (n == 1 ? chartW / 2f : chartW * i / (float)(n - 1));
                float y = topMargin + chartH - (float)(chartH * (_points[i].Value - min) / (max - min));
                coords[i] = new System.Drawing.PointF(x, y);
            }

            // Line
            if (n >= 2)
            {
                using var linePen = new Pen(_lineColor, 2.5f);
                g.DrawLines(linePen, coords);
            }

            // Points + labels
            using var dotBrush = new SolidBrush(_lineColor);
            using var dotBorder = new Pen(AppTheme.Surface, 2);
            using var labelFont = new Font("Segoe UI Variable Text", 8F);
            using var labelBrush = new SolidBrush(AppTheme.Trace);
            using var valueFont = new Font("Bahnschrift SemiBold", 9F);
            using var valueBrush = new SolidBrush(AppTheme.Ink);

            for (int i = 0; i < n; i++)
            {
                var p = coords[i];
                g.FillEllipse(dotBrush, p.X - 4, p.Y - 4, 8, 8);
                g.DrawEllipse(dotBorder, p.X - 4, p.Y - 4, 8, 8);

                // Value above
                string v = FormatValue(_points[i].Value);
                var vsz = g.MeasureString(v, valueFont);
                g.DrawString(v, valueFont, valueBrush,
                    p.X - vsz.Width / 2, p.Y - vsz.Height - 6);

                // Label below
                var lsz = g.MeasureString(_points[i].Label, labelFont);
                g.DrawString(_points[i].Label, labelFont, labelBrush,
                    p.X - lsz.Width / 2, topMargin + chartH + 8);
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