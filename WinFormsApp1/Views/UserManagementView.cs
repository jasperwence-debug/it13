using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using App.Domain.Entities;
using App.Infrastructure;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// MODULE 1 — User Management & Client Companies (SaaS Tenants) Control Center.
    ///
    /// Primary Actor: Super Admin (Platform Programmer & SaaS Vendor).
    ///
    /// Key Capabilities:
    ///   1. Client Companies Directory: Companies that availed/bought the CRM platform
    ///      (Company Code, Name, Subscribed Tier, Admin User, Branches, and Renewal).
    ///   2. Maintenance Access (Tenant Impersonation / Support Mode):
    ///      Grants Super Admin temporary diagnostic access to inspect and maintain a specific
    ///      tenant's dashboard, schedules, branches, and work orders with a top banner and exit toggle.
    ///   3. System Users & Role Permissions: Staff user credentials, roles, and suspension controls.
    /// </summary>
    public class UserManagementView : BaseView
    {
        public enum ViewMode
        {
            ClientCompanies,
            StaffUsers
        }

        public class TenantCompanyItem
        {
            public int CompanyId { get; set; }
            public string CompanyCode { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string Tier { get; set; } = string.Empty;
            public string Status { get; set; } = "Active";
            public string AdminUser { get; set; } = "admin";
            public int BranchesCount { get; set; }
            public DateTime EndDate { get; set; }
            public Company CompanyEntity { get; set; } = null!;
        }

        private ViewMode _currentMode = ViewMode.ClientCompanies;

        // Data caches
        private List<TenantCompanyItem> _companies = new();
        private List<TenantCompanyItem> _filteredCompanies = new();
        private List<User> _users = new();
        private List<User> _filteredUsers = new();
        private readonly HashSet<int> _suspendedUserIds = new();

        // Header Controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnTabCompanies = null!;
        private Button _btnTabUsers = null!;
        private Button _btnRefresh = null!;
        private Button _btnNewUser = null!;

        // Dynamic KPI Ribbon Cards
        private Label _lblKpi1Title = null!;
        private Label _lblKpi1Val = null!;
        private Label _lblKpi1Sub = null!;

        private Label _lblKpi2Title = null!;
        private Label _lblKpi2Val = null!;
        private Label _lblKpi2Sub = null!;

        private Label _lblKpi3Title = null!;
        private Label _lblKpi3Val = null!;
        private Label _lblKpi3Sub = null!;

        private Label _lblKpi4Title = null!;
        private Label _lblKpi4Val = null!;
        private Label _lblKpi4Sub = null!;

        // Filters
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private Label _lblRoleFilter = null!;
        private ComboBox _cmbRoleFilter = null!;

        // Grid & Empty state
        private DataGridView _grid = null!;
        private Panel _pnlGridContainer = null!;
        private Panel _pnlEmptyState = null!;
        private Label _lblEmptyTitle = null!;

        private bool _hasLoaded;
        private static readonly Font _fontBold = new("Segoe UI", 8.5F, FontStyle.Bold);

        public UserManagementView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                LoadAllData();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                LoadAllData();
            }
        }

        // ============================================================
        // UI Construction
        // ============================================================
        private void BuildUI()
        {
            SuspendLayout();
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
            ApplyCardStyle(card);
            Controls.Add(card);

            // ── 1. Top Header Bar (68px) ─────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            card.Controls.Add(pnlHeader);

            var pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 10, 0, 0)
            };
            pnlHeader.Controls.Add(pnlHeaderLeft);

            _lblTitle = new Label
            {
                Text = "Client Companies (SaaS Tenants)",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft,
                UseMnemonic = false
            };
            pnlHeaderLeft.Controls.Add(_lblTitle);

            _lblCount = new Label
            {
                Text = "Loading tenant portfolio...",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlHeaderLeft.Controls.Add(_lblCount);

            // Right header controls: Segmented Tabs & Action Buttons
            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 16, 0, 0)
            };
            pnlHeader.Controls.Add(pnlHeaderRight);

            // Segment Tabs
            _btnTabCompanies = new Button
            {
                Text = "🏢  Client Companies (Tenants)",
                Height = 34,
                Width = 200,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235), // Active primary blue
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
            };
            _btnTabCompanies.FlatAppearance.BorderSize = 0;
            _btnTabCompanies.Click += (s, e) => SwitchTab(ViewMode.ClientCompanies);
            pnlHeaderRight.Controls.Add(_btnTabCompanies);

            _btnTabUsers = new Button
            {
                Text = "👤  System Users",
                Height = 34,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            _btnTabUsers.FlatAppearance.BorderSize = 0;
            _btnTabUsers.Click += (s, e) => SwitchTab(ViewMode.StaffUsers);
            pnlHeaderRight.Controls.Add(_btnTabUsers);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 34,
                Width = 90,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += (s, e) => LoadAllData();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            _btnNewUser = new Button
            {
                Text = "+  Provision User",
                Height = 34,
                Width = 145,
                Margin = new Padding(0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewUser);
            _btnNewUser.Click += OnNewUserClick;
            pnlHeaderRight.Controls.Add(_btnNewUser);

            // ── 2. KPI Summary Ribbon (4 Cards) ──────────────────────
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 98,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 4, 20, 8)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            card.Controls.Add(pnlKpis);

            var (k1, t1, v1, s1) = CreateKpiCard("TOTAL CLIENT COMPANIES", "0", "Businesses running your CRM", Color.FromArgb(30, 41, 59));
            _lblKpi1Title = t1; _lblKpi1Val = v1; _lblKpi1Sub = s1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, t2, v2, s2) = CreateKpiCard("MICRO TIERS (TENANT A)", "0", "Main Transaction + Collection", Color.FromArgb(37, 99, 235));
            _lblKpi2Title = t2; _lblKpi2Val = v2; _lblKpi2Sub = s2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, t3, v3, s3) = CreateKpiCard("SMALL TIERS (TENANT B)", "0", "BI Dashboard + Retention", Color.FromArgb(22, 163, 74));
            _lblKpi3Title = t3; _lblKpi3Val = v3; _lblKpi3Sub = s3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, t4, v4, s4) = CreateKpiCard("ENTERPRISE TIERS (TENANT C)", "0", "Multi-Branch Operations", Color.FromArgb(147, 51, 234));
            _lblKpi4Title = t4; _lblKpi4Val = v4; _lblKpi4Sub = s4;
            pnlKpis.Controls.Add(k4, 3, 0);

            // ── 3. Search & Filter Bar ────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 6, 24, 8)
            };
            card.Controls.Add(pnlSearch);

            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 350,
                Height = 32,
                BackColor = Color.White
            };
            pnlSearchBox.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlSearchBox.Width - 1, pnlSearchBox.Height - 1);
            };
            pnlSearch.Controls.Add(pnlSearchBox);

            _btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnClearSearch.FlatAppearance.BorderSize = 0;
            _btnClearSearch.Click += (s, e) => { _txtSearch.Text = string.Empty; _txtSearch.Focus(); };
            pnlSearchBox.Controls.Add(_btnClearSearch);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.BodyFont,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "🔍  Search by company code, name, or plan..."
            };
            _txtSearch.Location = new Point(6, 6);
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                ApplyFilter();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer);

            _lblRoleFilter = new Label
            {
                Text = "Role:",
                Dock = DockStyle.Left,
                Width = 45,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };
            pnlSearch.Controls.Add(_lblRoleFilter);

            _cmbRoleFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 180,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            _cmbRoleFilter.Items.AddRange(new object[]
            {
                "All Roles",
                Roles.SuperAdmin,
                Roles.Admin,
                Roles.Manager,
                Roles.SalesStaff
            });
            _cmbRoleFilter.SelectedIndex = 0;
            _cmbRoleFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlSearch.Controls.Add(_cmbRoleFilter);

            _cmbRoleFilter.BringToFront();
            _lblRoleFilter.BringToFront();
            pnlSpacer.BringToFront();
            pnlSearchBox.SendToBack();

            var pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            card.Controls.Add(pnlDivider);

            // ── 4. Grid Container ────────────────────────────────────
            _pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
            card.Controls.Add(_pnlGridContainer);

            // Empty state
            _pnlEmptyState = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Visible = false };
            _pnlGridContainer.Controls.Add(_pnlEmptyState);

            _lblEmptyTitle = new Label
            {
                Text = "No Records Found",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(_lblEmptyTitle);

            // DataGridView
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 42 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                ShowCellToolTips = true
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _grid.CellContentClick += OnGridCellContentClick;
            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(pnlDivider, 1);
            card.Controls.SetChildIndex(pnlSearch, 2);
            card.Controls.SetChildIndex(pnlKpis, 3);
            card.Controls.SetChildIndex(pnlHeader, 4);

            ConfigureGridColumns();
            ResumeLayout(false);
        }

        // ============================================================
        // Tab Navigation
        // ============================================================
        private void SwitchTab(ViewMode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;

            if (_currentMode == ViewMode.ClientCompanies)
            {
                _btnTabCompanies.BackColor = Color.FromArgb(37, 99, 235);
                _btnTabCompanies.ForeColor = Color.White;

                _btnTabUsers.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabUsers.ForeColor = Color.FromArgb(71, 85, 105);

                _lblTitle.Text = "Client Companies (SaaS Tenants)";
                _txtSearch.PlaceholderText = "🔍  Search by company code, name, or plan...";
                _lblRoleFilter.Visible = false;
                _cmbRoleFilter.Visible = false;
            }
            else
            {
                _btnTabUsers.BackColor = Color.FromArgb(37, 99, 235);
                _btnTabUsers.ForeColor = Color.White;

                _btnTabCompanies.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabCompanies.ForeColor = Color.FromArgb(71, 85, 105);

                _lblTitle.Text = "User Management & Role Permissions";
                _txtSearch.PlaceholderText = "🔍  Search by username or role...";
                _lblRoleFilter.Visible = true;
                _cmbRoleFilter.Visible = true;
            }

            ConfigureGridColumns();
            UpdateKpis();
            ApplyFilter();
        }

        // ============================================================
        // Grid Column Definitions
        // ============================================================
        private void ConfigureGridColumns()
        {
            _grid.Columns.Clear();

            if (_currentMode == ViewMode.ClientCompanies)
            {
                _grid.Columns.AddRange(new DataGridViewColumn[]
                {
                    new DataGridViewTextBoxColumn { Name = "colCompCode", HeaderText = "Company Code", Width = 130, MinimumWidth = 110 },
                    new DataGridViewTextBoxColumn { Name = "colCompName", HeaderText = "Cleaning Business (Client)", Width = 230, MinimumWidth = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                    new DataGridViewTextBoxColumn { Name = "colTier",     HeaderText = "Subscribed Plan Tier", Width = 180, MinimumWidth = 150 },
                    new DataGridViewTextBoxColumn { Name = "colAdmin",    HeaderText = "Assigned Admin", Width = 130, MinimumWidth = 110 },
                    new DataGridViewTextBoxColumn { Name = "colBranches", HeaderText = "Branches", Width = 100, MinimumWidth = 80 },
                    new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "Plan Status", Width = 110, MinimumWidth = 90 },
                    new DataGridViewButtonColumn  { Name = "colAction",   HeaderText = "Maintenance & Support", Width = 180, MinimumWidth = 160,
                        Text = "🔧 Maintenance Access", UseColumnTextForButtonValue = false, FlatStyle = FlatStyle.Flat }
                });

                _grid.Columns["colCompCode"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                _grid.Columns["colBranches"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                _grid.Columns.AddRange(new DataGridViewColumn[]
                {
                    new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "User ID",        Width = 90,  MinimumWidth = 80  },
                    new DataGridViewTextBoxColumn { Name = "colUsername", HeaderText = "Username",       Width = 180, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                    new DataGridViewTextBoxColumn { Name = "colRole",     HeaderText = "Assigned Role",  Width = 160, MinimumWidth = 130 },
                    new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "Account Status", Width = 140, MinimumWidth = 120 },
                    new DataGridViewButtonColumn  { Name = "colAction",   HeaderText = "Actions",        Width = 140, MinimumWidth = 120,
                        Text = "Toggle Status", UseColumnTextForButtonValue = false, FlatStyle = FlatStyle.Flat }
                });

                _grid.Columns["colId"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private void LoadAllData()
        {
            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);

                // 1. Load Companies & Subscriptions
                var comps = db.Companies.OrderBy(c => c.CompanyId).ToList();
                var subs = db.Subscriptions.ToList();
                var branches = db.Branches.ToList();

                _companies = comps.Select(c =>
                {
                    var sub = subs.FirstOrDefault(s => s.CompanyId == c.CompanyId);
                    var branchCount = branches.Count(b => b.CompanyId == c.CompanyId);

                    string tierLabel = sub != null ? sub.Tier.ToString() : "Micro";
                    if (tierLabel == "Micro") tierLabel = "Tenant A (Micro)";
                    else if (tierLabel == "Small") tierLabel = "Tenant B (Small)";
                    else if (tierLabel == "Medium") tierLabel = "Tenant C (Enterprise)";

                    return new TenantCompanyItem
                    {
                        CompanyId = c.CompanyId,
                        CompanyCode = c.CompanyCode,
                        CompanyName = c.CompanyName,
                        Tier = tierLabel,
                        Status = sub?.Status ?? (c.IsActive ? "Active" : "Inactive"),
                        AdminUser = "admin",
                        BranchesCount = branchCount,
                        EndDate = sub?.EndDate ?? DateTime.Today.AddMonths(1),
                        CompanyEntity = c
                    };
                }).ToList();

                // 2. Load System Users
                _users = db.Users.OrderBy(u => u.Id).ToList();

                UpdateKpis();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Error loading data: {ex.Message}", false);
            }
        }

        private void UpdateKpis()
        {
            if (_currentMode == ViewMode.ClientCompanies)
            {
                _lblKpi1Title.Text = "TOTAL CLIENT COMPANIES";
                _lblKpi1Val.Text = _companies.Count.ToString();
                _lblKpi1Sub.Text = "Businesses on your platform";

                _lblKpi2Title.Text = "MICRO TIERS (TENANT A)";
                _lblKpi2Val.Text = _companies.Count(c => c.Tier.Contains("Micro") || c.Tier.Contains("A")).ToString();
                _lblKpi2Sub.Text = "Main Transaction + Collection";

                _lblKpi3Title.Text = "SMALL TIERS (TENANT B)";
                _lblKpi3Val.Text = _companies.Count(c => c.Tier.Contains("Small") || c.Tier.Contains("B")).ToString();
                _lblKpi3Sub.Text = "BI Analytics + Actions";

                _lblKpi4Title.Text = "ENTERPRISE TIERS (TENANT C)";
                _lblKpi4Val.Text = _companies.Count(c => c.Tier.Contains("Enterprise") || c.Tier.Contains("Medium") || c.Tier.Contains("C")).ToString();
                _lblKpi4Sub.Text = "Multi-Branch Operations";
            }
            else
            {
                _lblKpi1Title.Text = "TOTAL SYSTEM USERS";
                _lblKpi1Val.Text = _users.Count.ToString();
                _lblKpi1Sub.Text = "Provisioned staff profiles";

                _lblKpi2Title.Text = "ADMINISTRATORS";
                _lblKpi2Val.Text = _users.Count(u => u.Role == Roles.SuperAdmin || u.Role == Roles.Admin).ToString();
                _lblKpi2Sub.Text = "Super Admin & Business Admins";

                _lblKpi3Title.Text = "OPERATIONS MANAGERS";
                _lblKpi3Val.Text = _users.Count(u => u.Role == Roles.Manager).ToString();
                _lblKpi3Sub.Text = "Dispatch & Crew Supervisors";

                _lblKpi4Title.Text = "SALES REPRESENTATIVES";
                _lblKpi4Val.Text = _users.Count(u => u.Role == Roles.SalesStaff).ToString();
                _lblKpi4Sub.Text = "Lead intake & outreach";
            }
        }

        private void ApplyFilter()
        {
            string query = _txtSearch.Text.Trim().ToLower();

            if (_currentMode == ViewMode.ClientCompanies)
            {
                _filteredCompanies = _companies.FindAll(c =>
                {
                    if (string.IsNullOrEmpty(query)) return true;
                    return c.CompanyCode.ToLower().Contains(query) ||
                           c.CompanyName.ToLower().Contains(query) ||
                           c.Tier.ToLower().Contains(query);
                });
            }
            else
            {
                string selectedRole = _cmbRoleFilter.SelectedItem?.ToString() ?? "All Roles";
                _filteredUsers = _users.FindAll(u =>
                {
                    bool matchesQuery = string.IsNullOrEmpty(query) ||
                        u.Username.ToLower().Contains(query) ||
                        u.Role.ToLower().Contains(query);

                    if (!matchesQuery) return false;

                    if (selectedRole != "All Roles" && !string.Equals(u.Role, selectedRole, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                });
            }

            RebuildGrid();
        }

        private void RebuildGrid()
        {
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();

                if (_currentMode == ViewMode.ClientCompanies)
                {
                    if (_filteredCompanies.Count == 0)
                    {
                        _pnlEmptyState.Visible = true;
                        _grid.Visible = false;
                        _lblEmptyTitle.Text = "No Client Companies Found";
                        _lblCount.Text = "0 companies registered";
                        return;
                    }

                    _pnlEmptyState.Visible = false;
                    _grid.Visible = true;

                    foreach (var c in _filteredCompanies)
                    {
                        var row = new DataGridViewRow();
                        row.CreateCells(_grid,
                            c.CompanyCode,
                            c.CompanyName,
                            c.Tier,
                            $"👤 {c.AdminUser}",
                            c.BranchesCount > 0 ? $"{c.BranchesCount} Hubs" : "Single Hub",
                            $"● {c.Status}",
                            "🔧 Maintenance Access"
                        );

                        row.Tag = c;

                        // Company code bold
                        row.Cells[0].Style.Font = _fontBold;
                        row.Cells[0].Style.ForeColor = Color.FromArgb(30, 41, 59);

                        // Tier badge color
                        var cellTier = row.Cells[2];
                        cellTier.Style.Font = _fontBold;
                        if (c.Tier.Contains("Micro") || c.Tier.Contains("A")) cellTier.Style.ForeColor = Color.FromArgb(37, 99, 235);
                        else if (c.Tier.Contains("Small") || c.Tier.Contains("B")) cellTier.Style.ForeColor = Color.FromArgb(22, 163, 74);
                        else cellTier.Style.ForeColor = Color.FromArgb(147, 51, 234);

                        // Status color
                        var cellStatus = row.Cells[5];
                        cellStatus.Style.Font = _fontBold;
                        cellStatus.Style.ForeColor = string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)
                            ? Color.FromArgb(22, 163, 74)
                            : Color.FromArgb(220, 38, 38);

                        // Maintenance button styling
                        var cellAction = row.Cells[6];
                        cellAction.Style.BackColor = Color.FromArgb(254, 243, 199); // Amber-100
                        cellAction.Style.ForeColor = Color.FromArgb(180, 83, 9);   // Amber-700
                        cellAction.Style.Font = _fontBold;

                        _grid.Rows.Add(row);
                    }

                    _lblCount.Text = $"{_filteredCompanies.Count} client company{( _filteredCompanies.Count == 1 ? "" : "ies")} registered on platform";
                }
                else
                {
                    if (_filteredUsers.Count == 0)
                    {
                        _pnlEmptyState.Visible = true;
                        _grid.Visible = false;
                        _lblEmptyTitle.Text = "No User Accounts Found";
                        _lblCount.Text = "0 users found";
                        return;
                    }

                    _pnlEmptyState.Visible = false;
                    _grid.Visible = true;

                    foreach (var u in _filteredUsers)
                    {
                        bool isSuspended = _suspendedUserIds.Contains(u.Id);
                        string statusText = isSuspended ? "○ Suspended" : "● Active";
                        string actionText = isSuspended ? "Reactivate" : "Suspend";

                        string roleBadge = u.Role switch
                        {
                            Roles.SuperAdmin => "👑 Super Admin",
                            Roles.Admin => "🟠 Admin",
                            Roles.Manager => "🟢 Manager",
                            Roles.SalesStaff => "🔵 Sales Staff",
                            _ => u.Role
                        };

                        var row = new DataGridViewRow();
                        row.CreateCells(_grid,
                            $"USR-{u.Id:D4}",
                            u.Username,
                            roleBadge,
                            statusText,
                            actionText
                        );

                        row.Tag = u.Id;

                        var cellRole = row.Cells[2];
                        cellRole.Style.Font = _fontBold;
                        if (u.Role == Roles.SuperAdmin) cellRole.Style.ForeColor = Color.FromArgb(161, 98, 7);
                        else if (u.Role == Roles.Admin) cellRole.Style.ForeColor = Color.FromArgb(194, 65, 12);
                        else if (u.Role == Roles.Manager) cellRole.Style.ForeColor = Color.FromArgb(21, 128, 61);
                        else cellRole.Style.ForeColor = Color.FromArgb(29, 78, 216);

                        var cellStatus = row.Cells[3];
                        cellStatus.Style.Font = _fontBold;
                        cellStatus.Style.ForeColor = isSuspended ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);

                        _grid.Rows.Add(row);
                    }

                    _lblCount.Text = $"{_filteredUsers.Count} active user profile{(_filteredUsers.Count == 1 ? "" : "s")}";
                }
            }
            finally
            {
                _grid.ResumeLayout();
            }
        }

        // ============================================================
        // Grid Action Handler
        // ============================================================
        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "colAction") return;

            if (_currentMode == ViewMode.ClientCompanies)
            {
                if (_grid.Rows[e.RowIndex].Tag is TenantCompanyItem comp)
                {
                    var result = MessageBox.Show(
                        $"Activate Maintenance Mode for '{comp.CompanyName}' ({comp.CompanyCode})?\n\nThis will temporarily load this tenant's workspace so you can inspect and maintain their operational records, schedules, and work orders.\n\nYou can exit maintenance mode at any time using the banner at the top of the window.",
                        "Confirm Maintenance Access",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        var mainForm = FindForm() as MainForm;
                        mainForm?.ActivateTenantMaintenanceMode(comp.CompanyEntity);
                    }
                }
            }
            else
            {
                var row = _grid.Rows[e.RowIndex];
                if (row.Tag is int userId)
                {
                    var user = _users.FirstOrDefault(u => u.Id == userId);
                    if (user == null) return;

                    if (user.Role == Roles.SuperAdmin)
                    {
                        MessageBox.Show("Super Administrator accounts cannot be suspended.", "Access Guard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    bool isSuspended = _suspendedUserIds.Contains(userId);
                    string actionWord = isSuspended ? "reactivate" : "suspend";

                    var result = MessageBox.Show(
                        $"Are you sure you want to {actionWord} access for user '{user.Username}' ({user.Role})?",
                        "Confirm User Status Change",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        if (isSuspended) _suspendedUserIds.Remove(userId);
                        else _suspendedUserIds.Add(userId);

                        ApplyFilter();
                        ShowToast($"User '{user.Username}' account status updated.", true);
                    }
                }
            }
        }

        private void OnNewUserClick(object? sender, EventArgs e)
        {
            using var dlg = new NewUserDialog();
            dlg.UserCreated += () =>
            {
                ShowToast("New user account provisioned successfully!", true);
                LoadAllData();
            };
            dlg.ShowDialog(FindForm());
        }

        private static (Panel card, Label titleLabel, Label valLabel, Label subLabel) CreateKpiCard(
            string title,
            string initialVal,
            string subtext,
            Color valColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin = new Padding(4),
                Padding = new Padding(14, 8, 14, 8)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Theme.Border, 1);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16
            };
            card.Controls.Add(lblTitle);

            var valLabel = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(valLabel);

            var lblSub = new Label
            {
                Text = subtext,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = Theme.TextSubtle,
                Dock = DockStyle.Bottom,
                Height = 16
            };
            card.Controls.Add(lblSub);

            return (card, lblTitle, valLabel, lblSub);
        }

        public override void ApplyViewPermissions(string userRole)
        {
            if (_btnNewUser != null)
            {
                _btnNewUser.Visible = (userRole == Roles.SuperAdmin);
            }
            if (_grid != null)
            {
                var col = _grid.Columns["colAction"];
                if (col != null) col.Visible = (userRole == Roles.SuperAdmin);
            }
        }
    }
}
