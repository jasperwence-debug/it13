using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using App.Domain.Entities;
using App.WinForms.Core;

namespace App.WinForms
{
    public class MainForm : Form
    {
        // ============================================================
        // Layout
        // ============================================================
        private Panel _pnlSidebar = null!;
        private Panel _pnlMenu = null!;
        private Panel _pnlContent = null!;
        private Panel _pnlHeader = null!;
        private Label _lblPageTitle = null!;
        private Label _lblUser = null!;
        private Label _lblBrand = null!;
        private Label _lblSidebarUserInfo = null!;
        private Button btnLogout = null!;

        // Header Action Controls
        private Button _btnSidebarToggle = null!;
        private Button _btnQuickSearch = null!;
        private Button _btnQuickAction = null!;
        private Button _btnNotifications = null!;
        private ContextMenuStrip _mnuQuickAction = null!;
        private bool _isSidebarCollapsed = false;

        private readonly NotificationService _notificationService = new();
        private readonly List<NotificationItem> _currentNotifications = new();
        private readonly System.Windows.Forms.Timer _notifTimer = new() { Interval = 30000 };

        private readonly Dictionary<Button, (string Icon, string Label, string Key)> _buttonMeta = new();
        private readonly ToolTip _toolTip = new() { ShowAlways = true };

        // Standalone Top-Level Divider & 3 Category Headers (Font: 8pt, Bold, Color: #64748B)
        private Panel pnlDashboardDivider = null!;
        private Label lblHeaderOperations = null!;
        private Label lblHeaderPerformance = null!;
        private Label lblHeaderAdministration = null!;

        // Sidebar Buttons
        private Button btnDashboard = null!;
        private Button btnLeads = null!;
        private Button btnClientContract = null!;
        private Button btnSchedulingDispatch = null!;
        private Button btnBranches = null!;
        private Button btnSalesRetention = null!;
        private Button btnFinancial = null!;
        private Button btnReportsAudit = null!;
        private Button btnUserManagement = null!;
        private Button btnManageSubscription = null!;
        private Button btnTermsManagement = null!;

        private readonly List<Button> _sidebarButtons = new();
        private Control[] _sidebarControlsInOrder = null!;

        // Super Admin SaaS Maintenance Mode Controls
        private Panel _pnlMaintenanceBanner = null!;
        private Label _lblMaintenanceInfo = null!;
        private Button _btnExitMaintenance = null!;

        public bool IsLoggedOut { get; private set; }

        public MainForm()
        {
            InitializeUI();
            BuildQuickActionMenu();
            ApplyRolePermissions();
            EnforceSidebarOrder();

            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            _notifTimer.Tick += async (s, e) => await RefreshNotificationBadgeAsync();

            // Default View Routing and Sidebar Z-Order on Form Load
            Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            EnforceSidebarOrder();
            LoadDefaultView();
            _ = RefreshNotificationBadgeAsync();
            _notifTimer.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnforceSidebarOrder();
        }

        // ============================================================
        // UI Construction
        // ============================================================
        private void InitializeUI()
        {
            Text = "Cleaning Services CRM";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            Font = new Font("Segoe UI", 9F);

            // ============================================================
            // SIDEBAR
            // ============================================================
            _pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Color.FromArgb(30, 41, 59)
            };
            Controls.Add(_pnlSidebar);

            // Brand
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            pnlBrand.Paint += (s, e) =>
            {
                var stripeRect = new Rectangle(0, pnlBrand.Height - 3, pnlBrand.Width, 3);
                using var stripeBrush = new LinearGradientBrush(
                    stripeRect,
                    Color.FromArgb(37, 99, 235), // Blue-600
                    Color.FromArgb(79, 70, 229), // Indigo-600
                    LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(stripeBrush, stripeRect);
            };
            _pnlSidebar.Controls.Add(pnlBrand);

            _lblBrand = new Label
            {
                Text = "🧹  CLEANING CRM",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };
            pnlBrand.Controls.Add(_lblBrand);

            // Sidebar User & Logout Footer
            var pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 10, 12, 10)
            };
            _pnlSidebar.Controls.Add(pnlSidebarFooter);

            _lblSidebarUserInfo = new Label
            {
                Text = $"{SessionManager.CurrentUser?.Username ?? "User"}  •  {SessionManager.CurrentUser?.Role ?? "Guest"}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlSidebarFooter.Controls.Add(_lblSidebarUserInfo);

            btnLogout = new Button
            {
                Text = "🚪  Sign Out",
                Dock = DockStyle.Bottom,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to sign out?", "Confirm Sign Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    IsLoggedOut = true;
                    SessionManager.Logout();
                    Close();
                }
            };
            pnlSidebarFooter.Controls.Add(btnLogout);

            // Menu container
            _pnlMenu = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 10)
            };
            _pnlSidebar.Controls.Add(_pnlMenu);
            _pnlMenu.BringToFront();

            // Standalone Topmost Action: Dashboard Button
            btnDashboard = CreateSidebarButton("dashboard", "📊", "Dashboard");

            // Subtle divider separating standalone Dashboard from categorized sections
            pnlDashboardDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 12,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = new Padding(14, 5, 14, 6)
            };
            var pnlDividerLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(40, 53, 72)
            };
            pnlDashboardDivider.Controls.Add(pnlDividerLine);

            // Create 3 Category Headers (Font: 8pt Bold, Color: #64748B)
            lblHeaderOperations = CreateGroupLabel("OPERATIONS");
            lblHeaderPerformance = CreateGroupLabel("PERFORMANCE");
            lblHeaderAdministration = CreateGroupLabel("ADMINISTRATION");

            // Create Sidebar Buttons with display names matching official Use Cases
            btnLeads = CreateSidebarButton("leads", "🎯", "Leads & Inquiries");
            btnClientContract = CreateSidebarButton("clients", "📋", "Client & Contract");
            btnSchedulingDispatch = CreateSidebarButton("scheduling", "📅", "Scheduling & Dispatch");
            btnBranches = CreateSidebarButton("branches", "🏢", "Operating Branches");
            btnSalesRetention = CreateSidebarButton("sales", "💼", "Sales & Retention");
            btnFinancial = CreateSidebarButton("financial", "💰", "Financial Management");
            btnReportsAudit = CreateSidebarButton("reports", "📈", "Reports & Audit");
            btnUserManagement = CreateSidebarButton("users", "👥", "User Management");
            btnManageSubscription = CreateSidebarButton("subscription", "🔁", "Manage Subscription");
            btnTermsManagement = CreateSidebarButton("terms", "📜", "Terms & Management");

            // Add to _sidebarButtons collection for active highlight handling
            _sidebarButtons.AddRange(new[]
            {
                btnDashboard,
                btnLeads,
                btnClientContract,
                btnSchedulingDispatch,
                btnBranches,
                btnSalesRetention,
                btnFinancial,
                btnReportsAudit,
                btnUserManagement,
                btnManageSubscription,
                btnTermsManagement
            });

            // Master controls ordering: Standalone Dashboard at the absolute top (Index 0), followed by categories
            _sidebarControlsInOrder = new Control[]
            {
                btnDashboard,
                pnlDashboardDivider,

                lblHeaderOperations,
                btnLeads,
                btnClientContract,
                btnSchedulingDispatch,
                btnBranches,

                lblHeaderPerformance,
                btnSalesRetention,
                btnFinancial,
                btnReportsAudit,

                lblHeaderAdministration,
                btnUserManagement,
                btnManageSubscription,
                btnTermsManagement
            };

            // Add sidebar controls to container and enforce explicit child indices
            _pnlMenu.SuspendLayout();
            foreach (var ctrl in _sidebarControlsInOrder)
            {
                _pnlMenu.Controls.Add(ctrl);
            }
            EnforceSidebarOrder();
            _pnlMenu.ResumeLayout(true);

            // ============================================================
            // HEADER
            // ============================================================
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(16, 11, 16, 11)
            };
            Controls.Add(_pnlHeader);

            // Right Area: User Pill + Quick Action + Quick Search (Dock = Right)
            var pnlRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.White,
                WrapContents = false,
                Margin = Padding.Empty
            };
            _pnlHeader.Controls.Add(pnlRight);

            var userRole = SessionManager.CurrentUser?.Role ?? "Guest";
            var userName = SessionManager.CurrentUser?.Username ?? "User";

            _lblUser = new Label
            {
                Text = $"👤  {userName}  •  {userRole}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.FromArgb(241, 245, 249),
                Height = 38,
                AutoSize = true,
                Padding = new Padding(12, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(8, 0, 0, 0),
                UseMnemonic = false
            };
            pnlRight.Controls.Add(_lblUser);

            _btnNotifications = new Button
            {
                Text = "🔔",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                Height = 38,
                Width = 46,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                UseMnemonic = false
            };
            _btnNotifications.FlatAppearance.BorderSize = 0;
            _btnNotifications.Click += (s, e) => OpenNotificationCenter();
            _toolTip.SetToolTip(_btnNotifications, "Notifications & Approvals");
            pnlRight.Controls.Add(_btnNotifications);

            _btnQuickAction = new Button
            {
                Text = "⚡  + Quick Action ▾",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 99, 235),
                Height = 38,
                Width = 165,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                UseMnemonic = false
            };
            _btnQuickAction.FlatAppearance.BorderSize = 0;
            Theme.ApplyPrimaryButtonStyle(_btnQuickAction);
            _btnQuickAction.Click += (s, e) =>
            {
                BuildQuickActionMenu();
                _mnuQuickAction.Show(_btnQuickAction, 0, _btnQuickAction.Height + 4);
            };
            pnlRight.Controls.Add(_btnQuickAction);

            _btnQuickSearch = new Button
            {
                Text = "🔍   Search (Ctrl+K)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Height = 38,
                Width = 200,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                UseMnemonic = false
            };
            _btnQuickSearch.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            _btnQuickSearch.FlatAppearance.BorderSize = 1;
            _btnQuickSearch.Click += (s, e) => OpenCommandPalette();
            _toolTip.SetToolTip(_btnQuickSearch, "Universal Spotlight Search (Ctrl+K)");
            pnlRight.Controls.Add(_btnQuickSearch);

            // Left Area: Sidebar Toggle + Breadcrumb Title (Dock = Fill)
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pnlHeader.Controls.Add(pnlLeft);

            _btnSidebarToggle = new Button
            {
                Text = "☰",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 38,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnSidebarToggle.FlatAppearance.BorderSize = 0;
            _btnSidebarToggle.Click += (s, e) => ToggleSidebar();
            _toolTip.SetToolTip(_btnSidebarToggle, "Toggle sidebar (Ctrl+B)");
            pnlLeft.Controls.Add(_btnSidebarToggle);

            var pnlSpcLeft = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.White };
            pnlLeft.Controls.Add(pnlSpcLeft);

            _lblPageTitle = new Label
            {
                Text = "Cleaning CRM  ›  Dashboard",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                AutoEllipsis = true
            };
            pnlLeft.Controls.Add(_lblPageTitle);

            // Ensure proper layout inside _pnlHeader (pnlRight docked Right first, pnlLeft fills remaining)
            _pnlHeader.Controls.SetChildIndex(pnlLeft, 0);
            _pnlHeader.Controls.SetChildIndex(pnlRight, 1);

            // Ensure proper layout inside pnlLeft (toggle button on left, label fills remainder)
            pnlLeft.Controls.SetChildIndex(_lblPageTitle, 0);
            pnlLeft.Controls.SetChildIndex(pnlSpcLeft, 1);
            pnlLeft.Controls.SetChildIndex(_btnSidebarToggle, 2);

            var pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            _pnlHeader.Controls.Add(pnlHeaderBorder);

            // ============================================================
            // SUPER ADMIN MAINTENANCE MODE BANNER
            // ============================================================
            _pnlMaintenanceBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(254, 243, 199), // Amber-100
                Padding = new Padding(20, 6, 20, 6),
                Visible = false
            };
            _pnlMaintenanceBanner.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(245, 158, 11), 1); // Amber-500
                e.Graphics.DrawLine(p, 0, _pnlMaintenanceBanner.Height - 1, _pnlMaintenanceBanner.Width, _pnlMaintenanceBanner.Height - 1);
            };
            Controls.Add(_pnlMaintenanceBanner);

            _lblMaintenanceInfo = new Label
            {
                Text = "🛠️ SUPER ADMIN MAINTENANCE MODE: Diagnosing Tenant",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14), // Amber-800
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            _pnlMaintenanceBanner.Controls.Add(_lblMaintenanceInfo);

            _btnExitMaintenance = new Button
            {
                Text = "✕  Exit Maintenance Mode",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(217, 119, 6), // Amber-600
                FlatStyle = FlatStyle.Flat,
                Height = 30,
                Width = 195,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnExitMaintenance.FlatAppearance.BorderSize = 0;
            _btnExitMaintenance.Click += (s, e) => DeactivateTenantMaintenanceMode();
            _pnlMaintenanceBanner.Controls.Add(_btnExitMaintenance);

            // ============================================================
            // CONTENT
            // ============================================================
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Theme.Background
            };
            Controls.Add(_pnlContent);

            // Enforce explicit docking priority in Form.Controls:
            // WinForms docks controls in reverse ChildIndex order (3 -> 2 -> 1 -> 0):
            // Index 3: _pnlSidebar (DockStyle.Left) - claims full height on the left
            // Index 2: _pnlHeader (DockStyle.Top) - claims top edge
            // Index 1: _pnlMaintenanceBanner (DockStyle.Top) - claims space below header when active
            // Index 0: _pnlContent (DockStyle.Fill) - fills remaining space (NEVER overlaps header!)
            Controls.SetChildIndex(_pnlContent, 0);
            Controls.SetChildIndex(_pnlMaintenanceBanner, 1);
            Controls.SetChildIndex(_pnlHeader, 2);
            Controls.SetChildIndex(_pnlSidebar, 3);
        }

        // ============================================================
        // Sidebar Z-Order Hierarchy Enforcement
        // Strictly sets child indices so btnDashboard renders at the absolute top (Top=0)
        // ============================================================
        private void EnforceSidebarOrder()
        {
            if (_pnlMenu == null || _sidebarControlsInOrder == null) return;

            _pnlMenu.SuspendLayout();

            // In WinForms Dock.Top layout, the control with the highest child index is docked to Top=0 first.
            // Assigning SetChildIndex in reverse ensures _sidebarControlsInOrder[0] (Dashboard) gets index Count-1 (Top=0),
            // guaranteeing perfect top-to-bottom Z-order with Dashboard as the standalone top-most item.
            int count = _sidebarControlsInOrder.Length;
            for (int i = 0; i < count; i++)
            {
                _pnlMenu.Controls.SetChildIndex(_sidebarControlsInOrder[i], count - 1 - i);
            }

            _pnlMenu.ResumeLayout(true);
        }

        // ============================================================
        // RBAC Permissions Logic
        // ============================================================
        public void ApplyRolePermissions()
        {
            // FAILSAFE: If SessionManager.CurrentUser is null, immediately hide all modules and redirect to LoginView
            if (SessionManager.CurrentUser == null)
            {
                HideAllModules();
                RedirectToLogin();
                return;
            }

            var role = SessionManager.CurrentUser.Role;

            if (SessionManager.IsMaintenanceMode && SessionManager.MaintenanceTargetCompany != null)
            {
                var company = SessionManager.MaintenanceTargetCompany;
                _pnlMaintenanceBanner.Visible = true;
                _lblMaintenanceInfo.Text = $"🛠️ SUPER ADMIN MAINTENANCE MODE: Diagnosing Tenant '{company.CompanyName}' ({company.CompanyCode})";

                // In maintenance mode, unlock all operational modules for the Super Admin
                btnDashboard.Visible = true;
                pnlDashboardDivider.Visible = true;

                btnLeads.Visible = true;
                btnClientContract.Visible = true;
                btnSchedulingDispatch.Visible = true;
                btnBranches.Visible = true;
                lblHeaderOperations.Visible = true;

                btnSalesRetention.Visible = true;
                btnFinancial.Visible = true;
                btnReportsAudit.Visible = true;
                lblHeaderPerformance.Visible = true;

                btnUserManagement.Visible = true;
                btnManageSubscription.Visible = true;
                btnTermsManagement.Visible = true;
                lblHeaderAdministration.Visible = true;
            }
            else
            {
                _pnlMaintenanceBanner.Visible = false;

                // DASHBOARD: Standalone top-level button visible to all authenticated roles.
                // Super Admin views SaaS Platform Executive metrics; Tenant roles view Cleaning Operations BI.
                btnDashboard.Visible = true;
                pnlDashboardDivider.Visible = true;

                // OPERATIONS: Leads, Customers, Schedule (Tenant roles). Operating Branches (Super Admin platform control).
                btnLeads.Visible = (role != Roles.SuperAdmin);
                btnClientContract.Visible = (role != Roles.SuperAdmin);
                btnSchedulingDispatch.Visible = (role != Roles.SuperAdmin);
                btnBranches.Visible = (role == Roles.SuperAdmin);
                lblHeaderOperations.Visible = (btnLeads.Visible || btnClientContract.Visible || btnSchedulingDispatch.Visible || btnBranches.Visible);

                // PERFORMANCE: Retention visible to Tenant roles. Financials visible ONLY to Tenant Admin. Reports/Audit visible to SuperAdmin and Management.
                btnSalesRetention.Visible = (role != Roles.SuperAdmin);
                btnFinancial.Visible = (role == Roles.Admin);
                btnReportsAudit.Visible = (role != Roles.SalesStaff);
                lblHeaderPerformance.Visible = (btnSalesRetention.Visible || btnFinancial.Visible || btnReportsAudit.Visible);

                // ADMINISTRATION: Platform Master Tier.
                // Super Admin handles selling the system to tenant admins, subscription plans, user provisioning, and maintenance.
                btnUserManagement.Visible = (role == Roles.SuperAdmin);
                btnManageSubscription.Visible = (role == Roles.SuperAdmin);
                btnTermsManagement.Visible = true;
                lblHeaderAdministration.Visible = (btnUserManagement.Visible || btnManageSubscription.Visible || btnTermsManagement.Visible);
            }

            BuildQuickActionMenu();

            // Re-enforce exact sidebar top-to-bottom order so Z-order is never disrupted
            EnforceSidebarOrder();
        }

        private void HideAllModules()
        {
            _pnlMaintenanceBanner.Visible = false;

            btnDashboard.Visible = false;
            pnlDashboardDivider.Visible = false;
            btnLeads.Visible = false;
            btnClientContract.Visible = false;
            btnSchedulingDispatch.Visible = false;
            btnBranches.Visible = false;
            btnSalesRetention.Visible = false;
            btnFinancial.Visible = false;
            btnReportsAudit.Visible = false;
            btnUserManagement.Visible = false;
            btnManageSubscription.Visible = false;
            btnTermsManagement.Visible = false;

            lblHeaderOperations.Visible = false;
            lblHeaderPerformance.Visible = false;
            lblHeaderAdministration.Visible = false;

            _pnlContent.Controls.Clear();
        }

        private void RedirectToLogin()
        {
            BeginInvoke(new Action(() =>
            {
                using var loginView = new Views.LoginView();
                if (loginView.ShowDialog() == DialogResult.OK && SessionManager.CurrentUser != null)
                {
                    ApplyRolePermissions();
                    LoadDefaultView();
                }
                else
                {
                    Close();
                }
            }));
        }

        // ============================================================
        // Default View Routing: Role-Tailored Landing Views
        // ============================================================
        public void LoadDefaultView()
        {
            Button? initialButton = null;
            var role = SessionManager.CurrentUser?.Role;

            // Role-Tailored Landing Views:
            // - Sales Staff: Sales & Retention (Client calls & outreach)
            // - Manager: Scheduling & Dispatch (Operational command board)
            // - Admin: BI Dashboard (Executive performance & overview)
            // - Super Admin: User Management (User directory & security)
            if (role == Roles.SalesStaff && btnSalesRetention.Visible)
            {
                initialButton = btnSalesRetention;
            }
            else if (role == Roles.Manager && btnSchedulingDispatch.Visible)
            {
                initialButton = btnSchedulingDispatch;
            }
            else if (role == Roles.Admin && btnDashboard.Visible)
            {
                initialButton = btnDashboard;
            }
            else if (role == Roles.SuperAdmin && btnDashboard.Visible)
            {
                initialButton = btnDashboard;
            }
            else if (role == Roles.SuperAdmin && btnManageSubscription.Visible)
            {
                initialButton = btnManageSubscription;
            }
            else if (role == Roles.SuperAdmin && btnUserManagement.Visible)
            {
                initialButton = btnUserManagement;
            }
            else
            {
                // Fallback priority
                if (btnDashboard.Visible) initialButton = btnDashboard;
                else if (btnManageSubscription.Visible) initialButton = btnManageSubscription;
                else if (btnSchedulingDispatch.Visible) initialButton = btnSchedulingDispatch;
                else if (btnClientContract.Visible) initialButton = btnClientContract;
                else if (btnSalesRetention.Visible) initialButton = btnSalesRetention;
                else if (btnFinancial.Visible) initialButton = btnFinancial;
                else if (btnUserManagement.Visible) initialButton = btnUserManagement;
            }

            if (initialButton != null)
            {
                initialButton.PerformClick();
            }
            else
            {
                SelectInitialView();
            }
        }

        private void SelectInitialView()
        {
            Button? initialButton = null;

            if (SessionManager.CurrentUser?.Role == Roles.SuperAdmin)
            {
                if (btnDashboard.Visible) initialButton = btnDashboard;
                else if (btnManageSubscription.Visible) initialButton = btnManageSubscription;
                else if (btnUserManagement.Visible) initialButton = btnUserManagement;
                else if (btnReportsAudit.Visible) initialButton = btnReportsAudit;
            }
            else
            {
                if (btnDashboard.Visible)
                    initialButton = btnDashboard;
                else if (btnClientContract.Visible)
                    initialButton = btnClientContract;
                else if (btnSchedulingDispatch.Visible)
                    initialButton = btnSchedulingDispatch;
                else if (btnSalesRetention.Visible)
                    initialButton = btnSalesRetention;
                else if (btnFinancial.Visible)
                    initialButton = btnFinancial;
                else if (btnUserManagement.Visible)
                    initialButton = btnUserManagement;
            }

            if (initialButton != null)
            {
                SetActiveButton(initialButton);
                var key = initialButton.Tag?.ToString() ?? "dashboard";
                var headerText = _buttonMeta.TryGetValue(initialButton, out var meta) ? meta.Label : GetCleanLabel(initialButton.Text);
                ShowView(key, headerText);
            }
        }

        // ============================================================
        // Collapsible Sidebar & Keyboard Shortcuts
        // ============================================================
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.K)
            {
                e.SuppressKeyPress = true;
                OpenCommandPalette();
            }
            else if (e.Control && e.KeyCode == Keys.B)
            {
                e.SuppressKeyPress = true;
                ToggleSidebar();
            }
        }

        public void ToggleSidebar()
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            _pnlSidebar.Width = _isSidebarCollapsed ? 70 : 260;
            _lblBrand.Text = _isSidebarCollapsed ? "🧹" : "🧹  CLEANING CRM";
            _lblBrand.Font = _isSidebarCollapsed ? new Font("Segoe UI", 16F, FontStyle.Bold) : new Font("Segoe UI", 12F, FontStyle.Bold);

            pnlDashboardDivider.Visible = !_isSidebarCollapsed && btnDashboard.Visible;
            lblHeaderOperations.Visible = !_isSidebarCollapsed && (btnLeads.Visible || btnClientContract.Visible || btnSchedulingDispatch.Visible || btnBranches.Visible);
            lblHeaderPerformance.Visible = !_isSidebarCollapsed && (btnSalesRetention.Visible || btnFinancial.Visible || btnReportsAudit.Visible);
            lblHeaderAdministration.Visible = !_isSidebarCollapsed && (btnUserManagement.Visible || btnManageSubscription.Visible || btnTermsManagement.Visible);

            _lblSidebarUserInfo.Visible = !_isSidebarCollapsed;
            btnLogout.Text = _isSidebarCollapsed ? "🚪" : "🚪  Sign Out";
            _toolTip.SetToolTip(btnLogout, "Sign Out");

            foreach (var btn in _sidebarButtons)
            {
                if (_buttonMeta.TryGetValue(btn, out var meta))
                {
                    if (_isSidebarCollapsed)
                    {
                        btn.Text = meta.Icon;
                        btn.TextAlign = ContentAlignment.MiddleCenter;
                        btn.Padding = Padding.Empty;
                    }
                    else
                    {
                        btn.Text = $"   {meta.Icon}   {meta.Label}";
                        btn.TextAlign = ContentAlignment.MiddleLeft;
                        btn.Padding = new Padding(12, 0, 0, 0);
                    }
                }
            }
        }

        // ============================================================
        // Quick Action Context Menu
        // ============================================================
        private void BuildQuickActionMenu()
        {
            _mnuQuickAction = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 9.5F),
                ShowImageMargin = false,
                BackColor = Color.White
            };

            var role = SessionManager.CurrentUser?.Role ?? Roles.SalesStaff;

            if (role == Roles.SalesStaff)
            {
                AddQuickActionItem("⚡  + New Booking Request", () => OpenNewBookingRequestDialog());
                AddQuickActionItem("📋  + New Lead / Inquiry", () => NavigateToKey("leads"));
                AddQuickActionItem("👤  Search Customer Directory", () => NavigateToKey("clients"));
                AddQuickActionItem("🔄  Review Retention Pipeline", () => NavigateToKey("sales"));
            }
            else if (role == Roles.Manager)
            {
                AddQuickActionItem("⚡  Dispatch Operations Center", () => NavigateToKey("scheduling"));
                AddQuickActionItem("⚡  + New Booking Request (Assist Staff)", () => OpenNewBookingRequestDialog());
                AddQuickActionItem("👤  Search Customer Directory", () => NavigateToKey("clients"));
            }
            else if (role == Roles.Admin)
            {
                AddQuickActionItem("💰  Invoices & Payment Settlement", () => NavigateToKey("financial"));
                AddQuickActionItem("📊  View Executive Dashboard", () => NavigateToKey("dashboard"));
                AddQuickActionItem("⚡  + New Booking Request (Override)", () => OpenNewBookingRequestDialog());
                AddQuickActionItem("📈  View Reports & Audit", () => NavigateToKey("reports"));
            }
            else if (role == Roles.SuperAdmin)
            {
                AddQuickActionItem("🔁  Tenant Subscriptions & Quotas", () => NavigateToKey("subscription"));
                AddQuickActionItem("👤  + Provision Tenant Admin Account", () => OpenNewUserDialog());
                AddQuickActionItem("👥  Open User & Tenant Directory", () => NavigateToKey("users"));
                AddQuickActionItem("📜  View System Compliance & Audit Trail", () => NavigateToKey("reports"));
            }
        }

        private void AddQuickActionItem(string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (s, e) => action();
            _mnuQuickAction.Items.Add(item);
        }

        // ============================================================
        // Command Palette & Global Launchers
        // ============================================================
        private void OpenCommandPalette()
        {
            using var palette = new Views.CommandPaletteDialog();
            palette.OnItemSelected = item =>
            {
                HandlePaletteAction(item);
            };
            palette.ShowDialog(this);
        }

        private void HandlePaletteAction(Views.PaletteItem item)
        {
            switch (item.Type)
            {
                case Views.PaletteItemType.Navigate:
                    NavigateToKey(item.Tag);
                    break;
                case Views.PaletteItemType.Action:
                    if (item.Tag == "action_new_booking") OpenNewBookingRequestDialog();
                    else if (item.Tag == "action_new_workorder") OpenNewWorkOrderDialog();
                    else if (item.Tag == "action_new_user") OpenNewUserDialog();
                    break;
                case Views.PaletteItemType.Customer:
                    NavigateToKey("clients");
                    break;
                case Views.PaletteItemType.WorkOrder:
                    if (item.DataPayload is WorkOrderDto wo)
                    {
                        using var dlg = new Views.DispatchWorkOrderDialog(wo);
                        dlg.ShowDialog(this);
                    }
                    else
                    {
                        NavigateToKey("scheduling");
                    }
                    break;
                case Views.PaletteItemType.Invoice:
                    NavigateToKey("financial");
                    break;
            }
        }

        public void NavigateToKey(string key)
        {
            var btn = _sidebarButtons.FirstOrDefault(b => b.Tag?.ToString() == key);
            if (btn != null && btn.Visible)
            {
                SetActiveButton(btn);
                string label = _buttonMeta.TryGetValue(btn, out var meta) ? meta.Label : GetCleanLabel(btn.Text);
                ShowView(key, label);
            }
        }

        private void OpenNewBookingRequestDialog()
        {
            using var dlg = new Views.NewBookingRequestDialog();
            dlg.BookingRequestSaved += async order =>
            {
                if (order != null)
                {
                    await RefreshNotificationBadgeAsync();
                    MessageBox.Show($"Booking request WO-{order.ServiceRequestId:D4} successfully created!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            dlg.ShowDialog(this);
        }

        private void OpenNewWorkOrderDialog()
        {
            using var dlg = new Views.NewWorkOrderDialog();
            dlg.WorkOrderSaved += async () =>
            {
                await RefreshNotificationBadgeAsync();
            };
            dlg.ShowDialog(this);
        }

        private void OpenNewUserDialog()
        {
            using var dlg = new Views.NewUserDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                MessageBox.Show("New staff account created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ============================================================
        // Operations Notification & Approval Handlers
        // ============================================================
        private async void OpenNotificationCenter()
        {
            using var dlg = new Views.NotificationCenterDialog(_currentNotifications);
            dlg.ShowUnderButton(_btnNotifications, this);
            if (dlg.SelectedActionItem != null)
            {
                await HandleNotificationActionAsync(dlg.SelectedActionItem);
            }
        }

        private async Task HandleNotificationActionAsync(Core.NotificationItem item)
        {
            if (item.TargetType == "dispatch" && item.Payload is WorkOrderDto wo)
            {
                using var dispatchDlg = new Views.DispatchWorkOrderDialog(wo);
                dispatchDlg.WorkOrderUpdated += async updated =>
                {
                    await RefreshNotificationBadgeAsync();
                };
                dispatchDlg.ShowDialog(this);
                await RefreshNotificationBadgeAsync();
            }
            else if (item.TargetType == "financial")
            {
                NavigateToKey("financial");
            }
            else if (item.TargetType == "scheduling")
            {
                NavigateToKey("scheduling");
            }
            else if (item.TargetType == "subscription")
            {
                NavigateToKey("subscription");
            }
        }

        public async Task RefreshNotificationBadgeAsync()
        {
            try
            {
                var items = await _notificationService.GetNotificationsForCurrentRoleAsync();
                _currentNotifications.Clear();
                _currentNotifications.AddRange(items);

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UpdateNotificationBadgeUI));
                }
                else
                {
                    UpdateNotificationBadgeUI();
                }
            }
            catch
            {
                // Non-critical background telemetry
            }
        }

        private void UpdateNotificationBadgeUI()
        {
            if (_btnNotifications == null) return;

            int count = _currentNotifications.Count;
            if (count > 0)
            {
                _btnNotifications.Text = $"🔔 {count}";
                _btnNotifications.Width = 62;
                _btnNotifications.BackColor = Color.FromArgb(254, 242, 242);
                _btnNotifications.ForeColor = Color.FromArgb(220, 38, 38);
                _toolTip.SetToolTip(_btnNotifications, $"{count} item{(count == 1 ? "" : "s")} awaiting your approval / review");
            }
            else
            {
                _btnNotifications.Text = "🔔";
                _btnNotifications.Width = 46;
                _btnNotifications.BackColor = Color.FromArgb(241, 245, 249);
                _btnNotifications.ForeColor = Color.FromArgb(71, 85, 105);
                _toolTip.SetToolTip(_btnNotifications, "No pending notifications");
            }
        }

        // ============================================================
        // Sidebar helpers
        // ============================================================
        private static Label CreateGroupLabel(string text)
        {
            return new Label
            {
                Text = "  " + text,
                ForeColor = Color.FromArgb(100, 116, 139), // #64748B
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(10, 0, 0, 6),
                UseMnemonic = false
            };
        }

        private Button CreateSidebarButton(string key, string icon, string label)
        {
            var btn = new Button
            {
                Text = $"   {icon}   {label}",
                Tag = key,
                Dock = DockStyle.Top,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            btn.Click += SidebarButton_Click;

            // Draw left gradient stripe on active navigation button
            btn.Paint += (s, e) =>
            {
                if (btn.BackColor != Color.FromArgb(30, 41, 59))
                {
                    var stripeRect = new Rectangle(0, 0, 4, btn.Height);
                    using var brush = new LinearGradientBrush(
                        stripeRect,
                        Color.FromArgb(37, 99, 235), // Blue-600
                        Color.FromArgb(79, 70, 229), // Indigo-600
                        LinearGradientMode.Vertical);
                    e.Graphics.FillRectangle(brush, stripeRect);
                }
            };

            _buttonMeta[btn] = (icon, label, key);
            _toolTip.SetToolTip(btn, label);

            return btn;
        }

        private void SidebarButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            SetActiveButton(btn);

            var key = btn.Tag?.ToString() ?? "";
            var headerText = _buttonMeta.TryGetValue(btn, out var meta) ? meta.Label : GetCleanLabel(btn.Text);

            ShowView(key, headerText);
        }

        private void SetActiveButton(Button activeBtn)
        {
            foreach (var b in _sidebarButtons)
            {
                b.BackColor = Color.FromArgb(30, 41, 59);
                b.ForeColor = Color.FromArgb(203, 213, 225);
                b.Invalidate();
            }

            activeBtn.BackColor = Color.FromArgb(30, 27, 75); // Deep Indigo active tone
            activeBtn.ForeColor = Color.White;
            activeBtn.Invalidate();
        }

        private static string GetCleanLabel(string buttonText)
        {
            var trimmed = buttonText.Trim();
            var idx = 0;
            while (idx < trimmed.Length && !char.IsLetter(trimmed[idx]))
            {
                idx++;
            }
            return idx < trimmed.Length ? trimmed.Substring(idx) : trimmed;
        }

        // ============================================================
        // Router
        // ============================================================
        private void ShowView(string key, string headerText)
        {
            if ((key == "subscription" || key == "users" || key == "branches") && SessionManager.CurrentUser?.Role != Roles.SuperAdmin)
            {
                MessageBox.Show("Access Denied: Only Super Administrators have permission to access this module.", "Restricted Access", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!SessionManager.IsMaintenanceMode && SessionManager.CurrentUser?.Role == Roles.SuperAdmin &&
                (key == "clients" || key == "scheduling" || key == "workorders" || key == "leads" || key == "retention" || key == "sales" || key == "financial" || key == "dashboard"))
            {
                MessageBox.Show("Platform Notice: Super Administrators manage tenant subscriptions and system administration only. Operational modules (customers, schedules, work orders) are handled by tenant administrators.\n\nTo inspect or troubleshoot this company's operations, use 'Maintenance Access' from User Management.", "Access Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _lblPageTitle.Text = SessionManager.IsMaintenanceMode && SessionManager.MaintenanceTargetCompany != null
                ? $"Cleaning CRM  ›  [Maintenance: {SessionManager.MaintenanceTargetCompany.CompanyName}]  ›  {headerText}"
                : $"Cleaning CRM  ›  {headerText}";

            _pnlContent.Controls.Clear();

            UserControl? view = key switch
            {
                "dashboard"   => new Views.DashboardView(),
                "leads"       => new Views.LeadsView(),        // LAYER 1: lead management & conversion
                "clients"     => new Views.CustomersView(),    // LAYER 2: master customer profiles
                "scheduling"  => new Views.ScheduleView(),     // LAYER 3: dispatch command center & schedule board
                "workorders"  => new Views.WorkOrdersView(),   // LAYER 3: work orders master ledger
                "branches"    => new Views.BranchManagementView(), // TENANT C: regional branch operations
                "sales"       => new Views.RetentionView(),    // LAYER 4: retention & customer health
                "retention"   => new Views.RetentionView(),    // LAYER 4: retention alias
                "financial"   => new Views.FinancialManagementView(), // LAYER 5: financial invoicing & payments
                "users"       => new Views.UserManagementView(),      // LAYER 0: super admin user directory
                "subscription"=> new Views.SubscriptionManagementView(), // MASTER TIER: tenant subscription control
                "reports"     => new Views.ReportsAuditView(),       // LAYER 6: BI reporting & audit trail
                _             => new Views.PlaceholderView(headerText)
            };

            if (view != null)
            {
                // VIEW-LEVEL ARCHITECTURE: Apply permissions before rendering
                if (view is BaseView baseView && SessionManager.CurrentUser != null)
                {
                    baseView.ApplyViewPermissions(SessionManager.CurrentUser.Role);
                }

                view.Dock = DockStyle.Fill;
                _pnlContent.Controls.Add(view);
            }
        }

        // ============================================================
        // SaaS Multi-Tenant Support & Maintenance Mode (Super Admin)
        // ============================================================
        public void ActivateTenantMaintenanceMode(Company company)
        {
            SessionManager.EnterMaintenanceMode(company);
            ApplyRolePermissions();

            var btn = _sidebarButtons.FirstOrDefault(b => b.Tag?.ToString() == "dashboard");
            if (btn != null) SetActiveButton(btn);

            ShowView("dashboard", $"Tenant Dashboard ({company.CompanyName})");

            MessageBox.Show(
                $"Maintenance Mode Activated for '{company.CompanyName}' ({company.CompanyCode}).\n\nYou now have temporary diagnostic access to inspect this tenant's dashboard, schedules, and work orders.\n\nClick 'Exit Maintenance Mode' at the top banner when you are done.",
                "Maintenance Mode Active",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public void DeactivateTenantMaintenanceMode()
        {
            var compName = SessionManager.MaintenanceTargetCompany?.CompanyName ?? "Tenant";
            SessionManager.ExitMaintenanceMode();
            ApplyRolePermissions();

            var btn = _sidebarButtons.FirstOrDefault(b => b.Tag?.ToString() == "users");
            if (btn != null) SetActiveButton(btn);

            ShowView("users", "User Management & Client Companies");

            MessageBox.Show(
                $"Exited Maintenance Mode for '{compName}'.\n\nReturned to SaaS Vendor Platform view.",
                "Maintenance Mode Closed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}