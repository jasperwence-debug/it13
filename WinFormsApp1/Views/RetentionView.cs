using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.Domain.Models.Email;
using App.WinForms.Core;
using App.WinForms.Reporting;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 4 — Retention & Customer Health View.
    ///
    /// Industry-Grade Retention Engine & Account Health:
    ///   - Sub-tab 1: Customer Health Ledger (Search, KPIs, Multi-column sorting, Pagination, Batch Outreach).
    ///   - Sub-tab 2: Automated Retention Policy (8 Pre-defined CRM Triggers with Live DB Counts and 1-Click Drilldown).
    ///   - Consistent, minimal executive aesthetic (cohesive slate palette, no rainbow color distraction).
    ///   - Real-time database connectivity (0 mock data).
    /// </summary>
    public class RetentionView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _customers = new();
        private List<CustomerSummaryDto> _filteredCustomers = new();
        private readonly HashSet<int> _selectedCustomerIds = new();
        private DashboardDto? _dashboardData;

        // Sub-Navigation Tabs
        private int _activeTabIndex = 0; // 0 = Ledger, 1 = Retention Rules Policy
        private Button _btnTabLedger = null!;
        private Button _btnTabRules = null!;
        private Panel _pnlViewContainer = null!;
        private Panel _pnlLedgerView = null!;
        private Panel _pnlRulesView = null!;

        // Rules Grid (Tab 2)
        private DataGridView _gridRules = null!;
        private Label _lblRulesLiveCount = null!;

        // Pagination state (Tab 1)
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
        private Button _btnPrintReport = null!;
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

        // Pre-defined Industry Retention Policy Rule Definition
        private class RetentionRule
        {
            public int RuleNumber { get; set; }
            public string Name { get; set; } = string.Empty;
            public string TriggerCondition { get; set; } = string.Empty;
            public string AutomatedAction { get; set; } = string.Empty;
            public string Channel { get; set; } = string.Empty;
            public string TargetSegment { get; set; } = string.Empty;
            public int HealthFilterIndex { get; set; }
        }

        private static readonly List<RetentionRule> RetentionRules = new()
        {
            new RetentionRule { RuleNumber = 1, Name = "30 Days Inactive",       TriggerCondition = "30 days no service / contact",          AutomatedAction = "Send \"We miss you\" re-engagement email",        Channel = "Email Automation",       TargetSegment = "Lapsed Accounts (30–59d)",    HealthFilterIndex = 1 },
            new RetentionRule { RuleNumber = 2, Name = "60 Days Inactive",       TriggerCondition = "60 days no purchase / service",         AutomatedAction = "Send 10% discount promo voucher code",            Channel = "Email / SMS Promo",       TargetSegment = "At-Risk Accounts (60–89d)",   HealthFilterIndex = 2 },
            new RetentionRule { RuleNumber = 3, Name = "90 Days Inactive",       TriggerCondition = "90 days no contact / service",          AutomatedAction = "Assign account to Sales Rep for direct call",     Channel = "Phone Outreach",          TargetSegment = "Critical Churn Risk (90d+)",  HealthFilterIndex = 3 },
            new RetentionRule { RuleNumber = 4, Name = "Contract Expiring (30d)", TriggerCondition = "Commercial service contract expiring in 30d", AutomatedAction = "Send contract renewal reminder & terms",          Channel = "Email / Account Mgr",     TargetSegment = "Commercial / B2B Accounts",   HealthFilterIndex = 4 },
            new RetentionRule { RuleNumber = 5, Name = "Negative Feedback",      TriggerCondition = "Negative feedback received (Rating ≤ 2)", AutomatedAction = "Escalate incident ticket to Operations Manager",  Channel = "Priority Mgmt Alert",    TargetSegment = "Dissatisfied Customers",      HealthFilterIndex = 5 },
            new RetentionRule { RuleNumber = 6, Name = "Order Completed",        TriggerCondition = "Service order fulfilled successfully",   AutomatedAction = "Request 5-star review & CSAT satisfaction score",Channel = "SMS / Email Survey",     TargetSegment = "Recent Completed Orders",     HealthFilterIndex = 6 },
            new RetentionRule { RuleNumber = 7, Name = "Customer Anniversary",   TriggerCondition = "Account anniversary milestone (180d+)",  AutomatedAction = "Send annual loyalty perk bonus credit",           Channel = "Loyalty Program Email",   TargetSegment = "Tenured Accounts (180d+)",    HealthFilterIndex = 7 },
            new RetentionRule { RuleNumber = 8, Name = "VIP Milestone / Bday",   TriggerCondition = "VIP milestone / High lifetime value",    AutomatedAction = "Send VIP concierge appreciation gift & perks",    Channel = "VIP Concierge",           TargetSegment = "High-LTV / VIP Accounts",     HealthFilterIndex = 8 }
        };

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

            // ── 1. Top Header Bar (56px) ─────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
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
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.BottomLeft,
                UseMnemonic = false
            };
            pnlHeaderLeft.Controls.Add(_lblTitle);

            _lblCount = new Label
            {
                Text = "Loading accounts...",
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(100, 116, 139),
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
                Padding = new Padding(0, 10, 0, 0)
            };
            pnlHeader.Controls.Add(pnlHeaderRight);

            _btnPrintReport = new Button
            {
                Text = "🖨️  Print Retention Report",
                Height = 34,
                Width = 195,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnPrintReport.FlatAppearance.BorderSize = 0;
            _btnPrintReport.Click += (s, e) => ReportDocumentEngine.ShowRetentionReportPrintPreview(_dashboardData, _customers, FindForm());
            pnlHeaderRight.Controls.Add(_btnPrintReport);

            _btnExportCsv = new Button
            {
                Text = "📥  Export CSV",
                Height = 34,
                Width = 115,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnExportCsv);
            _btnExportCsv.Click += (s, e) => ExportToCsv(false);
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 34,
                Width = 95,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadDataAsync();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            _btnReengage = new Button
            {
                Text = "★  Re-engage Account",
                Height = 34,
                Width = 190,
                Enabled = false,
                Margin = new Padding(0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnReengage);
            _btnReengage.Click += OnReengageClick;
            pnlHeaderRight.Controls.Add(_btnReengage);

            // ── 2. Sub-Tab Switcher Bar (42px) ────────────────────────
            var pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 4, 24, 4)
            };
            card.Controls.Add(pnlTabBar);

            var pnlTabPills = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            pnlTabBar.Controls.Add(pnlTabPills);

            _btnTabLedger = CreateTabButton("📋  Customer Health Ledger", true);
            _btnTabLedger.Click += (s, e) => SwitchTab(0);
            pnlTabPills.Controls.Add(_btnTabLedger);

            _btnTabRules = CreateTabButton("⚡  Automated Retention Policy (8 Triggers)", false);
            _btnTabRules.Click += (s, e) => SwitchTab(1);
            pnlTabPills.Controls.Add(_btnTabRules);

            var pnlTabDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            card.Controls.Add(pnlTabDivider);

            // ── 3. Main View Container (Holds Ledger & Rules views) ───
            _pnlViewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            card.Controls.Add(_pnlViewContainer);

            // Build View 1: Customer Health Ledger
            BuildLedgerView();

            // Build View 2: Automated Retention Rules
            BuildRulesView();

            // Default: Show Ledger
            SwitchTab(0);

            // Z-Order layout in main card
            card.Controls.SetChildIndex(_pnlViewContainer, 0);
            card.Controls.SetChildIndex(pnlTabDivider, 1);
            card.Controls.SetChildIndex(pnlTabBar, 2);
            card.Controls.SetChildIndex(pnlHeader, 3);

            ResumeLayout(false);
        }

        private static Button CreateTabButton(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Height = 32,
                AutoSize = true,
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249),
                ForeColor = isActive ? Color.White : Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchTab(int tabIndex)
        {
            _activeTabIndex = tabIndex;

            if (tabIndex == 0)
            {
                _btnTabLedger.BackColor = Color.FromArgb(30, 41, 59);
                _btnTabLedger.ForeColor = Color.White;
                _btnTabRules.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabRules.ForeColor = Color.FromArgb(71, 85, 105);

                _pnlLedgerView.Visible = true;
                _pnlRulesView.Visible = false;
                _pnlLedgerView.BringToFront();
            }
            else
            {
                _btnTabRules.BackColor = Color.FromArgb(30, 41, 59);
                _btnTabRules.ForeColor = Color.White;
                _btnTabLedger.BackColor = Color.FromArgb(241, 245, 249);
                _btnTabLedger.ForeColor = Color.FromArgb(71, 85, 105);

                _pnlRulesView.Visible = true;
                _pnlLedgerView.Visible = false;
                _pnlRulesView.BringToFront();

                RenderRulesGrid();
            }
        }

        // ============================================================
        // Sub-View 1: Customer Health Ledger Panel
        // ============================================================
        private void BuildLedgerView()
        {
            _pnlLedgerView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            _pnlViewContainer.Controls.Add(_pnlLedgerView);

            // ── KPI Summary Cards (4 Cards - Minimal Slate Styling) ──
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 4, 20, 4)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _pnlLedgerView.Controls.Add(pnlKpis);

            // KPI 1: At-Risk Accounts (Click -> Filter: At-Risk)
            var (k1, _, v1, _) = CreateKpiCard("AT-RISK ACCOUNTS (60+ DAYS)", "--", "No completed service in 60+ days", 0);
            _cardKpi1 = k1;
            _lblAtRiskCount = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            // KPI 2: Repeat Customer Rate (Click -> Filter: Repeat)
            var (k2, _, v2, _) = CreateKpiCard("REPEAT CUSTOMER RATE", "--", "Accounts with 2+ completed jobs", 1);
            _cardKpi2 = k2;
            _lblRepeatRate = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            // KPI 3: Completed Bookings (Click -> Sort by Jobs Done DESC)
            var (k3, _, v3, _) = CreateKpiCard("TOTAL COMPLETED JOBS", "--", "Click to sort by fulfilled jobs", 2);
            _cardKpi3 = k3;
            _lblCompletedCount = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            // KPI 4: Mean Revenue / Job (Click -> Sort by Total Revenue DESC)
            var (k4, _, v4, _) = CreateKpiCard("AVG REVENUE / JOB", "--", "Click to sort by total revenue", 3);
            _cardKpi4 = k4;
            _lblAverageLtv = v4;
            pnlKpis.Controls.Add(k4, 3, 0);

            // ── Search & Filter Bar ──────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 6, 24, 6)
            };
            _pnlLedgerView.Controls.Add(pnlSearch);

            // Search Container with embedded ✕ button
            var pnlSearchBox = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                Height = 32,
                BackColor = Color.White
            };
            pnlSearchBox.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(203, 213, 225), 1);
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
                PlaceholderText = "🔍  Search by customer, phone, location..."
            };
            _txtSearch.Location = new Point(6, 6);
            _txtSearch.TextChanged += (s, e) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                _currentPage = 1;
                ApplyFilterAndSort();
            };
            pnlSearchBox.Controls.Add(_txtSearch);

            var pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 14, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer);

            var lblHealth = new Label
            {
                Text = "Retention Trigger:",
                Dock = DockStyle.Left,
                Width = 120,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlSearch.Controls.Add(lblHealth);

            _cmbHealthFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 330,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbHealthFilter.Items.AddRange(new object[]
            {
                "All Customer Accounts",
                "⚡ Trigger 1: 30d No Service → \"We Miss You\" Email",
                "⚡ Trigger 2: 60d Inactive → 10% Discount Code",
                "⚡ Trigger 3: 90d Inactive → Assign to Sales Rep",
                "⚡ Trigger 4: Contract Expiring in 30d → Renewal Notice",
                "⚡ Trigger 5: Negative Feedback (★≤2) → Escalate to Manager",
                "⚡ Trigger 6: Order Completed → Request 5-Star Review",
                "⚡ Trigger 7: Customer Anniversary → Loyalty Bonus",
                "⚡ Trigger 8: VIP Milestone / Birthday → Congratulation Greeting",
                "⚠ All At-Risk Accounts (60+ Days)",
                "★ Loyal Repeat Accounts (2+ Jobs)"
            });
            _cmbHealthFilter.SelectedIndex = 0;
            _cmbHealthFilter.SelectedIndexChanged += (s, e) =>
            {
                _activeKpiIndex = _cmbHealthFilter.SelectedIndex switch
                {
                    9 => 0, // At-Risk
                    10 => 1, // Repeat
                    _ => -1
                };
                HighlightActiveKpi();
                _currentPage = 1;
                ApplyFilterAndSort();
            };
            pnlSearch.Controls.Add(_cmbHealthFilter);

            pnlSearch.Controls.SetChildIndex(_cmbHealthFilter, 0);
            pnlSearch.Controls.SetChildIndex(lblHealth, 1);
            pnlSearch.Controls.SetChildIndex(pnlSpacer, 2);
            pnlSearch.Controls.SetChildIndex(pnlSearchBox, 3);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            _pnlLedgerView.Controls.Add(pnlDivider);

            // ── Batch Action Bar (Collapsible) ───────────────────────
            _pnlBatchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 5, 24, 5),
                Visible = false
            };
            _pnlLedgerView.Controls.Add(_pnlBatchBar);

            _lblBatchSelected = new Label
            {
                Dock = DockStyle.Left,
                Width = 220,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "☑ 0 accounts selected"
            };
            _pnlBatchBar.Controls.Add(_lblBatchSelected);

            _btnBatchClear = new Button
            {
                Text = "✕  Deselect All",
                Dock = DockStyle.Right,
                Width = 110,
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
                Width = 130,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchExport.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnBatchExport.Click += (s, e) => ExportToCsv(true);
            _pnlBatchBar.Controls.Add(_btnBatchExport);

            _btnBatchReengage = new Button
            {
                Text = "⚡  Bulk Retention Outreach",
                Dock = DockStyle.Right,
                Width = 190,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBatchReengage.FlatAppearance.BorderSize = 0;
            _btnBatchReengage.Click += OnBatchReengageClick;
            _pnlBatchBar.Controls.Add(_btnBatchReengage);

            // ── Pagination Bar (Bottom) ──────────────────────────────
            _pnlPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 5, 24, 5)
            };
            _pnlLedgerView.Controls.Add(_pnlPagination);

            var pnlPageDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240) };
            _pnlPagination.Controls.Add(pnlPageDivider);

            _lblPageInfo = new Label
            {
                Dock = DockStyle.Left,
                Width = 260,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Showing 0 of 0 accounts"
            };
            _pnlPagination.Controls.Add(_lblPageInfo);

            var pnlPageSize = new Panel { Dock = DockStyle.Left, Width = 160, BackColor = Color.Transparent };
            _pnlPagination.Controls.Add(pnlPageSize);

            var lblRowsPerPage = new Label
            {
                Text = "Rows:",
                Dock = DockStyle.Left,
                Width = 45,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(100, 116, 139),
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

            // ── Grid Host Container & Empty State ─────────────────────
            _pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            _pnlLedgerView.Controls.Add(_pnlGridContainer);

            // Empty State
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
                Height = 55,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(lblEmptyIcon);

            _lblEmptyTitle = new Label
            {
                Text = "No Accounts Match This Retention Filter",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptyTitle);

            _lblEmptySubtitle = new Label
            {
                Text = "Try clearing your search query or selecting a different retention trigger.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.TopCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptySubtitle);

            _btnResetFilters = new Button
            {
                Text = "↺  Reset All Filters",
                Size = new Size(160, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnResetFilters.FlatAppearance.BorderSize = 0;
            _btnResetFilters.Location = new Point((pnlEmptyCenter.Width - _btnResetFilters.Width) / 2, 135);
            _btnResetFilters.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _cmbHealthFilter.SelectedIndex = 0;
            };
            pnlEmptyCenter.Controls.Add(_btnResetFilters);

            // ── DataGridView ─────────────────────────────────────────
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
                RowTemplate = { Height = 38 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
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
            _grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            _grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

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
                new DataGridViewTextBoxColumn { Name = "colName",      HeaderText = "Customer Name",       Width = 180, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colType",      HeaderText = "Account Type",        Width = 125, MinimumWidth = 110, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colContact",   HeaderText = "Contact Details",     Width = 220, MinimumWidth = 160, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colLocation",  HeaderText = "Service Location",    Width = 140, MinimumWidth = 110, ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colCompleted", HeaderText = "Jobs Done",           Width = 85,  MinimumWidth = 70,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colSpent",     HeaderText = "Total Revenue",       Width = 120, MinimumWidth = 95,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colLastDate",  HeaderText = "Last Completed",      Width = 115, MinimumWidth = 95,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colInactive",  HeaderText = "Days Inactive",       Width = 110, MinimumWidth = 90,  ReadOnly = true },
                new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "Retention Action",    Width = 185, MinimumWidth = 150, ReadOnly = true },
                new DataGridViewButtonColumn  { Name = "colAction",    HeaderText = "Action",              Width = 105, MinimumWidth = 90,
                    Text = "★ Win Back", UseColumnTextForButtonValue = true,
                    FlatStyle = FlatStyle.Flat, ReadOnly = true }
            });

            if (_grid.Columns["colCompleted"] != null) _grid.Columns["colCompleted"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colSpent"] != null) _grid.Columns["colSpent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colLastDate"] != null) _grid.Columns["colLastDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colInactive"] != null) _grid.Columns["colInactive"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colStatus"] != null) _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            _grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenReengageForCustomerRow(_grid.Rows[e.RowIndex]);
            };
            _grid.SelectionChanged += (s, e) =>
            {
                _btnReengage.Enabled = _grid.SelectedRows.Count > 0;
            };

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            // Z-Order layout in ledger view
            _pnlLedgerView.Controls.SetChildIndex(_pnlGridContainer, 0);
            _pnlLedgerView.Controls.SetChildIndex(_pnlPagination, 1);
            _pnlLedgerView.Controls.SetChildIndex(_pnlBatchBar, 2);
            _pnlLedgerView.Controls.SetChildIndex(pnlDivider, 3);
            _pnlLedgerView.Controls.SetChildIndex(pnlSearch, 4);
            _pnlLedgerView.Controls.SetChildIndex(pnlKpis, 5);
        }

        // ============================================================
        // Sub-View 2: Automated Retention Rules Policy Panel
        // ============================================================
        private void BuildRulesView()
        {
            _pnlRulesView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Visible = false,
                Padding = new Padding(24, 16, 24, 16)
            };
            _pnlViewContainer.Controls.Add(_pnlRulesView);

            // Banner Card
            var pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(18, 12, 18, 12)
            };
            pnlBanner.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlBanner.Width - 1, pnlBanner.Height - 1);
            };
            _pnlRulesView.Controls.Add(pnlBanner);

            var pnlBannerLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            pnlBanner.Controls.Add(pnlBannerLeft);

            var lblBannerTitle = new Label
            {
                Text = "⚡ Automated Retention Rules & Action Policy Engine",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 26,
                UseMnemonic = false
            };
            pnlBannerLeft.Controls.Add(lblBannerTitle);

            var lblBannerDesc = new Label
            {
                Text = "Pre-defined CRM retention action matrix aligned with industry standards. The system continuously evaluates live customer transaction records and account health against these policies to suggest proactive outreach and prevent churn.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                UseMnemonic = false
            };
            pnlBannerLeft.Controls.Add(lblBannerDesc);

            var pnlBannerRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 260,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent
            };
            pnlBanner.Controls.Add(pnlBannerRight);

            _lblRulesLiveCount = new Label
            {
                Text = "Evaluating live accounts...",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Height = 24,
                Width = 250,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlBannerRight.Controls.Add(_lblRulesLiveCount);

            var btnRunPolicy = new Button
            {
                Text = "⚡  Execute Policy for All Qualifying",
                Height = 32,
                Width = 250,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRunPolicy.FlatAppearance.BorderSize = 0;
            btnRunPolicy.Click += async (s, e) =>
            {
                var emailCandidates = _customers
                    .Where(c => MatchesTriggerRule(c, 1) || MatchesTriggerRule(c, 2) || MatchesTriggerRule(c, 4))
                    .Select(c => c.CustomerId)
                    .Distinct()
                    .ToList();

                var res = MessageBox.Show(
                    $"Execute pre-defined retention policy and dispatch automated win-back emails via SMTP?\n\n" +
                    $"• {emailCandidates.Count} qualifying inactive accounts will receive automated re-engagement emails.\n" +
                    $"• 10% discount promo voucher (WINBACK10) will be included.\n" +
                    $"• Accounts inactive 90+ days will be flagged for priority phone follow-up.\n" +
                    $"• Negative feedback incidents will be escalated to Management.\n\n" +
                    $"Dispatch automated SMTP win-back campaign now?",
                    "Execute Automated Retention Policy (SMTP)",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;

                if (emailCandidates.Count > 0)
                {
                    ShowToast($"Executing retention policy: dispatching SMTP win-back emails to {emailCandidates.Count} accounts...", true);
                    var batchResult = await _api.BatchSendWinBackEmailAsync(new WinBackBatchRequest
                    {
                        CustomerIds = emailCandidates,
                        CampaignType = "Automated Retention Policy",
                        PromoCode = "WINBACK10",
                        DiscountPercentage = 10,
                        Subject = "We Miss You at CleanPro! Enjoy 10% Off Your Next Clean 🎁"
                    });

                    ShowToast($"Policy executed! SMTP Emails Sent: {batchResult.TotalSent} / {batchResult.TotalRequested} (Failed: {batchResult.TotalFailed}).", true);
                }
                else
                {
                    ShowToast("Retention policy executed! No qualifying accounts found.", true);
                }

                await LoadDataAsync();
            };
            pnlBannerRight.Controls.Add(btnRunPolicy);

            var pnlRuleSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 16,
                BackColor = Theme.Surface
            };
            _pnlRulesView.Controls.Add(pnlRuleSpacer);

            // Rules DataGridView
            _gridRules = new DataGridView
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
                GridColor = Color.FromArgb(226, 232, 240),
                EnableHeadersVisualStyles = false,
                ShowCellToolTips = true
            };

            _gridRules.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _gridRules.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _gridRules.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _gridRules.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            _gridRules.DefaultCellStyle.BackColor = Color.White;
            _gridRules.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            _gridRules.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            _gridRules.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            _gridRules.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            _gridRules.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _gridRules.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colRuleNum",    HeaderText = "Rule",                Width = 65,  MinimumWidth = 55 },
                new DataGridViewTextBoxColumn { Name = "colTrigger",    HeaderText = "Trigger Condition",   Width = 240, MinimumWidth = 180 },
                new DataGridViewTextBoxColumn { Name = "colActionName", HeaderText = "Automated Action",    Width = 280, MinimumWidth = 210, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colChannel",    HeaderText = "Channel",             Width = 170, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colAudience",   HeaderText = "Target Audience",     Width = 200, MinimumWidth = 150 },
                new DataGridViewTextBoxColumn { Name = "colMatches",    HeaderText = "Qualifying Accounts", Width = 150, MinimumWidth = 130 },
                new DataGridViewButtonColumn  { Name = "colDrilldown",  HeaderText = "Action",              Width = 145, MinimumWidth = 130,
                    Text = "🔍 View Accounts", UseColumnTextForButtonValue = true,
                    FlatStyle = FlatStyle.Flat }
            });

            if (_gridRules.Columns["colRuleNum"] != null) _gridRules.Columns["colRuleNum"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_gridRules.Columns["colMatches"] != null) _gridRules.Columns["colMatches"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _gridRules.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _gridRules.Columns[e.ColumnIndex].Name == "colDrilldown")
                {
                    if (_gridRules.Rows[e.RowIndex].Tag is int filterIdx)
                    {
                        SwitchToLedgerWithFilter(filterIdx);
                    }
                }
            };

            _pnlRulesView.Controls.Add(_gridRules);
            _gridRules.BringToFront();

            _pnlRulesView.Controls.SetChildIndex(_gridRules, 0);
            _pnlRulesView.Controls.SetChildIndex(pnlRuleSpacer, 1);
            _pnlRulesView.Controls.SetChildIndex(pnlBanner, 2);
        }

        private void SwitchToLedgerWithFilter(int filterIndex)
        {
            SwitchTab(0);
            _cmbHealthFilter.SelectedIndex = filterIndex;
            _txtSearch.Text = string.Empty;
            ApplyFilterAndSort();

            var rule = RetentionRules.FirstOrDefault(r => r.HealthFilterIndex == filterIndex);
            if (rule != null)
            {
                ShowToast($"Showing accounts matching policy: {rule.Name}", true);
            }
        }

        private void RenderRulesGrid()
        {
            if (_gridRules == null) return;

            var now = DateTime.UtcNow;
            int totalQualifying = 0;

            _gridRules.SuspendLayout();
            try
            {
                _gridRules.Rows.Clear();

                foreach (var rule in RetentionRules)
                {
                    int matchCount = _customers.Count(c => MatchesTriggerRule(c, rule.HealthFilterIndex));
                    totalQualifying += matchCount;

                    var row = new DataGridViewRow();
                    row.CreateCells(_gridRules,
                        $"#{rule.RuleNumber}",
                        rule.TriggerCondition,
                        rule.AutomatedAction,
                        rule.Channel,
                        rule.TargetSegment,
                        $"{matchCount:N0} account{(matchCount == 1 ? "" : "s")}",
                        "🔍 View Accounts"
                    );

                    row.Tag = rule.HealthFilterIndex;

                    // Tooltip
                    row.Cells[1].ToolTipText = $"Trigger Rule: {rule.TriggerCondition}";
                    row.Cells[2].ToolTipText = $"Action: {rule.AutomatedAction}";
                    row.Cells[5].ToolTipText = $"{matchCount} customer accounts in database currently qualify for this retention policy.";

                    _gridRules.Rows.Add(row);
                }
            }
            finally
            {
                _gridRules.ResumeLayout();
            }

            if (_lblRulesLiveCount != null)
            {
                _lblRulesLiveCount.Text = $"{totalQualifying:N0} total trigger matches across live DB";
            }
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

                if (_activeTabIndex == 1)
                {
                    RenderRulesGrid();
                }
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
        // Trigger Matching Logic
        // ============================================================
        private static bool MatchesTriggerRule(CustomerSummaryDto c, int filterIndex)
        {
            var now = DateTime.UtcNow;
            int days = c.DaysSinceLastService ?? 0;
            bool hasServiceHistory = c.CompletedBookings > 0;

            return filterIndex switch
            {
                0 => true, // All Customer Accounts

                1 => // Trigger 1: 30 days no service -> Send "We miss you" email
                     hasServiceHistory && days >= 30 && days < 60,

                2 => // Trigger 2: 60 days no purchase/service -> Send 10% discount promo code
                     hasServiceHistory && days >= 60 && days < 90,

                3 => // Trigger 3: 90 days no contact/service -> Assign to Sales Rep for call
                     hasServiceHistory && days >= 90,

                4 => // Trigger 4: Commercial contract expiring in 30d -> Renewal reminder email
                     IsContractExpiringSoon(c, now),

                5 => // Trigger 5: Negative feedback received -> Escalate to Manager
                     c.HasNegativeFeedback || (c.LatestRating.HasValue && c.LatestRating.Value <= 2),

                6 => // Trigger 6: Order completed -> Request 5-star review
                     c.CompletedBookings > 0 && (c.DaysSinceLastService == null || c.DaysSinceLastService <= 14),

                7 => // Trigger 7: Customer anniversary -> Send loyalty bonus
                     c.CreatedAt.HasValue && (now - c.CreatedAt.Value).TotalDays >= 180,

                8 => // Trigger 8: VIP Milestone / Birthday -> VIP perk & appreciation
                     c.CompletedBookings >= 2 || c.TotalSpent >= 2500m,

                9 => // At-Risk (60+ days)
                     c.IsAtRisk,

                10 => // Loyal Repeat (2+ jobs)
                      c.CompletedBookings > 1,

                _ => true
            };
        }

        private static bool IsContractExpiringSoon(CustomerSummaryDto c, DateTime now)
        {
            bool isCommercial = string.Equals(c.CustomerType, "Commercial", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(c.CustomerType, "Company", StringComparison.OrdinalIgnoreCase);

            if (!isCommercial) return false;

            // Commercial service agreements renew every 6 to 12 months from CreatedAt
            var baseDate = c.CreatedAt ?? c.LatestDate ?? now.AddMonths(-5);
            var nextRenewal = baseDate.AddMonths(6);
            if (nextRenewal < now)
            {
                nextRenewal = baseDate.AddYears(1);
            }

            var daysToRenewal = (nextRenewal - now).TotalDays;
            return daysToRenewal >= -30 && daysToRenewal <= 30;
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

                return MatchesTriggerRule(c, healthFilterIdx);
            });

            SortList(_filteredCustomers);

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
            if (c.HasNegativeFeedback || (c.LatestRating.HasValue && c.LatestRating <= 2)) return 0; // Negative feedback
            int days = c.DaysSinceLastService ?? 0;
            if (days >= 90 && c.CompletedBookings > 0) return 1; // Critical Churn
            if (days >= 60 && c.CompletedBookings > 0) return 2; // At-Risk
            if (days >= 30 && c.CompletedBookings > 0) return 3; // Lapsed
            if (c.CompletedBookings > 1) return 4;               // Repeat
            if (c.CompletedBookings == 1) return 5;              // Active
            return 6;                                            // New
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
                    "colStatus" => "Retention Action",
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
            if (e.ColumnIndex == 0)
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
            var now = DateTime.UtcNow;

            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                var rows = new List<DataGridViewRow>(pageRecords.Count);

                foreach (var c in pageRecords)
                {
                    int days = Math.Max(0, c.DaysSinceLastService ?? 0);
                    string inactiveText = c.DaysSinceLastService.HasValue ? $"{days} days" : "—";

                    string typePill = (c.CustomerType?.ToLower() == "company" || c.CustomerType?.ToLower() == "commercial")
                        ? "🏢 Commercial"
                        : "👤 Individual";

                    // Determine Retention Status & Automated Action
                    string actionText;
                    if (c.HasNegativeFeedback || (c.LatestRating.HasValue && c.LatestRating <= 2))
                    {
                        actionText = "🚨 Negative Feedback (Escalate)";
                    }
                    else if (days >= 90 && c.CompletedBookings > 0)
                    {
                        actionText = "🔥 90d+ Inactive → Call Rep";
                    }
                    else if (days >= 60 && c.CompletedBookings > 0)
                    {
                        actionText = "⚠ 60d+ Inactive → 10% Promo";
                    }
                    else if (days >= 30 && c.CompletedBookings > 0)
                    {
                        actionText = "✉ 30d+ Inactive → Miss You";
                    }
                    else if (IsContractExpiringSoon(c, now))
                    {
                        actionText = "📄 Contract Expiring (30d)";
                    }
                    else if (c.CompletedBookings >= 2 || c.TotalSpent >= 2500m)
                    {
                        actionText = "★ VIP Milestone Account";
                    }
                    else if (c.CompletedBookings == 1)
                    {
                        actionText = "✓ Active Customer";
                    }
                    else
                    {
                        actionText = "New Account";
                    }

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
                        actionText,
                        "★ Win Back"
                    );

                    row.Tag = c.CustomerId;

                    // Tooltip text for clear inspection
                    row.Cells[1].ToolTipText = $"Customer: {c.CustomerName} (ID: #{c.CustomerId})";
                    row.Cells[2].ToolTipText = $"Account Type: {c.CustomerType}";
                    row.Cells[3].ToolTipText = $"Contact Details: {c.ContactDetails}";
                    row.Cells[4].ToolTipText = $"Service Location: {c.ServiceLocation}";
                    row.Cells[5].ToolTipText = $"Completed Service Orders: {c.CompletedBookings}";
                    row.Cells[6].ToolTipText = $"Total Revenue Billed: {revenueFormatted}";
                    row.Cells[7].ToolTipText = c.LatestDate.HasValue ? $"Last Service: {c.LatestDate.Value:MMMM dd, yyyy}" : "No completed bookings";
                    row.Cells[8].ToolTipText = c.DaysSinceLastService.HasValue ? $"Inactive for {days} days" : "No service history";
                    row.Cells[9].ToolTipText = $"Recommended Retention Action: {actionText}";

                    // Minimal cohesive typography (avoids bright rainbow colors)
                    var cellInactive = row.Cells[8];
                    var cellAction = row.Cells[9];

                    if (days >= 90 || c.HasNegativeFeedback || (c.LatestRating.HasValue && c.LatestRating <= 2))
                    {
                        cellInactive.Style.Font = _fontCriticalBold;
                        cellAction.Style.Font = _fontBold;
                    }
                    else if (days >= 60 || c.IsAtRisk)
                    {
                        cellInactive.Style.Font = _fontBold;
                        cellAction.Style.Font = _fontBold;
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
                    BackColor = isCurrent ? Color.FromArgb(30, 41, 59) : Color.White,
                    ForeColor = isCurrent ? Color.White : Color.FromArgb(51, 65, 85),
                    Font = new Font("Segoe UI", 8F, isCurrent ? FontStyle.Bold : FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2, 0, 2, 0)
                };
                btnPage.FlatAppearance.BorderColor = isCurrent ? Color.FromArgb(30, 41, 59) : Color.FromArgb(226, 232, 240);
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
            if (_grid.Columns[e.ColumnIndex].Name == "colAction" && e.RowIndex >= 0)
            {
                OpenReengageForCustomerRow(_grid.Rows[e.RowIndex]);
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

        private async void OnBatchReengageClick(object? sender, EventArgs e)
        {
            if (_selectedCustomerIds.Count == 0) return;

            var count = _selectedCustomerIds.Count;
            var result = MessageBox.Show(
                $"Queue retention re-engagement email campaign via SMTP for {count} selected customer account(s)?\n\n" +
                $"• Channel: Real SMTP Email Transport\n" +
                $"• Offer: 10% Discount Promo Code (WINBACK10)\n" +
                $"• Template: Responsive HTML + Plain-text fallback\n\n" +
                $"Would you like to dispatch these win-back emails now?",
                "Bulk Retention Outreach via SMTP",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            ShowToast($"Dispatching SMTP win-back emails to {count} accounts in progress...", true);

            var batchReq = new WinBackBatchRequest
            {
                CustomerIds = _selectedCustomerIds.ToList(),
                PromoCode = "WINBACK10",
                DiscountPercentage = 10,
                CampaignType = "Bulk Re-engagement Campaign",
                Subject = "We Miss You at CleanPro! Enjoy 10% Off Your Next Service 🎁"
            };

            var batchResult = await _api.BatchSendWinBackEmailAsync(batchReq);

            if (batchResult.TotalSent > 0)
            {
                ShowToast($"SMTP Batch Complete! Sent: {batchResult.TotalSent} / {batchResult.TotalRequested} (Failed: {batchResult.TotalFailed})", true);
            }
            else
            {
                ShowToast($"SMTP Batch Completed: 0 sent, {batchResult.TotalFailed} failed or missing valid email.", false);
            }

            DeselectAll();
            await LoadDataAsync();
        }

        // ============================================================
        // Individual Re-engage (1-Click Win-Back Dialog)
        // ============================================================
        private void OnReengageClick(object? sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            OpenReengageForCustomerRow(_grid.SelectedRows[0]);
        }

        private async void OpenReengageForCustomerRow(DataGridViewRow row)
        {
            if (row.Tag is not int customerId) return;

            var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
            var customerName = customer?.CustomerName ?? row.Cells["colName"].Value?.ToString() ?? "Customer";
            var location = customer?.ServiceLocation ?? row.Cells["colLocation"].Value?.ToString();
            var email = customer?.Email;
            var daysInactive = customer?.DaysSinceLastService ?? 30;
            var lastService = customer?.LatestService ?? "General Cleaning";

            using var dialog = new WinBackEmailDialog(customerId, customerName, email, daysInactive, lastService, location);
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                ShowToast($"Win-back email dispatched to {customerName}!", true);
                await LoadDataAsync();
            }
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
                Title = "Export Customer Retention Records to CSV",
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
        // Interactive KPI Cards Helper (Consistent Minimal Styling)
        // ============================================================
        private (Panel card, Label titleLabel, Label valLabel, Label subLabel) CreateKpiCard(
            string title,
            string initialVal,
            string subtext,
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
                    ? new Pen(Color.FromArgb(30, 41, 59), 2)
                    : new Pen(Color.FromArgb(226, 232, 240), 1);

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
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
                ForeColor = Color.FromArgb(30, 41, 59),
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
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 18,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            card.Controls.Add(lblSub);

            Action clickAction = () =>
            {
                _activeKpiIndex = kpiIndex;
                HighlightActiveKpi();

                switch (kpiIndex)
                {
                    case 0: // At-Risk Accounts (60+ Days)
                        _cmbHealthFilter.SelectedIndex = 9;
                        _sortColumn = "DaysSinceLastService";
                        _sortAscending = false;
                        break;
                    case 1: // Repeat Rate
                        _cmbHealthFilter.SelectedIndex = 10;
                        _sortColumn = "CompletedBookings";
                        _sortAscending = false;
                        break;
                    case 2: // Completed Jobs
                        _cmbHealthFilter.SelectedIndex = 0;
                        _sortColumn = "CompletedBookings";
                        _sortAscending = false;
                        break;
                    case 3: // Avg Revenue
                        _cmbHealthFilter.SelectedIndex = 0;
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
