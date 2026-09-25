using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 3 — Field Service Dispatch & Scheduling Command Center.
    ///
    /// Industry-Level Scheduling & Dispatch:
    ///   - Default View = "All" (Show data first, filter second).
    ///   - Tabs: [All] [Today] [Tomorrow] [This Week] [Pending] [Completed].
    ///   - Date Range Filter: From [Date ▼] To [Date ▼] [Apply] [Clear].
    ///   - Full Pagination: 20 rows/page, Page X of Y, [< Prev] [Next >], Rows per page selector.
    ///   - Default Sort: Scheduled Date DESC (newest first).
    ///   - Empty state placeholder with 1-click filter reset.
    ///   - Header totals: "Showing X of Y bookings".
    /// </summary>
    public class ScheduleView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<WorkOrderDto> _allWorkOrders = new();
        private List<WorkOrderDto> _filteredOrders = new();
        private List<string> _staffList = new();

        // Active State
        private string _activeViewMode = "All"; // Default is "All"
        private bool _isCustomDateRangeActive = false;
        private DateTime _customFromDate = DateTime.Today.AddDays(-30);
        private DateTime _customToDate = DateTime.Today.AddDays(30);

        // Pagination State
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;

        // Header Controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnNewBooking = null!;

        // KPI Metric Badges (Ribbon)
        private Label _lblKpiTotal = null!;
        private Label _lblKpiScheduled = null!;
        private Label _lblKpiInProgress = null!;
        private Label _lblKpiCompleted = null!;
        private Label _lblKpiBacklog = null!;

        // View Mode Filter Buttons
        private readonly List<Button> _viewModeButtons = new();

        // Filters
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbStaffFilter = null!;
        private ComboBox _cmbStatusFilter = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private Button _btnApplyDateRange = null!;
        private Button _btnClearDateRange = null!;

        // Grid & Empty State
        private DataGridView _grid = null!;
        private Panel _pnlEmptyState = null!;
        private Label _lblEmptyTitle = null!;
        private Label _lblEmptySub = null!;
        private Button _btnResetFilters = null!;

        // Pagination Controls
        private Panel _pnlPagination = null!;
        private Label _lblPageInfo = null!;
        private Button _btnFirstPage = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Button _btnLastPage = null!;
        private FlowLayoutPanel _pnlPageNumbers = null!;
        private ComboBox _cmbPageSize = null!;

        private bool _hasLoaded;
        private static readonly Font _fontBold = new("Segoe UI", 8.5F, FontStyle.Bold);

        public ScheduleView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadScheduleAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadScheduleAsync();
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
                Width = 420,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlTitleBox);

            _lblTitle = new Label
            {
                Text = "Dispatch & Scheduling",
                Font = Theme.SubHeaderFont,
                ForeColor = Theme.TextDark,
                Location = new Point(0, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlTitleBox.Controls.Add(_lblTitle);

            _lblCount = new Label
            {
                Text = "Loading all bookings...",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(0, 34),
                AutoSize = true
            };
            pnlTitleBox.Controls.Add(_lblCount);

            // Right header actions
            _btnNewBooking = new Button
            {
                Text = "+ New Booking Request",
                Dock = DockStyle.Right,
                Width = 195,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewBooking);
            _btnNewBooking.Click += OnNewBookingClick;
            pnlHeader.Controls.Add(_btnNewBooking);

            var pnlSpH1 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpH1);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Dock = DockStyle.Right,
                Width = 95,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadScheduleAsync();
            pnlHeader.Controls.Add(_btnRefresh);

            // ── 2. Compact Dispatch Metrics Ribbon ──────────────────
            var pnlKpiRibbon = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
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

            _lblKpiTotal = CreateRibbonBadge(flowKpis, "📋  All Bookings: --", Color.FromArgb(30, 41, 59), Color.FromArgb(241, 245, 249));
            _lblKpiTotal.Cursor = Cursors.Hand;
            _lblKpiTotal.Click += (s, e) => SelectViewMode("All");

            _lblKpiScheduled = CreateRibbonBadge(flowKpis, "📅  Scheduled: --", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));
            _lblKpiInProgress = CreateRibbonBadge(flowKpis, "⚡  In-Progress: --", Color.FromArgb(79, 70, 229), Color.FromArgb(238, 242, 255));
            _lblKpiCompleted = CreateRibbonBadge(flowKpis, "✔  Completed: --", Color.FromArgb(22, 163, 74), Color.FromArgb(240, 253, 244));
            _lblKpiCompleted.Cursor = Cursors.Hand;
            _lblKpiCompleted.Click += (s, e) => SelectViewMode("Completed");

            _lblKpiBacklog = CreateRibbonBadge(flowKpis, "⚠  Pending Dispatch: --", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199));
            _lblKpiBacklog.Cursor = Cursors.Hand;
            _lblKpiBacklog.Click += (s, e) => SelectViewMode("Pending");

            // ── 3. View Mode Tabs Bar (Row 1) ───────────────────────
            var pnlModeTabsRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 6, 24, 4)
            };
            card.Controls.Add(pnlModeTabsRow);

            var flowModeTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Theme.Surface
            };
            pnlModeTabsRow.Controls.Add(flowModeTabs);

            // [All] is FIRST and DEFAULT!
            AddViewModeButton(flowModeTabs, "All", "All Bookings");
            AddViewModeButton(flowModeTabs, "Today", "Today");
            AddViewModeButton(flowModeTabs, "Tomorrow", "Tomorrow");
            AddViewModeButton(flowModeTabs, "Week", "This Week");
            AddViewModeButton(flowModeTabs, "Pending", "⚡ Pending Dispatch");
            AddViewModeButton(flowModeTabs, "Completed", "✔ Completed");

            // ── 4. Date Range & Search/Filter Bar (Row 2) ───────────
            var pnlFilterRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 6, 24, 6)
            };
            card.Controls.Add(pnlFilterRow);

            // Left: Date Range Filter
            var pnlDateRange = new Panel
            {
                Dock = DockStyle.Left,
                Width = 470,
                BackColor = Theme.Surface
            };
            pnlFilterRow.Controls.Add(pnlDateRange);

            var lblFrom = new Label
            {
                Text = "From:",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(0, 8),
                AutoSize = true
            };
            pnlDateRange.Controls.Add(lblFrom);

            _dtpFrom = new DateTimePicker
            {
                Location = new Point(40, 5),
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Font = Theme.BodyFont,
                Value = _customFromDate
            };
            pnlDateRange.Controls.Add(_dtpFrom);

            var lblTo = new Label
            {
                Text = "To:",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(164, 8),
                AutoSize = true
            };
            pnlDateRange.Controls.Add(lblTo);

            _dtpTo = new DateTimePicker
            {
                Location = new Point(190, 5),
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Font = Theme.BodyFont,
                Value = _customToDate
            };
            pnlDateRange.Controls.Add(_dtpTo);

            _btnApplyDateRange = new Button
            {
                Text = "Apply",
                Location = new Point(314, 4),
                Size = new Size(65, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnApplyDateRange.FlatAppearance.BorderSize = 0;
            _btnApplyDateRange.Click += (s, e) =>
            {
                _isCustomDateRangeActive = true;
                _customFromDate = _dtpFrom.Value.Date;
                _customToDate = _dtpTo.Value.Date;
                _currentPage = 1;
                ApplyFilter();
            };
            pnlDateRange.Controls.Add(_btnApplyDateRange);

            _btnClearDateRange = new Button
            {
                Text = "Clear",
                Location = new Point(384, 4),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            _btnClearDateRange.FlatAppearance.BorderSize = 0;
            _btnClearDateRange.Click += (s, e) =>
            {
                _isCustomDateRangeActive = false;
                _currentPage = 1;
                ApplyFilter();
            };
            pnlDateRange.Controls.Add(_btnClearDateRange);

            // Right: Filters (Status, Crew, Search)
            var pnlSearchFilters = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            pnlFilterRow.Controls.Add(pnlSearchFilters);
            pnlSearchFilters.BringToFront();

            // Search box with clear button
            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Right,
                Width = 220,
                Height = 30,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(8, 4, 6, 4)
            };
            pnlSearchBox.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearchBox.Width - 1, pnlSearchBox.Height - 1);
            };
            pnlSearchFilters.Controls.Add(pnlSearchBox);

            _btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 20,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 7F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnClearSearch.FlatAppearance.BorderSize = 0;
            _btnClearSearch.Click += (s, e) => { _txtSearch.Text = ""; _btnClearSearch.Visible = false; };
            pnlSearchBox.Controls.Add(_btnClearSearch);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = Theme.BodyFont,
                PlaceholderText = "🔍  Search..."
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                _currentPage = 1;
                ApplyFilter();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSpF1 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlSearchFilters.Controls.Add(pnlSpF1);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 130,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[]
            {
                "All Statuses",
                "Scheduled",
                "InProgress",
                "Completed",
                "Requested",
                "Rescheduled"
            });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; ApplyFilter(); };
            pnlSearchFilters.Controls.Add(_cmbStatusFilter);

            var pnlSpF2 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlSearchFilters.Controls.Add(pnlSpF2);

            _cmbStaffFilter = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 140,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStaffFilter.Items.Add("All Technicians");
            _cmbStaffFilter.SelectedIndex = 0;
            _cmbStaffFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; ApplyFilter(); };
            pnlSearchFilters.Controls.Add(_cmbStaffFilter);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── 5. Pagination Bar (Bottom) ───────────────────────────
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
                Text = "Showing 0 bookings",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Left,
                Width = 320,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlPagination.Controls.Add(_lblPageInfo);

            // Right: Pagination Controls
            var pnlPageControls = new Panel
            {
                Dock = DockStyle.Right,
                Width = 480,
                BackColor = Color.Transparent
            };
            _pnlPagination.Controls.Add(pnlPageControls);

            var lblPageSize = new Label
            {
                Text = "Rows:",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(0, 10),
                AutoSize = true
            };
            pnlPageControls.Controls.Add(lblPageSize);

            _cmbPageSize = new ComboBox
            {
                Location = new Point(42, 6),
                Width = 70,
                Font = Theme.CaptionFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPageSize.Items.AddRange(new object[] { "10", "20", "50", "100" });
            _cmbPageSize.SelectedIndex = 1; // 20 per page default
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

            // ── 6. Full-Width Grid & Empty State Placeholder ────────
            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            card.Controls.Add(pnlGridContainer);

            // Empty state overlay
            _pnlEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false
            };
            pnlGridContainer.Controls.Add(_pnlEmptyState);

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
                Text = "No bookings found",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(_lblEmptyTitle);

            _lblEmptySub = new Label
            {
                Text = "No bookings match your selected timeframe, filters, or search term.",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlEmptyState.Controls.Add(_lblEmptySub);

            _btnResetFilters = new Button
            {
                Text = "Reset All Filters",
                Size = new Size(140, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnResetFilters.FlatAppearance.BorderSize = 0;
            _btnResetFilters.Click += (s, e) => ResetAllFilters();
            _pnlEmptyState.Controls.Add(_btnResetFilters);
            _pnlEmptyState.Resize += (s, e) =>
            {
                _btnResetFilters.Location = new Point((_pnlEmptyState.Width - _btnResetFilters.Width) / 2, 160);
            };

            // Grid
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
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            // Columns
            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Scheduled Date", Width = 125, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "Order #",        Width = 90,  MinimumWidth = 75  },
                new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "Customer",        Width = 200, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colService",  HeaderText = "Service Type",   Width = 150, MinimumWidth = 110 },
                new DataGridViewTextBoxColumn { Name = "colStaff",    HeaderText = "Assigned Crew",  Width = 140, MinimumWidth = 110 },
                new DataGridViewTextBoxColumn { Name = "colPrice",    HeaderText = "Price",          Width = 120, MinimumWidth = 95  },
                new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "Status",         Width = 115, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colRating",   HeaderText = "Rating & QA",    Width = 135, MinimumWidth = 100 },
                new DataGridViewButtonColumn  { Name = "colDispatch", HeaderText = "Action",         Width = 105, MinimumWidth = 90,
                    Text = "⚡ Manage", UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat }
            });

            if (_grid.Columns["colId"] is { } colId)
                colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colPrice"] is { } colPrice)
                colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colStatus"] is { } colStatus)
                colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.CellClick += OnGridCellClick;
            _grid.CellDoubleClick += OnGridCellDoubleClick;

            pnlGridContainer.Controls.Add(_grid);

            // Z-Order layout inside card
            card.Controls.SetChildIndex(pnlGridContainer, 0);
            card.Controls.SetChildIndex(_pnlPagination, 1);
            card.Controls.SetChildIndex(pnlDivider, 2);
            card.Controls.SetChildIndex(pnlFilterRow, 3);
            card.Controls.SetChildIndex(pnlModeTabsRow, 4);
            card.Controls.SetChildIndex(pnlKpiRibbon, 5);
            card.Controls.SetChildIndex(pnlHeader, 6);

            ResumeLayout(false);
        }

        // ============================================================
        // Helpers for Controls
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
                Margin = new Padding(0, 1, 10, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            container.Controls.Add(lbl);
            return lbl;
        }

        private void AddViewModeButton(FlowLayoutPanel container, string modeKey, string label)
        {
            bool isDefault = (modeKey == _activeViewMode);
            var btn = new Button
            {
                Text = label,
                Tag = modeKey,
                Height = 32,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = isDefault ? Theme.Primary : Color.FromArgb(241, 245, 249),
                ForeColor = isDefault ? Color.White : Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SelectViewMode(modeKey);
            _viewModeButtons.Add(btn);
            container.Controls.Add(btn);
        }

        private Button CreatePageNavButton(string text, int width, Control parent, Action onClick)
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
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
            return btn;
        }

        private void SelectViewMode(string modeKey)
        {
            _activeViewMode = modeKey;
            foreach (var btn in _viewModeButtons)
            {
                bool isActive = (btn.Tag?.ToString() == modeKey);
                btn.BackColor = isActive ? Theme.Primary : Color.FromArgb(241, 245, 249);
                btn.ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105);
            }
            _currentPage = 1;
            ApplyFilter();
        }

        private void ResetAllFilters()
        {
            _txtSearch.Text = "";
            _btnClearSearch.Visible = false;
            _cmbStaffFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndex = 0;
            _isCustomDateRangeActive = false;
            SelectViewMode("All");
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadScheduleAsync()
        {
            if (IsDisposed || Disposing) return;
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Refreshing bookings...";

            try
            {
                var ordersTask = _api.GetWorkOrdersAsync();
                var staffTask = _api.GetAvailableStaffAsync();

                await Task.WhenAll(ordersTask, staffTask);

                _allWorkOrders = await ordersTask;
                _staffList = await staffTask;

                if (!IsDisposed && !Disposing)
                {
                    PopulateStaffFilter();
                    UpdateMetrics();
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    ShowToast($"Failed to load schedule: {ex.Message}", false);
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

        private void PopulateStaffFilter()
        {
            string currentSelection = _cmbStaffFilter.SelectedItem?.ToString() ?? "All Technicians";
            _cmbStaffFilter.Items.Clear();
            _cmbStaffFilter.Items.Add("All Technicians");

            foreach (var staff in _staffList.Distinct().OrderBy(s => s))
            {
                if (!string.IsNullOrWhiteSpace(staff))
                {
                    _cmbStaffFilter.Items.Add(staff);
                }
            }

            int idx = _cmbStaffFilter.Items.IndexOf(currentSelection);
            _cmbStaffFilter.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void UpdateMetrics()
        {
            int total = _allWorkOrders.Count;
            int scheduledCount = _allWorkOrders.Count(o => o.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase));
            int inProgress = _allWorkOrders.Count(o => o.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase));
            int completed = _allWorkOrders.Count(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
            int backlog = _allWorkOrders.Count(o => o.Status.Equals("Requested", StringComparison.OrdinalIgnoreCase));

            _lblKpiTotal.Text = $"📋  All Bookings: {total}";
            _lblKpiScheduled.Text = $"📅  Scheduled: {scheduledCount}";
            _lblKpiInProgress.Text = $"⚡  In-Progress: {inProgress}";
            _lblKpiCompleted.Text = $"✔  Completed: {completed}";
            _lblKpiBacklog.Text = $"⚠  Pending Dispatch: {backlog}";

            var pendingBtn = _viewModeButtons.FirstOrDefault(b => b.Tag?.ToString() == "Pending");
            if (pendingBtn != null)
            {
                pendingBtn.Text = backlog > 0 ? $"⚡ Pending Dispatch ({backlog})" : "⚡ Pending Dispatch";
            }
        }

        // ============================================================
        // Filtering & Sorting (Default DESC)
        // ============================================================
        private void ApplyFilter()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ApplyFilter));
                return;
            }

            var today = DateTime.Today;

            // 1. Timeframe filter
            IEnumerable<WorkOrderDto> query = _allWorkOrders;

            switch (_activeViewMode)
            {
                case "Today":
                    query = query.Where(o => o.PreferredDate.Date == today);
                    break;
                case "Tomorrow":
                    query = query.Where(o => o.PreferredDate.Date == today.AddDays(1));
                    break;
                case "Week":
                    query = query.Where(o => o.PreferredDate.Date >= today && o.PreferredDate.Date <= today.AddDays(7));
                    break;
                case "Pending":
                    query = query.Where(o => o.Status.Equals("Requested", StringComparison.OrdinalIgnoreCase));
                    break;
                case "Completed":
                    query = query.Where(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
                    break;
                default: // "All"
                    // Show all active bookings
                    break;
            }

            // 2. Custom Date Range filter
            if (_isCustomDateRangeActive)
            {
                query = query.Where(o => o.PreferredDate.Date >= _customFromDate && o.PreferredDate.Date <= _customToDate);
            }

            // 3. Status filter
            string selectedStatus = _cmbStatusFilter.SelectedItem?.ToString() ?? "All Statuses";
            if (selectedStatus != "All Statuses")
            {
                query = query.Where(o => string.Equals(o.Status, selectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Staff filter
            string selectedStaff = _cmbStaffFilter.SelectedItem?.ToString() ?? "All Technicians";
            if (selectedStaff != "All Technicians")
            {
                query = query.Where(o => string.Equals(o.AssignedStaff, selectedStaff, StringComparison.OrdinalIgnoreCase));
            }

            // 5. Search text filter
            string search = _txtSearch.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.ToLowerInvariant().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.AssignedStaff) && o.AssignedStaff.ToLowerInvariant().Contains(search)) ||
                    (!string.IsNullOrEmpty(o.ServiceType) && o.ServiceType.ToLowerInvariant().Contains(search)) ||
                    ($"wo-{o.ServiceRequestId:d4}".Contains(search)));
            }

            // 6. DEFAULT SORT: Scheduled Date DESC (Newest/Latest first)
            _filteredOrders = query
                .OrderByDescending(o => o.PreferredDate)
                .ThenByDescending(o => o.ServiceRequestId)
                .ToList();

            // Calculate total pages
            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredOrders.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            // Update header count
            string rangeNote = _isCustomDateRangeActive ? $" [{_customFromDate:MMM dd} - {_customToDate:MMM dd}]" : "";
            _lblCount.Text = $"Showing {_filteredOrders.Count} of {_allWorkOrders.Count} bookings in {_activeViewMode}{rangeNote}";

            RenderPage();
        }

        // ============================================================
        // Pagination & Grid Rendering
        // ============================================================
        private void GoToPage(int page)
        {
            if (page < 1 || page > _totalPages) return;
            _currentPage = page;
            RenderPage();
        }

        private void RenderPage()
        {
            if (_filteredOrders.Count == 0)
            {
                _grid.Visible = false;
                _pnlEmptyState.Visible = true;
                _lblEmptyTitle.Text = $"No bookings found in '{_activeViewMode}'";
                _lblPageInfo.Text = "Showing 0 bookings";
                _btnFirstPage.Enabled = false;
                _btnPrevPage.Enabled = false;
                _btnNextPage.Enabled = false;
                _btnLastPage.Enabled = false;
                _pnlPageNumbers.Controls.Clear();
                return;
            }

            _pnlEmptyState.Visible = false;
            _grid.Visible = true;

            int skip = (_currentPage - 1) * _pageSize;
            var pageItems = _filteredOrders.Skip(skip).Take(_pageSize).ToList();

            int startRow = skip + 1;
            int endRow = skip + pageItems.Count;
            _lblPageInfo.Text = $"Showing {startRow}–{endRow} of {_filteredOrders.Count} bookings (Page {_currentPage} of {_totalPages})";

            // Update Nav Buttons
            _btnFirstPage.Enabled = _currentPage > 1;
            _btnPrevPage.Enabled = _currentPage > 1;
            _btnNextPage.Enabled = _currentPage < _totalPages;
            _btnLastPage.Enabled = _currentPage < _totalPages;

            // Render Page Number Buttons (Show window of 5 pages)
            UpdatePageNumberButtons();

            // Bind Grid Rows
            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                var rows = new List<DataGridViewRow>(pageItems.Count);

                foreach (var o in pageItems)
                {
                    string priceDisplay = o.ActualPrice.HasValue
                        ? $"₱{o.ActualPrice.Value:N2}"
                        : (o.QuotedPrice.HasValue ? $"₱{o.QuotedPrice.Value:N2} (est)" : "—");

                    string ratingDisplay = "—";
                    if (o.Rating.HasValue)
                    {
                        string stars = new string('⭐', Math.Clamp(o.Rating.Value, 1, 5));
                        string verdict = !string.IsNullOrEmpty(o.InspectionStatus) ? $" [{o.InspectionStatus}]" : "";
                        ratingDisplay = $"{stars}{verdict}";
                    }

                    var row = new DataGridViewRow();
                    row.CreateCells(_grid,
                        o.PreferredDate.ToString("yyyy-MM-dd (ddd)"),
                        $"WO-{o.ServiceRequestId:D4}",
                        o.CustomerName,
                        o.ServiceType,
                        !string.IsNullOrEmpty(o.AssignedStaff) ? o.AssignedStaff : "⚠️ Unassigned",
                        priceDisplay,
                        o.Status,
                        ratingDisplay,
                        "⚡ Manage"
                    );

                    row.Tag = o;

                    var cellStatus = row.Cells[6];
                    ApplyStatusCellStyle(cellStatus, o.Status);

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

        private static void ApplyStatusCellStyle(DataGridViewCell cell, string status)
        {
            cell.Style.Font = _fontBold;
            switch (status)
            {
                case "Completed":
                    cell.Style.ForeColor = Color.FromArgb(22, 163, 74);
                    cell.Style.BackColor = Color.FromArgb(240, 253, 244);
                    break;
                case "InProgress":
                    cell.Style.ForeColor = Color.FromArgb(79, 70, 229);
                    cell.Style.BackColor = Color.FromArgb(238, 242, 255);
                    break;
                case "Scheduled":
                    cell.Style.ForeColor = Color.FromArgb(37, 99, 235);
                    cell.Style.BackColor = Color.FromArgb(239, 246, 255);
                    break;
                case "Requested":
                    cell.Style.ForeColor = Color.FromArgb(180, 83, 9);
                    cell.Style.BackColor = Color.FromArgb(254, 243, 199);
                    break;
                case "Rescheduled":
                    cell.Style.ForeColor = Color.FromArgb(147, 51, 234);
                    cell.Style.BackColor = Color.FromArgb(250, 245, 255);
                    break;
                default:
                    cell.Style.ForeColor = Color.FromArgb(100, 116, 139);
                    break;
            }
        }

        // ============================================================
        // Grid Interaction
        // ============================================================
        private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name == "colDispatch" && _grid.Rows[e.RowIndex].Tag is WorkOrderDto o)
            {
                OpenDispatchDialog(o);
            }
        }

        private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Rows[e.RowIndex].Tag is WorkOrderDto o)
            {
                OpenDispatchDialog(o);
            }
        }

        private void OpenDispatchDialog(WorkOrderDto order)
        {
            using var dialog = new DispatchWorkOrderDialog(order);
            dialog.WorkOrderUpdated += async (updated) =>
            {
                ShowToast($"Work Order WO-{order.ServiceRequestId:D4} updated.", true);
                await LoadScheduleAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        private void OnNewBookingClick(object? sender, EventArgs e)
        {
            using var dialog = new NewWorkOrderDialog();
            dialog.WorkOrderSaved += async () =>
            {
                ShowToast("New booking request scheduled successfully!", true);
                await LoadScheduleAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        // ============================================================
        // Role-Based Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            // Use Case 4: Scheduling & Dispatch
            // Manager: FULL (Create bookings, assign crew, dispatch, execute operations)
            // Sales Staff: PARTIAL (Create booking requests, view schedule; cannot dispatch crew)
            // Super Admin: VIEW (Read-only schedule view)
            // Admin: VIEW (Read-only schedule view)
            bool canBook = (userRole == Roles.Manager || userRole == Roles.SalesStaff);
            if (_btnNewBooking != null) _btnNewBooking.Visible = canBook;

            if (_grid.Columns["colDispatch"] is DataGridViewButtonColumn colDispatch)
            {
                colDispatch.Text = (userRole == Roles.Manager) ? "⚡ Manage" : "🔍 View";
            }
        }
    }
}
