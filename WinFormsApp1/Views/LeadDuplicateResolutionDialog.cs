using System;
using System.Drawing;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    public enum DuplicateResolutionChoice
    {
        UseExisting,
        CreateAnyway,
        Cancel
    }

    /// <summary>
    /// Modal dialog presented when converting a Lead matches an existing Customer.
    /// Allows the user to choose between linking the existing record or creating a new one.
    /// </summary>
    public class LeadDuplicateResolutionDialog : Form
    {
        public DuplicateResolutionChoice UserChoice { get; private set; } = DuplicateResolutionChoice.Cancel;

        public LeadDuplicateResolutionDialog(LeadDto lead, LeadDuplicateMatchDto duplicateInfo)
        {
            Text = "Potential Duplicate Customer Detected";
            Size = new Size(620, 520);
            MinimumSize = new Size(580, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            BuildUI(lead, duplicateInfo);
        }

        private void BuildUI(LeadDto lead, LeadDuplicateMatchDto duplicateInfo)
        {
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Theme.Surface
            };
            Controls.Add(pnlMain);

            // Banner
            var pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(254, 243, 199), // Light Amber
                Padding = new Padding(12)
            };
            var lblBanner = new Label
            {
                Dock = DockStyle.Fill,
                Text = "⚠  A customer profile with matching contact information already exists in the system.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14), // Dark Amber
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBanner.Controls.Add(lblBanner);
            pnlMain.Controls.Add(pnlBanner);

            // Comparison table / info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 230,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 12, 0, 12)
            };

            var lblExistingHeader = new Label
            {
                Text = "Existing Customer Profile:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(0, 8),
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblExistingHeader);

            var lblExistingDetails = new Label
            {
                Text = $"• Customer ID: CUST-{duplicateInfo.ExistingCustomerId:D4}\n" +
                       $"• Name: {duplicateInfo.ExistingCustomerName}\n" +
                       $"• Contact: {duplicateInfo.ExistingContactInfo}\n" +
                       $"• Service Address: {(string.IsNullOrWhiteSpace(duplicateInfo.ExistingLocation) ? "(None recorded)" : duplicateInfo.ExistingLocation)}",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                Location = new Point(12, 32),
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblExistingDetails);

            var lblLeadHeader = new Label
            {
                Text = "Converting Lead Inquiry:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(0, 124),
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblLeadHeader);

            var lblLeadDetails = new Label
            {
                Text = $"• Lead: LD-{lead.LeadId:D4} — {lead.LeadName}\n" +
                       $"• Contact: {lead.ContactInfo}\n" +
                       $"• Service Address: {(string.IsNullOrWhiteSpace(lead.ServiceAddress) ? "(None recorded)" : lead.ServiceAddress)}",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                Location = new Point(12, 148),
                AutoSize = true
            };
            pnlInfo.Controls.Add(lblLeadDetails);

            pnlMain.Controls.Add(pnlInfo);

            // Question / instructions
            var lblQuestion = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Would you like to link this lead to the existing customer profile, or create a brand new customer record?",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlMain.Controls.Add(lblQuestion);

            // Action Buttons
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Theme.Surface
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 38,
                Dock = DockStyle.Right
            };
            Theme.ApplySecondaryButtonStyle(btnCancel);
            btnCancel.Click += (s, e) =>
            {
                UserChoice = DuplicateResolutionChoice.Cancel;
                DialogResult = DialogResult.Cancel;
                Close();
            };
            pnlButtons.Controls.Add(btnCancel);

            var pnlSpacer1 = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Theme.Surface };
            pnlButtons.Controls.Add(pnlSpacer1);

            var btnCreateAnyway = new Button
            {
                Text = "Create Anyway",
                Width = 140,
                Height = 38,
                Dock = DockStyle.Right
            };
            Theme.ApplySecondaryButtonStyle(btnCreateAnyway);
            btnCreateAnyway.Click += (s, e) =>
            {
                UserChoice = DuplicateResolutionChoice.CreateAnyway;
                DialogResult = DialogResult.OK;
                Close();
            };
            pnlButtons.Controls.Add(btnCreateAnyway);

            var pnlSpacer2 = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Theme.Surface };
            pnlButtons.Controls.Add(pnlSpacer2);

            var btnUseExisting = new Button
            {
                Text = "✓  Use Existing Customer",
                Width = 210,
                Height = 38,
                Dock = DockStyle.Right
            };
            Theme.ApplyPrimaryButtonStyle(btnUseExisting);
            btnUseExisting.Click += (s, e) =>
            {
                UserChoice = DuplicateResolutionChoice.UseExisting;
                DialogResult = DialogResult.OK;
                Close();
            };
            pnlButtons.Controls.Add(btnUseExisting);

            pnlMain.Controls.Add(pnlButtons);
        }
    }
}
