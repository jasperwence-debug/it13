using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// TENANT C (MEDIUM ENTERPRISE) — Multi-Branch Operations & Directory Center.
    ///
    /// Responsibilities according to Final Laboratory Exam:
    ///   - Regional Branching: Hub creation, multi-location workforce dispatch.
    ///   - Quota check: Enforces Tenant C tier requirement for multi-branching.
    ///   - Regional performance tracking and branch-level overview.
    /// </summary>
    public class BranchManagementView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<BranchDto> _allBranches = new();
        private List<BranchDto> _filteredBranches = new();

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnNewBranch = null!;

        // KPI Metric Labels
        private Label _lblTotalBranches = null!;
        private Label _lblCitiesCovered = null!;
        private Label _lblAssignedCrews = null!;
        private Label _lblActiveOrders = null!;

        // Filter Controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbCityFilter = null!;
        private ComboBox _cmbStatusFilter = null!;

        // Grid & Empty state
        private Panel _pnlGridContainer = null!;
        private DataGridView _grid = null!;
        private Panel _pnlEmptyState = null!;
        private Button _btnResetFilters = null!;

        private bool _hasLoaded;

        public BranchManagementView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadBranchesAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadBranchesAsync();
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
                Width = 520,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlHeaderLeft);

            _lblTitle = new Label
            {
                Text = "Regional Branch Management & Hubs",
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
                Text = "Loading branch operations records...",
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

            _btnNewBranch = new Button
            {
                Text = "⚡  + Register Branch",
                Height = 36,
                Width = 160,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewBranch);
            _btnNewBranch.Click += (s, e) => OpenCreateBranchDialog();
            pnlHeaderRight.Controls.Add(_btnNewBranch);

            _btnExportCsv = new Button
            {
                Text = "📥  Export CSV",
                Height = 36,
                Width = 130,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnExportCsv);
            _btnExportCsv.Click += (s, e) => ExportBranchesToCsv();
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 36,
                Width = 95,
                Margin = new Padding(0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadBranchesAsync();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            // ── 2. KPI Summary Cards (4 Cards) ───────────────────────
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

            var (k1, _, v1, _) = CreateKpiCard("OPERATING BRANCHES", "0 Hubs", "Active regional centers", Color.FromArgb(79, 70, 229));
            _lblTotalBranches = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, _, v2, _) = CreateKpiCard("REGIONAL COVERAGE", "0 Cities", "Geographic service footprint", Color.FromArgb(37, 99, 235));
            _lblCitiesCovered = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, _, v3, _) = CreateKpiCard("ENTERPRISE TIER CAP", "10 Hubs", "Tenant C entitlement limit", Color.FromArgb(22, 163, 74));
            _lblAssignedCrews = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, _, v4, _) = CreateKpiCard("ACTIVE WORK ORDERS", "0 Orders", "In-progress branch services", Color.FromArgb(234, 88, 12));
            _lblActiveOrders = v4;
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

            // Search Box
            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
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
            _btnClearSearch.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _txtSearch.Focus();
            };
            pnlSearchBox.Controls.Add(_btnClearSearch);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.BodyFont,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "🔍  Search by branch name, code, manager..."
            };
            _txtSearch.Location = new Point(6, 6);
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                ApplyFilter();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSpacer1 = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer1);

            // City Filter
            var lblCity = new Label { Text = "City:", Dock = DockStyle.Left, Width = 45, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleRight };
            pnlSearch.Controls.Add(lblCity);

            _cmbCityFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 180,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCityFilter.Items.Add("All Cities");
            _cmbCityFilter.SelectedIndex = 0;
            _cmbCityFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlSearch.Controls.Add(_cmbCityFilter);

            var pnlSpacer2 = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer2);

            // Status Filter
            var lblStatus = new Label { Text = "Status:", Dock = DockStyle.Left, Width = 55, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleRight };
            pnlSearch.Controls.Add(lblStatus);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 140,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[] { "All Statuses", "Active Only", "Inactive Only" });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlSearch.Controls.Add(_cmbStatusFilter);

            _cmbStatusFilter.BringToFront();
            lblStatus.BringToFront();
            pnlSpacer2.BringToFront();
            _cmbCityFilter.BringToFront();
            lblCity.BringToFront();
            pnlSpacer1.BringToFront();
            pnlSearchBox.SendToBack();

            var pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            card.Controls.Add(pnlDivider);

            // ── 4. Grid Container & Empty State ──────────────────────
            _pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
            card.Controls.Add(_pnlGridContainer);

            // Empty State
            _pnlEmptyState = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Visible = false };
            _pnlGridContainer.Controls.Add(_pnlEmptyState);

            var pnlEmptyCenter = new Panel { Size = new Size(420, 200), BackColor = Theme.Surface };
            _pnlEmptyState.Controls.Add(pnlEmptyCenter);
            _pnlEmptyState.Resize += (s, e) =>
            {
                pnlEmptyCenter.Location = new Point(
                    Math.Max(10, (_pnlEmptyState.Width - pnlEmptyCenter.Width) / 2),
                    Math.Max(10, (_pnlEmptyState.Height - pnlEmptyCenter.Height) / 2)
                );
            };

            var lblEmptyIcon = new Label { Text = "🏢", Font = new Font("Segoe UI", 32F), Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };
            pnlEmptyCenter.Controls.Add(lblEmptyIcon);

            var lblEmptyTitle = new Label { Text = "No Branches Found", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter };
            pnlEmptyCenter.Controls.Add(lblEmptyTitle);

            var lblEmptySub = new Label { Text = "No operating branches match the selected filters.", Font = new Font("Segoe UI", 9F), ForeColor = Theme.TextMuted, Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.TopCenter };
            pnlEmptyCenter.Controls.Add(lblEmptySub);

            _btnResetFilters = new Button
            {
                Text = "↺  Reset Filters",
                Size = new Size(140, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnResetFilters.FlatAppearance.BorderSize = 0;
            _btnResetFilters.Location = new Point((pnlEmptyCenter.Width - _btnResetFilters.Width) / 2, 135);
            _btnResetFilters.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _cmbCityFilter.SelectedIndex = 0;
                _cmbStatusFilter.SelectedIndex = 0;
            };
            pnlEmptyCenter.Controls.Add(_btnResetFilters);

            // ── 5. DataGridView ──────────────────────────────────────
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

            _grid.DataError += (s, e) => { e.ThrowException = false; };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Branch Code", Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Branch Name", Width = 230, MinimumWidth = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colCity", HeaderText = "City / Region", Width = 140, MinimumWidth = 110 },
                new DataGridViewTextBoxColumn { Name = "colAddress", HeaderText = "Physical Address", Width = 180, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colManager", HeaderText = "Operations Supervisor", Width = 160, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "Contact Phone", Width = 120, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colOrders", HeaderText = "Active Jobs", Width = 95, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", Width = 100, MinimumWidth = 80 },
                new DataGridViewButtonColumn { Name = "colAction", HeaderText = "Action", Width = 120, MinimumWidth = 100,
                    Text = "⚡ Manage", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat }
            });

            if (_grid.Columns["colCode"] != null) _grid.Columns["colCode"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colCity"] != null) _grid.Columns["colCity"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colPhone"] != null) _grid.Columns["colPhone"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colOrders"] != null) _grid.Columns["colOrders"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colStatus"] != null) _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellFormatting += OnGridCellFormatting;

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(pnlDivider, 1);
            card.Controls.SetChildIndex(pnlSearch, 2);
            card.Controls.SetChildIndex(pnlKpis, 3);
            card.Controls.SetChildIndex(pnlHeader, 4);

            ResumeLayout(false);
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadBranchesAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Loading regional branches from database...";

            try
            {
                // Ensure LocalDB table and seed rows exist
                try
                {
                    using var seedDb = new App.Infrastructure.AppDbContext();
                    App.Infrastructure.AppDbContext.EnsureSeedData(seedDb);
                }
                catch { }

                try
                {
                    _allBranches = await _api.GetBranchesAsync();
                }
                catch
                {
                    _allBranches = new List<BranchDto>();
                }

                if (_allBranches == null || _allBranches.Count == 0)
                {
                    // Direct LocalDB fallback
                    using var db = new App.Infrastructure.AppDbContext();
                    var list = db.Branches
                        .OrderBy(b => b.City)
                        .ThenBy(b => b.BranchCode)
                        .ToList();

                    var companies = db.Companies.ToDictionary(c => c.CompanyId, c => c);

                    _allBranches = list.Select(b =>
                    {
                        companies.TryGetValue(b.CompanyId, out var comp);
                        return new BranchDto
                        {
                            BranchId = b.BranchId,
                            CompanyId = b.CompanyId,
                            CompanyCode = comp?.CompanyCode ?? "",
                            CompanyName = comp?.CompanyName ?? "Enterprise Co.",
                            BranchCode = b.BranchCode,
                            BranchName = b.BranchName,
                            Address = b.Address,
                            City = b.City,
                            Phone = b.Phone,
                            Email = b.Email,
                            ManagerName = b.ManagerName,
                            IsActive = b.IsActive,
                            ActiveWorkOrdersCount = db.ServiceRequests.Count(sr => sr.BranchId == b.BranchId && sr.IsActive && sr.Status != "Completed" && sr.Status != "Cancelled"),
                            CreatedAt = b.CreatedAt,
                            UpdatedAt = b.UpdatedAt
                        };
                    }).ToList();
                }

                PopulateCityFilter();
                UpdateKpis();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to load branches: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void PopulateCityFilter()
        {
            string currentSelection = _cmbCityFilter.SelectedItem?.ToString() ?? "All Cities";
            var cities = _allBranches.Select(b => b.City).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();

            _cmbCityFilter.Items.Clear();
            _cmbCityFilter.Items.Add("All Cities");
            foreach (var city in cities)
            {
                _cmbCityFilter.Items.Add(city);
            }

            int idx = _cmbCityFilter.Items.IndexOf(currentSelection);
            _cmbCityFilter.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void UpdateKpis()
        {
            int totalActive = _allBranches.Count(b => b.IsActive);
            int citiesCount = _allBranches.Where(b => b.IsActive).Select(b => b.City).Distinct().Count();
            int totalOrders = _allBranches.Sum(b => b.ActiveWorkOrdersCount);

            _lblTotalBranches.Text = $"{totalActive} Hub{(totalActive == 1 ? "" : "s")}";
            _lblCitiesCovered.Text = $"{citiesCount} Cit{(citiesCount == 1 ? "y" : "ies")}";
            _lblAssignedCrews.Text = "10 Hub Cap";
            _lblActiveOrders.Text = $"{totalOrders} Order{(totalOrders == 1 ? "" : "s")}";

            _lblCount.Text = $"Total Registered Operating Branches: {_allBranches.Count} hub{(_allBranches.Count == 1 ? "" : "s")}";
        }

        private void ApplyFilter()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            string selectedCity = _cmbCityFilter.SelectedItem?.ToString() ?? "All Cities";
            int statusIdx = _cmbStatusFilter.SelectedIndex;

            _filteredBranches = _allBranches.FindAll(b =>
            {
                bool matchesQuery = string.IsNullOrEmpty(query) ||
                    b.BranchCode.ToLower().Contains(query) ||
                    b.BranchName.ToLower().Contains(query) ||
                    b.City.ToLower().Contains(query) ||
                    b.ManagerName.ToLower().Contains(query) ||
                    b.Address.ToLower().Contains(query);

                if (!matchesQuery) return false;

                if (selectedCity != "All Cities" && !string.Equals(b.City, selectedCity, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (statusIdx == 1 && !b.IsActive) return false;
                if (statusIdx == 2 && b.IsActive) return false;

                return true;
            });

            PopulateGrid();
        }

        private void PopulateGrid()
        {
            _grid.Rows.Clear();

            if (_filteredBranches.Count == 0)
            {
                _grid.Visible = false;
                _pnlEmptyState.Visible = true;
                _pnlEmptyState.BringToFront();
                return;
            }

            _pnlEmptyState.Visible = false;
            _grid.Visible = true;

            foreach (var b in _filteredBranches)
            {
                int rowIdx = _grid.Rows.Add(
                    b.BranchCode,
                    b.BranchName,
                    b.City,
                    b.Address,
                    b.ManagerName,
                    b.Phone,
                    b.ActiveWorkOrdersCount.ToString(),
                    b.IsActive ? "Active" : "Inactive",
                    "⚡ Manage"
                );

                _grid.Rows[rowIdx].Tag = b;
            }
        }

        private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

            var colName = _grid.Columns[e.ColumnIndex].Name;
            var val = e.Value?.ToString() ?? "";

            if (colName == "colStatus")
            {
                if (val.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }
            else if (colName == "colCode")
            {
                e.CellStyle!.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                e.CellStyle.ForeColor = Color.FromArgb(79, 70, 229);
            }
        }

        private async void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "colAction") return;

            if (_grid.Rows[e.RowIndex].Tag is BranchDto branch)
            {
                using var dlg = new BranchDialog(branch.CompanyId, branch);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.BranchSaved)
                {
                    ShowToast($"Branch {branch.BranchCode} updated successfully!", true);
                    await LoadBranchesAsync();
                }
            }
        }

        private async void OpenCreateBranchDialog()
        {
            // Default to Tenant C Enterprise company ID
            int targetCompanyId = 3;
            try
            {
                using var db = new App.Infrastructure.AppDbContext();
                var ent = db.Companies.FirstOrDefault(c => c.CompanyCode == "T-ENTERPRISE");
                if (ent != null) targetCompanyId = ent.CompanyId;
            }
            catch { }

            using var dlg = new BranchDialog(targetCompanyId);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.BranchSaved)
            {
                ShowToast("New operating branch registered successfully!", true);
                await LoadBranchesAsync();
            }
        }

        private void ExportBranchesToCsv()
        {
            if (_filteredBranches.Count == 0)
            {
                MessageBox.Show("No branch records available to export.", "Empty Directory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Branch_Operations_Directory_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Operating Branches"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var sw = new StreamWriter(sfd.FileName);
                sw.WriteLine("BranchId,CompanyCode,CompanyName,BranchCode,BranchName,City,Address,Phone,Email,ManagerName,IsActive,ActiveWorkOrders");

                foreach (var b in _filteredBranches)
                {
                    sw.WriteLine(string.Join(",",
                        b.BranchId,
                        EscapeCsv(b.CompanyCode),
                        EscapeCsv(b.CompanyName),
                        EscapeCsv(b.BranchCode),
                        EscapeCsv(b.BranchName),
                        EscapeCsv(b.City),
                        EscapeCsv(b.Address),
                        EscapeCsv(b.Phone),
                        EscapeCsv(b.Email),
                        EscapeCsv(b.ManagerName),
                        b.IsActive,
                        b.ActiveWorkOrdersCount
                    ));
                }

                MessageBox.Show($"Exported {_filteredBranches.Count} branch records successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string val)
        {
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
                return $"\"{val.Replace("\"", "\"\"")}\"";
            return val;
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
    }
}
