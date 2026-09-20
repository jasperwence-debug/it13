using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms.Views
{
    public class RecordsView : UserControl
    {
        public RecordsView()
        {
            BackColor = Color.White;
            Padding = new Padding(20);

            var lbl = new Label
            {
                Text = "Saved Records (grid + CRUD goes here in next step)",
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lbl);
        }
    }
}