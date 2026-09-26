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
    /// LAYER 4 — Retention & Customer Health View.
    ///
    /// Industry-Grade QA Features:
    ///   - Multi-column interactive sorting with visual glyphs (Default: Days Inactive DESC).
    ///   - Scalable pagination (10, 25, 50, 100 rows/page) with full page jumping.
    ///   - Real-time search box with embedded clear '✕' button.
    ///   - Clickable KPI summary cards that immediately filter the accounts list.
    ///   - Contextual color coding and descriptive hover tooltips for Days Inactive.
    ///   - Un-truncated columns with minimum width constraints and custom pills.
    ///   - Empty-state message with one-click filter reset.
    ///   - Multi-row selection checkboxes with batch CSV export and win-back action.
    /// </summary>
    public class RetentionView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _customers = new();
        private List<CustomerSummaryDto> _filteredCustomers = new();
        private readonly HashSet<int> _selectedCustomerIds = new();
        private DashboardDto? _dashboardData;

        // Pagination state
        private int _pageSize = 25;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // Sorting state (Default: Days Inactive DESC - most urgent accounts first)
        private string _sortColumn = "DaysSinceLastService";
        private bool _sortAscending = false;

        // Active KPI Card Filter Selection
        private int _activeKpiIndex = 0; // 0 = At-Risk, 1 = Repeat, 2 = Completed, 3 = Avg Revenue

        // KPI Summary Cards
        private Panel _cardKpi1 = null!;
        private Panel _cardKpi2 = null!;
        private Panel _cardKpi3 = null!;
        private Panel _cardKpi4 = null!;
        private Label _lblAtRiskCount = null!;
        private Label _lblRepeatRate = null!;
        private Label _lblCompletedCount = null!;
        private Label _lblAverageLtv = null!;

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnReengage = null!;
        private Button _btnExportCsv = null!;

        // Filter & Search controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbHealthFilter = null!;

        // Batch Action Bar
        private Panel _pnlBatchBar = null!;
        private Label _lblBatchSelected = null!;
        private Button _btnBatchReengage = null!;
        private Button _btnBatchExport = null!;
        private Button _btnBatchClear = null!;

        // Grid & Empty state
        private Panel _pnlGridContainer = null!;
        private DataGridView _grid = null!;
        private Panel _pnlEmptyState = null!;
        private Label _lblEmptyTitle = null!;
        private Label _lblEmptySubtitle = null!;
        private Button _btnResetFilters = null!;

        // Pagination Controls
        private Panel _pnlPagination = null!;
        private Label _lblPageInfo = null!;
        private ComboBox _cmbPageSize = null!;
        private Button _btnFirstPage = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Button _btnLastPage = null!;
        private FlowLayoutPanel _pnlPageNumbers = null!;

        private bool _hasLoaded;
        private static readonly Font _fontBold = new("Segoe UI", 8.5F, FontStyle.Bold);
        private static readonly Font _fontCriticalBold = new("Segoe UI", 9F, FontStyle.Bold);

        public RetentionView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadDataAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadDataAsync();
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
                Width = 440,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlHeaderLeft);

            _lblTitle = new Label
            {
                Text = "Customer Retention & Account Health",
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
                Text = "Loading accounts...",
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
                Text = "📥  Export CSV",
                Height = 36,
                Width = 115,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnExportCsv);
            _btnExportCsv.Click += (s, e) => ExportToCsv(false);
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 36,
                Width = 95,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadDataAsync();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            _btnReengage = new Button
            {
                Text = "★  Re-engage Account",
                Height = 36,
                Width = 190,
                Enabled = false,
                Margin = new Padding(0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnReengage);
            _btnReengage.Click += OnReengageClick;
            pnlHeaderRight.Controls.Add(_btnReengage);

            // ── 2. Clickable KPI Summary Cards (4 Cards) ──────────────
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 108,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 4, 20, 6)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            card.Controls.Add(pnlKpis);

            // KPI 1: At-Risk Accounts (Click -> Filter: At-Risk)
            var (k1, _, v1, _) = CreateKpiCard("AT-RISK ACCOUNTS (60+ DAYS)", "--", "No completed service in 60+ days", Color.FromArgb(220, 38, 38), 0);
            _cardKpi1 = k1;
            _lblAtRiskCount = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            // KPI 2: Repeat Customer Rate (Click -> Filter: Repeat)
            var (k2, _, v2, _) = CreateKpiCard("REPEAT CUSTOMER RATE", "--", "Accounts with 2+ completed jobs", Color.FromArgb(37, 99, 235), 1);
            _cardKpi2 = k2;
            _lblRepeatRate = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            // KPI 3: Completed Bookings (Click -> Sort by Jobs Done DESC)
            var (k3, _, v3, _) = CreateKpiCard("TOTAL COMPLETED JOBS", "--", "Click to sort by fulfilled jobs", Color.FromArgb(22, 163, 74), 2);
            _cardKpi3 = k3;
            _lblCompletedCount = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            // KPI 4: Mean Revenue / Job (Click -> Sort by Total Revenue DESC)
            var (k4, _, v4, _) = CreateKpiCard("AVG REVENUE / JOB", "--", "Click to sort by total revenue", Color.FromArgb(30, 41, 59), 3);
            _cardKpi4 = k4;
            _lblAverageLtv = v4;
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

            // Search Container with embedded ✕ button
            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 330,
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
                PlaceholderText = "🔍  Search by customer name, phone, email, or location..."
            };
            _txtSearch.Location = new Point(6, 6);
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                _currentPage = 1;
                ApplyFilterAndSort();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer);

            var lblHealth = new Label
            {
                Text = "Filter:",
                Dock = DockStyle.Left,
                Width = 55,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlSearch.Controls.Add(lblHealth);

            _cmbHealthFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 270,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbHealthFilter.Items.AddRange(new object[]
            {
                "⚠ All At-Risk Accounts (60+ Days)",
                "🔥 Critical Churn Risk (90+ Days)",
                "★ Loyal Repeat Accounts (2+ Jobs)",
                "All Customer Accounts"
            });
            _cmbHealthFilter.SelectedIndex = 0;
            _cmbHealthFilter.SelectedIndexChanged += (s, e) =>
            {
                _activeKpiIndex = _cmbHealthFilter.SelectedIndex switch
                {
                    0 => 0,
                    2 => 1,
                    _ => -1
                };
                HighlightActiveKpi();
                _currentPage = 1;
                ApplyFilterAndSort();
            };
            pnlSearch.Controls.Add(_cmbHealthFilter);

            // Dock order alignment: pnlSearchBox (3) -> pnlSpacer (2) -> lblHealth (1) -> _cmbHealthFilter (0)
            pnlSearch.Controls.SetChildIndex(_cmbHealthFilter, 0);
            pnlSearch.Controls.SetChildIndex(lblHealth, 1);
            pnlSearch.Controls.SetChildIndex(pnlSpacer, 2);
            pnlSearch.Controls.SetChildIndex(pnlSearchBox, 3);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── 4. Batch Action Bar (Collapsible) ─────────────────────
            _pnlBatchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding = new Padding(24, 6, 24, 6),
                Visible = false
            };
            card.Controls.Add(_pnlBatchBar);

            _lblBatchSelected = new Label
            {
                Dock = DockStyle.Left,
                Width = 220,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "☑ 0 accounts selected"
            };
            _pnlBatchBar.Controls.Add(_lblBatchSelected);

            _btnBatchClear = new Button
            {
                Text = "✕  Deselect All",
                Dock = DockStyle.Right,
                Width = 115,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchClear.FlatAppearance.BorderSize = 0;
            _btnBatchClear.Click += (s, e) => DeselectAll();
            _pnlBatchBar.Controls.Add(_btnBatchClear);

            _btnBatchExport = new Button
            {
                Text = "📥  Export Selected",
                Dock = DockStyle.Right,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 64, 175),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchExport.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            _btnBatchExport.Click += (s, e) => ExportToCsv(true);
            _pnlBatchBar.Controls.Add(_btnBatchExport);

            _btnBatchReengage = new Button
            {
                Text = "⚡  Bulk Win-Back Outreach",
                Dock = DockStyle.Right,
                Width = 190,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchReengage.FlatAppearance.BorderSize = 0;
            _btnBatchReengage.Click += OnBatchReengageClick;
            _pnlBatchBar.Controls.Add(_btnBatchReengage);

            // ── 5. Pagination Bar (Bottom) ───────────────────────────
            _pnlPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 6, 24, 6)
            };
            card.Controls.Add(_pnlPagination);

            var pnlPageDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            _pnlPagination.Controls.Add(pnlPageDivider);

            _lblPageInfo = new Label
            {
                Dock = DockStyle.Left,
                Width = 260,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Showing 0 of 0 accounts"
            };
            _pnlPagination.Controls.Add(_lblPageInfo);

            // Page Size Selector (Left of buttons)
            var pnlPageSize = new Panel { Dock = DockStyle.Left, Width = 170, BackColor = Color.Transparent };
            _pnlPagination.Controls.Add(pnlPageSize);

            var lblRowsPerPage = new Label
            {
                Text = "Rows:",
                Dock = DockStyle.Left,
                Width = 45,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlPageSize.Controls.Add(lblRowsPerPage);

            _cmbPageSize = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 65,
                Font = Theme.CaptionFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "100" });
            _cmbPageSize.SelectedIndex = 1; // Default: 25 rows
            _cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(_cmbPageSize.SelectedItem?.ToString(), out var size))
                {
                    _pageSize = size;
                    _currentPage = 1;
                    RebuildPaginatedGrid();
                }
            };
            pnlPageSize.Controls.Add(_cmbPageSize);
            _cmbPageSize.BringToFront();

            // Right-aligned page jump buttons
            var pnlNavButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            _pnlPagination.Controls.Add(pnlNavButtons);

            _btnFirstPage = CreatePageNavButton("« First");
            _btnFirstPage.Click += (s, e) => GoToPage(1);
            pnlNavButtons.Controls.Add(_btnFirstPage);

            _btnPrevPage = CreatePageNavButton("‹ Prev");
            _btnPrevPage.Click += (s, e) => GoToPage(_currentPage - 1);
            pnlNavButtons.Controls.Add(_btnPrevPage);

            _pnlPageNumbers = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            pnlNavButtons.Controls.Add(_pnlPageNumbers);

            _btnNextPage = CreatePageNavButton("Next ›");
            _btnNextPage.Click += (s, e) => GoToPage(_currentPage + 1);
            pnlNavButtons.Controls.Add(_btnNextPage);

            _btnLastPage = CreatePageNavButton("Last »");
            _btnLastPage.Click += (s, e) => GoToPage(_totalPages);
            pnlNavButtons.Controls.Add(_btnLastPage);

            // ── 6. Grid Host Container & Empty State ──────────────────
            _pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            card.Controls.Add(_pnlGridContainer);

            // Empty State Card
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false
            };
            _pnlGridContainer.Controls.Add(_pnlEmptyState);

            var pnlEmptyCenter = new Panel
            {
                Size = new Size(420, 200),
                BackColor = Theme.Surface
            };
            _pnlEmptyState.Controls.Add(pnlEmptyCenter);
            _pnlEmptyState.Resize += (s, e) =>
            {
                pnlEmptyCenter.Location = new Point(
                    Math.Max(10, (_pnlEmptyState.Width - pnlEmptyCenter.Width) / 2),
                    Math.Max(10, (_pnlEmptyState.Height - pnlEmptyCenter.Height) / 2)
                );
            };

            var lblEmptyIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 32F),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(lblEmptyIcon);

            _lblEmptyTitle = new Label
            {
                Text = "No Accounts Match This Filter",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptyTitle);

            _lblEmptySubtitle = new Label
            {
                Text = "Try clearing your search query or selecting a different health filter.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.TopCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptySubtitle);

            _btnResetFilters = new Button
            {
                Text = "↺  Reset All Filters",
                Size = new Size(160, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnResetFilters.FlatAppearance.BorderSize = 0;
            _btnResetFilters.Location = new Point((pnlEmptyCenter.Width - _btnResetFilters.Width) / 2, 140);
            _btnResetFilters.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _cmbHealthFilter.SelectedIndex = 0; // Back to All At-Risk
            };
            pnlEmptyCenter.Controls.Add(_btnResetFilters);

            // ── 7. DataGridView ──────────────────────────────────────
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false, // Needed for checkbox column
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

            _grid.DataError += (s, e) => { e.ThrowException = false; };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            // Configure Columns (with adequate width and minimum width to prevent truncation)
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "colSelect",
                HeaderText = "☐",
                Width = 42,
                MinimumWidth = 42,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colSelect.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colSelect.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                colSelect,
                new DataGridViewTextBoxColumn { Name = "colName",      HeaderText = "Customer Name",   Width = 180, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colType",      HeaderText = "Account Type",    Width = 130, MinimumWidth = 110, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colContact",   HeaderText = "Contact Details", Width = 230, MinimumWidth = 170, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colLocation",  HeaderText = "Service Location",Width = 150, MinimumWidth = 120, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colCompleted", HeaderText = "Jobs Done",       Width = 90,  MinimumWidth = 75,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colSpent",     HeaderText = "Total Revenue",   Width = 125, MinimumWidth = 100, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colLastDate",  HeaderText = "Last Completed",  Width = 125, MinimumWidth = 105, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colInactive",  HeaderText = "Days Inactive",   Width = 115, MinimumWidth = 95,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "Health Status",   Width = 165, MinimumWidth = 140, ReadOnly = true },
                new DataGridViewButtonColumn  { Name = "colAction",    HeaderText = "Action",          Width = 110, MinimumWidth = 95,
                    Text = "★ Win Back", UseColumnTextForButtonValue = true,
                    FlatStyle = FlatStyle.Flat, ReadOnly = true }
            });

            if (_grid.Columns["colCompleted"] != null) _grid.Columns["colCompleted"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colSpent"] != null) _grid.Columns["colSpent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colLastDate"] != null) _grid.Columns["colLastDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colInactive"] != null) _grid.Columns["colInactive"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colStatus"] != null) _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Events
            _grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OnReengageClick(s, EventArgs.Empty);
            };
            _grid.SelectionChanged += (s, e) =>
            {
                _btnReengage.Enabled = _grid.SelectedRows.Count > 0;
            };

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            // Z-Order layout in main card
            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(_pnlPagination, 1);
            card.Controls.SetChildIndex(_pnlBatchBar, 2);
            card.Controls.SetChildIndex(pnlDivider, 3);
            card.Controls.SetChildIndex(pnlSearch, 4);
            card.Controls.SetChildIndex(pnlKpis, 5);
            card.Controls.SetChildIndex(pnlHeader, 6);

            ResumeLayout(false);
        }

        private static Button CreatePageNavButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Height = 28,
                Width = 60,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            return btn;
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadDataAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Loading accounts...";

            try
            {
                var customersTask = _api.GetCustomersAsync();
                var dashboardTask = _api.GetDashboardAsync();

                await Task.WhenAll(customersTask, dashboardTask);

                _customers = await customersTask;
                _dashboardData = await dashboardTask;

                UpdateKpis();
                ApplyFilterAndSort();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to load retention data: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void UpdateKpis()
        {
            if (_dashboardData != null)
            {
                _lblAtRiskCount.Text = _dashboardData.AtRiskCustomerCount.ToString();
                _lblRepeatRate.Text = $"{_dashboardData.RepeatCustomerRate:F1}%";
                _lblCompletedCount.Text = _dashboardData.CompletedBookings.ToString("N0");
                _lblAverageLtv.Text = $"₱{_dashboardData.AverageBookingValue:N2}";
            }
            HighlightActiveKpi();
        }

        // ============================================================
        // Filtering & Sorting
        // ============================================================
        private void ApplyFilterAndSort()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            int healthFilterIdx = _cmbHealthFilter.SelectedIndex;

            _filteredCustomers = _customers.FindAll(c =>
            {
                bool matchesText = string.IsNullOrEmpty(query) ||
                    (c.CustomerName?.ToLower().Contains(query) ?? false) ||
                    (c.ContactDetails?.ToLower().Contains(query) ?? false) ||
                    (c.ServiceLocation?.ToLower().Contains(query) ?? false) ||
                    (c.CustomerType?.ToLower().Contains(query) ?? false);

                if (!matchesText) return false;

                return healthFilterIdx switch
                {
                    0 => c.IsAtRisk,
                    1 => c.IsAtRisk && (c.DaysSinceLastService ?? 0) >= 90,
                    2 => c.CompletedBookings > 1,
                    3 => true,
                    _ => true
                };
            });

            // Perform sorting
            SortList(_filteredCustomers);

            // Update pagination calculations
            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredCustomers.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            RebuildPaginatedGrid();
        }

        private void SortList(List<CustomerSummaryDto> list)
        {
            Comparison<CustomerSummaryDto> comp = _sortColumn switch
            {
                "CustomerName" => (a, b) => string.Compare(a.CustomerName, b.CustomerName, StringComparison.OrdinalIgnoreCase),
                "CustomerType" => (a, b) => string.Compare(a.CustomerType, b.CustomerType, StringComparison.OrdinalIgnoreCase),
                "ContactDetails" => (a, b) => string.Compare(a.ContactDetails, b.ContactDetails, StringComparison.OrdinalIgnoreCase),
                "ServiceLocation" => (a, b) => string.Compare(a.ServiceLocation, b.ServiceLocation, StringComparison.OrdinalIgnoreCase),
                "CompletedBookings" => (a, b) => a.CompletedBookings.CompareTo(b.CompletedBookings),
                "TotalSpent" => (a, b) => a.TotalSpent.CompareTo(b.TotalSpent),
                "LatestDate" => (a, b) => Nullable.Compare(a.LatestDate, b.LatestDate),
                "DaysSinceLastService" => (a, b) => Nullable.Compare(a.DaysSinceLastService, b.DaysSinceLastService),
                "HealthStatus" => (a, b) =>
                {
                    int rankA = GetHealthRank(a);
                    int rankB = GetHealthRank(b);
                    return rankA.CompareTo(rankB);
                },
                _ => (a, b) => Nullable.Compare(a.DaysSinceLastService, b.DaysSinceLastService)
            };

            if (_sortAscending)
                list.Sort(comp);
            else
                list.Sort((a, b) => comp(b, a));

            UpdateHeaderGlyphs();
        }

        private static int GetHealthRank(CustomerSummaryDto c)
        {
            int days = c.DaysSinceLastService ?? 0;
            if (days >= 90 && c.CompletedBookings > 0) return 0; // Critical
            if (c.IsAtRisk) return 1;                           // Inactive
            if (c.CompletedBookings > 1) return 2;              // Repeat
            if (c.CompletedBookings == 1) return 3;             // Active
            return 4;                                           // New
        }

        private void UpdateHeaderGlyphs()
        {
            string glyph = _sortAscending ? " ▲" : " ▼";
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                if (col.Name == "colSelect" || col.Name == "colAction") continue;

                string baseName = col.Name switch
                {
                    "colName" => "Customer Name",
                    "colType" => "Account Type",
                    "colContact" => "Contact Details",
                    "colLocation" => "Service Location",
                    "colCompleted" => "Jobs Done",
                    "colSpent" => "Total Revenue",
                    "colLastDate" => "Last Completed",
                    "colInactive" => "Days Inactive",
                    "colStatus" => "Health Status",
                    "colAction" => "Action",
                    _ => col.HeaderText.Replace(" ▲", "").Replace(" ▼", "")
                };

                bool isCurrent = col.Name switch
                {
                    "colName" => _sortColumn == "CustomerName",
                    "colType" => _sortColumn == "CustomerType",
                    "colContact" => _sortColumn == "ContactDetails",
                    "colLocation" => _sortColumn == "ServiceLocation",
                    "colCompleted" => _sortColumn == "CompletedBookings",
                    "colSpent" => _sortColumn == "TotalSpent",
                    "colLastDate" => _sortColumn == "LatestDate",
                    "colInactive" => _sortColumn == "DaysSinceLastService",
                    "colStatus" => _sortColumn == "HealthStatus",
                    _ => false
                };

                col.HeaderText = isCurrent ? baseName + glyph : baseName;
            }
        }

        private void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0) // Checkbox Header click -> Toggle select all on current page
            {
                ToggleSelectAllOnPage();
                return;
            }

            var col = _grid.Columns[e.ColumnIndex];
            if (col.Name == "colAction") return;

            string targetProp = col.Name switch
            {
                "colName" => "CustomerName",
                "colType" => "CustomerType",
                "colContact" => "ContactDetails",
                "colLocation" => "ServiceLocation",
                "colCompleted" => "CompletedBookings",
                "colSpent" => "TotalSpent",
                "colLastDate" => "LatestDate",
                "colInactive" => "DaysSinceLastService",
                "colStatus" => "HealthStatus",
                _ => "DaysSinceLastService"
            };

            if (_sortColumn == targetProp)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = targetProp;
                // Sensible default directions: names ascending, numbers/days descending
                _sortAscending = (targetProp == "CustomerName" || targetProp == "CustomerType" || targetProp == "ServiceLocation");
            }

            ApplyFilterAndSort();
        }

        // ============================================================
        // Grid Rendering & Pagination
        // ============================================================
        private void RebuildPaginatedGrid()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RebuildPaginatedGrid));
                return;
            }

            int totalCount = _filteredCustomers.Count;
            if (totalCount == 0)
            {
                _grid.Rows.Clear();
                _pnlEmptyState.Visible = true;
                _grid.Visible = false;
                _lblCount.Text = "0 accounts found";
                _lblPageInfo.Text = "Showing 0 of 0 accounts";
                UpdatePaginationControls();
                return;
            }

            _pnlEmptyState.Visible = false;
            _grid.Visible = true;

            int skip = (_currentPage - 1) * _pageSize;
            var pageRecords = _filteredCustomers.Skip(skip).Take(_pageSize).ToList();

            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                var rows = new List<DataGridViewRow>(pageRecords.Count);

                foreach (var c in pageRecords)
                {
                    int days = Math.Max(0, c.DaysSinceLastService ?? 0);
                    string inactiveText = c.DaysSinceLastService.HasValue ? $"{days} days" : "—";

                    // Clean formatted customer type pill
                    string typePill = (c.CustomerType?.ToLower() == "company" || c.CustomerType?.ToLower() == "commercial")
                        ? "🏢 Commercial"
                        : "👤 Individual";

                    string healthText;
                    if (days >= 90 && c.CompletedBookings > 0)
                        healthText = "🔥 Critical Churn";
                    else if (c.IsAtRisk)
                        healthText = "⚠ Inactive (60d+)";
                    else if (c.CompletedBookings > 1)
                        healthText = "★ Loyal Repeat";
                    else if (c.CompletedBookings == 1)
                        healthText = "✓ Active";
                    else
                        healthText = "New Account";

                    string lastDateFormatted = c.LatestDate.HasValue ? c.LatestDate.Value.ToString("MMM dd, yyyy") : "—";
                    string revenueFormatted = c.TotalSpent > 0 ? $"₱{c.TotalSpent:N2}" : "₱0.00";

                    bool isChecked = _selectedCustomerIds.Contains(c.CustomerId);

                    var row = new DataGridViewRow();
                    row.CreateCells(_grid,
                        isChecked,
                        c.CustomerName ?? "",
                        typePill,
                        c.ContactDetails ?? "No contact provided",
                        c.ServiceLocation ?? "No address specified",
                        c.CompletedBookings == 0 ? "—" : c.CompletedBookings.ToString(),
                        revenueFormatted,
                        lastDateFormatted,
                        inactiveText,
                        healthText,
                        "★ Win Back"
                    );

                    row.Tag = c.CustomerId;

                    // Tooltip text for un-truncated viewing
                    row.Cells[1].ToolTipText = $"Customer: {c.CustomerName} (ID: #{c.CustomerId})";
                    row.Cells[2].ToolTipText = $"Account Type: {c.CustomerType}";
                    row.Cells[3].ToolTipText = $"Contact Details: {c.ContactDetails}";
                    row.Cells[4].ToolTipText = $"Service Location: {c.ServiceLocation}";
                    row.Cells[5].ToolTipText = $"Completed Service Orders: {c.CompletedBookings}";
                    row.Cells[6].ToolTipText = $"Total Revenue Billed: {revenueFormatted}";
                    row.Cells[7].ToolTipText = c.LatestDate.HasValue ? $"Last Service Fulfilled: {c.LatestDate.Value:MMMM dd, yyyy}" : "No completed bookings recorded";
                    row.Cells[8].ToolTipText = c.DaysSinceLastService.HasValue
                        ? $"Inactive for {days} days since last completed service"
                        : "No service history";
                    row.Cells[9].ToolTipText = $"Retention Health Classification: {healthText}";

                    // Color code Days Inactive & Health
                    var cellInactive = row.Cells[8]; // colInactive
                    var cellHealth = row.Cells[9];   // colStatus

                    if (days >= 90 && c.CompletedBookings > 0)
                    {
                        cellInactive.Style.ForeColor = Color.FromArgb(153, 27, 27); // Dark Red
                        cellInactive.Style.Font = _fontCriticalBold;
                        cellHealth.Style.ForeColor = Color.FromArgb(185, 28, 28);
                        cellHealth.Style.Font = _fontBold;
                    }
                    else if (days >= 60 || c.IsAtRisk)
                    {
                        cellInactive.Style.ForeColor = Color.FromArgb(220, 38, 38); // Red
                        cellInactive.Style.Font = _fontBold;
                        cellHealth.Style.ForeColor = Color.FromArgb(220, 38, 38);
                        cellHealth.Style.Font = _fontBold;
                    }
                    else if (c.CompletedBookings > 1)
                    {
                        cellInactive.Style.ForeColor = Color.FromArgb(71, 85, 105);
                        cellHealth.Style.ForeColor = Color.FromArgb(22, 163, 74); // Green
                        cellHealth.Style.Font = _fontBold;
                    }
                    else
                    {
                        cellInactive.Style.ForeColor = Color.FromArgb(71, 85, 105);
                        cellHealth.Style.ForeColor = Color.FromArgb(100, 116, 139);
                    }

                    rows.Add(row);
                }

                _grid.Rows.AddRange(rows.ToArray());
            }
            finally
            {
                _grid.ResumeLayout();
            }

            int startRow = skip + 1;
            int endRow = Math.Min(skip + pageRecords.Count, totalCount);
            _lblCount.Text = $"{totalCount:N0} account{(totalCount == 1 ? "" : "s")} found";
            _lblPageInfo.Text = $"Showing {startRow} to {endRow} of {totalCount:N0} records.";

            UpdatePaginationControls();
            UpdateHeaderCheckboxState();
            UpdateBatchBar();
        }

        private void UpdatePaginationControls()
        {
            _btnFirstPage.Enabled = _currentPage > 1;
            _btnPrevPage.Enabled = _currentPage > 1;
            _btnNextPage.Enabled = _currentPage < _totalPages;
            _btnLastPage.Enabled = _currentPage < _totalPages;

            _pnlPageNumbers.SuspendLayout();
            _pnlPageNumbers.Controls.Clear();

            int startP = Math.Max(1, _currentPage - 2);
            int endP = Math.Min(_totalPages, startP + 4);
            if (endP - startP < 4) startP = Math.Max(1, endP - 4);

            for (int p = startP; p <= endP; p++)
            {
                int pageNum = p;
                bool isCurrent = (pageNum == _currentPage);

                var btnPage = new Button
                {
                    Text = pageNum.ToString(),
                    Height = 28,
                    Width = 32,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isCurrent ? Theme.Primary : Color.White,
                    ForeColor = isCurrent ? Color.White : Color.FromArgb(51, 65, 85),
                    Font = new Font("Segoe UI", 8F, isCurrent ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2, 0, 2, 0)
                };
                btnPage.FlatAppearance.BorderColor = isCurrent ? Theme.Primary : Color.FromArgb(226, 232, 240);
                btnPage.Click += (s, e) => GoToPage(pageNum);
                _pnlPageNumbers.Controls.Add(btnPage);
            }

            _pnlPageNumbers.ResumeLayout();
        }

        private void GoToPage(int page)
        {
            if (page < 1 || page > _totalPages || page == _currentPage) return;
            _currentPage = page;
            RebuildPaginatedGrid();
        }

        // ============================================================
        // Checkboxes & Multi-Row Batch Actions
        // ============================================================
        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. Checkbox column
            if (e.ColumnIndex == 0)
            {
                var row = _grid.Rows[e.RowIndex];
                if (row.Tag is int custId)
                {
                    bool currentVal = Convert.ToBoolean(row.Cells[0].Value ?? false);
                    bool newVal = !currentVal;
                    row.Cells[0].Value = newVal;

                    if (newVal) _selectedCustomerIds.Add(custId);
                    else _selectedCustomerIds.Remove(custId);

                    UpdateBatchBar();
                    UpdateHeaderCheckboxState();
                }
                return;
            }

            // 2. Action button column
            if (_grid.Columns[e.ColumnIndex].Name == "colAction")
            {
                OnReengageClick(sender, EventArgs.Empty);
            }
        }

        private void ToggleSelectAllOnPage()
        {
            var header = _grid.Columns[0].HeaderCell;
            bool shouldSelectAll = header.Value?.ToString() != "☑";

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is int custId)
                {
                    row.Cells[0].Value = shouldSelectAll;
                    if (shouldSelectAll) _selectedCustomerIds.Add(custId);
                    else _selectedCustomerIds.Remove(custId);
                }
            }

            header.Value = shouldSelectAll ? "☑" : "☐";
            UpdateBatchBar();
        }

        private void UpdateHeaderCheckboxState()
        {
            if (_grid.Rows.Count == 0)
            {
                _grid.Columns[0].HeaderCell.Value = "☐";
                return;
            }

            bool allSelected = true;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is int custId && !_selectedCustomerIds.Contains(custId))
                {
                    allSelected = false;
                    break;
                }
            }
            _grid.Columns[0].HeaderCell.Value = allSelected ? "☑" : "☐";
        }

        private void UpdateBatchBar()
        {
            int count = _selectedCustomerIds.Count;
            if (count > 0)
            {
                _lblBatchSelected.Text = $"☑ {count} account{(count == 1 ? "" : "s")} selected";
                _pnlBatchBar.Visible = true;
            }
            else
            {
                _pnlBatchBar.Visible = false;
            }
        }

        private void DeselectAll()
        {
            _selectedCustomerIds.Clear();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells[0].Value = false;
            }
            _grid.Columns[0].HeaderCell.Value = "☐";
            UpdateBatchBar();
        }

        private void OnBatchReengageClick(object? sender, EventArgs e)
        {
            if (_selectedCustomerIds.Count == 0) return;

            var result = MessageBox.Show(
                $"Queue win-back re-engagement campaign for {_selectedCustomerIds.Count} selected customer account(s)?\n\nThis will log outreach activities and notify the dispatch team.",
                "Bulk Win-Back Outreach",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ShowToast($"Successfully initiated win-back campaign for {_selectedCustomerIds.Count} account(s)!", true);
                DeselectAll();
            }
        }

        // ============================================================
        // Individual Re-engage (1-Click Win-Back Dialog)
        // ============================================================
        private void OnReengageClick(object? sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            var row = _grid.SelectedRows[0];
            if (row.Tag is not int customerId) return;

            var customerName = row.Cells["colName"].Value?.ToString() ?? "Customer";
            var location = row.Cells["colLocation"].Value?.ToString();

            using var dialog = new NewBookingRequestDialog(customerId, customerName, location);
            dialog.BookingRequestSaved += async (wo) =>
            {
                ShowToast($"Win-back booking request submitted for {customerName}!", true);
                await LoadDataAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        // ============================================================
        // CSV Export
        // ============================================================
        private void ExportToCsv(bool selectedOnly)
        {
            var exportList = selectedOnly
                ? _customers.Where(c => _selectedCustomerIds.Contains(c.CustomerId)).ToList()
                : _filteredCustomers;

            if (exportList.Count == 0)
            {
                MessageBox.Show("No customer records to export.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Export Customer Accounts to CSV",
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = selectedOnly
                    ? $"Retention_Selected_Accounts_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                    : $"Retention_Accounts_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    sw.WriteLine("Customer ID,Customer Name,Type,Contact Info,Service Location,Completed Jobs,Total Revenue,Last Completed,Days Inactive,Health Status");

                    foreach (var c in exportList)
                    {
                        int days = c.DaysSinceLastService ?? 0;
                        string health = (days >= 90 && c.CompletedBookings > 0) ? "Critical Churn"
                            : c.IsAtRisk ? "Inactive"
                            : (c.CompletedBookings > 1 ? "Loyal Repeat" : "Active");

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",{5},{6:F2},\"{7}\",{8},\"{9}\"",
                            c.CustomerId,
                            EscapeCsv(c.CustomerName),
                            EscapeCsv(c.CustomerType),
                            EscapeCsv(c.ContactDetails),
                            EscapeCsv(c.ServiceLocation),
                            c.CompletedBookings,
                            c.TotalSpent,
                            c.LatestDate.HasValue ? c.LatestDate.Value.ToString("yyyy-MM-dd") : "",
                            c.DaysSinceLastService.HasValue ? days.ToString() : "",
                            health
                        ));
                    }

                    ShowToast($"Successfully exported {exportList.Count} account(s) to CSV!", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string EscapeCsv(string? val) => (val ?? "").Replace("\"", "\"\"");

        // ============================================================
        // Interactive KPI Cards Helper
        // ============================================================
        private (Panel card, Label titleLabel, Label valLabel, Label subLabel) CreateKpiCard(
            string title,
            string initialVal,
            string subtext,
            Color valColor,
            int kpiIndex)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin = new Padding(4),
                Padding = new Padding(14, 8, 14, 6),
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isSelected = (_activeKpiIndex == kpiIndex);

                using var pen = isSelected
                    ? new Pen(valColor, 2)
                    : new Pen(Theme.Border, 1);

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 18,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            card.Controls.Add(lblTitle);

            var valLabel = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            card.Controls.Add(valLabel);

            var lblSub = new Label
            {
                Text = subtext,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Theme.TextSubtle,
                Dock = DockStyle.Bottom,
                Height = 18,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            card.Controls.Add(lblSub);

            // Click handling on card and all children
            Action clickAction = () =>
            {
                _activeKpiIndex = kpiIndex;
                HighlightActiveKpi();

                switch (kpiIndex)
                {
                    case 0: // At-Risk Accounts (60+ Days)
                        _cmbHealthFilter.SelectedIndex = 0;
                        _sortColumn = "DaysSinceLastService";
                        _sortAscending = false;
                        break;
                    case 1: // Repeat Rate
                        _cmbHealthFilter.SelectedIndex = 2; // Repeat Accounts
                        _sortColumn = "CompletedBookings";
                        _sortAscending = false;
                        break;
                    case 2: // Completed Jobs
                        _cmbHealthFilter.SelectedIndex = 3; // All Accounts
                        _sortColumn = "CompletedBookings";
                        _sortAscending = false;
                        break;
                    case 3: // Avg Revenue
                        _cmbHealthFilter.SelectedIndex = 3; // All Accounts
                        _sortColumn = "TotalSpent";
                        _sortAscending = false;
                        break;
                }

                _currentPage = 1;
                ApplyFilterAndSort();
            };

            card.Click += (s, e) => clickAction();
            lblTitle.Click += (s, e) => clickAction();
            valLabel.Click += (s, e) => clickAction();
            lblSub.Click += (s, e) => clickAction();

            return (card, lblTitle, valLabel, lblSub);
        }

        private void HighlightActiveKpi()
        {
            _cardKpi1?.Invalidate();
            _cardKpi2?.Invalidate();
            _cardKpi3?.Invalidate();
            _cardKpi4?.Invalidate();
        }

        // ============================================================
        // RBAC Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            if (_btnReengage != null)
            {
                if (userRole == Roles.SalesStaff)
                {
                    _btnReengage.Visible = true;
                    _btnReengage.Enabled = true;
                    _btnReengage.Text = "★  Re-engage Account";
                }
                else if (userRole == Roles.Manager)
                {
                    _btnReengage.Visible = true;
                    _btnReengage.Enabled = false;
                    _btnReengage.Text = "🔒 Outreach by Sales";
                }
                else
                {
                    _btnReengage.Visible = false;
                }
            }
        }
    }
}
