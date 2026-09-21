using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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

        // 4 Non-clickable Category Headers (Font: 8pt, Bold, Color: #64748B)
        private Label lblHeaderOverview = null!;
        private Label lblHeaderOperations = null!;
        private Label lblHeaderPerformance = null!;
        private Label lblHeaderAdministration = null!;

        // Sidebar Buttons
        private Button btnDashboard = null!;
        private Button btnClientContract = null!;
        private Button btnSchedulingDispatch = null!;
        private Button btnWorkOrder = null!;
        private Button btnSalesRetention = null!;
        private Button btnFinancial = null!;
        private Button btnReportsAudit = null!;
        private Button btnUserManagement = null!;
        private Button btnManageSubscription = null!;
        private Button btnTermsManagement = null!;

        private readonly List<Button> _sidebarButtons = new();
        private Control[] _sidebarControlsInOrder = null!;

        public MainForm()
        {
            InitializeUI();
            ApplyRolePermissions();
            EnforceSidebarOrder();

            // Default View Routing and Sidebar Z-Order on Form Load
            Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            EnforceSidebarOrder();
            LoadDefaultView();
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
            _pnlSidebar.Controls.Add(pnlBrand);

            var lblBrand = new Label
            {
                Text = "🧹  CLEANING CRM",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlBrand.Controls.Add(lblBrand);

            // Sidebar User & Logout Footer
            var pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 10, 12, 10)
            };
            _pnlSidebar.Controls.Add(pnlSidebarFooter);

            var lblSidebarUserInfo = new Label
            {
                Text = $"{SessionManager.CurrentUser?.Username ?? "User"}  •  {SessionManager.CurrentUser?.Role ?? "Guest"}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSidebarFooter.Controls.Add(lblSidebarUserInfo);

            var btnLogout = new Button
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

            // Create 4 Category Headers (Font: 8pt Bold, Color: #64748B)
            lblHeaderOverview = CreateGroupLabel("OVERVIEW");
            lblHeaderOperations = CreateGroupLabel("OPERATIONS");
            lblHeaderPerformance = CreateGroupLabel("PERFORMANCE");
            lblHeaderAdministration = CreateGroupLabel("ADMINISTRATION");

            // Create Sidebar Buttons with display names
            btnDashboard = CreateSidebarButton("dashboard", "📊", "Dashboard");
            btnClientContract = CreateSidebarButton("clients", "📋", "Customers");
            btnSchedulingDispatch = CreateSidebarButton("scheduling", "📅", "Schedule");
            btnWorkOrder = CreateSidebarButton("workorders", "🔧", "Work Orders");
            btnSalesRetention = CreateSidebarButton("sales", "💼", "Retention");
            btnFinancial = CreateSidebarButton("financial", "💰", "Financials");
            btnReportsAudit = CreateSidebarButton("reports", "📈", "Reports");
            btnUserManagement = CreateSidebarButton("users", "👥", "Users");
            btnManageSubscription = CreateSidebarButton("subscription", "🔁", "Subscriptions");
            btnTermsManagement = CreateSidebarButton("terms", "📜", "Settings");

            // Add to _sidebarButtons collection for active highlight handling
            _sidebarButtons.AddRange(new[]
            {
                btnDashboard,
                btnClientContract,
                btnSchedulingDispatch,
                btnWorkOrder,
                btnSalesRetention,
                btnFinancial,
                btnReportsAudit,
                btnUserManagement,
                btnManageSubscription,
                btnTermsManagement
            });

            // Master controls ordering: OVERVIEW (Index 0, 1) at the absolute top
            _sidebarControlsInOrder = new Control[]
            {
                lblHeaderOverview,
                btnDashboard,

                lblHeaderOperations,
                btnClientContract,
                btnSchedulingDispatch,
                btnWorkOrder,

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
                Padding = new Padding(20, 0, 20, 0)
            };
            Controls.Add(_pnlHeader);
            _pnlHeader.BringToFront();

            _lblPageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Dock = DockStyle.Left,
                Width = 400,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlHeader.Controls.Add(_lblPageTitle);

            var userRole = SessionManager.CurrentUser?.Role ?? "Guest";
            var userName = SessionManager.CurrentUser?.Username ?? "User";

            _lblUser = new Label
            {
                Text = $"👤  {userName} ({userRole})",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.White,
                Dock = DockStyle.Right,
                Width = 300,
                TextAlign = ContentAlignment.MiddleRight
            };
            _pnlHeader.Controls.Add(_lblUser);

            var pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            _pnlHeader.Controls.Add(pnlHeaderBorder);

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
            _pnlContent.BringToFront();
        }

        // ============================================================
        // Sidebar Z-Order Hierarchy Enforcement
        // Strictly sets child indices so lblHeaderOverview and btnDashboard render at the top (Top=0, Top=34)
        // ============================================================
        private void EnforceSidebarOrder()
        {
            if (_pnlMenu == null || _sidebarControlsInOrder == null) return;

            _pnlMenu.SuspendLayout();

            // In WinForms Dock.Top layout, the control with the highest child index is docked to Top=0 first.
            // Assigning SetChildIndex in reverse ensures _sidebarControlsInOrder[0] (OVERVIEW) gets index Count-1 (Top=0),
            // _sidebarControlsInOrder[1] (Dashboard) gets index Count-2 (Top=34), guaranteeing perfect top-to-bottom Z-order.
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

            // OVERVIEW: Dashboard visible to ALL.
            btnDashboard.Visible = true;
            lblHeaderOverview.Visible = true;

            // OPERATIONS: Customers & Schedule visible to ALL. Work Orders hidden from Roles.SalesStaff.
            btnClientContract.Visible = true;
            btnSchedulingDispatch.Visible = true;
            btnWorkOrder.Visible = (role != Roles.SalesStaff);
            lblHeaderOperations.Visible = (btnClientContract.Visible || btnSchedulingDispatch.Visible || btnWorkOrder.Visible);

            // PERFORMANCE: Retention visible to ALL. Financials visible ONLY to Roles.Admin. Reports hidden from Roles.SalesStaff.
            btnSalesRetention.Visible = true;
            btnFinancial.Visible = (role == Roles.Admin);
            btnReportsAudit.Visible = (role != Roles.SalesStaff);
            lblHeaderPerformance.Visible = (btnSalesRetention.Visible || btnFinancial.Visible || btnReportsAudit.Visible);

            // ADMINISTRATION: Users visible ONLY to Roles.SuperAdmin. Subscriptions & Settings visible to ALL.
            btnUserManagement.Visible = (role == Roles.SuperAdmin);
            btnManageSubscription.Visible = true;
            btnTermsManagement.Visible = true;
            lblHeaderAdministration.Visible = (btnUserManagement.Visible || btnManageSubscription.Visible || btnTermsManagement.Visible);

            // Re-enforce exact sidebar top-to-bottom order so Z-order is never disrupted
            EnforceSidebarOrder();
        }

        private void HideAllModules()
        {
            btnDashboard.Visible = false;
            btnClientContract.Visible = false;
            btnSchedulingDispatch.Visible = false;
            btnWorkOrder.Visible = false;
            btnSalesRetention.Visible = false;
            btnFinancial.Visible = false;
            btnReportsAudit.Visible = false;
            btnUserManagement.Visible = false;
            btnManageSubscription.Visible = false;
            btnTermsManagement.Visible = false;

            lblHeaderOverview.Visible = false;
            lblHeaderOperations.Visible = false;
            lblHeaderPerformance.Visible = false;
            lblHeaderAdministration.Visible = false;

            _pnlContent.Controls.Clear();
        }

        private void RedirectToLogin()
        {
            BeginInvoke(new Action(() =>
            {
                using var loginForm = new Views.LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK && SessionManager.CurrentUser != null)
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
        // Default View Routing: Loads Dashboard View on startup
        // ============================================================
        public void LoadDefaultView()
        {
            if (btnDashboard != null && btnDashboard.Visible)
            {
                btnDashboard.PerformClick();
            }
            else if (btnClientContract != null && btnClientContract.Visible)
            {
                btnClientContract.PerformClick();
            }
            else
            {
                SelectInitialView();
            }
        }

        private void SelectInitialView()
        {
            Button? initialButton = null;

            if (btnDashboard.Visible)
                initialButton = btnDashboard;
            else if (btnClientContract.Visible)
                initialButton = btnClientContract;
            else if (btnSchedulingDispatch.Visible)
                initialButton = btnSchedulingDispatch;
            else if (btnWorkOrder.Visible)
                initialButton = btnWorkOrder;
            else if (btnSalesRetention.Visible)
                initialButton = btnSalesRetention;
            else if (btnFinancial.Visible)
                initialButton = btnFinancial;
            else if (btnUserManagement.Visible)
                initialButton = btnUserManagement;

            if (initialButton != null)
            {
                SetActiveButton(initialButton);
                var key = initialButton.Tag?.ToString() ?? "dashboard";
                var headerText = GetCleanLabel(initialButton.Text);
                ShowView(key, headerText);
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
                Padding = new Padding(10, 0, 0, 6)
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
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            btn.Click += SidebarButton_Click;
            return btn;
        }

        private void SidebarButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            SetActiveButton(btn);

            var key = btn.Tag?.ToString() ?? "";
            var headerText = GetCleanLabel(btn.Text);

            ShowView(key, headerText);
        }

        private void SetActiveButton(Button activeBtn)
        {
            foreach (var b in _sidebarButtons)
                b.BackColor = Color.FromArgb(30, 41, 59);

            activeBtn.BackColor = Color.FromArgb(59, 130, 246);
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
            _lblPageTitle.Text = headerText;

            _pnlContent.Controls.Clear();

            UserControl? view = key switch
            {
                "dashboard" => new Views.DashboardView(),
                "clients" => new Views.ClientContractView(),   // ← wizard + table inside
                _ => new Views.PlaceholderView(headerText)
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
    }
}