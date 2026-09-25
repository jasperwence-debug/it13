using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace App.WinForms
{
    /// <summary>
    /// Centralized Design System for the WinForms CRM.
    /// All views must reference these tokens — never hardcode colors or fonts.
    /// </summary>
    public static class Theme
    {
        // ============================================================
        // COLOR PALETTE
        // ============================================================

        /// <summary>Main page background. #F8FAFC (Slate-50)</summary>
        public static readonly Color Background = Color.FromArgb(248, 250, 252);

        /// <summary>Card and surface background. #FFFFFF (Pure White)</summary>
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);

        /// <summary>Sidebar background. #111827 (Deep Slate/Navy-900)</summary>
        public static readonly Color SidebarBackground = Color.FromArgb(17, 24, 39);

        /// <summary>Sidebar brand panel background. #0F172A</summary>
        public static readonly Color SidebarBrand = Color.FromArgb(15, 23, 42);

        /// <summary>Primary brand blue (buttons, active pills). #2563EB</summary>
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);

        /// <summary>Primary hover/pressed (darker blue). #1D4ED8</summary>
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);

        /// <summary>High-emphasis text (headers, primary labels). #0F172A</summary>
        public static readonly Color TextDark = Color.FromArgb(15, 23, 42);

        /// <summary>Medium-emphasis text (subtitles, secondary info). #64748B</summary>
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);

        /// <summary>Subtle text (placeholders, inactive icons, timestamps). #94A3B8</summary>
        public static readonly Color TextSubtle = Color.FromArgb(148, 163, 184);

        /// <summary>Card and divider border. #E2E8F0</summary>
        public static readonly Color Border = Color.FromArgb(226, 232, 240);

        /// <summary>Subtle border / surface tint. #F1F5F9</summary>
        public static readonly Color BorderLight = Color.FromArgb(241, 245, 249);

        /// <summary>Input control background. #F8FAFC</summary>
        public static readonly Color InputBackground = Color.FromArgb(248, 250, 252);

        /// <summary>Input border default. #E2E8F0</summary>
        public static readonly Color InputBorder = Color.FromArgb(226, 232, 240);

        /// <summary>Input border focused. #3B82F6</summary>
        public static readonly Color InputBorderFocused = Color.FromArgb(59, 130, 246);

        /// <summary>Positive metric / badge green background. #ECFDF5</summary>
        public static readonly Color BadgeGreenBg = Color.FromArgb(236, 253, 245);

        /// <summary>Positive metric / badge green text. #059669</summary>
        public static readonly Color BadgeGreenText = Color.FromArgb(5, 150, 105);

        /// <summary>Negative metric / badge red background. #FEF2F2</summary>
        public static readonly Color BadgeRedBg = Color.FromArgb(254, 242, 242);

        /// <summary>Negative metric / badge red text. #DC2626</summary>
        public static readonly Color BadgeRedText = Color.FromArgb(220, 38, 38);

        /// <summary>KPI Icon box blue background. #EFF6FF</summary>
        public static readonly Color IconBoxBlue = Color.FromArgb(239, 246, 255);

        /// <summary>KPI Icon box green background. #F0FDF4</summary>
        public static readonly Color IconBoxGreen = Color.FromArgb(240, 253, 244);

        /// <summary>KPI Icon box amber background. #FFFBEB</summary>
        public static readonly Color IconBoxAmber = Color.FromArgb(255, 251, 235);

        /// <summary>KPI Icon box rose background. #FFF1F2</summary>
        public static readonly Color IconBoxRose = Color.FromArgb(255, 241, 242);

        /// <summary>Success green. #10B981</summary>
        public static readonly Color Success = Color.FromArgb(16, 185, 129);

        /// <summary>Success tint. #F0FDF4</summary>
        public static readonly Color SuccessLight = Color.FromArgb(240, 253, 244);

        /// <summary>Danger red. #DC2626</summary>
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);

        /// <summary>Danger tint. #FEF2F2</summary>
        public static readonly Color DangerLight = Color.FromArgb(254, 242, 242);

        /// <summary>Warning amber. #F59E0B</summary>
        public static readonly Color Warning = Color.FromArgb(245, 158, 11);

        /// <summary>Selection tint. #EFF6FF</summary>
        public static readonly Color SelectionBlue = Color.FromArgb(239, 246, 255);

        /// <summary>Chart scheduled series blue. #2563EB</summary>
        public static readonly Color ChartBlue = Color.FromArgb(37, 99, 235);

        /// <summary>Chart completed series green. #10B981</summary>
        public static readonly Color ChartGreen = Color.FromArgb(16, 185, 129);

        // ============================================================
        // TYPOGRAPHY
        // ============================================================

        /// <summary>Page/section heading. Segoe UI 16pt Bold.</summary>
        public static readonly Font HeaderFont = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);

        /// <summary>Card and section sub-heading. Segoe UI 11pt Bold.</summary>
        public static readonly Font SubHeaderFont = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);

        /// <summary>KPI value display. Segoe UI 20pt Bold.</summary>
        public static readonly Font KpiValueFont = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);

        /// <summary>Standard body text. Segoe UI 9.5pt.</summary>
        public static readonly Font BodyFont = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        /// <summary>Bold body text (field labels, column headers). Segoe UI 9pt Bold.</summary>
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);

        /// <summary>Small / caption text. Segoe UI 8.5pt.</summary>
        public static readonly Font CaptionFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);

        /// <summary>Micro label (badges, tags). Segoe UI 8pt Bold.</summary>
        public static readonly Font TagFont = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);

        // ============================================================
        // GLOBAL: DataGridView Styler
        // Strips all 3D chrome and applies flat SaaS aesthetics.
        // ============================================================

        /// <summary>
        /// Applies the application-standard flat style to any DataGridView.
        /// Call once after the grid is created; handles all colors, fonts,
        /// borders, selection, alternating rows, and row height.
        /// </summary>
        public static void ApplyGridStyle(DataGridView grid)
        {
            // ---- Structure / Frame ----
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.BackgroundColor = Surface;
            grid.GridColor = Border;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ScrollBars = ScrollBars.Both;

            // ---- Column Headers ----
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Background;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = BodyBoldFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Background;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // ---- Row Defaults ----
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = TextDark;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.SelectionBackColor = SelectionBlue;
            grid.DefaultCellStyle.SelectionForeColor = TextDark;
            grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

            // ---- Alternating Rows ----
            grid.AlternatingRowsDefaultCellStyle.BackColor = BorderLight;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextDark;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionBlue;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextDark;

            // ---- Row Height ----
            grid.RowTemplate.Height = 40;
        }

        // ============================================================
        // GLOBAL: Button Stylers
        // ============================================================

        /// <summary>
        /// Primary CTA button: modern gradient (#2563EB -> #4F46E5), hover shift (#1D4ED8 -> #4338CA),
        /// crisp white label, smooth rounded corners.
        /// Use for the single most important action in a view or dialog (Submit, Create, Save, Confirm).
        /// </summary>
        public static void ApplyPrimaryButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Primary;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;

            btn.Paint -= OnPrimaryButtonPaint;
            btn.Paint += OnPrimaryButtonPaint;

            btn.MouseEnter -= OnButtonStateInvalidate;
            btn.MouseEnter += OnButtonStateInvalidate;
            btn.MouseLeave -= OnButtonStateInvalidate;
            btn.MouseLeave += OnButtonStateInvalidate;
            btn.MouseDown -= OnButtonStateInvalidate;
            btn.MouseDown += OnButtonStateInvalidate;
            btn.MouseUp -= OnButtonStateInvalidate;
            btn.MouseUp += OnButtonStateInvalidate;
        }

        private static void OnButtonStateInvalidate(object? sender, EventArgs e)
        {
            if (sender is Button b) b.Invalidate();
        }

        private static void OnPrimaryButtonPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = btn.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (var bgBrush = new SolidBrush(btn.Parent?.BackColor ?? Surface))
            {
                g.FillRectangle(bgBrush, rect);
            }

            var isHover = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
            var isPressed = isHover && (Control.MouseButtons & MouseButtons.Left) != 0;

            var drawRect = new Rectangle(0, 0, btn.Width, btn.Height);

            Color startColor = (isHover || isPressed) ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235);
            Color endColor = (isHover || isPressed) ? Color.FromArgb(67, 56, 202) : Color.FromArgb(79, 70, 229);

            if (!btn.Enabled)
            {
                startColor = Color.FromArgb(148, 163, 184);
                endColor = Color.FromArgb(148, 163, 184);
            }

            using (var brush = new LinearGradientBrush(drawRect, startColor, endColor, LinearGradientMode.Horizontal))
            {
                using var path = CreateRoundedRectanglePath(drawRect, 6);
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                g,
                btn.Text,
                btn.Font,
                drawRect,
                btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Secondary / neutral button: light slate background, subtle border, dark label.
        /// Use for Cancel, Back, Refresh, and secondary operational actions without visual noise.
        /// </summary>
        public static void ApplySecondaryButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225); // #CBD5E1
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240); // #E2E8F0
            btn.BackColor = Color.FromArgb(241, 245, 249); // #F1F5F9
            btn.ForeColor = Color.FromArgb(51, 65, 85);    // #334155
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        /// <summary>
        /// Destructive / cancellation button: subtle muted red border & tint, dark crimson label.
        /// Avoids screaming loud colors while maintaining clear intent.
        /// </summary>
        public static void ApplyDestructiveButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202); // #FECDD3
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226); // #FEE2E2
            btn.BackColor = Color.FromArgb(254, 242, 242); // #FEF2F2
            btn.ForeColor = Color.FromArgb(185, 28, 28);    // #B91C1C
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        /// <summary>
        /// Danger button: solid red background, white label, flat.
        /// Use for Delete, Remove, and irreversible destructive actions.
        /// </summary>
        public static void ApplyDangerButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28); // #B91C1C
            btn.BackColor = Danger;
            btn.ForeColor = Color.White;
            btn.Font = BodyBoldFont;
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }
    }
}
