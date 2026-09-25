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
    /// MASTER TIER — Subscription Management & Multi-Tenant Control Center.
    ///
    /// Responsibilities according to Final Laboratory Exam:
    ///   - Master tier Super Admin governance over tenant companies.
    ///   - Plan allocations:
    ///       * Tenant A (Micro Company): Main Transaction + Data Collection
    ///       * Tenant B (Small Company): Business Intelligence + Actions
    ///       * Tenant C (Medium Enterprise): Branching + Business Intelligence + Actions
    ///   - MRR, active subscriber metrics, quota tracking, and plan upgrades.
    /// </summary>
    public class SubscriptionManagementView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<SubscriptionDto> _allSubscriptions = new();
        private List<SubscriptionDto> _filteredSubscriptions = new();

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;

        // KPI Metric Labels
        private Label _lblMrr = null!;
        private Label _lblActiveTenants = null!;
        private Label _lblTierDistribution = null!;
        private Label _lblExpiringCount = null!;

        // Filter Controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbTierFilter = null!;
        private ComboBox _cmbStatusFilter = null!;

        // Grid & Empty state
        private Panel _pnlGridContainer = null!;
        private DataGridView _grid = null!;
        private Panel _pnlEmptyState = null!;
        private Button _btnResetFilters = null!;

        private bool _hasLoaded;

        public SubscriptionManagementView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadSubscriptionsAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadSubscriptionsAsync();
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
                Width = 500,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlHeaderLeft);

            _lblTitle = new Label
            {
                Text = "Master Tier: Tenant Subscriptions & Plans",
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
                Text = "Loading tenant subscription records...",
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

            _btnExportCsv = new Button
            {
                Text = "📥  Export Ledger CSV",
                Height = 36,
                Width = 160,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnExportCsv);
            _btnExportCsv.Click += (s, e) => ExportSubscriptionsToCsv();
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 36,
                Width = 95,
                Margin = new Padding(0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadSubscriptionsAsync();
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

            var (k1, _, v1, _) = CreateKpiCard("MONTHLY RECURRING (MRR)", "₱0.00", "Total active subscriber billing", Color.FromArgb(37, 99, 235));
            _lblMrr = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, _, v2, _) = CreateKpiCard("ACTIVE TENANTS", "0", "Provisioned company accounts", Color.FromArgb(22, 163, 74));
            _lblActiveTenants = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, _, v3, _) = CreateKpiCard("TIER ALLOCATION", "0 / 0 / 0", "Micro / Small / Enterprise", Color.FromArgb(79, 70, 229));
            _lblTierDistribution = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, _, v4, _) = CreateKpiCard("RENEWALS DUE (<30D)", "0", "Awaiting billing cycle renewal", Color.FromArgb(234, 88, 12));
            _lblExpiringCount = v4;
            pnlKpis.Controls.Add(k4, 3, 0);

            // ── 3. Plan Specification Banner (3 Tier Comparison Cards) ──
            var pnlTierBanner = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 6, 20, 8)
            };
            pnlTierBanner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlTierBanner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlTierBanner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            card.Controls.Add(pnlTierBanner);

            pnlTierBanner.Controls.Add(CreateTierSpecCard("TENANT A — MICRO COMPANY", "₱2,500 / mo", "✔ Main Transaction\n✔ Data Collection\n👤 3 Users  •  🏢 1 Branch", Color.FromArgb(100, 116, 139)), 0, 0);
            pnlTierBanner.Controls.Add(CreateTierSpecCard("TENANT B — SMALL COMPANY", "₱5,500 / mo", "✔ Business Intelligence\n✔ Actions & Retention\n👤 10 Users  •  🏢 1 Branch", Color.FromArgb(37, 99, 235)), 1, 0);
            pnlTierBanner.Controls.Add(CreateTierSpecCard("TENANT C — MEDIUM ENTERPRISE", "₱12,000 / mo", "✔ Multi-Branching\n✔ Enterprise BI & Actions\n👤 50 Users  •  🏢 10 Branches", Color.FromArgb(79, 70, 229)), 2, 0);

            // ── 4. Search & Filter Bar ────────────────────────────────
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
                PlaceholderText = "🔍  Search by company name or code..."
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

            // Tier Filter
            var lblTier = new Label { Text = "Tier:", Dock = DockStyle.Left, Width = 45, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleRight };
            pnlSearch.Controls.Add(lblTier);

            _cmbTierFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 200,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbTierFilter.Items.AddRange(new object[]
            {
                "All Tiers",
                "Tenant A (Micro)",
                "Tenant B (Small)",
                "Tenant C (Medium)"
            });
            _cmbTierFilter.SelectedIndex = 0;
            _cmbTierFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlSearch.Controls.Add(_cmbTierFilter);

            var pnlSpacer2 = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer2);

            // Status Filter
            var lblStatus = new Label { Text = "Status:", Dock = DockStyle.Left, Width = 55, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), TextAlign = ContentAlignment.MiddleRight };
            pnlSearch.Controls.Add(lblStatus);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 150,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[]
            {
                "All Statuses",
                "Active",
                "Trial",
                "Suspended",
                "Cancelled"
            });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlSearch.Controls.Add(_cmbStatusFilter);

            // Reorder dock flow
            _cmbStatusFilter.BringToFront();
            lblStatus.BringToFront();
            pnlSpacer2.BringToFront();
            _cmbTierFilter.BringToFront();
            lblTier.BringToFront();
            pnlSpacer1.BringToFront();
            pnlSearchBox.SendToBack();

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── 5. Grid Container & Empty State ──────────────────────
            _pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            card.Controls.Add(_pnlGridContainer);

            // Empty State
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false
            };
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

            var lblEmptyIcon = new Label { Text = "🔁", Font = new Font("Segoe UI", 32F), Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };
            pnlEmptyCenter.Controls.Add(lblEmptyIcon);

            var lblEmptyTitle = new Label { Text = "No Subscriptions Found", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter };
            pnlEmptyCenter.Controls.Add(lblEmptyTitle);

            var lblEmptySub = new Label { Text = "No tenant subscription records match your search or filter criteria.", Font = new Font("Segoe UI", 9F), ForeColor = Theme.TextMuted, Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.TopCenter };
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
                _cmbTierFilter.SelectedIndex = 0;
                _cmbStatusFilter.SelectedIndex = 0;
            };
            pnlEmptyCenter.Controls.Add(_btnResetFilters);

            // ── 6. DataGridView ──────────────────────────────────────
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
                new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Tenant Code", Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colCompany", HeaderText = "Company Name", Width = 220, MinimumWidth = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colTier", HeaderText = "Subscription Tier", Width = 160, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colCycle", HeaderText = "Billing Cycle", Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Rate (₱/mo)", Width = 120, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colUsers", HeaderText = "User Cap", Width = 95, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { Name = "colBranches", HeaderText = "Branch Cap", Width = 95, MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { Name = "colEndDate", HeaderText = "Next Renewal", Width = 120, MinimumWidth = 100 },
                new DataGridViewButtonColumn { Name = "colAction", HeaderText = "Action", Width = 130, MinimumWidth = 110,
                    Text = "⚡ Manage Plan", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat }
            });

            if (_grid.Columns["colCode"] != null) _grid.Columns["colCode"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colTier"] != null) _grid.Columns["colTier"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colStatus"] != null) _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colCycle"] != null) _grid.Columns["colCycle"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colPrice"] != null) _grid.Columns["colPrice"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colUsers"] != null) _grid.Columns["colUsers"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colBranches"] != null) _grid.Columns["colBranches"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colEndDate"] != null) _grid.Columns["colEndDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellFormatting += OnGridCellFormatting;

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            // Set Z-Order
            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(pnlDivider, 1);
            card.Controls.SetChildIndex(pnlSearch, 2);
            card.Controls.SetChildIndex(pnlTierBanner, 3);
            card.Controls.SetChildIndex(pnlKpis, 4);
            card.Controls.SetChildIndex(pnlHeader, 5);

            ResumeLayout(false);
        }

        private static Panel CreateTierSpecCard(string title, string price, string features, Color accentColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6, 4, 6, 4),
                Padding = new Padding(12, 8, 12, 8)
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);

                // Top color accent bar
                using var brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, pnl.Width, 3);
            };

            var lblPrice = new Label
            {
                Text = price,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.TopRight
            };
            pnl.Controls.Add(lblPrice);

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 20
            };
            pnl.Controls.Add(lblT);
            lblT.BringToFront();

            var lblF = new Label
            {
                Text = features,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill
            };
            pnl.Controls.Add(lblF);
            lblF.BringToFront();

            return pnl;
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadSubscriptionsAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Loading tenant records from database...";

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
                    _allSubscriptions = await _api.GetSubscriptionsAsync();
                }
                catch
                {
                    _allSubscriptions = new List<SubscriptionDto>();
                }

                if (_allSubscriptions == null || _allSubscriptions.Count == 0)
                {
                    // Direct LocalDB fallback
                    using var db = new App.Infrastructure.AppDbContext();
                    var dbList = db.Subscriptions
                        .OrderBy(s => s.Tier)
                        .ThenBy(s => s.SubscriptionId)
                        .ToList();

                    var companies = db.Companies.ToDictionary(c => c.CompanyId, c => c);

                    _allSubscriptions = dbList.Select(s =>
                    {
                        companies.TryGetValue(s.CompanyId, out var comp);
                        return new SubscriptionDto
                        {
                            SubscriptionId = s.SubscriptionId,
                            CompanyId = s.CompanyId,
                            CompanyCode = comp?.CompanyCode ?? $"T-{s.CompanyId:D2}",
                            CompanyName = comp?.CompanyName ?? "Registered Tenant",
                            Tier = s.Tier.ToString(),
                            TierValue = (int)s.Tier,
                            Status = s.Status,
                            BillingCycle = s.BillingCycle,
                            MonthlyPrice = s.MonthlyPrice,
                            StartDate = s.StartDate,
                            EndDate = s.EndDate,
                            MaxUsers = s.MaxUsers,
                            MaxBranches = s.MaxBranches,
                            HasDataCollection = s.HasDataCollection,
                            HasTransactions = s.HasTransactions,
                            HasBusinessIntelligence = s.HasBusinessIntelligence,
                            HasActions = s.HasActions,
                            HasBranching = s.HasBranching,
                            Notes = s.Notes,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt
                        };
                    }).ToList();
                }

                UpdateKpis();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to load subscriptions: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void UpdateKpis()
        {
            decimal mrr = _allSubscriptions.Where(s => s.Status == "Active").Sum(s => s.MonthlyPrice);
            int activeTenants = _allSubscriptions.Count(s => s.Status == "Active");

            int microCount = _allSubscriptions.Count(s => s.TierValue == 1 || s.Tier.Equals("Micro", StringComparison.OrdinalIgnoreCase));
            int smallCount = _allSubscriptions.Count(s => s.TierValue == 2 || s.Tier.Equals("Small", StringComparison.OrdinalIgnoreCase));
            int medCount = _allSubscriptions.Count(s => s.TierValue == 3 || s.Tier.Equals("Medium", StringComparison.OrdinalIgnoreCase));

            DateTime cutoff = DateTime.UtcNow.AddDays(30);
            int expiring = _allSubscriptions.Count(s => s.Status == "Active" && s.EndDate <= cutoff);

            _lblMrr.Text = $"₱{mrr:N2}";
            _lblActiveTenants.Text = $"{activeTenants} Tenant{(activeTenants == 1 ? "" : "s")}";
            _lblTierDistribution.Text = $"{microCount} Micro / {smallCount} Small / {medCount} Enterprise";
            _lblExpiringCount.Text = $"{expiring} Plan{(expiring == 1 ? "" : "s")}";

            _lblCount.Text = $"Total Registered Tenant Subscriptions: {_allSubscriptions.Count} account{(_allSubscriptions.Count == 1 ? "" : "s")}";
        }

        // ============================================================
        // Filtering
        // ============================================================
        private void ApplyFilter()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            int tierIdx = _cmbTierFilter.SelectedIndex;
            int statusIdx = _cmbStatusFilter.SelectedIndex;

            _filteredSubscriptions = _allSubscriptions.FindAll(s =>
            {
                bool matchesQuery = string.IsNullOrEmpty(query) ||
                    s.CompanyName.ToLower().Contains(query) ||
                    s.CompanyCode.ToLower().Contains(query) ||
                    s.Tier.ToLower().Contains(query);

                if (!matchesQuery) return false;

                if (tierIdx > 0 && s.TierValue != tierIdx)
                    return false;

                if (statusIdx > 0)
                {
                    string selectedStatus = _cmbStatusFilter.SelectedItem?.ToString() ?? "";
                    if (!string.Equals(s.Status, selectedStatus, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            });

            PopulateGrid();
        }

        private void PopulateGrid()
        {
            _grid.Rows.Clear();

            if (_filteredSubscriptions.Count == 0)
            {
                _grid.Visible = false;
                _pnlEmptyState.Visible = true;
                _pnlEmptyState.BringToFront();
                return;
            }

            _pnlEmptyState.Visible = false;
            _grid.Visible = true;

            foreach (var sub in _filteredSubscriptions)
            {
                string tierDisplay = sub.TierValue switch
                {
                    1 => "Tenant A (Micro)",
                    2 => "Tenant B (Small)",
                    3 => "Tenant C (Enterprise)",
                    _ => sub.Tier
                };

                int rowIdx = _grid.Rows.Add(
                    sub.CompanyCode,
                    sub.CompanyName,
                    tierDisplay,
                    sub.Status,
                    sub.BillingCycle,
                    $"₱{sub.MonthlyPrice:N2}",
                    sub.MaxUsers.ToString(),
                    sub.MaxBranches.ToString(),
                    sub.EndDate.ToString("yyyy-MM-dd"),
                    "⚡ Manage Plan"
                );

                _grid.Rows[rowIdx].Tag = sub;
            }
        }

        private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

            var colName = _grid.Columns[e.ColumnIndex].Name;
            var val = e.Value?.ToString() ?? "";

            if (colName == "colTier")
            {
                if (val.Contains("Micro"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(71, 85, 105);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (val.Contains("Small"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(37, 99, 235);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (val.Contains("Enterprise"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(79, 70, 229);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }
            else if (colName == "colStatus")
            {
                if (val.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (val.Equals("Suspended", StringComparison.OrdinalIgnoreCase) || val.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(234, 88, 12);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            }
        }

        public override void ApplyViewPermissions(string userRole)
        {
            base.ApplyViewPermissions(userRole);
            if (userRole != Roles.SuperAdmin)
            {
                var col = _grid.Columns["colAction"];
                if (col != null)
                {
                    col.Visible = false;
                }
            }
        }

        private async void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "colAction") return;

            if (SessionManager.CurrentUser?.Role != Roles.SuperAdmin)
            {
                MessageBox.Show("Access Denied: Only Super Administrators have permission to modify tenant subscriptions.", "Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_grid.Rows[e.RowIndex].Tag is SubscriptionDto sub)
            {
                using var dlg = new EditSubscriptionDialog(sub);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.SubscriptionUpdated)
                {
                    ShowToast($"Subscription for {sub.CompanyName} updated successfully!", true);
                    await LoadSubscriptionsAsync();
                }
            }
        }

        // ============================================================
        // Export CSV
        // ============================================================
        private void ExportSubscriptionsToCsv()
        {
            if (_filteredSubscriptions.Count == 0)
            {
                MessageBox.Show("No subscription records available to export.", "Empty Ledger", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Tenant_Subscriptions_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Tenant Subscriptions Ledger"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var sw = new StreamWriter(sfd.FileName);
                sw.WriteLine("SubscriptionId,CompanyCode,CompanyName,Tier,TierLevel,Status,BillingCycle,MonthlyPrice,StartDate,EndDate,MaxUsers,MaxBranches,HasTransactions,HasDataCollection,HasBusinessIntelligence,HasActions,HasBranching,Notes");

                foreach (var s in _filteredSubscriptions)
                {
                    sw.WriteLine(string.Join(",",
                        s.SubscriptionId,
                        EscapeCsv(s.CompanyCode),
                        EscapeCsv(s.CompanyName),
                        EscapeCsv(s.Tier),
                        s.TierValue,
                        EscapeCsv(s.Status),
                        EscapeCsv(s.BillingCycle),
                        s.MonthlyPrice.ToString("F2", CultureInfo.InvariantCulture),
                        s.StartDate.ToString("yyyy-MM-dd"),
                        s.EndDate.ToString("yyyy-MM-dd"),
                        s.MaxUsers,
                        s.MaxBranches,
                        s.HasTransactions,
                        s.HasDataCollection,
                        s.HasBusinessIntelligence,
                        s.HasActions,
                        s.HasBranching,
                        EscapeCsv(s.Notes ?? "")
                    ));
                }

                MessageBox.Show($"Exported {_filteredSubscriptions.Count} subscription records successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
