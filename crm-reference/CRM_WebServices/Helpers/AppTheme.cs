using System.Drawing;
using System.Windows.Forms;

namespace CRM.winforms.Helpers
{
    /// <summary>
    /// KonSpot design tokens. Locked palette derived from #1447E6 (Signal).
    /// Typography: Bahnschrift (display) + Segoe UI Variable (body).
    /// </summary>
    public static class AppTheme
    {
        // ─── Core Palette ─────────────────────────────────────
        public static Color Ink => Color.FromArgb(0x0B, 0x21, 0x49); // #0B2149  sidebar / deep text
        public static Color Signal => Color.FromArgb(0x14, 0x47, 0xE6); // #1447E6  primary brand
        public static Color SignalHover => Color.FromArgb(0x0F, 0x38, 0xB8); // #0F38B8  darker shade
        public static Color SignalPressed => Color.FromArgb(0x0A, 0x2B, 0x8A); // even deeper
        public static Color Paper => Color.FromArgb(0xF5, 0xF7, 0xFB); // #F5F7FB  content bg
        public static Color Surface => Color.FromArgb(0xFF, 0xFF, 0xFF); // #FFFFFF  cards
        public static Color Line => Color.FromArgb(0xE1, 0xE6, 0xF0); // #E1E6F0  hairlines
        public static Color Trace => Color.FromArgb(0x5C, 0x6A, 0x87); // #5C6A87  secondary text

        // ─── Semantic (pill) ──────────────────────────────────
        public static Color SuccessBg => Color.FromArgb(0xE6, 0xF4, 0xEA);
        public static Color SuccessText => Color.FromArgb(0x1B, 0x7A, 0x47);
        public static Color WarningBg => Color.FromArgb(0xFD, 0xF5, 0xE4);
        public static Color WarningText => Color.FromArgb(0xA3, 0x6A, 0x0E);
        public static Color DangerBg => Color.FromArgb(0xFB, 0xEB, 0xEB);
        public static Color DangerText => Color.FromArgb(0xA3, 0x24, 0x24);
        public static Color InfoBg => Color.FromArgb(0xE7, 0xEE, 0xFE);
        public static Color InfoText => Color.FromArgb(0x0F, 0x38, 0xB8);
        public static Color NeutralBg => Color.FromArgb(0xEE, 0xF1, 0xF7);
        public static Color NeutralText => Color.FromArgb(0x5C, 0x6A, 0x87);

        // ─── Backward-compatibility aliases (older code) ──────
        public static Color Primary => Signal;
        public static Color PrimaryHover => SignalHover;
        public static Color PrimaryLight => InfoBg;
        public static Color SidebarBg => Ink;
        public static Color SidebarText => Color.FromArgb(0xC9, 0xD4, 0xE6);
        public static Color SidebarActive => Signal;
        public static Color SidebarHoverOverlay => Color.FromArgb(30, 255, 255, 255);
        public static Color ContentBg => Paper;
        public static Color CardBg => Surface;
        public static Color SurfaceBg => Surface;
        public static Color Border => Line;
        public static Color BorderColor => Line;
        public static Color GridLine => Line;
        public static Color TextPrimary => Color.FromArgb(0x0F, 0x17, 0x2A);
        public static Color TextSecondary => Trace;
        public static Color TextMuted => Color.FromArgb(0x8A, 0x95, 0xAB);
        public static Color Success => SuccessText;
        public static Color Warning => WarningText;
        public static Color Danger => DangerText;
        public static Color SuccessTextBg => SuccessBg;
        public static Color StatusActiveBg => SuccessBg;
        public static Color StatusActiveText => SuccessText;
        public static Color StatusInactiveBg => NeutralBg;
        public static Color StatusInactiveText => NeutralText;
        public static Color StatusFollowUpBg => WarningBg;
        public static Color StatusFollowUpText => WarningText;
        public static Color FocusBorder => Signal;

        // ─── Fonts ────────────────────────────────────────────
        // Display family — Bahnschrift ships with Windows 10/11
        public static Font FontWordmark => new("Bahnschrift SemiBold", 13F, FontStyle.Regular);
        public static Font FontTitle => new("Bahnschrift SemiBold", 22F, FontStyle.Regular);
        public static Font FontHeading => new("Bahnschrift Medium", 15F, FontStyle.Regular);
        public static Font FontSubheading => new("Bahnschrift Medium", 11F, FontStyle.Regular);
        public static Font FontKpiNumber => new("Bahnschrift Light", 32F, FontStyle.Regular);
        public static Font FontKpiLabel => new("Segoe UI Variable Text Semibold", 8.5F, FontStyle.Regular);

        // Body — Segoe UI Variable (Windows 11)
        public static Font FontBody => new("Segoe UI Variable Text", 10F, FontStyle.Regular);
        public static Font FontBodySmall => new("Segoe UI Variable Text", 9F, FontStyle.Regular);
        public static Font FontLabel => new("Segoe UI Variable Text Semibold", 8.5F, FontStyle.Regular);
        public static Font FontGridHeader => new("Segoe UI Variable Text Semibold", 8.5F, FontStyle.Regular);
        public static Font FontGridBody => new("Segoe UI Variable Text", 9.5F, FontStyle.Regular);
        public static Font FontPill => new("Segoe UI Variable Text Semibold", 8.5F, FontStyle.Regular);

        // Legacy aliases
        public static Font FontHeadingSmall => FontSubheading;
        public static Font FontHeadingLarge => FontHeading;
        public static Font FontBodySemibold => FontLabel;

        // ─── Grid Styling ─────────────────────────────────────
        public static void ApplyGridStyle(DataGridView grid)
        {
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Surface;
            grid.GridColor = Line;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Trace;
            grid.ColumnHeadersDefaultCellStyle.Font = FontGridHeader;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            grid.RowTemplate.Height = 52;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.SelectionBackColor = InfoBg;
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0x0F, 0x17, 0x2A);
            grid.DefaultCellStyle.Font = FontGridBody;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(0x0F, 0x17, 0x2A);
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Surface; // no zebra
        }
    }
}