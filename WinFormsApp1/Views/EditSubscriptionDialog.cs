using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    public class EditSubscriptionDialog : Form
    {
        private readonly SubscriptionDto _sub;
        private readonly ApiClient _api = new();

        private ComboBox _cmbTier = null!;
        private ComboBox _cmbStatus = null!;
        private ComboBox _cmbBillingCycle = null!;
        private TextBox _txtPrice = null!;
        private DateTimePicker _dtpEndDate = null!;
        private TextBox _txtNotes = null!;

        // Entitlement preview labels
        private Label _lblEntitlements = null!;
        private Label _lblLimits = null!;

        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public bool SubscriptionUpdated { get; private set; }

        public EditSubscriptionDialog(SubscriptionDto sub)
        {
            _sub = sub;
            InitializeUI();
            PopulateData();
        }

        private void InitializeUI()
        {
            Text = $"Manage Subscription — {_sub.CompanyName}";
            Size = new Size(580, 640);
            MinimumSize = new Size(580, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 12, 24, 12)
            };
            Controls.Add(pnlHeader);

            var lblHeaderTitle = new Label
            {
                Text = "⚡  Manage Tenant Subscription",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            var lblHeaderSub = new Label
            {
                Text = $"Company: {_sub.CompanyName} ({_sub.CompanyCode})",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlHeader.Controls.Add(lblHeaderSub);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 12, 20, 12)
            };
            Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(_btnCancel);

            var spc = new Panel { Dock = DockStyle.Right, Width = 10 };
            pnlFooter.Controls.Add(spc);

            _btnSave = new Button
            {
                Text = "💾  Save Subscription",
                Width = 160,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await SaveChangesAsync();
            pnlFooter.Controls.Add(_btnSave);

            // Main Content Area
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            int y = 10;

            // Plan Tier
            var lblTier = new Label { Text = "Subscription Plan Tier:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblTier);
            y += 24;

            _cmbTier = new ComboBox
            {
                Location = new Point(24, y),
                Width = 510,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            _cmbTier.Items.AddRange(new object[]
            {
                "Tenant A — Micro Company (Main Transaction & Data Collection)",
                "Tenant B — Small Company (Business Intelligence & Actions)",
                "Tenant C — Medium Enterprise (Branching, BI & Actions)"
            });
            _cmbTier.SelectedIndexChanged += (s, e) => UpdateTierPreview();
            pnlBody.Controls.Add(_cmbTier);
            y += 40;

            // Entitlements Box
            var pnlPreview = new Panel
            {
                Location = new Point(24, y),
                Width = 510,
                Height = 90,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12)
            };
            pnlBody.Controls.Add(pnlPreview);

            _lblEntitlements = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(30, 41, 59),
                Text = "Entitlements: ..."
            };
            pnlPreview.Controls.Add(_lblEntitlements);

            _lblLimits = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Text = "Limits: ..."
            };
            pnlPreview.Controls.Add(_lblLimits);
            y += 105;

            // Row: Status & Billing Cycle
            var lblStatus = new Label { Text = "Account Status:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblStatus);

            var lblBilling = new Label { Text = "Billing Frequency:", Location = new Point(285, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblBilling);
            y += 24;

            _cmbStatus = new ComboBox { Location = new Point(24, y), Width = 245, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbStatus.Items.AddRange(new object[] { "Active", "Trial", "Suspended", "Cancelled" });
            pnlBody.Controls.Add(_cmbStatus);

            _cmbBillingCycle = new ComboBox { Location = new Point(285, y), Width = 249, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbBillingCycle.Items.AddRange(new object[] { "Monthly", "Quarterly", "Annual" });
            pnlBody.Controls.Add(_cmbBillingCycle);
            y += 40;

            // Row: Monthly Price & Renewal End Date
            var lblPrice = new Label { Text = "Monthly Rate (₱):", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblPrice);

            var lblEndDate = new Label { Text = "Renewal / Expiry Date:", Location = new Point(285, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblEndDate);
            y += 24;

            _txtPrice = new TextBox { Location = new Point(24, y), Width = 245, Font = new Font("Segoe UI", 9F) };
            pnlBody.Controls.Add(_txtPrice);

            _dtpEndDate = new DateTimePicker { Location = new Point(285, y), Width = 249, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9F) };
            pnlBody.Controls.Add(_dtpEndDate);
            y += 40;

            // Notes
            var lblNotes = new Label { Text = "Administrative Notes / SLA:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblNotes);
            y += 24;

            _txtNotes = new TextBox { Location = new Point(24, y), Width = 510, Height = 55, Multiline = true, Font = new Font("Segoe UI", 9F) };
            pnlBody.Controls.Add(_txtNotes);
        }

        private void PopulateData()
        {
            _cmbTier.SelectedIndex = Math.Clamp(_sub.TierValue - 1, 0, 2);
            _cmbStatus.SelectedItem = _sub.Status;
            if (_cmbStatus.SelectedIndex < 0) _cmbStatus.SelectedIndex = 0;

            _cmbBillingCycle.SelectedItem = _sub.BillingCycle;
            if (_cmbBillingCycle.SelectedIndex < 0) _cmbBillingCycle.SelectedIndex = 0;

            _txtPrice.Text = _sub.MonthlyPrice.ToString("N2");
            _dtpEndDate.Value = _sub.EndDate > DateTime.MinValue ? _sub.EndDate : DateTime.UtcNow.AddMonths(1);
            _txtNotes.Text = _sub.Notes ?? "";

            UpdateTierPreview();
        }

        private void UpdateTierPreview()
        {
            int selectedTier = _cmbTier.SelectedIndex + 1; // 1=Micro, 2=Small, 3=Medium
            switch (selectedTier)
            {
                case 1:
                    _lblLimits.Text = "👤 Max Users: 3  |  🏢 Max Branches: 1  |  Standard Tier A";
                    _lblEntitlements.Text = "Included Features:\n✔ Main Transaction (Booking & Work Orders)\n✔ Data Collection (Lead & Customer Intake)\n✖ Business Intelligence    ✖ Actions    ✖ Branching";
                    break;
                case 2:
                    _lblLimits.Text = "👤 Max Users: 10  |  🏢 Max Branches: 1  |  Growth Tier B";
                    _lblEntitlements.Text = "Included Features:\n✔ Main Transaction & Data Collection\n✔ Business Intelligence (KPI Dashboards & Retention Analytics)\n✔ Actions & Automated Retention Alerts    ✖ Branching";
                    break;
                case 3:
                    _lblLimits.Text = "👤 Max Users: 50+  |  🏢 Max Branches: 10  |  Enterprise Tier C";
                    _lblEntitlements.Text = "Included Features:\n✔ All Core Transactions & Data Collection\n✔ Multi-Branching Management & Cross-Location Routing\n✔ Enterprise Business Intelligence & Advanced Actions";
                    break;
            }
        }

        private async Task SaveChangesAsync()
        {
            if (!decimal.TryParse(_txtPrice.Text.Replace("₱", "").Replace(",", "").Trim(), out var price) || price < 0)
            {
                MessageBox.Show("Please enter a valid non-negative monthly price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPrice.Focus();
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            try
            {
                var updateDto = new SubscriptionUpdateDto
                {
                    Tier = _cmbTier.SelectedIndex + 1,
                    Status = _cmbStatus.SelectedItem?.ToString() ?? "Active",
                    BillingCycle = _cmbBillingCycle.SelectedItem?.ToString() ?? "Monthly",
                    MonthlyPrice = price,
                    EndDate = _dtpEndDate.Value,
                    Notes = _txtNotes.Text.Trim()
                };

                var (success, msg) = await _api.UpdateSubscriptionAsync(_sub.SubscriptionId, updateDto);
                if (!success)
                {
                    // Fallback to direct LocalDB update
                    using var db = new App.Infrastructure.AppDbContext();
                    var sub = await db.Subscriptions.FindAsync(_sub.SubscriptionId);
                    if (sub != null)
                    {
                        var tier = (App.Domain.Enums.SubscriptionTier)updateDto.Tier;
                        sub.ApplyTierDefaults(tier);
                        sub.Status = updateDto.Status;
                        sub.BillingCycle = updateDto.BillingCycle;
                        sub.MonthlyPrice = updateDto.MonthlyPrice ?? sub.MonthlyPrice;
                        sub.EndDate = updateDto.EndDate ?? sub.EndDate;
                        sub.Notes = updateDto.Notes;
                        sub.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                        success = true;
                    }
                }

                if (success)
                {
                    SubscriptionUpdated = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save subscription: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "💾  Save Subscription";
            }
        }
    }
}
