using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls.Charts
{
    /// <summary>
    /// Donut chart with legend. Pass a list of (label, value, color) slices.
    /// </summary>
    public class DonutChartControl : Control
    {
        public class Slice
        {
            public string Label { get; set; } = "";
            public double Value { get; set; }
            public Color Color { get; set; } = AppTheme.Signal;
        }

        private readonly List<Slice> _slices = new();
        private string _title = "";
        private string _centerLabel = "";
        private string _centerValue = "";
        private string _emptyText = "No data available";

        public DonutChartControl()
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
        public void SetCenter(string label, string value)
        {
            _centerLabel = label; _centerValue = value; Invalidate();
        }
        public void SetEmptyText(string text) { _emptyText = text; Invalidate(); }

        public void SetData(IEnumerable<Slice> slices)
        {
            _slices.Clear();
            _slices.AddRange(slices);
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

            if (_slices.Count == 0 || _slices.Sum(s => s.Value) <= 0)
            {
                using var ef = new Font("Segoe UI Variable Text", 10F, FontStyle.Italic);
                using var eb = new SolidBrush(AppTheme.TextMuted);
                var sz = g.MeasureString(_emptyText, ef);
                g.DrawString(_emptyText, ef, eb,
                    (Width - sz.Width) / 2, (Height - sz.Height) / 2);
                return;
            }

            // Donut area on the left
            int donutSize = Math.Min(Height - top - 40, Width / 2 - 40);
            if (donutSize < 60) donutSize = 60;
            int cx = 40;
            int cy = top + 20;
            var donutRect = new Rectangle(cx, cy, donutSize, donutSize);
            int thickness = (int)(donutSize * 0.32);

            double total = _slices.Sum(s => s.Value);
            float startAngle = -90f;

            using (var borderPen = new Pen(AppTheme.NeutralBg, thickness))
                g.DrawEllipse(borderPen, donutRect);

            foreach (var s in _slices)
            {
                float sweep = (float)(360.0 * s.Value / total);
                using var brush = new Pen(s.Color, thickness)
                {
                    StartCap = LineCap.Flat,
                    EndCap = LineCap.Flat
                };
                g.DrawArc(brush, donutRect, startAngle, sweep);
                startAngle += sweep;
            }

            // Center text
            if (!string.IsNullOrEmpty(_centerValue))
            {
                using var valFont = new Font("Bahnschrift SemiBold", 20F);
                using var valBrush = new SolidBrush(AppTheme.Ink);
                var valSz = g.MeasureString(_centerValue, valFont);
                g.DrawString(_centerValue, valFont, valBrush,
                    donutRect.X + (donutRect.Width - valSz.Width) / 2,
                    donutRect.Y + (donutRect.Height - valSz.Height) / 2 - 6);
            }
            if (!string.IsNullOrEmpty(_centerLabel))
            {
                using var lbFont = new Font("Segoe UI Variable Text Semibold", 8F);
                using var lbBrush = new SolidBrush(AppTheme.Trace);
                var lbSz = g.MeasureString(_centerLabel, lbFont);
                g.DrawString(_centerLabel, lbFont, lbBrush,
                    donutRect.X + (donutRect.Width - lbSz.Width) / 2,
                    donutRect.Y + donutRect.Height / 2 + 14);
            }

            // Legend on the right
            int legendX = donutRect.Right + 30;
            int legendY = cy + 10;
            int legendRowH = 26;

            using var legendFont = new Font("Segoe UI Variable Text", 9F);
            using var legendBrush = new SolidBrush(AppTheme.Ink);
            using var legendValueFont = new Font("Bahnschrift SemiBold", 9F);
            using var legendValueBrush = new SolidBrush(AppTheme.TextPrimary);

            foreach (var s in _slices)
            {
                if (legendY + legendRowH > Height - 10) break;

                using (var dotBrush = new SolidBrush(s.Color))
                    g.FillEllipse(dotBrush, legendX, legendY + 5, 10, 10);

                g.DrawString(s.Label, legendFont, legendBrush, legendX + 18, legendY + 1);

                string val = ((int)s.Value).ToString();
                var vsz = g.MeasureString(val, legendValueFont);
                g.DrawString(val, legendValueFont, legendValueBrush,
                    Width - vsz.Width - 20, legendY + 1);

                legendY += legendRowH;
            }
        }
    }
}