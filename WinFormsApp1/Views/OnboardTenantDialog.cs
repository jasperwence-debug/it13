using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Infrastructure;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Industry-Grade Multi-Tenant Provisioning & Onboarding Wizard.
    ///
    /// Allows the Super Admin (SaaS Vendor) to onboard a new cleaning company:
    ///   1. Company Profile & Tenant Identity (Company Code, Name, Region, Contact).
    ///   2. Root Company Administrator Account (Credentials & Named Role).
    ///   3. Commercial Subscription Plan Tier (Micro, Small, Enterprise) & Pricing.
    ///   4. Day-1 Operational & Legal Seeding (Branch Hub, Service Catalog, Legal Policies).
    ///   5. Atomic Database Persistence & Immediate Maintenance Mode Activation.
    /// </summary>
    public class OnboardTenantDialog : Form
    {
        public event Action<Company>? TenantOnboarded;

        // Section 1: Company Profile
        private TextBox _txtCompanyName = null!;
        private TextBox _txtCompanyCode = null!;
        private ComboBox _cmbCity = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;

        // Section 2: Root Admin Account
        private TextBox _txtAdminUser = null!;
        private TextBox _txtAdminPass = null!;
        private TextBox _txtAdminName = null!;

        // Section 3: Commercial Plan Tier
        private ComboBox _cmbTier = null!;
        private ComboBox _cmbBillingCycle = null!;
        private TextBox _txtPrice = null!;
        private Label _lblTierSummary = null!;
        private Label _lblLimitsSummary = null!;

        // Section 4: Day-1 Seeding
        private CheckBox _chkBranch = null!;
        private CheckBox _chkServices = null!;
        private CheckBox _chkLegal = null!;
        private CheckBox _chkRegistry = null!;

        // Footer Actions
        private Label _lblStatus = null!;
        private Button _btnSubmit = null!;
        private Button _btnCancel = null!;

        public OnboardTenantDialog()
        {
            BuildUI();
            UpdateTierDefaults();
        }

        private void BuildUI()
        {
            Text = "🏢  Onboard New Client Company — Multi-Tenant Provisioning Wizard";
            Size = new Size(680, 760);
            MinimumSize = new Size(640, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            // ── 1. Top Header Banner ─────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 12, 24, 12)
            };
            Controls.Add(pnlHeader);

            var lblHeaderTitle = new Label
            {
                Text = "🏢  SaaS Tenant Provisioning & Onboarding Wizard",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 26,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            var lblHeaderSub = new Label
            {
                Text = "Establish isolated tenant boundary, root admin account, subscription tier, and Day-1 defaults",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblHeaderSub);

            // ── 2. Bottom Footer Action Bar ──────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 14, 24, 14)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };
            Controls.Add(pnlFooter);

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.Empty,
                UseMnemonic = false
            };
            pnlFooter.Controls.Add(_lblStatus);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Width = 95,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            var spc = new Panel { Dock = DockStyle.Right, Width = 10 };
            pnlFooter.Controls.Add(spc);

            _btnSubmit = new Button
            {
                Text = "🚀  Complete Provisioning",
                Width = 195,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            Theme.ApplyPrimaryButtonStyle(_btnSubmit);
            _btnSubmit.Click += async (s, e) => await ExecuteProvisioningAsync();
            pnlFooter.Controls.Add(_btnSubmit);

            // ── 3. Main Scrollable Form Body ─────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            int y = 8;

            // SECTION 1: Company Profile & Tenant Identity
            var pnlSec1 = CreateSectionHeader("1. Company Profile & Tenant Identity", "Legal business entity, tenant identifier key, and primary regional contact");
            pnlSec1.Location = new Point(16, y);
            pnlSec1.Width = pnlBody.Width - 52;
            pnlSec1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBody.Controls.Add(pnlSec1);
            y += pnlSec1.Height + 10;

            // Company Name
            var lblCompName = new Label { Text = "Cleaning Business Legal Name: *", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCompName);
            y += 22;

            _txtCompanyName = new TextBox { Location = new Point(20, y), Width = 340, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. Apex Commercial Cleaning Inc." };
            _txtCompanyName.TextChanged += (s, e) => AutoSuggestCompanyCode();
            pnlBody.Controls.Add(_txtCompanyName);

            // Company Code (Tenant Key)
            var lblCompCode = new Label { Text = "Tenant Code (Unique DB Key): *", Location = new Point(380, y - 22), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCompCode);

            _txtCompanyCode = new TextBox { Location = new Point(380, y), Width = 210, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), CharacterCasing = CharacterCasing.Upper, PlaceholderText = "e.g. T-APEX" };
            pnlBody.Controls.Add(_txtCompanyCode);
            y += 36;

            // Operating City & Phone
            var lblCity = new Label { Text = "Headquarters City / Hub Location:", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCity);

            var lblPhone = new Label { Text = "Official Contact Phone:", Location = new Point(380, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblPhone);
            y += 22;

            _cmbCity = new ComboBox { Location = new Point(20, y), Width = 340, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDown };
            _cmbCity.Items.AddRange(new object[] { "Metro Manila", "Makati City", "Quezon City", "Taguig City (BGC)", "Pasig City", "Cebu City", "Davao City", "Angeles City", "Bacolod City" });
            _cmbCity.SelectedIndex = 0;
            _txtPhone = new TextBox { Location = new Point(380, y), Width = 210, MaxLength = 11, Font = new Font("Segoe UI", 9.5F), Text = "09175550199", PlaceholderText = "e.g. 09171234567" };
            _txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            };
            pnlBody.Controls.Add(_txtPhone);
            y += 36;

            // Official Email
            var lblEmail = new Label { Text = "Corporate Billing / Operations Email:", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblEmail);
            y += 22;

            _txtEmail = new TextBox { Location = new Point(20, y), Width = 570, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "operations@company.com" };
            pnlBody.Controls.Add(_txtEmail);
            y += 46;

            // SECTION 2: Root Company Administrator Account
            var pnlSec2 = CreateSectionHeader("2. Root Company Administrator Account", "Initial system administrator credentials provisioned for this tenant's owner");
            pnlSec2.Location = new Point(16, y);
            pnlSec2.Width = pnlBody.Width - 52;
            pnlSec2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBody.Controls.Add(pnlSec2);
            y += pnlSec2.Height + 10;

            var lblAdminUser = new Label { Text = "Admin Username: *", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblAdminUser);

            var lblAdminPass = new Label { Text = "Initial Password: *", Location = new Point(310, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblAdminPass);
            y += 22;

            _txtAdminUser = new TextBox { Location = new Point(20, y), Width = 270, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. apex_admin" };
            pnlBody.Controls.Add(_txtAdminUser);

            _txtAdminPass = new TextBox { Location = new Point(310, y), Width = 280, Font = new Font("Segoe UI", 9.5F), Text = "admin123", PasswordChar = '•' };
            pnlBody.Controls.Add(_txtAdminPass);
            y += 36;

            var lblAdminName = new Label { Text = "Administrator Full Name / Contact Person:", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblAdminName);
            y += 22;

            _txtAdminName = new TextBox { Location = new Point(20, y), Width = 570, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "e.g. Juan dela Cruz (Managing Director)" };
            pnlBody.Controls.Add(_txtAdminName);
            y += 46;

            // SECTION 3: Commercial Plan Tier & Entitlements
            var pnlSec3 = CreateSectionHeader("3. Commercial Subscription Plan Tier & Quota", "Assign multi-tenant licensing tier, billing cycle, and feature entitlements");
            pnlSec3.Location = new Point(16, y);
            pnlSec3.Width = pnlBody.Width - 52;
            pnlSec3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBody.Controls.Add(pnlSec3);
            y += pnlSec3.Height + 10;

            var lblTier = new Label { Text = "Subscription Plan Tier: *", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblTier);

            var lblCycle = new Label { Text = "Billing Frequency:", Location = new Point(380, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCycle);
            y += 22;

            _cmbTier = new ComboBox { Location = new Point(20, y), Width = 340, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTier.Items.AddRange(new object[]
            {
                "Tenant A — Micro Company (₱2,500/mo)",
                "Tenant B — Small Company (₱5,500/mo)",
                "Tenant C — Medium Enterprise (₱12,000/mo)"
            });
            _cmbTier.SelectedIndex = 1; // Default to Tenant B
            _cmbTier.SelectedIndexChanged += (s, e) => UpdateTierDefaults();
            pnlBody.Controls.Add(_cmbTier);

            _cmbBillingCycle = new ComboBox { Location = new Point(380, y), Width = 210, Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbBillingCycle.Items.AddRange(new object[] { "Monthly", "Quarterly", "Annual (10% Discount)" });
            _cmbBillingCycle.SelectedIndex = 0;
            _cmbBillingCycle.SelectedIndexChanged += (s, e) => UpdateTierDefaults();
            pnlBody.Controls.Add(_cmbBillingCycle);
            y += 36;

            var lblPrice = new Label { Text = "Negotiated Monthly Price (₱):", Location = new Point(20, y), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblPrice);
            y += 22;

            _txtPrice = new TextBox { Location = new Point(20, y), Width = 160, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "5500.00" };
            pnlBody.Controls.Add(_txtPrice);

            // Entitlements summary card
            var pnlEntCard = new Panel
            {
                Location = new Point(195, y - 10),
                Width = 395,
                Height = 64,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlEntCard.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlEntCard.Width - 1, pnlEntCard.Height - 1);
            };
            pnlBody.Controls.Add(pnlEntCard);

            _lblLimitsSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Text = "Limits: Max 10 Users | 1 Branch Hub",
                UseMnemonic = false
            };
            pnlEntCard.Controls.Add(_lblLimitsSummary);

            _lblTierSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Text = "Unlocks: BI Analytics + Retention CRM + Quick Actions",
                UseMnemonic = false
            };
            pnlEntCard.Controls.Add(_lblTierSummary);
            y += 66;

            // SECTION 4: Day-1 Operational & Legal Seeding
            var pnlSec4 = CreateSectionHeader("4. Day-1 Operational & Legal Seeding", "Eliminates empty-system friction by pre-populating core operational assets");
            pnlSec4.Location = new Point(16, y);
            pnlSec4.Width = pnlBody.Width - 52;
            pnlSec4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlBody.Controls.Add(pnlSec4);
            y += pnlSec4.Height + 10;

            _chkBranch = new CheckBox
            {
                Text = "Provision Initial Operational Branch Hub in headquarters city",
                Location = new Point(24, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            pnlBody.Controls.Add(_chkBranch);
            y += 28;

            _chkServices = new CheckBox
            {
                Text = "Pre-seed Standard Service Catalog (General House Cleaning, Deep Sanitization, Move-In Turnover)",
                Location = new Point(24, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlBody.Controls.Add(_chkServices);
            y += 26;

            _chkLegal = new CheckBox
            {
                Text = "Pre-populate Customer Master Service Agreement (MSA) & 24h Cancellation Policy in Terms Management",
                Location = new Point(24, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlBody.Controls.Add(_chkLegal);
            y += 26;

            _chkRegistry = new CheckBox
            {
                Text = "Synchronize tenant record with local development tenant registry (tenants.json)",
                Location = new Point(24, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            pnlBody.Controls.Add(_chkRegistry);
            y += 40;
        }

        private static Panel CreateSectionHeader(string title, string subtitle)
        {
            var pnl = new Panel { Height = 44, BackColor = Color.Transparent };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 20,
                UseMnemonic = false
            };
            pnl.Controls.Add(lblT);

            var lblS = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 18,
                UseMnemonic = false
            };
            pnl.Controls.Add(lblS);

            var div = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnl.Controls.Add(div);

            return pnl;
        }

        private void AutoSuggestCompanyCode()
        {
            if (string.IsNullOrWhiteSpace(_txtCompanyName.Text)) return;

            // Only auto-suggest if user hasn't explicitly customized it to something else
            string raw = _txtCompanyName.Text.Trim();
            string[] words = raw.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            string code;
            if (words.Length == 1)
            {
                code = "T-" + (raw.Length > 8 ? raw.Substring(0, 8) : raw).ToUpper();
            }
            else
            {
                string initials = string.Concat(words.Select(w => w[0])).ToUpper();
                code = "T-" + initials;
            }

            if (string.IsNullOrEmpty(_txtCompanyCode.Text) || _txtCompanyCode.Text.StartsWith("T-"))
            {
                _txtCompanyCode.Text = code;
            }

            // Also suggest admin username
            if (string.IsNullOrEmpty(_txtAdminUser.Text))
            {
                string slug = words[0].ToLower();
                _txtAdminUser.Text = $"{slug}_admin";
            }
        }

        private void UpdateTierDefaults()
        {
            int idx = _cmbTier.SelectedIndex;
            bool isAnnual = _cmbBillingCycle.SelectedIndex == 2;

            switch (idx)
            {
                case 0: // Micro
                    _txtPrice.Text = isAnnual ? "2250.00" : "2500.00";
                    _lblLimitsSummary.Text = "Limits: Max 3 Users | 1 Branch Hub";
                    _lblTierSummary.Text = "Entitlements: Core Transactions + Customer Lead Intake";
                    break;

                case 1: // Small
                    _txtPrice.Text = isAnnual ? "4950.00" : "5500.00";
                    _lblLimitsSummary.Text = "Limits: Max 10 Users | 1 Branch Hub";
                    _lblTierSummary.Text = "Entitlements: Core CRM + BI Analytics + Retention Management";
                    break;

                case 2: // Enterprise
                    _txtPrice.Text = isAnnual ? "10800.00" : "12000.00";
                    _lblLimitsSummary.Text = "Limits: Max 50 Users | Up to 10 Branch Hubs";
                    _lblTierSummary.Text = "Entitlements: Regional Branching + Multi-Hub Workforce Dispatch";
                    break;
            }
        }

        // ============================================================
        // Execution & Database Transaction
        // ============================================================
        private async System.Threading.Tasks.Task ExecuteProvisioningAsync()
        {
            _lblStatus.Text = string.Empty;

            var companyName = _txtCompanyName.Text.Trim();
            var companyCode = _txtCompanyCode.Text.Trim().ToUpper();
            var city = _cmbCity.Text.Trim();
            var phone = _txtPhone.Text.Trim();
            var email = _txtEmail.Text.Trim();

            var adminUser = _txtAdminUser.Text.Trim().ToLower();
            var adminPass = _txtAdminPass.Text;

            // 1. Rigorous Validation
            if (string.IsNullOrWhiteSpace(companyName) || companyName.Length < 3)
            {
                _lblStatus.Text = "⚠ Company Legal Name must be at least 3 characters.";
                _txtCompanyName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(companyCode) || companyCode.Length < 2)
            {
                _lblStatus.Text = "⚠ Tenant Code must be at least 2 characters.";
                _txtCompanyCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(adminUser) || adminUser.Length < 3)
            {
                _lblStatus.Text = "⚠ Administrator Username must be at least 3 characters.";
                _txtAdminUser.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(adminPass) || adminPass.Length < 4)
            {
                _lblStatus.Text = "⚠ Initial Password must be at least 4 characters.";
                _txtAdminPass.Focus();
                return;
            }

            if (!decimal.TryParse(_txtPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
            {
                _lblStatus.Text = "⚠ Please specify a valid monthly subscription price.";
                _txtPrice.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!ValidationHelper.IsValidPhoneNumber(phone, isRequired: false, out var phoneErr))
                {
                    _lblStatus.Text = phoneErr;
                    _txtPhone.Focus();
                    return;
                }
            }

            _btnSubmit.Enabled = false;
            _btnSubmit.Text = "⏳  Provisioning...";

            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);

                // 2. Uniqueness Checks
                if (db.Companies.Any(c => c.CompanyCode.ToUpper() == companyCode))
                {
                    _lblStatus.Text = $"⚠ Tenant Code '{companyCode}' is already registered in the system.";
                    _txtCompanyCode.Focus();
                    return;
                }

                if (db.Users.Any(u => u.Username.ToLower() == adminUser))
                {
                    _lblStatus.Text = $"⚠ Username '{adminUser}' already exists. Choose a different username.";
                    _txtAdminUser.Focus();
                    return;
                }

                // Determine Tier
                var tier = _cmbTier.SelectedIndex switch
                {
                    0 => SubscriptionTier.Micro,
                    1 => SubscriptionTier.Small,
                    2 => SubscriptionTier.Medium,
                    _ => SubscriptionTier.Small
                };

                var billingCycle = _cmbBillingCycle.SelectedItem?.ToString() ?? "Monthly";
                DateTime startDate = DateTime.UtcNow;
                DateTime endDate = billingCycle.Contains("Annual") ? startDate.AddYears(1) : startDate.AddMonths(1);

                Company? createdCompany = null;

                // 3. Atomic Database Transaction
                using (var tx = db.Database.BeginTransaction())
                {
                    // A. Company Entity
                    var company = new Company
                    {
                        CompanyCode = companyCode,
                        CompanyName = companyName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Companies.Add(company);
                    db.SaveChanges(); // Generates CompanyId

                    createdCompany = company;

                    // B. Subscription Plan Entity
                    var subscription = new Subscription
                    {
                        CompanyId = company.CompanyId,
                        Tier = tier,
                        Status = "Active",
                        BillingCycle = billingCycle.Contains("Annual") ? "Annual" : (billingCycle.Contains("Quarterly") ? "Quarterly" : "Monthly"),
                        MonthlyPrice = price,
                        StartDate = startDate,
                        EndDate = endDate,
                        Notes = $"Onboarded by Super Admin via Provisioning Wizard on {DateTime.Now:yyyy-MM-dd HH:mm}"
                    };
                    subscription.ApplyTierDefaults(tier);
                    subscription.MonthlyPrice = price;
                    db.Subscriptions.Add(subscription);

                    // C. Root Tenant Admin User
                    var user = new User
                    {
                        Username = adminUser,
                        PasswordHash = adminPass,
                        Role = Roles.Admin
                    };
                    db.Users.Add(user);

                    // D. Primary Branch Hub (if requested)
                    if (_chkBranch.Checked)
                    {
                        var branch = new Branch
                        {
                            CompanyId = company.CompanyId,
                            BranchCode = $"{companyCode}-HQ",
                            BranchName = $"{companyName} (Main Hub)",
                            City = string.IsNullOrWhiteSpace(city) ? "Metro Manila" : city,
                            Address = $"{city} Operations Central Depot",
                            Phone = string.IsNullOrWhiteSpace(phone) ? "+63 917 000 0000" : phone,
                            Email = string.IsNullOrWhiteSpace(email) ? $"{adminUser}@{companyCode.ToLower()}.ph" : email,
                            ManagerName = string.IsNullOrWhiteSpace(_txtAdminName.Text) ? adminUser : _txtAdminName.Text.Trim(),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Branches.Add(branch);
                    }

                    // E. Core Services Pre-seeding
                    if (_chkServices.Checked && !db.Services.Any())
                    {
                        db.Services.AddRange(
                            new Service { ServiceName = "General House Cleaning", Category = "Residential", BasePrice = 1500m, Description = "Standard dusting, mopping, trash disposal, and surface sanitation." },
                            new Service { ServiceName = "Deep Sanitization & Disinfection", Category = "Commercial", BasePrice = 3800m, Description = "High-touch disinfection, kitchen degreasing, and deep restroom sanitizing." },
                            new Service { ServiceName = "Move-In / Move-Out Turnover", Category = "Turnover", BasePrice = 4500m, Description = "Comprehensive empty-unit restoration and turnover sanitization." }
                        );
                    }

                    db.SaveChanges();
                    tx.Commit();
                }

                // 4. Update local registry (tenants.json) if requested
                if (_chkRegistry.Checked)
                {
                    TryUpdateTenantJsonRegistry(companyCode, companyName);
                }

                // 5. Success Flow & Optional Instant Maintenance Mode Launch
                TenantOnboarded?.Invoke(createdCompany);

                var prompt = MessageBox.Show(
                    $"Tenant Company '{companyName}' ({companyCode}) successfully provisioned and committed to the database!\n\n" +
                    $"• Plan Tier: {tier} (₱{price:N2}/mo)\n" +
                    $"• Root Administrator: {adminUser}\n" +
                    $"• Initial Hub: {(_chkBranch.Checked ? $"{companyCode}-HQ" : "Single Hub")}\n\n" +
                    $"Would you like to activate Maintenance Mode now to configure this tenant's workspace?",
                    "Onboarding Completed Successfully",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (prompt == DialogResult.Yes && createdCompany != null)
                {
                    (Owner as MainForm ?? FindForm() as MainForm)?.ActivateTenantMaintenanceMode(createdCompany);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Database Error: {ex.Message}";
            }
            finally
            {
                _btnSubmit.Enabled = true;
                _btnSubmit.Text = "🚀  Complete Provisioning";
            }
        }

        private static void TryUpdateTenantJsonRegistry(string tenantCode, string displayName)
        {
            try
            {
                var candidatePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tenants.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "tenants.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "WinFormsApp1", "tenants.json")
                };

                foreach (var path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        var content = File.ReadAllText(path);
                        // Clean append if not already present
                        if (!content.Contains(tenantCode, StringComparison.OrdinalIgnoreCase))
                        {
                            string newEntry =
$@"    {{
      ""tenantId"": ""{tenantCode.ToLowerInvariant()}"",
      ""displayName"": ""{displayName}"",
      ""connectionString"": ""Server=(localdb)\\MSSQLLocalDB;Database=AppDb;Trusted_Connection=True;TrustServerCertificate=True;"",
      ""expectedSchemaVersion"": ""1.0.0"",
      ""isActive"": true,
      ""notes"": ""Onboarded via Super Admin Provisioning Wizard""
    }},
";
                            int insertIdx = content.IndexOf("\"tenants\": [");
                            if (insertIdx >= 0)
                            {
                                int bracketIdx = content.IndexOf('[', insertIdx);
                                if (bracketIdx >= 0)
                                {
                                    string updated = content.Insert(bracketIdx + 1, "\n" + newEntry);
                                    File.WriteAllText(path, updated);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Non-critical local JSON synchronization
            }
        }
    }
}
