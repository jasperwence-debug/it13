using System.Drawing;
using System.Windows.Forms;

namespace CRM.winforms.Helpers
{
    public static class UiGridHelper
    {
        public static void ApplyModernGridStyle(DataGridView grid, int rowHeight = 52)
        {
            AppTheme.ApplyGridStyle(grid);
            grid.RowTemplate.Height = rowHeight;
        }

        /// <summary>
        /// Adds the trailing "⋮" action column. Flat, no fill; hover shows a light surface.
        /// </summary>
        public static void AddActionsColumn(DataGridView grid, int width = 56)
        {
            if (grid.Columns.Contains("Actions"))
                grid.Columns.Remove("Actions");

            var col = new DataGridViewButtonColumn
            {
                Name = "Actions",
                HeaderText = "",
                Text = "⋮",
                UseColumnTextForButtonValue = true,
                Width = width,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI Variable Text", 14F, FontStyle.Bold),
                    ForeColor = AppTheme.Trace,
                    BackColor = AppTheme.Surface,
                    SelectionBackColor = AppTheme.InfoBg,
                    SelectionForeColor = AppTheme.Signal,
                    Padding = new Padding(0)
                }
            };

            grid.Columns.Add(col);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            col.Width = width;
        }
    }
}