using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms.Views
{
    public class PlaceholderView : BaseView
    {
        public PlaceholderView(string moduleName)
        {
            BackColor = Color.White;
            Padding = new Padding(40);

            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            Controls.Add(pnlCenter);

            var lblIcon = new Label
            {
                Text = "🚧",
                Font = new Font("Segoe UI Emoji", 48F),
                Dock = DockStyle.Top,
                Height = 100,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCenter.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = moduleName,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlCenter.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "This module is part of the Cleaning Services CRM.\n" +
                       "Full implementation coming soon.\n\n" +
                       "The sidebar, layout, and database connectivity are already in place.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 120,
                TextAlign = ContentAlignment.TopCenter
            };
            pnlCenter.Controls.Add(lblSub);
        }
    }
}