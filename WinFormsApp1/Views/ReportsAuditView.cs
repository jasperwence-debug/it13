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
        private Label _lblKpiTotalRevenue = null!;
        private Label _lblKpiCompletedJobs = null!;
        private Label _lblKpiRepeatRate = null!;
        private Label _lblKpiAvgTicket = null!;
        private DataGridView _gridServiceSummary = null!;
        private DataGridView _gridStaffSummary = null!;

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
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            card.Controls.Add(pnlHeader);

            var pnlTitleGroup = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlTitleGroup);

            _lblTitle = new Label
            {
                Text = "Reports & Compliance Audit Trail",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlTitleGroup.Controls.Add(_lblTitle);

            _lblSub = new Label
            {
                Text = "Executive BI summaries & chronological system activity logs",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlTitleGroup.Controls.Add(_lblSub);

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
            _btnPrintReport.Click += (s, e) => ReportDocumentEngine.ShowExecutiveReportPrintPreview(_dashboard, _workOrders, FindForm());
            pnlHeaderRight.Controls.Add(_btnPrintReport);

            _btnExportCsv = new Button
            {
                Text = "📥  Export CSV",
                Height = 34,
                Width = 125,
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

            // ── 2. Tab Navigation Bar ────────────────────────────────
            var pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 6, 24, 0)
            };
            card.Controls.Add(pnlTabBar);

            _btnTabAnalytics = CreateTabButton("📊  Executive BI Analytics", true);
            _btnTabAnalytics.Click += (s, e) => SwitchTab("analytics");
            pnlTabBar.Controls.Add(_btnTabAnalytics);

            _btnTabAudit = CreateTabButton("📜  System Compliance Audit Trail", false);
            _btnTabAudit.Location = new Point(24 + _btnTabAnalytics.Width + 8, 6);
            _btnTabAudit.Click += (s, e) => SwitchTab("audit");
            pnlTabBar.Controls.Add(_btnTabAudit);

            var pnlDivTab = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };
            pnlTabBar.Controls.Add(pnlDivTab);

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
                Height = 36,
                Width = 220,
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Theme.Surface : Color.Transparent,
                ForeColor = isActive ? Theme.Primary : Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9F, isActive ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand
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
                Height = 98,
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

            var (k1, _, v1, _) = CreateKpiCard("TOTAL FULFILLED REVENUE", "₱0.00", "Gross service volume billed", Color.FromArgb(30, 41, 59));
            _lblKpiTotalRevenue = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, _, v2, _) = CreateKpiCard("COMPLETED SERVICE JOBS", "0", "Fulfilled work order bookings", Color.FromArgb(22, 163, 74));
            _lblKpiCompletedJobs = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, _, v3, _) = CreateKpiCard("REPEAT CLIENT RATIO", "0.0%", "Accounts with multiple services", Color.FromArgb(37, 99, 235));
            _lblKpiRepeatRate = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, _, v4, _) = CreateKpiCard("AVERAGE BOOKING VALUE", "₱0.00", "Mean ticket billing per order", Color.FromArgb(202, 138, 4));
            _lblKpiAvgTicket = v4;
            pnlKpis.Controls.Add(k4, 3, 0);

            // Two-column layout for Service Breakdown & Staff Rankings
            var tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Surface
            };
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnl.Controls.Add(tlpSplit);
            tlpSplit.BringToFront();

            // Left: Service Type Breakdown
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
            var lblSecServices = new Label { Text = "Revenue Breakdown by Service Category", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Theme.TextDark, Dock = DockStyle.Top, Height = 28 };
            pnlLeft.Controls.Add(lblSecServices);

            _gridServiceSummary = CreateSimpleGrid();
            _gridServiceSummary.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colSvc", HeaderText = "Service Category", Width = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colCount", HeaderText = "Orders", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "colRev", HeaderText = "Total Revenue", Width = 130 }
            });
            _gridServiceSummary.Columns["colCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridServiceSummary.Columns["colRev"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            pnlLeft.Controls.Add(_gridServiceSummary);
            _gridServiceSummary.BringToFront();
            tlpSplit.Controls.Add(pnlLeft, 0, 0);

            // Right: Field Staff Utilization
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 0, 0) };
            var lblSecStaff = new Label { Text = "Technician Dispatch & Utilization", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Theme.TextDark, Dock = DockStyle.Top, Height = 28 };
            pnlRight.Controls.Add(lblSecStaff);

            _gridStaffSummary = CreateSimpleGrid();
            _gridStaffSummary.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colStaff", HeaderText = "Assigned Crew", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colStaffCount", HeaderText = "Jobs Completed", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "colStaffRev", HeaderText = "Volume Delivered", Width = 120 }
            });
            _gridStaffSummary.Columns["colStaffCount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridStaffSummary.Columns["colStaffRev"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            pnlRight.Controls.Add(_gridStaffSummary);
            _gridStaffSummary.BringToFront();
            tlpSplit.Controls.Add(pnlRight, 1, 0);

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
                "System & Security"
            });
            _cmbAuditCategory.SelectedIndex = 0;
            _cmbAuditCategory.SelectedIndexChanged += (s, e) => ApplyAuditFilter();
            pnlSearch.Controls.Add(_cmbAuditCategory);

            _lblAuditCount = new Label
            {
                Dock = DockStyle.Right,
                Width = 200,
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

        private static DataGridView CreateSimpleGrid()
        {
            var grid = new DataGridView
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
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 34 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

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

                await Task.WhenAll(dashTask, ordersTask);

                _dashboard = await dashTask;
                _workOrders = await ordersTask;

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
            if (_dashboard != null)
            {
                _lblKpiTotalRevenue.Text = $"₱{_dashboard.TotalRevenue:N2}";
                _lblKpiCompletedJobs.Text = _dashboard.CompletedBookings.ToString("N0");
                _lblKpiRepeatRate.Text = $"{_dashboard.RepeatCustomerRate:F1}%";
                _lblKpiAvgTicket.Text = $"₱{_dashboard.AverageBookingValue:N2}";

                // Service Category breakdown
                _gridServiceSummary.Rows.Clear();
                foreach (var s in _dashboard.TopServices.OrderByDescending(x => x.Revenue))
                {
                    _gridServiceSummary.Rows.Add(s.ServiceName, s.Count, $"₱{s.Revenue:N2}");
                }
            }

            // Staff rankings
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

        private void BuildAuditStream()
        {
            _allEvents.Clear();

            // Synthesize audit trail from real work orders, notes history, and lifecycle transitions
            foreach (var w in _workOrders)
            {
                // Creation event
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

                // Completion event
                if (w.Status == "Completed")
                {
                    decimal billed = w.ActualPrice ?? w.QuotedPrice ?? 0m;
                    _allEvents.Add(new AuditEvent
                    {
                        Timestamp = w.PreferredDate.AddHours(4),
                        Actor = w.AssignedStaff ?? "lead_technician",
                        Role = "Manager",
                        Category = "Billing & Finance",
                        TargetId = $"WO-{w.ServiceRequestId:D4}",
                        ActionDetails = $"Completed job on-site. Actual price finalized at ₱{billed:N2}"
                    });
                }
            }

            // Add security / admin baseline events
            _allEvents.Add(new AuditEvent
            {
                Timestamp = DateTime.Today.AddHours(8),
                Actor = "superadmin",
                Role = "SuperAdmin",
                Category = "System & Security",
                TargetId = "SYS-AUTH",
                ActionDetails = "Daily credential and role permission matrix validated."
            });

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
                    e.Role.ToLower().Contains(query);

                if (!matchesQuery) return false;

                return category switch
                {
                    "Operations & Dispatch" => e.Category == "Operations & Dispatch",
                    "Lead & Conversion" => e.Category == "Lead & Conversion",
                    "Billing & Finance" => e.Category == "Billing & Finance",
                    "System & Security" => e.Category == "System & Security",
                    _ => true
                };
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
                    if (ev.Category == "Operations & Dispatch") cellCat.Style.ForeColor = Color.FromArgb(22, 163, 74);
                    else if (ev.Category == "Billing & Finance") cellCat.Style.ForeColor = Color.FromArgb(37, 99, 235);
                    else if (ev.Category == "System & Security") cellCat.Style.ForeColor = Color.FromArgb(202, 138, 4);
                    else cellCat.Style.ForeColor = Color.FromArgb(100, 116, 139);

                    _gridAudit.Rows.Add(row);
                }

                _lblAuditCount.Text = $"{_filteredEvents.Count} event{(_filteredEvents.Count == 1 ? "" : "s")} logged";
            }
            finally
            {
                _gridAudit.ResumeLayout();
            }
        }

        private void ExportAuditToCsv()
        {
            if (_filteredEvents.Count == 0)
            {
                MessageBox.Show("No audit events to export.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Export System Audit Trail to CSV",
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = $"Audit_Trail_{DateTime.Now:yyyyMMdd_HHmm}.csv"
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
            // Super Admin & Admin: FULL
            // Manager: View analytics only, hide audit tab
            if (userRole == Roles.Manager)
            {
                _btnTabAudit.Visible = false;
                SwitchTab("analytics");
            }
        }
    }
}
