using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms.Views
{
    /// <summary>
    /// Hosts the Data Collection wizard and Saved Records table
    /// inside one page — under the Client & Contract Management module.
    /// </summary>
    public class ClientContractView : UserControl
    {
        private TabControl _tabs = null!;

        public ClientContractView()
        {
            BackColor = Color.FromArgb(245, 246, 250);

            // Header
            var lblTitle = new Label
            {
                Text = "Client & Contract Management",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5, 5, 0, 0)
            };
            Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Collect customer details, service preferences, and contract info — " +
                       "all stored as part of the customer's profile.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(5, 0, 0, 0)
            };
            Controls.Add(lblSub);
            lblSub.BringToFront();
            lblTitle.BringToFront();

            // Tabs container
            var pnlTabs = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.FromArgb(245, 246, 250)
            };
            Controls.Add(pnlTabs);
            pnlTabs.BringToFront();

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(20, 8)
            };
            pnlTabs.Controls.Add(_tabs);

            // TAB 1 — Data Collection wizard
            var tabCollect = new TabPage("✏️  Collect Customer Data")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            _tabs.TabPages.Add(tabCollect);

            var wizard = new DataCollectionView { Dock = DockStyle.Fill };
            tabCollect.Controls.Add(wizard);

            // TAB 2 — Saved Records table
            var tabRecords = new TabPage("📊  Saved Records")
            {
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            _tabs.TabPages.Add(tabRecords);

            var records = new RecordsView { Dock = DockStyle.Fill };
            tabRecords.Controls.Add(records);
        }
    }
}