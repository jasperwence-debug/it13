using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms
{
    public class MainForm : Form
    {
        // ============================================================
        // Layout
        // ============================================================
        private Panel _pnlSidebar = null!;
        private Panel _pnlContent = null!;
        private Panel _pnlHeader = null!;
        private Label _lblPageTitle = null!;
        private Label _lblUser = null!;

        private readonly List<Button> _sidebarButtons = new();

        public MainForm()
        {
            InitializeUI();
            ShowView("dashboard", "Dashboard");
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
            BackColor = Color.FromArgb(245, 246, 250);
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

            // Menu container
            var pnlMenu = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 10)
            };
            _pnlSidebar.Controls.Add(pnlMenu);
            pnlMenu.BringToFront();

            // Menu items — DATA COLLECTION ITEMS REMOVED
            var menuItems = new List<(string key, string icon, string label, bool isGroup)>
            {
                ("", "", "MAIN MODULES", true),
                ("dashboard",    "📊", "Dashboard",             false),
                ("users",        "👥", "User Management",       false),
                ("sales",        "💼", "Sales & Retention",     false),
                ("clients",      "📋", "Client & Contract",     false),
                ("scheduling",   "📅", "Scheduling & Dispatch", false),
                ("workorders",   "🔧", "Work Order Management", false),
                ("financial",    "💰", "Financial Management",  false),
                ("reports",      "📈", "Reports & Audit",       false),
                ("subscription", "🔁", "Manage Subscription",   false),
                ("terms",        "📜", "Terms & Management",    false),
            };

            // Reverse for Dock=Top stacking
            for (int i = menuItems.Count - 1; i >= 0; i--)
            {
                var item = menuItems[i];
                if (item.isGroup)
                {
                    pnlMenu.Controls.Add(CreateGroupLabel(item.label));
                }
                else
                {
                    var btn = CreateSidebarButton(item.key, item.icon, item.label);
                    pnlMenu.Controls.Add(btn);
                    _sidebarButtons.Add(btn);
                }
            }

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
                Dock = DockStyle.Left,
                Width = 400,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlHeader.Controls.Add(_lblPageTitle);

            _lblUser = new Label
            {
                Text = "👤  Juan Dela Cruz  ▼",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Right,
                Width = 200,
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
                BackColor = Color.FromArgb(245, 246, 250)
            };
            Controls.Add(_pnlContent);
            _pnlContent.BringToFront();
        }

        // ============================================================
        // Sidebar helpers
        // ============================================================
        private static Label CreateGroupLabel(string text)
        {
            return new Label
            {
                Text = "  " + text,
                ForeColor = Color.FromArgb(148, 163, 184),
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

            var key = btn.Tag?.ToString() ?? "";
            var label = btn.Text.Trim();
            var headerText = label.Substring(label.IndexOf(' ')).Trim();

            foreach (var b in _sidebarButtons)
                b.BackColor = Color.FromArgb(30, 41, 59);

            btn.BackColor = Color.FromArgb(59, 130, 246);

            ShowView(key, headerText);
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
                view.Dock = DockStyle.Fill;
                _pnlContent.Controls.Add(view);
            }
        }
    }
}