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
    /// MODULE 1 — User Management & System Access Control.
    ///
    /// Primary Actor: Super Admin.
    /// Responsibilities:
    ///   - System user directory (SuperAdmin, Admin, Manager, SalesStaff).
    ///   - Role allocation and credential management.
    ///   - Account activation and suspension toggles.
    ///   - User summary KPI ribbon.
    /// </summary>
    public class UserManagementView : BaseView
    {
        private List<User> _users = new();
        private List<User> _filteredUsers = new();
        private readonly HashSet<int> _suspendedUserIds = new();

        // Header Controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnNewUser = null!;

        // KPI Summary Cards
        private Label _lblTotalUsers = null!;
        private Label _lblAdmins = null!;
        private Label _lblManagers = null!;
        private Label _lblSales = null!;

        // Filters
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbRoleFilter = null!;

        // Grid
        private DataGridView _grid = null!;
        private Panel _pnlGridContainer = null!;
        private Panel _pnlEmptyState = null!;

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
                LoadUsers();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                LoadUsers();
            }
        }

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

            // ── 1. Top Header Bar (60px) ─────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            card.Controls.Add(pnlHeader);

            var pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 440,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlHeaderLeft);

            _lblTitle = new Label
            {
                Text = "User Management & Role Permissions",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlHeaderLeft.Controls.Add(_lblTitle);

            _lblCount = new Label
            {
                Text = "Loading system accounts...",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlHeaderLeft.Controls.Add(_lblCount);

            // Right header buttons
            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 14, 0, 0)
            };
            pnlHeader.Controls.Add(pnlHeaderRight);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 36,
                Width = 95,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += (s, e) => LoadUsers();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            _btnNewUser = new Button
            {
                Text = "+  Add User Account",
                Height = 36,
                Width = 175,
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

            var (k1, _, v1, _) = CreateKpiCard("TOTAL SYSTEM USERS", "0", "Provisioned staff profiles", Color.FromArgb(30, 41, 59));
            _lblTotalUsers = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, _, v2, _) = CreateKpiCard("ADMINISTRATORS", "0", "Super Admin & Business Admins", Color.FromArgb(202, 138, 4));
            _lblAdmins = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, _, v3, _) = CreateKpiCard("OPERATIONS MANAGERS", "0", "Dispatch & Crew Supervisors", Color.FromArgb(22, 163, 74));
            _lblManagers = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, _, v4, _) = CreateKpiCard("SALES REPRESENTATIVES", "0", "Lead intake & client outreach", Color.FromArgb(37, 99, 235));
            _lblSales = v4;
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
                PlaceholderText = "🔍  Search by username or role..."
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

            var lblRole = new Label
            {
                Text = "Role:",
                Dock = DockStyle.Left,
                Width = 45,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSearch.Controls.Add(lblRole);

            _cmbRoleFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 200,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
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
            lblRole.BringToFront();
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

            var lblEmptyTitle = new Label
            {
                Text = "No User Accounts Found",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(lblEmptyTitle);

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
                RowTemplate = { Height = 40 },
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
            _grid.CellContentClick += OnGridCellContentClick;

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(pnlDivider, 1);
            card.Controls.SetChildIndex(pnlSearch, 2);
            card.Controls.SetChildIndex(pnlKpis, 3);
            card.Controls.SetChildIndex(pnlHeader, 4);

            ResumeLayout(false);
        }

        private void LoadUsers()
        {
            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);
                _users = db.Users.OrderBy(u => u.Id).ToList();

                UpdateKpis();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Error loading users: {ex.Message}", false);
            }
        }

        private void UpdateKpis()
        {
            _lblTotalUsers.Text = _users.Count.ToString();
            _lblAdmins.Text = _users.Count(u => u.Role == Roles.SuperAdmin || u.Role == Roles.Admin).ToString();
            _lblManagers.Text = _users.Count(u => u.Role == Roles.Manager).ToString();
            _lblSales.Text = _users.Count(u => u.Role == Roles.SalesStaff).ToString();
        }

        private void ApplyFilter()
        {
            string query = _txtSearch.Text.Trim().ToLower();
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

            RebuildGrid();
        }

        private void RebuildGrid()
        {
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();

                if (_filteredUsers.Count == 0)
                {
                    _pnlEmptyState.Visible = true;
                    _grid.Visible = false;
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

                    // Color code role & status
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
            finally
            {
                _grid.ResumeLayout();
            }
        }

        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (_grid.Columns[e.ColumnIndex].Name == "colAction")
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
                LoadUsers();
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
            // Super Admin: FULL
            // Others: View only or Restricted
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
