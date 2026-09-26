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

        // SaaS Dunning & Lifecycle Panel controls
        private Panel _pnlDunningCard = null!;
        private Label _lblDunningStatus = null!;
        private Label _lblDunningExplanation = null!;

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
            Size = new Size(620, 750);
            MinimumSize = new Size(600, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            // ── Top Header Banner ───────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 12, 24, 12)
            };
            Controls.Add(pnlHeader);

            var lblHeaderTitle = new Label
            {
                Text = "⚡  Manage Tenant Subscription & Dunning",
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

            // ── Bottom Footer Action Bar ────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 12, 20, 12)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
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
                Width = 170,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await SaveChangesAsync();
            pnlFooter.Controls.Add(_btnSave);

            // ── Main Scrollable Body ────────────────────────────────
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
                Width = 540,
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
            y += 38;

            // Entitlements Box
            var pnlPreview = new Panel
            {
                Location = new Point(24, y),
                Width = 540,
                Height = 84,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12)
            };
            pnlBody.Controls.Add(pnlPreview);

            _lblEntitlements = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(30, 41, 59),
                Text = "Entitlements: ..."
            };
            pnlPreview.Controls.Add(_lblEntitlements);

            _lblLimits = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Text = "Limits: ..."
            };
            pnlPreview.Controls.Add(_lblLimits);
            y += 96;

            // ── Section: Dunning & Lifecycle Status Panel ───────────
            var lblDunningTitle = new Label
            {
                Text = "Subscription Lifecycle & Dunning Status:",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark
            };
            pnlBody.Controls.Add(lblDunningTitle);
            y += 24;

            _pnlDunningCard = new Panel
            {
                Location = new Point(24, y),
                Width = 540,
                Height = 80,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(14, 10, 14, 10)
            };
            pnlBody.Controls.Add(_pnlDunningCard);

            _lblDunningStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Text = "● ACTIVE (Good Standing)"
            };
            _pnlDunningCard.Controls.Add(_lblDunningStatus);

            _lblDunningExplanation = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Text = "Subscription is active. The company's staff have full access to operations."
            };
            _pnlDunningCard.Controls.Add(_lblDunningExplanation);
            _lblDunningExplanation.BringToFront();
            y += 90;

            // ── Quick 1-Click Operations Ribbon ────────────────────
            var lblQuickOps = new Label
            {
                Text = "⚡ Super Admin 1-Click Lifecycle Actions:",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            pnlBody.Controls.Add(lblQuickOps);
            y += 22;

            var pnlActionBtns = new FlowLayoutPanel
            {
                Location = new Point(24, y),
                Width = 540,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            pnlBody.Controls.Add(pnlActionBtns);

            var btnGrace7 = new Button
            {
                Text = "📅  +7 Days Grace",
                Width = 130,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(254, 243, 199),
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnGrace7.FlatAppearance.BorderColor = Color.FromArgb(252, 211, 77);
            btnGrace7.Click += (s, e) => ApplyGraceExtension(7);
            pnlActionBtns.Controls.Add(btnGrace7);

            var btnRenewMo = new Button
            {
                Text = "✔  Settle & Renew (+1 Mo)",
                Width = 165,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 163, 74),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnRenewMo.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnRenewMo.Click += (s, e) => SettleAndRenew();
            pnlActionBtns.Controls.Add(btnRenewMo);

            var btnSuspend = new Button
            {
                Text = "🛑  Suspend Account",
                Width = 135,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnSuspend.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnSuspend.Click += (s, e) => SuspendTenant();
            pnlActionBtns.Controls.Add(btnSuspend);

            var btnReactivate = new Button
            {
                Text = "🔄  Reactivate",
                Width = 90,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnReactivate.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            btnReactivate.Click += (s, e) => ReactivateTenant();
            pnlActionBtns.Controls.Add(btnReactivate);

            y += 44;

            // Row: Status & Billing Frequency
            var lblStatus = new Label { Text = "Account Status:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblStatus);

            var lblBilling = new Label { Text = "Billing Frequency:", Location = new Point(300, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblBilling);
            y += 24;

            _cmbStatus = new ComboBox { Location = new Point(24, y), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbStatus.Items.AddRange(new object[] { "Active", "Grace Period", "Trial", "Suspended", "Cancelled" });
            _cmbStatus.SelectedIndexChanged += (s, e) => UpdateDunningDisplay();
            pnlBody.Controls.Add(_cmbStatus);

            _cmbBillingCycle = new ComboBox { Location = new Point(300, y), Width = 264, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbBillingCycle.Items.AddRange(new object[] { "Monthly", "Quarterly", "Annual" });
            pnlBody.Controls.Add(_cmbBillingCycle);
            y += 38;

            // Row: Monthly Price & Renewal End Date
            var lblPrice = new Label { Text = "Monthly Rate (₱):", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblPrice);

            var lblEndDate = new Label { Text = "Renewal / Expiry Date:", Location = new Point(300, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblEndDate);
            y += 24;

            _txtPrice = new TextBox { Location = new Point(24, y), Width = 260, Font = new Font("Segoe UI", 9F) };
            pnlBody.Controls.Add(_txtPrice);

            _dtpEndDate = new DateTimePicker { Location = new Point(300, y), Width = 264, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9F) };
            _dtpEndDate.ValueChanged += (s, e) => UpdateDunningDisplay();
            pnlBody.Controls.Add(_dtpEndDate);
            y += 38;

            // Administrative Notes
            var lblNotes = new Label { Text = "Administrative Notes / Audit Log:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblNotes);
            y += 24;

            _txtNotes = new TextBox { Location = new Point(24, y), Width = 540, Height = 60, Multiline = true, Font = new Font("Segoe UI", 9F), ScrollBars = ScrollBars.Vertical };
            pnlBody.Controls.Add(_txtNotes);
        }

        private void PopulateData()
        {
            _cmbTier.SelectedIndex = Math.Clamp(_sub.TierValue - 1, 0, 2);
            
            int statusIdx = _cmbStatus.FindStringExact(_sub.Status);
            _cmbStatus.SelectedIndex = statusIdx >= 0 ? statusIdx : 0;

            _cmbBillingCycle.SelectedItem = _sub.BillingCycle;
            if (_cmbBillingCycle.SelectedIndex < 0) _cmbBillingCycle.SelectedIndex = 0;

            _txtPrice.Text = _sub.MonthlyPrice.ToString("N2");
            _dtpEndDate.Value = _sub.EndDate > DateTime.MinValue ? _sub.EndDate : DateTime.UtcNow.AddMonths(1);
            _txtNotes.Text = _sub.Notes ?? "";

            UpdateTierPreview();
            UpdateDunningDisplay();
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

        private void UpdateDunningDisplay()
        {
            string status = _cmbStatus.SelectedItem?.ToString() ?? "Active";
            DateTime endDate = _dtpEndDate.Value.Date;
            DateTime today = DateTime.Today;

            if (status == "Suspended")
            {
                _pnlDunningCard.BackColor = Color.FromArgb(254, 242, 242);
                _lblDunningStatus.ForeColor = Color.FromArgb(185, 28, 28);
                _lblDunningStatus.Text = "🛑 ACCOUNT SUSPENDED";
                _lblDunningExplanation.Text = "Grace period has elapsed without payment or account was manually locked. Staff and cleaner access are restricted.";
            }
            else if (status == "Cancelled")
            {
                _pnlDunningCard.BackColor = Color.FromArgb(241, 245, 249);
                _lblDunningStatus.ForeColor = Color.FromArgb(100, 116, 139);
                _lblDunningStatus.Text = "✖ CANCELLED";
                _lblDunningExplanation.Text = "Subscription is terminated. Tenant account remains in archive cold storage.";
            }
            else if (status == "Grace Period" || endDate < today)
            {
                int daysSinceDue = Math.Max(0, (today - endDate).Days);
                int graceRemaining = Math.Max(0, 7 - daysSinceDue);

                _pnlDunningCard.BackColor = Color.FromArgb(254, 243, 199);
                _lblDunningStatus.ForeColor = Color.FromArgb(180, 83, 9);
                _lblDunningStatus.Text = $"⚠️ GRACE PERIOD ACTIVE ({graceRemaining} Days Left)";
                _lblDunningExplanation.Text = $"Renewal is overdue (Due: {endDate:MMM dd, yyyy}). In accordance with SaaS dunning standards, staff access is preserved during the grace period.";
            }
            else
            {
                _pnlDunningCard.BackColor = Color.FromArgb(240, 253, 244);
                _lblDunningStatus.ForeColor = Color.FromArgb(22, 163, 74);
                _lblDunningStatus.Text = "● ACTIVE (Good Standing)";
                _lblDunningExplanation.Text = $"Subscription is fully paid. Next billing renewal date is {endDate:MMM dd, yyyy}.";
            }
        }

        private void ApplyGraceExtension(int days)
        {
            DateTime current = _dtpEndDate.Value.Date;
            DateTime baseDate = current > DateTime.Today ? current : DateTime.Today;
            _dtpEndDate.Value = baseDate.AddDays(days);
            _cmbStatus.SelectedItem = "Grace Period";

            AppendNote($"[{DateTime.Now:yyyy-MM-dd}] Super Admin extended grace period by {days} days. New due date: {_dtpEndDate.Value:yyyy-MM-dd}.");
            UpdateDunningDisplay();

            MessageBox.Show(
                $"Grace period extended by {days} days for {_sub.CompanyName}.\nNew renewal date: {_dtpEndDate.Value:MMM dd, yyyy}.\n\nClick 'Save Subscription' to persist this change.",
                "Grace Period Granted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SettleAndRenew()
        {
            DateTime current = _dtpEndDate.Value.Date;
            DateTime baseDate = current > DateTime.Today ? current : DateTime.Today;
            _dtpEndDate.Value = baseDate.AddMonths(1);
            _cmbStatus.SelectedItem = "Active";

            AppendNote($"[{DateTime.Now:yyyy-MM-dd}] Payment received (₱{_txtPrice.Text}). Subscription renewed +1 month. Next billing: {_dtpEndDate.Value:yyyy-MM-dd}.");
            UpdateDunningDisplay();

            MessageBox.Show(
                $"Payment recorded! Subscription for {_sub.CompanyName} is now Active.\nRenewed until: {_dtpEndDate.Value:MMM dd, yyyy}.\n\nClick 'Save Subscription' to persist.",
                "Subscription Renewed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SuspendTenant()
        {
            var res = MessageBox.Show(
                $"Are you sure you want to suspend {_sub.CompanyName}?\n\nThis will restrict their account operations due to unpaid subscription.",
                "Confirm Account Suspension",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                _cmbStatus.SelectedItem = "Suspended";
                AppendNote($"[{DateTime.Now:yyyy-MM-dd}] Account suspended by Super Admin due to non-payment.");
                UpdateDunningDisplay();
            }
        }

        private void ReactivateTenant()
        {
            _cmbStatus.SelectedItem = "Active";
            if (_dtpEndDate.Value < DateTime.Today)
            {
                _dtpEndDate.Value = DateTime.Today.AddMonths(1);
            }
            AppendNote($"[{DateTime.Now:yyyy-MM-dd}] Account manually reactivated by Super Admin.");
            UpdateDunningDisplay();
        }

        private void AppendNote(string note)
        {
            if (string.IsNullOrWhiteSpace(_txtNotes.Text))
                _txtNotes.Text = note;
            else
                _txtNotes.Text = _txtNotes.Text.TrimEnd() + Environment.NewLine + note;
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
