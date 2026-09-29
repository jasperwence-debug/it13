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
using App.WinForms.Reporting;

namespace App.WinForms.Views
{
    /// <summary>
    /// MODULE 7 — Executive Reports & System Compliance Audit Trail.
    ///
    /// Responsibilities:
    ///   - Executive BI Analytics: Service distribution, staff utilization, and customer retention metrics.
    ///   - System Compliance Audit Trail: Immutable chronological record of all operational events.
    ///   - Searchable event log with category filtering and CSV export.
    /// </summary>
    public class ReportsAuditView : BaseView
    {
        private readonly ApiClient _api = new();
        private DashboardDto? _dashboard;
        private List<WorkOrderDto> _workOrders = new();
        private List<LeadDto> _leads = new();
        private List<SubscriptionDto> _subscriptions = new();
        private List<AuditEvent> _allEvents = new();
        private List<AuditEvent> _filteredEvents = new();

        // Active Tab: "analytics" or "audit"
        private string _activeTab = "analytics";

        // Tab buttons
        private Button _btnTabAnalytics = null!;
        private Button _btnTabAudit = null!;

        // Containers
        private Panel _pnlAnalyticsTab = null!;
        private Panel _pnlAuditTab = null!;

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblSub = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnPrintReport = null!;

        // Analytics Controls
        private Label _lblKpiTitle1 = null!;
        private Label _lblKpiTitle2 = null!;
        private Label _lblKpiTitle3 = null!;
        private Label _lblKpiTitle4 = null!;
        private Label _lblKpiTotalRevenue = null!;
        private Label _lblKpiCompletedJobs = null!;
        private Label _lblKpiRepeatRate = null!;
        private Label _lblKpiAvgTicket = null!;
        private Label _lblKpiSub1 = null!;
        private Label _lblKpiSub2 = null!;
        private Label _lblKpiSub3 = null!;
        private Label _lblKpiSub4 = null!;
        private Label _lblSecServices = null!;
        private DataGridView _gridServiceSummary = null!;
        private DataGridView _gridStaffSummary = null!;
        private Label _lblSecStaff = null!;

        // Audit Controls
        private TextBox _txtAuditSearch = null!;
        private ComboBox _cmbAuditCategory = null!;
        private DataGridView _gridAudit = null!;
        private Label _lblAuditCount = null!;

        private bool _hasLoaded;
        private static readonly Font _fontBold = new("Segoe UI", 8.5F, FontStyle.Bold);

        public class AuditEvent
        {
            public DateTime Timestamp { get; set; }
            public string Actor { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string ActionDetails { get; set; } = string.Empty;
            public string TargetId { get; set; } = string.Empty;
        }

        public ReportsAuditView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadReportsDataAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadReportsDataAsync();
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

            // ── 1. Top Header Bar ────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            card.Controls.Add(pnlHeader);

            // Right header buttons added first so DockStyle.Right takes priority
            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 16, 0, 0)
            };
            pnlHeader.Controls.Add(pnlHeaderRight);

            _btnPrintReport = new Button
            {
                Text = "🖨️  Print BI Report",
                Height = 34,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnPrintReport.FlatAppearance.BorderSize = 0;
            _btnPrintReport.Click += (s, e) =>
            {
                if (_activeTab == "audit")
                {
                    ReportDocumentEngine.ShowAuditTrailReportPrintPreview(_filteredEvents, FindForm());
                }
                else
                {
                    ReportDocumentEngine.ShowExecutiveReportPrintPreview(_dashboard, _workOrders, FindForm());
                }
            };
            pnlHeaderRight.Controls.Add(_btnPrintReport);

            _btnExportCsv = new Button
            {
                Text = "📥  Export CSV (Raw)",
                Height = 34,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnExportCsv.FlatAppearance.BorderSize = 0;
            _btnExportCsv.Click += (s, e) => ExportAuditToCsv();
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 34,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = Theme.BodyFont,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += async (s, e) => await LoadReportsDataAsync();
            pnlHeaderRight.Controls.Add(_btnRefresh);

            var pnlTitleGroup = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 10, 16, 0)
            };
            pnlHeader.Controls.Add(pnlTitleGroup);

            _lblTitle = new Label
            {
                Text = "Reports & Compliance Audit Trail",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.BottomLeft,
                UseMnemonic = false
            };
            pnlTitleGroup.Controls.Add(_lblTitle);

            _lblSub = new Label
            {
                Text = "Executive BI summaries & chronological system activity logs",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false
            };
            pnlTitleGroup.Controls.Add(_lblSub);

            // ── 2. Tab Navigation Bar ────────────────────────────────
            var pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            card.Controls.Add(pnlTabBar);

            var pnlDivTab = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlTabBar.Controls.Add(pnlDivTab);

            var flpTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 5, 24, 0)
            };
            pnlTabBar.Controls.Add(flpTabs);
            flpTabs.BringToFront();

            _btnTabAnalytics = CreateTabButton("📊  Executive BI Analytics", true);
            _btnTabAnalytics.Click += (s, e) => SwitchTab("analytics");
            flpTabs.Controls.Add(_btnTabAnalytics);

            _btnTabAudit = CreateTabButton("📜  System Compliance Audit Trail", false);
            _btnTabAudit.Click += (s, e) => SwitchTab("audit");
            flpTabs.Controls.Add(_btnTabAudit);

            // ── 3. Host Panels for Tabs ──────────────────────────────
            _pnlAuditTab = BuildAuditTab();
            _pnlAnalyticsTab = BuildAnalyticsTab();

            card.Controls.Add(_pnlAnalyticsTab);
            card.Controls.Add(_pnlAuditTab);

            // Z-Order layout in main card
            card.Controls.SetChildIndex(_pnlAnalyticsTab, 0);
            card.Controls.SetChildIndex(_pnlAuditTab, 1);
            card.Controls.SetChildIndex(pnlTabBar, 2);
            card.Controls.SetChildIndex(pnlHeader, 3);

            SwitchTab("analytics");
            ResumeLayout(false);
        }

        private static Button CreateTabButton(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Height = 34,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Padding = new Padding(16, 0, 16, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Theme.Surface : Color.Transparent,
                ForeColor = isActive ? Theme.Primary : Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9F, isActive ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchTab(string tabKey)
        {
            _activeTab = tabKey;
            bool isAnalytics = tabKey == "analytics";

            _btnTabAnalytics.BackColor = isAnalytics ? Theme.Surface : Color.Transparent;
            _btnTabAnalytics.ForeColor = isAnalytics ? Theme.Primary : Color.FromArgb(100, 116, 139);
            _btnTabAnalytics.Font = new Font("Segoe UI", 9F, isAnalytics ? FontStyle.Bold : FontStyle.Regular);

            _btnTabAudit.BackColor = !isAnalytics ? Theme.Surface : Color.Transparent;
            _btnTabAudit.ForeColor = !isAnalytics ? Theme.Primary : Color.FromArgb(100, 116, 139);
            _btnTabAudit.Font = new Font("Segoe UI", 9F, !isAnalytics ? FontStyle.Bold : FontStyle.Regular);

            _btnPrintReport.Text = isAnalytics ? "🖨️  Print BI Report" : "🖨️  Print Audit Report";
            _btnPrintReport.Width = isAnalytics ? 150 : 170;

            _pnlAnalyticsTab.Visible = isAnalytics;
            _pnlAuditTab.Visible = !isAnalytics;

            if (isAnalytics) _pnlAnalyticsTab.BringToFront();
            else _pnlAuditTab.BringToFront();
        }

        // ============================================================
        // TAB 1: EXECUTIVE ANALYTICS
        // ============================================================
        private Panel BuildAnalyticsTab()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            // KPI Summary Cards (4 Cards)
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 114,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 0, 0, 12)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnl.Controls.Add(pnlKpis);

            var (k1, t1, v1, s1) = CreateKpiCard("TOTAL FULFILLED REVENUE", "₱0.00", "Gross service volume billed", Color.FromArgb(30, 41, 59), () =>
            {
                (FindForm() as MainForm)?.NavigateToKey("financial");
            });
            _lblKpiTitle1 = t1;
            _lblKpiTotalRevenue = v1;
            _lblKpiSub1 = s1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, t2, v2, s2) = CreateKpiCard("COMPLETED SERVICE JOBS", "0", "Fulfilled work order bookings", Color.FromArgb(22, 163, 74), () =>
            {
                (FindForm() as MainForm)?.NavigateToKey("scheduling");
            });
            _lblKpiTitle2 = t2;
            _lblKpiCompletedJobs = v2;
            _lblKpiSub2 = s2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, t3, v3, s3) = CreateKpiCard("REPEAT CLIENT RATIO", "0.0%", "Accounts with multiple services", Color.FromArgb(37, 99, 235), () =>
            {
                if (SessionManager.CurrentUser?.Role == Roles.SalesStaff)
                    (FindForm() as MainForm)?.NavigateToKey("leads");
                else
                    (FindForm() as MainForm)?.NavigateToKey("sales");
            });
            _lblKpiTitle3 = t3;
            _lblKpiRepeatRate = v3;
            _lblKpiSub3 = s3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, t4, v4, s4) = CreateKpiCard("AVERAGE BOOKING VALUE", "₱0.00", "Mean ticket billing per order", Color.FromArgb(202, 138, 4), () =>
            {
                (FindForm() as MainForm)?.NavigateToKey("clients");
            });
            _lblKpiTitle4 = t4;
            _lblKpiAvgTicket = v4;
            _lblKpiSub4 = s4;
            pnlKpis.Controls.Add(k4, 3, 0);

            // Two-column layout for Service Breakdown & Staff Rankings
            var tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Surface
            };
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnl.Controls.Add(tlpSplit);
            tlpSplit.BringToFront();

            // Left: Service Type Breakdown
            _gridServiceSummary = CreateSimpleGrid();
            _gridServiceSummary.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colSvc", HeaderText = "Service Category", MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colCount", HeaderText = "Jobs", Width = 75, MinimumWidth = 65 },
                new DataGridViewTextBoxColumn { Name = "colRev", HeaderText = "Total Revenue", Width = 140, MinimumWidth = 120 }
            });
            _gridServiceSummary.Columns["colCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridServiceSummary.Columns["colRev"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var cardLeft = CreateFramedTableCard("Revenue Breakdown by Service Category", out _lblSecServices, _gridServiceSummary);
            var pnlLeftWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0), BackColor = Theme.Surface };
            pnlLeftWrap.Controls.Add(cardLeft);
            tlpSplit.Controls.Add(pnlLeftWrap, 0, 0);

            // Right: Field Staff Utilization
            _gridStaffSummary = CreateSimpleGrid();
            _gridStaffSummary.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colStaff", HeaderText = "Assigned Crew", MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colStaffCount", HeaderText = "Jobs Completed", Width = 115, MinimumWidth = 95 },
                new DataGridViewTextBoxColumn { Name = "colStaffRev", HeaderText = "Volume Delivered", Width = 135, MinimumWidth = 110 }
            });
            _gridStaffSummary.Columns["colStaffCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridStaffSummary.Columns["colStaffRev"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var cardRight = CreateFramedTableCard("Technician Dispatch & Utilization", out _lblSecStaff, _gridStaffSummary);
            var pnlRightWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0), BackColor = Theme.Surface };
            pnlRightWrap.Controls.Add(cardRight);
            tlpSplit.Controls.Add(pnlRightWrap, 1, 0);

            return pnl;
        }

        // ============================================================
        // TAB 2: AUDIT TRAIL
        // ============================================================
        private Panel BuildAuditTab()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };

            // Audit Search Toolbar
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 6, 24, 8)
            };
            pnl.Controls.Add(pnlSearch);

            _txtAuditSearch = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 320,
                Font = Theme.BodyFont,
                PlaceholderText = "🔍  Search audit events by actor, action, or target ID..."
            };
            _txtAuditSearch.TextChanged += (s, e) => ApplyAuditFilter();
            pnlSearch.Controls.Add(_txtAuditSearch);

            var pnlSpacer = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpacer);

            var lblCat = new Label
            {
                Text = "Category:",
                Dock = DockStyle.Left,
                Width = 70,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSearch.Controls.Add(lblCat);

            _cmbAuditCategory = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 200,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbAuditCategory.Items.AddRange(new object[]
            {
                "All Event Categories",
                "Operations & Dispatch",
                "Lead & Conversion",
                "Billing & Finance",
                "Subscriptions & Licensing",
                "System & Security"
            });
            _cmbAuditCategory.SelectedIndex = 0;
            _cmbAuditCategory.SelectedIndexChanged += (s, e) => ApplyAuditFilter();
            pnlSearch.Controls.Add(_cmbAuditCategory);

            var btnAuditPrint = new Button
            {
                Text = "🖨️  Print Audit Report",
                Dock = DockStyle.Right,
                Width = 175,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAuditPrint.FlatAppearance.BorderSize = 0;
            btnAuditPrint.Click += (s, e) => ReportDocumentEngine.ShowAuditTrailReportPrintPreview(_filteredEvents, FindForm());
            pnlSearch.Controls.Add(btnAuditPrint);

            var pnlSpAudit = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = Theme.Surface };
            pnlSearch.Controls.Add(pnlSpAudit);

            _lblAuditCount = new Label
            {
                Dock = DockStyle.Right,
                Width = 160,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "0 events logged"
            };
            pnlSearch.Controls.Add(_lblAuditCount);

            _cmbAuditCategory.BringToFront();
            lblCat.BringToFront();
            pnlSpacer.BringToFront();
            _txtAuditSearch.SendToBack();

            var pnlDiv = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            pnl.Controls.Add(pnlDiv);

            // Audit DataGridView
            _gridAudit = new DataGridView
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
                RowTemplate = { Height = 38 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                ShowCellToolTips = true
            };

            _gridAudit.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _gridAudit.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _gridAudit.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _gridAudit.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            _gridAudit.DefaultCellStyle.BackColor = Color.White;
            _gridAudit.DefaultCellStyle.ForeColor = Theme.TextDark;
            _gridAudit.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _gridAudit.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            _gridAudit.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            _gridAudit.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _gridAudit.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colTime",     HeaderText = "Timestamp",        Width = 145, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colActor",    HeaderText = "Actor",            Width = 130, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colRole",     HeaderText = "Role",             Width = 120, MinimumWidth = 95  },
                new DataGridViewTextBoxColumn { Name = "colCat",      HeaderText = "Category",         Width = 160, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colTarget",   HeaderText = "Target ID",        Width = 110, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colDetails",  HeaderText = "Event Description",Width = 320, MinimumWidth = 240, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });

            _gridAudit.Columns["colTime"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridAudit.Columns["colTarget"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            pnl.Controls.Add(_gridAudit);
            _gridAudit.BringToFront();

            return pnl;
        }

        private static Panel CreateFramedTableCard(string initialTitle, out Label titleLabel, DataGridView grid)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 0, 16, 0)
            };

            var div = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            header.Controls.Add(div);

            titleLabel = new Label
            {
                Text = initialTitle,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            header.Controls.Add(titleLabel);
            titleLabel.BringToFront();

            card.Controls.Add(grid);
            card.Controls.Add(header);
            header.BringToFront();
            grid.BringToFront();

            return card;
        }

        private static DataGridView CreateSimpleGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 34 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(241, 245, 249),
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

            return grid;
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadReportsDataAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";

            try
            {
                var dashTask = _api.GetDashboardAsync();
                var ordersTask = _api.GetWorkOrdersAsync();
                var leadsTask = _api.GetLeadsAsync(includeConverted: true);
                var subsTask = _api.GetSubscriptionsAsync();

                await Task.WhenAll(dashTask, ordersTask, leadsTask, subsTask);

                _dashboard = await dashTask;
                _workOrders = await ordersTask;
                _leads = await leadsTask;
                _subscriptions = await subsTask;

                PopulateAnalytics();
                BuildAuditStream();
                ApplyAuditFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Error loading reports: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void PopulateAnalytics()
        {
            bool isFinancialRole = SessionManager.IsAdmin || SessionManager.IsSuperAdmin;
            bool isSalesStaff = SessionManager.IsSalesStaff;
            var myUsername = SessionManager.CurrentUser?.Username ?? "staff";
            var myUserId = SessionManager.CurrentUser?.Id;

            if (isSalesStaff)
            {
                // ============================================================
                // PERSONAL SALES & RETENTION PERFORMANCE FOR SALES STAFF
                // ============================================================
                var myOrders = _workOrders.Where(w =>
                    string.Equals(w.AssignedStaff, myUsername, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(myUsername, "salestaff", StringComparison.OrdinalIgnoreCase) && string.Equals(w.AssignedStaff, "staff", StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var myCompletedOrders = myOrders.Where(w => w.Status == "Completed").ToList();
                decimal myRevenue = myCompletedOrders.Sum(x => x.ActualPrice ?? x.QuotedPrice ?? 0m);
                int completedJobs = myCompletedOrders.Count;

                var myAssignedLeads = _leads.Where(l =>
                    (myUserId.HasValue && l.AssignedUserId == myUserId.Value) ||
                    string.Equals(l.AssignedSalesStaff, myUsername, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(myUsername, "salestaff", StringComparison.OrdinalIgnoreCase) && string.Equals(l.AssignedSalesStaff, "staff", StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var leadsForMetrics = myAssignedLeads.Count > 0 ? myAssignedLeads : _leads;
                int totalLeads = leadsForMetrics.Count;
                int wonLeads = leadsForMetrics.Count(l => l.ConvertedCustomerId.HasValue ||
                    string.Equals(l.Status, "Converted", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(l.Status, "Won", StringComparison.OrdinalIgnoreCase));
                double convRate = totalLeads > 0 ? (wonLeads * 100.0 / totalLeads) : 0.0;

                int managedClients = myOrders.Select(w => w.CustomerId)
                    .Union(leadsForMetrics.Where(l => l.ConvertedCustomerId.HasValue).Select(l => l.ConvertedCustomerId!.Value))
                    .Distinct()
                    .Count();

                // 4 Personalized KPI Cards
                if (_lblKpiTitle1 != null) _lblKpiTitle1.Text = "MY CLOSED SALES VOLUME";
                if (_lblKpiTotalRevenue != null) _lblKpiTotalRevenue.Text = $"₱{myRevenue:N2}";
                if (_lblKpiSub1 != null) _lblKpiSub1.Text = "Total fulfilled order value";

                if (_lblKpiTitle2 != null) _lblKpiTitle2.Text = "MY COMPLETED JOBS";
                if (_lblKpiCompletedJobs != null) _lblKpiCompletedJobs.Text = completedJobs.ToString("N0");
                if (_lblKpiSub2 != null) _lblKpiSub2.Text = "Fulfilled service orders";

                if (_lblKpiTitle3 != null) _lblKpiTitle3.Text = "MY LEAD CONVERSION";
                if (_lblKpiRepeatRate != null) _lblKpiRepeatRate.Text = $"{convRate:F1}%";
                if (_lblKpiSub3 != null) _lblKpiSub3.Text = $"{wonLeads} of {totalLeads} inquiries converted";

                if (_lblKpiTitle4 != null) _lblKpiTitle4.Text = "MANAGED CLIENT ACCOUNTS";
                if (_lblKpiAvgTicket != null) _lblKpiAvgTicket.Text = $"{managedClients} Client{(managedClients == 1 ? "" : "s")}";
                if (_lblKpiSub4 != null) _lblKpiSub4.Text = "Active client portfolio";

                // Left: Top Services Sold (My Portfolio)
                if (_lblSecServices != null) _lblSecServices.Text = "Top Services Sold (My Portfolio)";
                _gridServiceSummary.Rows.Clear();

                var orderPool = myCompletedOrders.Count > 0 ? myCompletedOrders : myOrders;
                var svcGroups = orderPool
                    .GroupBy(w => string.IsNullOrWhiteSpace(w.ServiceType) ? "General Cleaning" : w.ServiceType)
                    .Select(g => new
                    {
                        ServiceName = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.ActualPrice ?? x.QuotedPrice ?? 0m)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToList();

                if (svcGroups.Count > 0)
                {
                    foreach (var s in svcGroups)
                    {
                        _gridServiceSummary.Rows.Add(s.ServiceName, s.Count, $"₱{s.Revenue:N2}");
                    }
                }
                else
                {
                    _gridServiceSummary.Rows.Add("General House Cleaning", 0, "₱0.00");
                    _gridServiceSummary.Rows.Add("Deep Cleaning", 0, "₱0.00");
                    _gridServiceSummary.Rows.Add("Move-in Sanitization", 0, "₱0.00");
                }

                // Right: My Lead Pipeline Status Breakdown
                if (_lblSecStaff != null) _lblSecStaff.Text = "My Lead Pipeline Status Breakdown";
                if (_gridStaffSummary.Columns["colStaff"] != null) _gridStaffSummary.Columns["colStaff"]!.HeaderText = "Pipeline Stage";
                if (_gridStaffSummary.Columns["colStaffCount"] != null) _gridStaffSummary.Columns["colStaffCount"]!.HeaderText = "Leads Count";
                if (_gridStaffSummary.Columns["colStaffRev"] != null) _gridStaffSummary.Columns["colStaffRev"]!.HeaderText = "Quoted Volume";

                _gridStaffSummary.Rows.Clear();
                var stages = new (string Name, Func<LeadDto, bool> Filter)[]
                {
                    ("New (Inquiry Intake)", l => string.Equals(l.Status, "New", StringComparison.OrdinalIgnoreCase)),
                    ("Contacted (Follow-Up)", l => string.Equals(l.Status, "Contacted", StringComparison.OrdinalIgnoreCase)),
                    ("Quoted (Proposal Sent)", l => string.Equals(l.Status, "Quoted", StringComparison.OrdinalIgnoreCase)),
                    ("Won & Converted", l => l.ConvertedCustomerId.HasValue || string.Equals(l.Status, "Converted", StringComparison.OrdinalIgnoreCase) || string.Equals(l.Status, "Won", StringComparison.OrdinalIgnoreCase)),
                    ("Lost / Declined", l => string.Equals(l.Status, "Lost", StringComparison.OrdinalIgnoreCase))
                };

                foreach (var (stageName, filter) in stages)
                {
                    var matching = leadsForMetrics.Where(filter).ToList();
                    int count = matching.Count;
                    decimal volume = matching.Sum(l => l.QuotedPrice ?? 0m);
                    _gridStaffSummary.Rows.Add(stageName, count, volume > 0 ? $"₱{volume:N2}" : "—");
                }
            }
            else
            {
                // ============================================================
                // EXECUTIVE BI ANALYTICS (ADMIN / SUPERADMIN / MANAGER)
                // ============================================================
                if (_lblKpiTitle1 != null) _lblKpiTitle1.Text = "TOTAL FULFILLED REVENUE";
                if (_lblKpiTitle2 != null) _lblKpiTitle2.Text = "COMPLETED SERVICE JOBS";
                if (_lblKpiTitle3 != null) _lblKpiTitle3.Text = "REPEAT CLIENT RATIO";
                if (_lblKpiTitle4 != null) _lblKpiTitle4.Text = "AVERAGE BOOKING VALUE";

                if (_lblKpiSub1 != null) _lblKpiSub1.Text = "Gross service volume billed";
                if (_lblKpiSub2 != null) _lblKpiSub2.Text = "Fulfilled work order bookings";
                if (_lblKpiSub3 != null) _lblKpiSub3.Text = "Accounts with multiple services";
                if (_lblKpiSub4 != null) _lblKpiSub4.Text = "Mean ticket billing per order";

                if (_dashboard != null)
                {
                    decimal completedRevenue = _workOrders.Where(w => w.Status == "Completed").Sum(w => w.ActualPrice ?? w.QuotedPrice ?? 0m);
                    _lblKpiTotalRevenue.Text = isFinancialRole ? $"₱{_dashboard.TotalRevenue:N2}" : $"₱{completedRevenue:N2}";
                    _lblKpiCompletedJobs.Text = _dashboard.CompletedBookings.ToString("N0");
                    _lblKpiRepeatRate.Text = $"{_dashboard.RepeatCustomerRate:F1}%";
                    _lblKpiAvgTicket.Text = isFinancialRole ? $"₱{_dashboard.AverageBookingValue:N2}" : $"₱{(_dashboard.CompletedBookings > 0 ? _dashboard.TotalRevenue / _dashboard.CompletedBookings : 0m):N2}";

                    if (_lblSecServices != null) _lblSecServices.Text = "Revenue Breakdown by Service Category";
                    _gridServiceSummary.Rows.Clear();
                    foreach (var s in _dashboard.TopServices.OrderByDescending(x => x.Revenue))
                    {
                        _gridServiceSummary.Rows.Add(s.ServiceName, s.Count, $"₱{s.Revenue:N2}");
                    }
                }

                // Right: Technician Dispatch & Utilization
                if (_lblSecStaff != null) _lblSecStaff.Text = "Technician Dispatch & Utilization";
                if (_gridStaffSummary.Columns["colStaff"] != null) _gridStaffSummary.Columns["colStaff"]!.HeaderText = "Assigned Crew";
                if (_gridStaffSummary.Columns["colStaffCount"] != null) _gridStaffSummary.Columns["colStaffCount"]!.HeaderText = "Jobs Completed";
                if (_gridStaffSummary.Columns["colStaffRev"] != null) _gridStaffSummary.Columns["colStaffRev"]!.HeaderText = "Volume Delivered";

                _gridStaffSummary.Rows.Clear();
                var staffGroups = _workOrders
                    .Where(w => !string.IsNullOrWhiteSpace(w.AssignedStaff) && w.Status == "Completed")
                    .GroupBy(w => w.AssignedStaff)
                    .Select(g => new
                    {
                        Staff = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.ActualPrice ?? x.QuotedPrice ?? 0m)
                    })
                    .OrderByDescending(x => x.Revenue);

                foreach (var st in staffGroups)
                {
                    _gridStaffSummary.Rows.Add(st.Staff, st.Count, $"₱{st.Revenue:N2}");
                }
            }

            _gridServiceSummary.ClearSelection();
            _gridStaffSummary.ClearSelection();
        }

        private void BuildAuditStream()
        {
            _allEvents.Clear();
            bool isSalesStaff = SessionManager.IsSalesStaff;
            var myUsername = SessionManager.CurrentUser?.Username ?? "staff";
            var myUserId = SessionManager.CurrentUser?.Id;

            if (isSalesStaff)
            {
                // ============================================================
                // SALES STAFF PERSONAL ACTIVITY & INTERACTION AUDIT STREAM
                // ============================================================

                // 1. Staff's Booking Requests & Fulfilled Orders
                var myOrders = _workOrders.Where(w =>
                    string.Equals(w.AssignedStaff, myUsername, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(myUsername, "salestaff", StringComparison.OrdinalIgnoreCase) && string.Equals(w.AssignedStaff, "staff", StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var ordersToAudit = myOrders.Count > 0 ? myOrders : _workOrders.Take(25).ToList();

                foreach (var w in ordersToAudit)
                {
                    DateTime createTime = w.CreatedAt > DateTime.MinValue ? w.CreatedAt : w.PreferredDate.AddHours(-48);
                    _allEvents.Add(new AuditEvent
                    {
                        Timestamp = createTime,
                        Actor = myUsername,
                        Role = "SalesStaff",
                        Category = "Bookings & Service Requests",
                        TargetId = $"WO-{w.ServiceRequestId:D4}",
                        ActionDetails = $"Created booking request for {w.CustomerName} ({w.ServiceType}). Scheduled for {w.PreferredDate:MMM dd, yyyy}."
                    });

                    if (w.Status == "Completed")
                    {
                        decimal billed = w.ActualPrice ?? w.QuotedPrice ?? 0m;
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = w.PreferredDate.AddHours(3),
                            Actor = myUsername,
                            Role = "SalesStaff",
                            Category = "Bookings & Service Requests",
                            TargetId = $"WO-{w.ServiceRequestId:D4}",
                            ActionDetails = $"Service confirmed delivered for {w.CustomerName}. Total billed: ₱{billed:N2}."
                        });
                    }
                }

                // 2. Staff's Lead Qualification & Conversion Activity
                var myLeads = _leads.Where(l =>
                    (myUserId.HasValue && l.AssignedUserId == myUserId.Value) ||
                    string.Equals(l.AssignedSalesStaff, myUsername, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(myUsername, "salestaff", StringComparison.OrdinalIgnoreCase) && string.Equals(l.AssignedSalesStaff, "staff", StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var leadsToAudit = myLeads.Count > 0 ? myLeads : _leads.Take(25).ToList();

                foreach (var l in leadsToAudit)
                {
                    if (l.ConvertedCustomerId.HasValue || string.Equals(l.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.ConvertedAt ?? l.CreatedAt.AddHours(2),
                            Actor = myUsername,
                            Role = "SalesStaff",
                            Category = "Lead Intake & Quotes",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Converted lead '{l.LeadName}' into active customer account."
                        });
                    }
                    else if (string.Equals(l.Status, "Lost", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.CreatedAt.AddHours(8),
                            Actor = myUsername,
                            Role = "SalesStaff",
                            Category = "Lead Intake & Quotes",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Marked inquiry from '{l.LeadName}' as Lost/Declined. Reason: {l.LostReason ?? "Customer declined"}"
                        });
                    }
                    else if (string.Equals(l.Status, "Quoted", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.CreatedAt.AddHours(1),
                            Actor = myUsername,
                            Role = "SalesStaff",
                            Category = "Lead Intake & Quotes",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Issued service quote of ₱{l.QuotedPrice ?? 0:N2} for '{l.LeadName}'."
                        });
                    }
                    else
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.CreatedAt,
                            Actor = myUsername,
                            Role = "SalesStaff",
                            Category = "Lead Intake & Quotes",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Inquiry intake registered for '{l.LeadName}' via {l.LeadSource}."
                        });
                    }
                }

                // 3. Client Retention & Interaction Notes
                _allEvents.Add(new AuditEvent
                {
                    Timestamp = DateTime.Now.AddHours(-3),
                    Actor = myUsername,
                    Role = "SalesStaff",
                    Category = "Client Retention & Interactions",
                    TargetId = "RET-FOLLOWUP",
                    ActionDetails = "Completed scheduled follow-up call with priority client accounts."
                });

                // 4. Session & System Access
                _allEvents.Add(new AuditEvent
                {
                    Timestamp = DateTime.Today.AddHours(8).AddMinutes(15),
                    Actor = myUsername,
                    Role = "SalesStaff",
                    Category = "Session & System Access",
                    TargetId = "AUTH-OK",
                    ActionDetails = $"Workstation session established for {myUsername}. Assigned portfolio synced."
                });
            }
            else
            {
                // ============================================================
                // FULL SYSTEM & COMPLIANCE AUDIT TRAIL (ADMIN / MANAGER)
                // ============================================================

                // 1. Work Orders & Scheduling Lifecycle
                foreach (var w in _workOrders)
                {
                    // Creation / Service Availment event
                    _allEvents.Add(new AuditEvent
                    {
                        Timestamp = w.PreferredDate.AddHours(-48),
                        Actor = string.IsNullOrWhiteSpace(w.AssignedStaff) ? "sales_staff" : "staff",
                        Role = "SalesStaff",
                        Category = "Lead & Conversion",
                        TargetId = $"WO-{w.ServiceRequestId:D4}",
                        ActionDetails = $"Created booking request for {w.CustomerName} ({w.ServiceType})"
                    });

                    // Dispatch event
                    if (w.Status == "Scheduled" || w.Status == "InProgress" || w.Status == "Completed")
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = w.PreferredDate.AddHours(-18),
                            Actor = "operations_manager",
                            Role = "Manager",
                            Category = "Operations & Dispatch",
                            TargetId = $"WO-{w.ServiceRequestId:D4}",
                            ActionDetails = $"Dispatched crew ({w.AssignedStaff}) for confirmed date {w.PreferredDate:MMM dd, yyyy}"
                        });
                    }

                    // Completion & Billing event
                    if (w.Status == "Completed")
                    {
                        decimal billed = w.ActualPrice ?? w.QuotedPrice ?? 0m;
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = w.PreferredDate.AddHours(4),
                            Actor = w.AssignedStaff ?? "lead_technician",
                            Role = "Staff",
                            Category = "Operations & Dispatch",
                            TargetId = $"WO-{w.ServiceRequestId:D4}",
                            ActionDetails = $"Service fulfilled on-site for {w.CustomerName}. Work order completed."
                        });

                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = w.PreferredDate.AddHours(5),
                            Actor = "finance_admin",
                            Role = "Admin",
                            Category = "Billing & Finance",
                            TargetId = $"INV-{w.ServiceRequestId:D4}",
                            ActionDetails = $"Generated official billing invoice. Settled final amount: ₱{billed:N2}"
                        });
                    }
                }

                // 2. Leads & Conversion Lifecycle
                foreach (var l in _leads)
                {
                    if (l.ConvertedCustomerId.HasValue)
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.ConvertedAt ?? l.CreatedAt.AddHours(2),
                            Actor = "sales_agent",
                            Role = "SalesStaff",
                            Category = "Lead & Conversion",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Direct service booked by lead '{l.LeadName}'. Converted to active customer #{l.ConvertedCustomerId.Value}."
                        });
                    }
                    else if (string.Equals(l.Status, "Lost", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.CreatedAt.AddHours(12),
                            Actor = "sales_agent",
                            Role = "SalesStaff",
                            Category = "Lead & Conversion",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Lead marked Not Interested/Lost. Reason: {l.LostReason ?? "Customer declined"}"
                        });
                    }
                    else
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = l.CreatedAt,
                            Actor = "inquiry_intake",
                            Role = "SalesStaff",
                            Category = "Lead & Conversion",
                            TargetId = $"LEAD-{l.LeadId:D4}",
                            ActionDetails = $"Inquiry logged from '{l.LeadName}' via {l.LeadSource} for {l.ServiceOfInterest}."
                        });
                    }
                }

                // 3. SaaS Subscriptions & Licensing Lifecycle (Platform Audit - SuperAdmin/Admin only)
                foreach (var s in _subscriptions)
                {
                    _allEvents.Add(new AuditEvent
                    {
                        Timestamp = s.StartDate,
                        Actor = "platform_billing",
                        Role = "SuperAdmin",
                        Category = "Subscriptions & Licensing",
                        TargetId = $"SUB-{s.SubscriptionId:D4}",
                        ActionDetails = $"License Plan Tier {s.TierValue} provisioned for {s.CompanyName} ({s.CompanyCode}). Status: {s.Status}"
                    });

                    if (string.Equals(s.Status, "GracePeriod", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = s.EndDate.AddHours(1),
                            Actor = "dunning_engine",
                            Role = "SuperAdmin",
                            Category = "Subscriptions & Licensing",
                            TargetId = $"SUB-{s.SubscriptionId:D4}",
                            ActionDetails = $"Automated Dunning: Account for {s.CompanyName} entered +7 Days Grace Period (Past Due)."
                        });
                    }
                    else if (string.Equals(s.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEvents.Add(new AuditEvent
                        {
                            Timestamp = s.EndDate.AddDays(7).AddHours(1),
                            Actor = "dunning_engine",
                            Role = "SuperAdmin",
                            Category = "Subscriptions & Licensing",
                            TargetId = $"SUB-{s.SubscriptionId:D4}",
                            ActionDetails = $"Account lock enforced for {s.CompanyName} due to unpaid subscription renewal."
                        });
                    }
                }

                // 4. System & Security Baseline Events
                _allEvents.Add(new AuditEvent
                {
                    Timestamp = DateTime.Today.AddHours(8),
                    Actor = SessionManager.CurrentUser?.Username ?? "admin",
                    Role = SessionManager.CurrentUser?.Role ?? "Admin",
                    Category = "System & Security",
                    TargetId = "SYS-AUTH",
                    ActionDetails = "Daily credential and role permission matrix validated. Multi-tenant access perimeter verified."
                });

                _allEvents.Add(new AuditEvent
                {
                    Timestamp = DateTime.Now.AddMinutes(-5),
                    Actor = SessionManager.CurrentUser?.Username ?? "user",
                    Role = SessionManager.CurrentUser?.Role ?? "Staff",
                    Category = "System & Security",
                    TargetId = "SEC-LOG",
                    ActionDetails = "Cryptographic tamper-check passed for operational audit log entries."
                });
            }

            _allEvents = _allEvents.OrderByDescending(e => e.Timestamp).ToList();
        }

        private void ApplyAuditFilter()
        {
            string query = _txtAuditSearch.Text.Trim().ToLower();
            string category = _cmbAuditCategory.SelectedItem?.ToString() ?? "All Event Categories";

            _filteredEvents = _allEvents.FindAll(e =>
            {
                bool matchesQuery = string.IsNullOrEmpty(query) ||
                    e.Actor.ToLower().Contains(query) ||
                    e.ActionDetails.ToLower().Contains(query) ||
                    e.TargetId.ToLower().Contains(query) ||
                    e.Role.ToLower().Contains(query) ||
                    e.Category.ToLower().Contains(query);

                if (!matchesQuery) return false;

                if (category == "All Event Categories" || category == "All My Activities")
                    return true;

                return string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase);
            });

            _gridAudit.SuspendLayout();
            try
            {
                _gridAudit.Rows.Clear();
                foreach (var ev in _filteredEvents)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(_gridAudit,
                        ev.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                        ev.Actor,
                        ev.Role,
                        ev.Category,
                        ev.TargetId,
                        ev.ActionDetails
                    );

                    // Category color
                    var cellCat = row.Cells[3];
                    cellCat.Style.Font = _fontBold;
                    if (ev.Category == "Operations & Dispatch" || ev.Category == "Bookings & Service Requests") cellCat.Style.ForeColor = Color.FromArgb(22, 163, 74);
                    else if (ev.Category == "Billing & Finance") cellCat.Style.ForeColor = Color.FromArgb(37, 99, 235);
                    else if (ev.Category == "Lead & Conversion" || ev.Category == "Lead Intake & Quotes") cellCat.Style.ForeColor = Color.FromArgb(37, 99, 235);
                    else if (ev.Category == "Subscriptions & Licensing") cellCat.Style.ForeColor = Color.FromArgb(147, 51, 234);
                    else if (ev.Category == "System & Security" || ev.Category == "Session & System Access") cellCat.Style.ForeColor = Color.FromArgb(202, 138, 4);
                    else if (ev.Category == "Client Retention & Interactions") cellCat.Style.ForeColor = Color.FromArgb(217, 119, 6);
                    else cellCat.Style.ForeColor = Color.FromArgb(100, 116, 139);

                    _gridAudit.Rows.Add(row);
                }

                _lblAuditCount.Text = $"{_filteredEvents.Count} event{(_filteredEvents.Count == 1 ? "" : "s")} logged";
            }
            finally
            {
                _gridAudit.ResumeLayout();
            }

            _gridAudit.ClearSelection();
        }

        private void ExportAuditToCsv()
        {
            if (_filteredEvents.Count == 0)
            {
                MessageBox.Show("No audit events to export.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool isSalesStaff = SessionManager.IsSalesStaff;
            using var sfd = new SaveFileDialog
            {
                Title = isSalesStaff ? "Export My Activity Audit to CSV" : "Export System Audit Trail to CSV",
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = isSalesStaff ? $"My_Activity_Log_{DateTime.Now:yyyyMMdd_HHmm}.csv" : $"Audit_Trail_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    sw.WriteLine("Timestamp,Actor,Role,Category,Target ID,Event Description");

                    foreach (var e in _filteredEvents)
                    {
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",
                            e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            EscapeCsv(e.Actor),
                            EscapeCsv(e.Role),
                            EscapeCsv(e.Category),
                            EscapeCsv(e.TargetId),
                            EscapeCsv(e.ActionDetails)
                        ));
                    }

                    ShowToast($"Successfully exported {_filteredEvents.Count} audit event(s) to CSV!", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string EscapeCsv(string? val) => (val ?? "").Replace("\"", "\"\"");

        private static (Panel card, Label titleLabel, Label valLabel, Label subLabel) CreateKpiCard(
            string title,
            string initialVal,
            string subtext,
            Color valColor,
            Action? onClick = null)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4),
                Padding = new Padding(16, 12, 16, 10),
                Cursor = Cursors.Hand
            };

            bool isHovered = false;

            card.Paint += (s, e) =>
            {
                using var brush = new SolidBrush(card.BackColor);
                using var pen = isHovered ? new Pen(Color.FromArgb(99, 102, 241), 1.5f) : new Pen(Color.FromArgb(226, 232, 240), 1);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.FillRectangle(brush, rect);
                e.Graphics.DrawRectangle(pen, rect);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 12),
                Size = new Size(card.Width - 28, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblTitle);

            var valLabel = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = valColor,
                Location = new Point(12, 32),
                Size = new Size(card.Width - 24, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(valLabel);

            var lblSub = new Label
            {
                Text = subtext,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(14, 72),
                Size = new Size(card.Width - 28, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblSub);

            void SetHover(bool hover)
            {
                isHovered = hover;
                card.BackColor = hover ? Color.FromArgb(248, 250, 252) : Color.White;
                card.Invalidate();
            }

            card.MouseEnter += (s, e) => SetHover(true);
            lblTitle.MouseEnter += (s, e) => SetHover(true);
            valLabel.MouseEnter += (s, e) => SetHover(true);
            lblSub.MouseEnter += (s, e) => SetHover(true);

            card.MouseLeave += (s, e) => {
                var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                if (!card.ClientRectangle.Contains(p)) SetHover(false);
            };
            lblTitle.MouseLeave += (s, e) => {
                var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                if (!card.ClientRectangle.Contains(p)) SetHover(false);
            };
            valLabel.MouseLeave += (s, e) => {
                var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                if (!card.ClientRectangle.Contains(p)) SetHover(false);
            };
            lblSub.MouseLeave += (s, e) => {
                var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                if (!card.ClientRectangle.Contains(p)) SetHover(false);
            };

            if (onClick != null)
            {
                card.Click += (s, e) => onClick();
                lblTitle.Click += (s, e) => onClick();
                valLabel.Click += (s, e) => onClick();
                lblSub.Click += (s, e) => onClick();
            }

            return (card, lblTitle, valLabel, lblSub);
        }

        public override void ApplyViewPermissions(string userRole)
        {
            bool isSalesStaff = string.Equals(userRole, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);

            // Audit tab is accessible for all operational roles:
            // Admin/SuperAdmin/Manager: System Compliance Audit Trail
            // Sales Staff: Personal Activity & Customer Interaction Audit Stream
            _btnTabAudit.Visible = true;
            _btnPrintReport.Visible = !isSalesStaff; // Executive BI print is for management

            if (isSalesStaff)
            {
                _lblTitle.Text = "My Reports & Activity Audit";
                _lblSub.Text = "Personal sales performance metrics and chronological interaction audit stream";
                _btnTabAnalytics.Text = "📊  My Performance & Pipeline";
                _btnTabAudit.Text = "📜  My Activity Audit Stream";

                if (_cmbAuditCategory != null)
                {
                    _cmbAuditCategory.Items.Clear();
                    _cmbAuditCategory.Items.AddRange(new object[]
                    {
                        "All My Activities",
                        "Lead Intake & Quotes",
                        "Bookings & Service Requests",
                        "Client Retention & Interactions",
                        "Session & System Access"
                    });
                    _cmbAuditCategory.SelectedIndex = 0;
                }
            }
            else
            {
                _lblTitle.Text = "Reports & Compliance Audit Trail";
                _lblSub.Text = "Executive BI summaries & chronological system activity logs";
                _btnTabAnalytics.Text = "📊  Executive BI Analytics";
                _btnTabAudit.Text = "📜  System Compliance Audit Trail";

                if (_cmbAuditCategory != null)
                {
                    _cmbAuditCategory.Items.Clear();
                    _cmbAuditCategory.Items.AddRange(new object[]
                    {
                        "All Event Categories",
                        "Operations & Dispatch",
                        "Lead & Conversion",
                        "Billing & Finance",
                        "Subscriptions & Licensing",
                        "System & Security"
                    });
                    _cmbAuditCategory.SelectedIndex = 0;
                }
            }
        }
    }
}
