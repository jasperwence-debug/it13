using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 2 — Customer Directory & Account Management (Client & Contract).
    ///
    /// Industry-level features:
    ///   - Clickable KPI Metric Badges for instant 1-click filtering.
    ///   - Fast Segment Pills with real-time row counts in each tab.
    ///   - Full Pagination with page size selector (10, 20, 50, 100) and page jump buttons.
    ///   - Programmatic column sorting on all columns (Name, Spend, Date, etc.).
    ///   - Real-time search with clear button and dedicated Empty-State overlay with Reset action.
    ///   - Un-truncated phone, email, and dates; consistent currency formatting with 2 decimals.
    ///   - Multi-row bulk selection checkboxes with Batch CSV Export.
    ///   - 1-click Customer Service History dialog and double-click row inspect.
    ///   - Strict Use Case RBAC permissions.
    /// </summary>
    public class CustomersView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _customers = new();
        private List<CustomerSummaryDto> _filteredCustomers = new();
        private readonly HashSet<int> _selectedCustomerIds = new();

        // Pagination state
        private int _pageSize = 25;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // Sorting state (default: newest first)
        private string _sortColumn = "CustomerId";
        private bool _sortAscending = false;

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnViewLeads = null!;
        private Button _btnNew = null!;

        public Button btnNew => _btnNew;

        // KPI Metric Badges (Clickable filters)
        private Label _lblKpiTotal = null!;
        private Label _lblKpiRepeat = null!;
        private Label _lblKpiAtRisk = null!;
        private Label _lblKpiLtv = null!;

        // Filter controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private readonly List<Button> _segmentButtons = new();
        private string _activeSegment = "All";

        // Batch Action Bar
        private Panel _pnlBatchBar = null!;
        private Label _lblBatchSelected = null!;
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
        private static readonly Font _fontSpentBold = new("Segoe UI", 9F, FontStyle.Bold);

        public CustomersView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadAsync();
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

            // ── 1. Top Header Bar ────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            card.Controls.Add(pnlHeader);

            var pnlTitleBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlTitleBox);

            _lblTitle = new Label
            {
                Text = "Customer Accounts & Contracts",
                Font = Theme.SubHeaderFont,
                ForeColor = Theme.TextDark,
                Location = new Point(0, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlTitleBox.Controls.Add(_lblTitle);

            _lblCount = new Label
            {
                Text = "Loading accounts...",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(0, 34),
                AutoSize = true
            };
            pnlTitleBox.Controls.Add(_lblCount);

            // Right header actions
            _btnNew = new Button
            {
                Text = "+ Inquire (New Lead)",
                Dock = DockStyle.Right,
                Width = 160,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnNew);
            _btnNew.Click += async (s, e) =>
            {
                using var dialog = new NewLeadDialog();
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    var choice = MessageBox.Show(
                        "New lead inquiry captured successfully!\n\nLeads are managed in the 'Leads & Inquiries' pipeline where you can quote pricing and convert them to formal customer accounts.\n\nWould you like to open Leads & Inquiries now?",
                        "Lead Captured",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (choice == DialogResult.Yes)
                    {
                        (FindForm() as MainForm)?.NavigateToKey("leads");
                    }
                    else
                    {
                        await LoadAsync();
                    }
                }
            };
            pnlHeader.Controls.Add(_btnNew);

            var pnlSpH1 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpH1);

            _btnViewLeads = new Button
            {
                Text = "🎯  Leads Pipeline",
                Dock = DockStyle.Right,
                Width = 140,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnViewLeads);
            _btnViewLeads.Click += (s, e) => (FindForm() as MainForm)?.NavigateToKey("leads");
            pnlHeader.Controls.Add(_btnViewLeads);

            var pnlSpH2 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpH2);

            _btnExportCsv = new Button
            {
                Text = "📥  Export All",
                Dock = DockStyle.Right,
                Width = 115,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnExportCsv);
            _btnExportCsv.Click += (s, e) => ExportCustomersToCsv(_customers, "All_Customers");
            pnlHeader.Controls.Add(_btnExportCsv);

            var pnlSpH3 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpH3);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Dock = DockStyle.Right,
                Width = 95,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadAsync();
            pnlHeader.Controls.Add(_btnRefresh);

            // ── 2. Clickable KPI Status Ribbon (1-Click Filters) ─────
            var pnlKpiRibbon = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 6, 24, 6)
            };
            pnlKpiRibbon.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, pnlKpiRibbon.Height - 1, pnlKpiRibbon.Width, pnlKpiRibbon.Height - 1);
            };
            card.Controls.Add(pnlKpiRibbon);

            var flowKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlKpiRibbon.Controls.Add(flowKpis);

            _lblKpiTotal = CreateRibbonBadge(flowKpis, "👥  Accounts: --", Color.FromArgb(30, 41, 59), Color.FromArgb(241, 245, 249));
            _lblKpiTotal.Click += (s, e) => SelectSegment("All");

            _lblKpiRepeat = CreateRibbonBadge(flowKpis, "★  Repeat: --", Color.FromArgb(22, 163, 74), Color.FromArgb(240, 253, 244));
            _lblKpiRepeat.Click += (s, e) => SelectSegment("Repeat");

            _lblKpiAtRisk = CreateRibbonBadge(flowKpis, "⚠  At-Risk: --", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242));
            _lblKpiAtRisk.Click += (s, e) => SelectSegment("At-Risk");

            _lblKpiLtv = CreateRibbonBadge(flowKpis, "💰  Total Revenue: --", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));
            _lblKpiLtv.Click += (s, e) => SelectSegment("VIP");

            // ── 3. Search Bar & Segment Pills with Live Counts ───────
            var pnlSearchRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 7, 24, 7)
            };
            card.Controls.Add(pnlSearchRow);

            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                Height = 34,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 6, 8, 6)
            };
            pnlSearchBox.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearchBox.Width - 1, pnlSearchBox.Height - 1);
            };
            pnlSearchRow.Controls.Add(pnlSearchBox);

            _btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 24,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnClearSearch.FlatAppearance.BorderSize = 0;
            _btnClearSearch.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _btnClearSearch.Visible = false;
            };
            pnlSearchBox.Controls.Add(_btnClearSearch);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                PlaceholderText = "🔍  Search by customer name, phone, email, or location..."
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                _currentPage = 1;
                ApplyFilter();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSegments = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(16, 1, 0, 0),
                BackColor = Theme.Surface
            };
            pnlSearchRow.Controls.Add(pnlSegments);
            pnlSegments.BringToFront();

            AddSegmentButton(pnlSegments, "All", "All Accounts");
            AddSegmentButton(pnlSegments, "Repeat", "★ Repeat (2+)");
            AddSegmentButton(pnlSegments, "At-Risk", "⚠ At-Risk (60d+)");
            AddSegmentButton(pnlSegments, "Commercial", "🏢 Commercial");
            AddSegmentButton(pnlSegments, "Residential", "🏠 Residential");
            AddSegmentButton(pnlSegments, "VIP", "💎 VIP (₱10k+)");

            // ── 4. Batch Selection Toolbar (Hidden by default) ────────
            _pnlBatchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(239, 246, 255), // Indigo-50
                Padding = new Padding(24, 6, 24, 6),
                Visible = false
            };
            _pnlBatchBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(191, 219, 254), 1);
                e.Graphics.DrawLine(pen, 0, _pnlBatchBar.Height - 1, _pnlBatchBar.Width, _pnlBatchBar.Height - 1);
            };
            card.Controls.Add(_pnlBatchBar);

            _lblBatchSelected = new Label
            {
                Text = "☑ 0 accounts selected",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0)
            };
            _pnlBatchBar.Controls.Add(_lblBatchSelected);

            _btnBatchClear = new Button
            {
                Text = "✕ Deselect All",
                Dock = DockStyle.Right,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchClear.FlatAppearance.BorderSize = 0;
            _btnBatchClear.Click += (s, e) => ClearBatchSelection();
            _pnlBatchBar.Controls.Add(_btnBatchClear);

            var pnlSpB = new Panel { Dock = DockStyle.Right, Width = 8 };
            _pnlBatchBar.Controls.Add(pnlSpB);

            _btnBatchExport = new Button
            {
                Text = "📥 Export Selected CSV",
                Dock = DockStyle.Right,
                Width = 160,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchExport.FlatAppearance.BorderSize = 0;
            _btnBatchExport.Click += (s, e) => ExportSelectedToCsv();
            _pnlBatchBar.Controls.Add(_btnBatchExport);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── 5. Bottom Pagination Bar ──────────────────────────────
            _pnlPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 6, 24, 6)
            };
            _pnlPagination.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, _pnlPagination.Width, 0);
            };
            card.Controls.Add(_pnlPagination);

            _lblPageInfo = new Label
            {
                Dock = DockStyle.Left,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 320
            };
            _pnlPagination.Controls.Add(_lblPageInfo);

            var pnlPageControls = new Panel
            {
                Dock = DockStyle.Right,
                Width = 460,
                BackColor = Color.Transparent
            };
            _pnlPagination.Controls.Add(pnlPageControls);

            var lblRows = new Label
            {
                Text = "Rows:",
                Location = new Point(0, 8),
                Width = 40,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlPageControls.Controls.Add(lblRows);

            _cmbPageSize = new ComboBox
            {
                Location = new Point(44, 6),
                Width = 70,
                Font = Theme.CaptionFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "100" });
            _cmbPageSize.SelectedIndex = 1; // 25 default
            _cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(_cmbPageSize.SelectedItem?.ToString(), out int sz))
                {
                    _pageSize = sz;
                    _currentPage = 1;
                    RenderPage();
                }
            };
            pnlPageControls.Controls.Add(_cmbPageSize);

            _btnLastPage = CreatePageNavButton("»", 32, pnlPageControls, () => GoToPage(_totalPages));
            _btnNextPage = CreatePageNavButton("Next >", 65, pnlPageControls, () => GoToPage(_currentPage + 1));

            _pnlPageNumbers = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 190,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlPageControls.Controls.Add(_pnlPageNumbers);

            _btnPrevPage = CreatePageNavButton("< Prev", 65, pnlPageControls, () => GoToPage(_currentPage - 1));
            _btnFirstPage = CreatePageNavButton("«", 32, pnlPageControls, () => GoToPage(1));

            // ── 6. Grid & Empty-State Container ──────────────────────
            _pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            card.Controls.Add(_pnlGridContainer);

            // Empty state overlay
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false
            };
            _pnlGridContainer.Controls.Add(_pnlEmptyState);

            var lblEmptyIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 36F),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.BottomCenter
            };
            _pnlEmptyState.Controls.Add(lblEmptyIcon);

            _lblEmptyTitle = new Label
            {
                Text = "No Customer Accounts Found",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(_lblEmptyTitle);

            _lblEmptySubtitle = new Label
            {
                Text = "No accounts match your current search query or segment filter.",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(_lblEmptySubtitle);

            var pnlResetWrap = new Panel { Dock = DockStyle.Top, Height = 45 };
            _pnlEmptyState.Controls.Add(pnlResetWrap);

            _btnResetFilters = new Button
            {
                Text = "↺  Reset All Filters",
                Width = 150,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnResetFilters.FlatAppearance.BorderSize = 0;
            _btnResetFilters.Location = new Point((_pnlEmptyState.Width - _btnResetFilters.Width) / 2, 6);
            _btnResetFilters.Anchor = AnchorStyles.Top;
            _btnResetFilters.Click += (s, e) => ResetFilters();
            pnlResetWrap.Controls.Add(_btnResetFilters);

            // DataGridView
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 42 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false
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

            // Add Columns
            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewCheckBoxColumn
                {
                    Name = "colSelect",
                    HeaderText = "☑",
                    Width = 38,
                    MinimumWidth = 38,
                    Resizable = DataGridViewTriState.False,
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colName",
                    HeaderText = "Customer Name",
                    Width = 190,
                    MinimumWidth = 140,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colType",
                    HeaderText = "Type",
                    Width = 125,
                    MinimumWidth = 95,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colContact",
                    HeaderText = "Phone / Email Contact",
                    Width = 220,
                    MinimumWidth = 170,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colLocation",
                    HeaderText = "Service Location",
                    Width = 200,
                    MinimumWidth = 150,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colBookings",
                    HeaderText = "Bookings",
                    Width = 90,
                    MinimumWidth = 70,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colSpent",
                    HeaderText = "Total Spent",
                    Width = 120,
                    MinimumWidth = 95,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colDate",
                    HeaderText = "Last Service",
                    Width = 135,
                    MinimumWidth = 115,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colRetention",
                    HeaderText = "Account Health",
                    Width = 140,
                    MinimumWidth = 110,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewButtonColumn
                {
                    Name = "colView",
                    HeaderText = "Action",
                    Width = 95,
                    MinimumWidth = 85,
                    Text = "History ↗",
                    UseColumnTextForButtonValue = true,
                    FlatStyle = FlatStyle.Flat,
                    ReadOnly = true
                }
            });

            if (_grid.Columns["colSelect"] is { } colSelect)
                colSelect.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colBookings"] is { } colBookings)
                colBookings.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colSpent"] is { } colSpent)
                colSpent.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colRetention"] is { } colRetention)
                colRetention.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellClick += OnGridCellClick;
            _grid.CellDoubleClick += OnGridCellDoubleClick;
            _grid.ColumnHeaderMouseClick += OnGridColumnHeaderClick;

            _pnlGridContainer.Controls.Add(_grid);

            // Z-Order layout inside card
            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(_pnlPagination, 1);
            card.Controls.SetChildIndex(pnlDivider, 2);
            card.Controls.SetChildIndex(_pnlBatchBar, 3);
            card.Controls.SetChildIndex(pnlSearchRow, 4);
            card.Controls.SetChildIndex(pnlKpiRibbon, 5);
            card.Controls.SetChildIndex(pnlHeader, 6);

            ResumeLayout(false);
        }

        // ============================================================
        // Ribbon Badge Helper
        // ============================================================
        private static Label CreateRibbonBadge(FlowLayoutPanel container, string text, Color foreColor, Color backColor)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = foreColor,
                BackColor = backColor,
                Height = 28,
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(0, 2, 10, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            container.Controls.Add(lbl);
            return lbl;
        }

        private static Button CreatePageNavButton(string text, int width, Control parent, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Right,
                Width = width,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
            return btn;
        }

        // ============================================================
        // Segment Buttons & Filtering
        // ============================================================
        private void AddSegmentButton(FlowLayoutPanel container, string key, string baseLabel)
        {
            var btn = new Button
            {
                Text = baseLabel,
                Tag = key,
                Height = 32,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = key == "All" ? Theme.Primary : Color.FromArgb(241, 245, 249),
                ForeColor = key == "All" ? Color.White : Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SelectSegment(key);
            _segmentButtons.Add(btn);
            container.Controls.Add(btn);
        }

        private void SelectSegment(string segmentKey)
        {
            _activeSegment = segmentKey;
            foreach (var btn in _segmentButtons)
            {
                bool isActive = (btn.Tag?.ToString() == segmentKey);
                btn.BackColor = isActive ? Theme.Primary : Color.FromArgb(241, 245, 249);
                btn.ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105);
            }
            _currentPage = 1;
            ApplyFilter();
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadAsync()
        {
            if (IsDisposed || Disposing) return;
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Refreshing customer accounts...";

            try
            {
                _customers = await _api.GetCustomersAsync();
                if (!IsDisposed && !Disposing)
                {
                    if (ApiClient.LastConnectionFailed)
                    {
                        _lblCount.Text = "⚠️ API Unreachable";
                        _lblCount.ForeColor = Color.FromArgb(220, 38, 38);
                        ShowToast("Cannot reach App.API at http://localhost:5000.", false);
                    }
                    else
                    {
                        _lblCount.ForeColor = Theme.TextMuted;
                    }

                    UpdateKpiMetrics();
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    ShowToast($"Failed to load customers: {ex.Message}", false);
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    _btnRefresh.Enabled = true;
                    _btnRefresh.Text = "↻  Refresh";
                }
            }
        }

        private void UpdateKpiMetrics()
        {
            int total = _customers.Count;
            int repeatCount = _customers.Count(c => c.CompletedBookings > 1);
            double repeatRate = total > 0 ? (repeatCount * 100.0 / total) : 0;
            int atRiskCount = _customers.Count(c => c.IsAtRisk);
            decimal totalLtv = _customers.Sum(c => c.TotalSpent);

            int commercialCount = _customers.Count(c => string.Equals(c.CustomerType, "Commercial", StringComparison.OrdinalIgnoreCase));
            int residentialCount = _customers.Count(c => string.Equals(c.CustomerType, "Residential", StringComparison.OrdinalIgnoreCase));
            int vipCount = _customers.Count(c => c.TotalSpent >= 10000);

            // Ribbon badges with 1-click filter hint
            _lblKpiTotal.Text = $"👥  {total:N0} Accounts (All)";
            _lblKpiRepeat.Text = $"★  {repeatRate:F0}% Repeat ({repeatCount})";
            _lblKpiAtRisk.Text = $"⚠  {atRiskCount:N0} At-Risk";
            _lblKpiLtv.Text = $"💰  ₱{totalLtv:N2} Total Spend";

            // Segment Tabs with Dynamic Row Counts
            foreach (var btn in _segmentButtons)
            {
                string tag = btn.Tag?.ToString() ?? "";
                btn.Text = tag switch
                {
                    "All"         => $"All Accounts ({total})",
                    "Repeat"      => $"★ Repeat ({repeatCount})",
                    "At-Risk"     => $"⚠ At-Risk ({atRiskCount})",
                    "Commercial"  => $"🏢 Commercial ({commercialCount})",
                    "Residential" => $"🏠 Residential ({residentialCount})",
                    "VIP"         => $"💎 VIP ({vipCount})",
                    _             => btn.Text
                };
            }
        }

        private void ApplyFilter()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ApplyFilter));
                return;
            }

            string filter = _txtSearch.Text.Trim().ToLowerInvariant();

            _filteredCustomers = _customers.FindAll(c =>
            {
                bool matchesText = string.IsNullOrEmpty(filter) ||
                    (!string.IsNullOrEmpty(c.CustomerName) && c.CustomerName.ToLowerInvariant().Contains(filter)) ||
                    (!string.IsNullOrEmpty(c.ContactDetails) && c.ContactDetails.ToLowerInvariant().Contains(filter)) ||
                    (!string.IsNullOrEmpty(c.ServiceLocation) && c.ServiceLocation.ToLowerInvariant().Contains(filter)) ||
                    (!string.IsNullOrEmpty(c.CustomerType) && c.CustomerType.ToLowerInvariant().Contains(filter));

                if (!matchesText) return false;

                return _activeSegment switch
                {
                    "Repeat"      => c.CompletedBookings > 1,
                    "At-Risk"     => c.IsAtRisk,
                    "Commercial"  => c.CustomerType.Equals("Commercial", StringComparison.OrdinalIgnoreCase),
                    "Residential" => c.CustomerType.Equals("Residential", StringComparison.OrdinalIgnoreCase),
                    "VIP"         => c.TotalSpent >= 10000,
                    _             => true
                };
            });

            ApplySorting();
            CalculatePagination();
            RenderPage();
        }

        private void ApplySorting()
        {
            _filteredCustomers = _sortColumn switch
            {
                "CustomerId" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.CustomerId).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.CustomerId).ToList(),
                "CustomerName" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.CustomerName).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.CustomerName).ToList(),
                "CustomerType" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.CustomerType).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.CustomerType).ToList(),
                "ContactDetails" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.ContactDetails).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.ContactDetails).ToList(),
                "ServiceLocation" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.ServiceLocation).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.ServiceLocation).ToList(),
                "TotalBookings" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.TotalBookings).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.TotalBookings).ToList(),
                "TotalSpent" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.TotalSpent).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.TotalSpent).ToList(),
                "LatestDate" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.LatestDate ?? DateTime.MinValue).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.LatestDate ?? DateTime.MinValue).ToList(),
                "RetentionStatus" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.IsAtRisk).ThenBy(c => c.CompletedBookings).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.IsAtRisk).ThenByDescending(c => c.CompletedBookings).ToList(),
                _ => _filteredCustomers
            };
        }

        private void CalculatePagination()
        {
            int total = _filteredCustomers.Count;
            _totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)_pageSize));

            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;
        }

        private void GoToPage(int page)
        {
            if (page < 1 || page > _totalPages || page == _currentPage) return;
            _currentPage = page;
            RenderPage();
        }

        private void RenderPage()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RenderPage));
                return;
            }

            int total = _filteredCustomers.Count;
            bool hasRecords = (total > 0);

            _grid.Visible = hasRecords;
            _pnlEmptyState.Visible = !hasRecords;

            if (!hasRecords)
            {
                _lblCount.Text = "0 customer accounts matching";
                _lblPageInfo.Text = "No accounts to display";
                _pnlPageNumbers.Controls.Clear();
                _btnFirstPage.Enabled = _btnPrevPage.Enabled = _btnNextPage.Enabled = _btnLastPage.Enabled = false;
                return;
            }

            int startIndex = (_currentPage - 1) * _pageSize;
            var pageRecords = _filteredCustomers.Skip(startIndex).Take(_pageSize).ToList();
            int endIndex = startIndex + pageRecords.Count;

            _lblCount.Text = $"Showing {total} of {_customers.Count} customer account{(total == 1 ? "" : "s")}";
            _lblPageInfo.Text = $"Showing {startIndex + 1} to {endIndex} of {total} records.";

            // Update page buttons
            _btnFirstPage.Enabled = _btnPrevPage.Enabled = (_currentPage > 1);
            _btnNextPage.Enabled = _btnLastPage.Enabled = (_currentPage < _totalPages);
            UpdatePageNumberButtons();

            // Populate Grid Rows
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                var rows = new List<DataGridViewRow>(pageRecords.Count);

                foreach (var c in pageRecords)
                {
                    int days = Math.Max(0, c.DaysSinceLastService ?? 0);
                    string retentionDisplay = c.IsAtRisk
                        ? $"⚠ At-Risk ({days}d)"
                        : (c.CompletedBookings > 1 ? $"★ Repeat ({c.CompletedBookings})" : (c.CompletedBookings == 1 ? "Active" : "New"));

                    string bookingsDisplay = $"{c.CompletedBookings} done / {c.TotalBookings}";
                    string lastServiceDisplay = c.LatestDate.HasValue
                        ? c.LatestDate.Value.ToString("MMM dd, yyyy")
                        : "—";

                    // Consistent currency display with 2 decimal places
                    string spentDisplay = $"₱{c.TotalSpent:N2}";

                    bool isChecked = _selectedCustomerIds.Contains(c.CustomerId);

                    var row = new DataGridViewRow();
                    row.CreateCells(_grid,
                        isChecked,
                        c.CustomerName ?? "",
                        c.CustomerType ?? "Client",
                        c.ContactDetails ?? "—",
                        c.ServiceLocation ?? "—",
                        bookingsDisplay,
                        spentDisplay,
                        lastServiceDisplay,
                        retentionDisplay,
                        "History ↗"
                    );

                    row.Tag = c;

                    // Color code type
                    if (string.Equals(c.CustomerType, "Commercial", StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells[2].Style.ForeColor = Color.FromArgb(79, 70, 229);
                        row.Cells[2].Style.Font = _fontBold;
                    }
                    else
                    {
                        row.Cells[2].Style.ForeColor = Color.FromArgb(2, 132, 199);
                        row.Cells[2].Style.Font = _fontBold;
                    }

                    // Color code retention health
                    var cellRetention = row.Cells[8];
                    if (c.IsAtRisk)
                    {
                        cellRetention.Style.ForeColor = Color.FromArgb(220, 38, 38);
                        cellRetention.Style.BackColor = Color.FromArgb(254, 242, 242);
                        cellRetention.Style.Font = _fontBold;
                    }
                    else if (c.CompletedBookings > 1)
                    {
                        cellRetention.Style.ForeColor = Color.FromArgb(22, 163, 74);
                        cellRetention.Style.BackColor = Color.FromArgb(240, 253, 244);
                        cellRetention.Style.Font = _fontBold;
                    }
                    else if (c.CompletedBookings == 1)
                    {
                        cellRetention.Style.ForeColor = Color.FromArgb(37, 99, 235);
                        cellRetention.Style.BackColor = Color.FromArgb(239, 246, 255);
                    }
                    else
                    {
                        cellRetention.Style.ForeColor = Color.FromArgb(100, 116, 139);
                    }

                    // Highlight VIP high-value accounts (₱10k+)
                    if (c.TotalSpent >= 10000)
                    {
                        row.Cells[6].Style.ForeColor = Color.FromArgb(22, 163, 74);
                        row.Cells[6].Style.Font = _fontSpentBold;
                    }

                    rows.Add(row);
                }

                _grid.Rows.AddRange(rows.ToArray());
            }
            finally
            {
                _grid.ResumeLayout();
            }
        }

        private void UpdatePageNumberButtons()
        {
            _pnlPageNumbers.SuspendLayout();
            _pnlPageNumbers.Controls.Clear();

            int start = Math.Max(1, _currentPage - 2);
            int end = Math.Min(_totalPages, start + 4);
            if (end - start < 4)
            {
                start = Math.Max(1, end - 4);
            }

            for (int p = start; p <= end; p++)
            {
                int pageNum = p;
                bool isCurrent = (pageNum == _currentPage);

                var btn = new Button
                {
                    Text = pageNum.ToString(),
                    Width = 32,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isCurrent ? Theme.Primary : Color.FromArgb(241, 245, 249),
                    ForeColor = isCurrent ? Color.White : Color.FromArgb(71, 85, 105),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Margin = new Padding(2, 0, 2, 0),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => GoToPage(pageNum);
                _pnlPageNumbers.Controls.Add(btn);
            }

            _pnlPageNumbers.ResumeLayout();
        }

        // ============================================================
        // Sorting
        // ============================================================
        private void OnGridColumnHeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;

            string colName = _grid.Columns[e.ColumnIndex].Name;

            // Handle Header Checkbox click to Select All on Current Page
            if (colName == "colSelect")
            {
                ToggleSelectAllCurrentPage();
                return;
            }

            string? propName = colName switch
            {
                "colName"      => "CustomerName",
                "colType"      => "CustomerType",
                "colContact"   => "ContactDetails",
                "colLocation"  => "ServiceLocation",
                "colBookings"  => "TotalBookings",
                "colSpent"     => "TotalSpent",
                "colDate"      => "LatestDate",
                "colRetention" => "RetentionStatus",
                _              => null
            };

            if (propName == null) return;

            if (_sortColumn == propName)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = propName;
                _sortAscending = true;
            }

            _currentPage = 1;
            ApplySorting();
            RenderPage();
        }

        // ============================================================
        // Batch Selection
        // ============================================================
        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (_grid.Columns[e.ColumnIndex].Name == "colSelect")
            {
                if (_grid.Rows[e.RowIndex].Tag is CustomerSummaryDto c)
                {
                    bool isNowChecked = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells["colSelect"].Value ?? false);
                    if (isNowChecked)
                    {
                        _selectedCustomerIds.Add(c.CustomerId);
                    }
                    else
                    {
                        _selectedCustomerIds.Remove(c.CustomerId);
                    }
                    UpdateBatchBar();
                }
            }
        }

        private void ToggleSelectAllCurrentPage()
        {
            bool allCurrentSelected = true;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is CustomerSummaryDto c && !_selectedCustomerIds.Contains(c.CustomerId))
                {
                    allCurrentSelected = false;
                    break;
                }
            }

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is CustomerSummaryDto c)
                {
                    if (allCurrentSelected)
                    {
                        _selectedCustomerIds.Remove(c.CustomerId);
                        row.Cells["colSelect"].Value = false;
                    }
                    else
                    {
                        _selectedCustomerIds.Add(c.CustomerId);
                        row.Cells["colSelect"].Value = true;
                    }
                }
            }

            UpdateBatchBar();
        }

        private void ClearBatchSelection()
        {
            _selectedCustomerIds.Clear();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells["colSelect"].Value = false;
            }
            UpdateBatchBar();
        }

        private void UpdateBatchBar()
        {
            int count = _selectedCustomerIds.Count;
            _pnlBatchBar.Visible = (count > 0);
            _lblBatchSelected.Text = $"☑ {count} account{(count == 1 ? "" : "s")} selected";
        }

        private void ExportSelectedToCsv()
        {
            var selectedList = _customers.Where(c => _selectedCustomerIds.Contains(c.CustomerId)).ToList();
            if (selectedList.Count == 0)
            {
                ShowToast("No accounts selected for batch export.", false);
                return;
            }

            ExportCustomersToCsv(selectedList, $"Selected_{selectedList.Count}_Customers");
        }

        // ============================================================
        // Grid Interaction
        // ============================================================
        private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name == "colView")
            {
                OpenServiceHistory(e.RowIndex);
            }
        }

        private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenServiceHistory(e.RowIndex);
        }

        private void OpenServiceHistory(int rowIndex)
        {
            if (_grid.Rows[rowIndex].Tag is not CustomerSummaryDto customer) return;

            using var dialog = new CustomerServiceHistoryDialog(customer.CustomerId, customer.CustomerName);
            dialog.ShowDialog(FindForm());
        }

        private void ResetFilters()
        {
            _txtSearch.Text = string.Empty;
            _sortColumn = "CustomerId";
            _sortAscending = false;
            SelectSegment("All");
        }

        // ============================================================
        // CSV Export Feature
        // ============================================================
        private void ExportCustomersToCsv(List<CustomerSummaryDto> listToExport, string filePrefix)
        {
            if (listToExport.Count == 0)
            {
                ShowToast("No customer records to export.", false);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Title = "Export Customer Accounts"
            };

            if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Customer ID,Customer Name,Type,Contact Details,Location,Total Bookings,Completed Bookings,Total Spent,Last Service Date,Health Status");

                foreach (var c in listToExport)
                {
                    string id = c.CustomerId.ToString();
                    string name = EscapeCsv(c.CustomerName);
                    string type = EscapeCsv(c.CustomerType);
                    string contact = EscapeCsv(c.ContactDetails);
                    string loc = EscapeCsv(c.ServiceLocation);
                    string spent = c.TotalSpent.ToString("F2");
                    string date = c.LatestDate.HasValue ? c.LatestDate.Value.ToString("yyyy-MM-dd") : "";
                    string health = c.IsAtRisk ? "At-Risk" : (c.CompletedBookings > 1 ? "Repeat" : "Active");

                    sb.AppendLine($"{id},{name},{type},{contact},{loc},{c.TotalBookings},{c.CompletedBookings},{spent},{date},{health}");
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                ShowToast($"Exported {listToExport.Count} customer records to CSV successfully!", true);
            }
            catch (Exception ex)
            {
                ShowToast($"Export failed: {ex.Message}", false);
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        // ============================================================
        // Role-Based Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            // Use Case 3: Client & Contract Management
            // Sales Staff: FULL (+ Add Lead, Export, History)
            // Admin: PARTIAL (+ Add Lead, Export, History)
            // Super Admin: VIEW (Read-only, Export, History)
            // Manager: VIEW (Read-only, Export, History)
            if (_btnNew != null)
            {
                _btnNew.Visible = (userRole == Roles.SalesStaff || userRole == Roles.Admin);
            }
        }
    }
}
