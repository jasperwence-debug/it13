using System.Drawing;
using System.Windows.Forms;
using CRM.winforms.Helpers;

namespace CRM.winforms.Controls
{
    public class ActionsColumn : DataGridViewButtonColumn
    {
        public ActionsColumn()
        {
            Name = "Actions";
            HeaderText = "";
            Text = "⋮";
            UseColumnTextForButtonValue = true;
            Width = 64;
            FlatStyle = FlatStyle.Flat;
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = AppTheme.TextSecondary,
                SelectionBackColor = AppTheme.Border,
                SelectionForeColor = AppTheme.TextPrimary,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(0)
            };
        }
    }
}