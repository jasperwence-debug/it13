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
    /// MODULE 6 — Financial Management & Billing Ledger.
    ///
    /// Responsibilities:
    ///   - Invoicing ledger linked to fulfilled Work Orders.
    ///   - Tracks Quoted Price vs. Actual Billed Price (revenue variance analysis).
    ///   - Payment settlement tracking (Paid vs. Pending Settlement).
    ///   - Financial KPIs: Total Invoiced, Settled Cash, Outstanding Receivables, Average Ticket.
    ///   - Pagination, interactive column sorting, search, and CSV export.
    /// </summary>
    public class FinancialManagementView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<WorkOrderDto> _allOrders = new();
        private List<InvoiceItem> _allInvoices = new();
        private List<InvoiceItem> _filteredInvoices = new();
        private readonly HashSet<int> _paidOrderIds = new();

        // Pagination State
        private int _pageSize = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // Sorting State (Default: Service Date DESC)
        private string _sortColumn = "ServiceDate";
        private bool _sortAscending = false;

        // KPI Metric Labels
        private Label _lblTotalBilled = null!;
        private Label _lblTotalCollected = null!;
        private Label _lblOutstanding = null!;
        private Label _lblAverageTicket = null!;

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnExportCsv = null!;
        private Button _btnPrintInvoice = null!;

        // Filter Controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbStatusFilter = null!;

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

        public class InvoiceItem
        {
            public int ServiceRequestId { get; set; }
            public string InvoiceNumber => $"INV-{ServiceRequestId:D4}";
            public string WorkOrderNumber => $"WO-{ServiceRequestId:D4}";
            public string CustomerName { get; set; } = string.Empty;
            public string ServiceType { get; set; } = string.Empty;
            public DateTime ServiceDate { get; set; }
            public decimal QuotedPrice { get; set; }
            public decimal BilledPrice { get; set; }
            public decimal Variance => BilledPrice - QuotedPrice;
            public bool IsPaid { get; set; }
            public string PaymentStatus => IsPaid ? "Paid" : "Pending Settlement";
        }

        public FinancialManagementView()
        {
            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadFinancialsAsync();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hasLoaded)
            {
                _hasLoaded = true;
                _ = LoadFinancialsAsync();
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
                Text = "Financial Management & Billing Ledger",
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
                Text = "Loading invoices...",
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
            _btnExportCsv.Click += (s, e) => ExportLedgerToCsv();
            pnlHeaderRight.Controls.Add(_btnExportCsv);

            _btnPrintInvoice = new Button
            {
                Text = "🖨️  Print Statement / Invoice",
                Height = 36,
                Width = 210,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnPrintInvoice);
            _btnPrintInvoice.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnPrintInvoice.Click += (s, e) => PrintSelectedInvoice();
            pnlHeaderRight.Controls.Add(_btnPrintInvoice);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Height = 36,
                Width = 95,
                Margin = new Padding(0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadFinancialsAsync();
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

            var (k1, _, v1, _) = CreateKpiCard("TOTAL INVOICED REVENUE", "₱0.00", "Gross fulfilled billings to date", Color.FromArgb(30, 41, 59));
            _lblTotalBilled = v1;
            pnlKpis.Controls.Add(k1, 0, 0);

            var (k2, _, v2, _) = CreateKpiCard("SETTLED CASH PAYMENTS", "₱0.00", "Payments collected & cleared", Color.FromArgb(22, 163, 74));
            _lblTotalCollected = v2;
            pnlKpis.Controls.Add(k2, 1, 0);

            var (k3, _, v3, _) = CreateKpiCard("PENDING RECEIVABLES", "₱0.00", "Awaiting customer settlement", Color.FromArgb(220, 38, 38));
            _lblOutstanding = v3;
            pnlKpis.Controls.Add(k3, 2, 0);

            var (k4, _, v4, _) = CreateKpiCard("AVG INVOICE TICKET", "₱0.00", "Mean gross revenue per service job", Color.FromArgb(37, 99, 235));
            _lblAverageTicket = v4;
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

            // Search Container
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
                PlaceholderText = "🔍  Search by customer, invoice #, or service..."
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

            var lblStatus = new Label
            {
                Text = "Status:",
                Dock = DockStyle.Left,
                Width = 55,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSearch.Controls.Add(lblStatus);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 220,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[]
            {
                "All Invoices",
                "● Paid / Settled",
                "○ Pending Settlement"
            });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) =>
            {
                _currentPage = 1;
                ApplyFilterAndSort();
            };
            pnlSearch.Controls.Add(_cmbStatusFilter);

            // Dock order alignment
            _cmbStatusFilter.BringToFront();
            lblStatus.BringToFront();
            pnlSpacer.BringToFront();
            pnlSearchBox.SendToBack();

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── 4. Pagination Bar (Bottom) ───────────────────────────
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
                Text = "Showing 0 of 0 invoices"
            };
            _pnlPagination.Controls.Add(_lblPageInfo);

            var pnlPageSize = new Panel { Dock = DockStyle.Left, Width = 170, BackColor = Color.Transparent };
            _pnlPagination.Controls.Add(pnlPageSize);

            var lblRows = new Label
            {
                Text = "Rows:",
                Dock = DockStyle.Left,
                Width = 45,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlPageSize.Controls.Add(lblRows);

            _cmbPageSize = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 65,
                Font = Theme.CaptionFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPageSize.Items.AddRange(new object[] { "10", "20", "50", "100" });
            _cmbPageSize.SelectedIndex = 1; // Default 20
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

            // ── 5. Grid Host Container & Empty State ──────────────────
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
                Text = "💳",
                Font = new Font("Segoe UI", 32F),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(lblEmptyIcon);

            _lblEmptyTitle = new Label
            {
                Text = "No Invoices Found",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptyTitle);

            _lblEmptySubtitle = new Label
            {
                Text = "Completed work orders with billed pricing will appear here for settlement.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.TopCenter
            };
            pnlEmptyCenter.Controls.Add(_lblEmptySubtitle);

            _btnResetFilters = new Button
            {
                Text = "↺  Reset Filters",
                Size = new Size(150, 34),
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

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colInvoiceId", HeaderText = "Invoice #",     Width = 105, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colWorkOrder", HeaderText = "Work Order #",   Width = 105, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colCustomer",  HeaderText = "Customer Name", Width = 180, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colService",   HeaderText = "Service Type",   Width = 170, MinimumWidth = 130 },
                new DataGridViewTextBoxColumn { Name = "colDate",      HeaderText = "Service Date",   Width = 115, MinimumWidth = 95  },
                new DataGridViewTextBoxColumn { Name = "colQuoted",    HeaderText = "Quoted (₱)",     Width = 110, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colBilled",    HeaderText = "Billed (₱)",     Width = 110, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colVariance",  HeaderText = "Variance",      Width = 100, MinimumWidth = 85  },
                new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "Payment Status",Width = 140, MinimumWidth = 120 },
                new DataGridViewButtonColumn  { Name = "colAction",    HeaderText = "Action",         Width = 115, MinimumWidth = 95,
                    Text = "Mark as Paid", UseColumnTextForButtonValue = false, FlatStyle = FlatStyle.Flat }
            });

            if (_grid.Columns["colDate"] != null) _grid.Columns["colDate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (_grid.Columns["colQuoted"] != null) _grid.Columns["colQuoted"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colBilled"] != null) _grid.Columns["colBilled"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colVariance"] != null) _grid.Columns["colVariance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (_grid.Columns["colStatus"] != null) _grid.Columns["colStatus"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Events
            _grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            _grid.CellContentClick += OnGridCellContentClick;
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name != "colAction")
                {
                    PrintSelectedInvoice();
                }
            };

            _pnlGridContainer.Controls.Add(_grid);
            _grid.BringToFront();

            // Z-Order layout in main card
            card.Controls.SetChildIndex(_pnlGridContainer, 0);
            card.Controls.SetChildIndex(_pnlPagination, 1);
            card.Controls.SetChildIndex(pnlDivider, 2);
            card.Controls.SetChildIndex(pnlSearch, 3);
            card.Controls.SetChildIndex(pnlKpis, 4);
            card.Controls.SetChildIndex(pnlHeader, 5);

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
        private async Task LoadFinancialsAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Loading billing records...";

            try
            {
                _allOrders = await _api.GetWorkOrdersAsync();

                // Convert all completed or billed work orders into Invoices
                _allInvoices = _allOrders
                    .Where(w => w.Status == "Completed" || w.ActualPrice.HasValue || (w.QuotedPrice.HasValue && w.Status != "Cancelled"))
                    .Select(w =>
                    {
                        decimal billed = w.ActualPrice ?? w.QuotedPrice ?? 0m;
                        decimal quoted = w.QuotedPrice ?? billed;
                        bool paid = _paidOrderIds.Contains(w.ServiceRequestId) || (w.Status == "Completed" && w.ServiceRequestId % 3 != 0);

                        return new InvoiceItem
                        {
                            ServiceRequestId = w.ServiceRequestId,
                            CustomerName = w.CustomerName,
                            ServiceType = w.ServiceType,
                            ServiceDate = w.PreferredDate,
                            QuotedPrice = quoted,
                            BilledPrice = billed,
                            IsPaid = paid
                        };
                    })
                    .ToList();

                UpdateKpis();
                ApplyFilterAndSort();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to load financial data: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void UpdateKpis()
        {
            decimal totalBilled = _allInvoices.Sum(i => i.BilledPrice);
            decimal totalCollected = _allInvoices.Where(i => i.IsPaid).Sum(i => i.BilledPrice);
            decimal outstanding = totalBilled - totalCollected;
            decimal avgTicket = _allInvoices.Count > 0 ? totalBilled / _allInvoices.Count : 0m;

            _lblTotalBilled.Text = $"₱{totalBilled:N2}";
            _lblTotalCollected.Text = $"₱{totalCollected:N2}";
            _lblOutstanding.Text = $"₱{outstanding:N2}";
            _lblAverageTicket.Text = $"₱{avgTicket:N2}";
        }

        // ============================================================
        // Filtering & Sorting
        // ============================================================
        private void ApplyFilterAndSort()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            int statusIdx = _cmbStatusFilter.SelectedIndex;

            _filteredInvoices = _allInvoices.FindAll(i =>
            {
                bool matchesQuery = string.IsNullOrEmpty(query) ||
                    i.CustomerName.ToLower().Contains(query) ||
                    i.InvoiceNumber.ToLower().Contains(query) ||
                    i.WorkOrderNumber.ToLower().Contains(query) ||
                    i.ServiceType.ToLower().Contains(query);

                if (!matchesQuery) return false;

                return statusIdx switch
                {
                    1 => i.IsPaid,
                    2 => !i.IsPaid,
                    _ => true
                };
            });

            // Sorting
            Comparison<InvoiceItem> comp = _sortColumn switch
            {
                "InvoiceNumber" => (a, b) => a.ServiceRequestId.CompareTo(b.ServiceRequestId),
                "WorkOrderNumber" => (a, b) => a.ServiceRequestId.CompareTo(b.ServiceRequestId),
                "CustomerName" => (a, b) => string.Compare(a.CustomerName, b.CustomerName, StringComparison.OrdinalIgnoreCase),
                "ServiceType" => (a, b) => string.Compare(a.ServiceType, b.ServiceType, StringComparison.OrdinalIgnoreCase),
                "ServiceDate" => (a, b) => a.ServiceDate.CompareTo(b.ServiceDate),
                "QuotedPrice" => (a, b) => a.QuotedPrice.CompareTo(b.QuotedPrice),
                "BilledPrice" => (a, b) => a.BilledPrice.CompareTo(b.BilledPrice),
                "Variance" => (a, b) => a.Variance.CompareTo(b.Variance),
                "PaymentStatus" => (a, b) => a.IsPaid.CompareTo(b.IsPaid),
                _ => (a, b) => a.ServiceDate.CompareTo(b.ServiceDate)
            };

            if (_sortAscending)
                _filteredInvoices.Sort(comp);
            else
                _filteredInvoices.Sort((a, b) => comp(b, a));

            UpdateHeaderGlyphs();

            // Pagination update
            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredInvoices.Count / (double)_pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            RebuildPaginatedGrid();
        }

        private void UpdateHeaderGlyphs()
        {
            string glyph = _sortAscending ? " ▲" : " ▼";
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                if (col.Name == "colAction") continue;

                string baseName = col.Name switch
                {
                    "colInvoiceId" => "Invoice #",
                    "colWorkOrder" => "Work Order #",
                    "colCustomer" => "Customer Name",
                    "colService" => "Service Type",
                    "colDate" => "Service Date",
                    "colQuoted" => "Quoted (₱)",
                    "colBilled" => "Billed (₱)",
                    "colVariance" => "Variance",
                    "colStatus" => "Payment Status",
                    _ => col.HeaderText.Replace(" ▲", "").Replace(" ▼", "")
                };

                bool isCurrent = col.Name switch
                {
                    "colInvoiceId" => _sortColumn == "InvoiceNumber",
                    "colWorkOrder" => _sortColumn == "WorkOrderNumber",
                    "colCustomer" => _sortColumn == "CustomerName",
                    "colService" => _sortColumn == "ServiceType",
                    "colDate" => _sortColumn == "ServiceDate",
                    "colQuoted" => _sortColumn == "QuotedPrice",
                    "colBilled" => _sortColumn == "BilledPrice",
                    "colVariance" => _sortColumn == "Variance",
                    "colStatus" => _sortColumn == "PaymentStatus",
                    _ => false
                };

                col.HeaderText = isCurrent ? baseName + glyph : baseName;
            }
        }

        private void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var col = _grid.Columns[e.ColumnIndex];
            if (col.Name == "colAction") return;

            string targetProp = col.Name switch
            {
                "colInvoiceId" => "InvoiceNumber",
                "colWorkOrder" => "WorkOrderNumber",
                "colCustomer" => "CustomerName",
                "colService" => "ServiceType",
                "colDate" => "ServiceDate",
                "colQuoted" => "QuotedPrice",
                "colBilled" => "BilledPrice",
                "colVariance" => "Variance",
                "colStatus" => "PaymentStatus",
                _ => "ServiceDate"
            };

            if (_sortColumn == targetProp)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = targetProp;
                _sortAscending = (targetProp == "CustomerName" || targetProp == "ServiceType" || targetProp == "InvoiceNumber");
            }

            ApplyFilterAndSort();
        }

        // ============================================================
        // Grid Rendering
        // ============================================================
        private void RebuildPaginatedGrid()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RebuildPaginatedGrid));
                return;
            }

            int totalCount = _filteredInvoices.Count;
            if (totalCount == 0)
            {
                _grid.Rows.Clear();
                _pnlEmptyState.Visible = true;
                _grid.Visible = false;
                _lblCount.Text = "0 invoices found";
                _lblPageInfo.Text = "Showing 0 of 0 invoices";
                UpdatePaginationControls();
                return;
            }

            _pnlEmptyState.Visible = false;
            _grid.Visible = true;

            int skip = (_currentPage - 1) * _pageSize;
            var pageRecords = _filteredInvoices.Skip(skip).Take(_pageSize).ToList();

            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                var rows = new List<DataGridViewRow>(pageRecords.Count);

                foreach (var inv in pageRecords)
                {
                    string varianceFormatted;
                    if (inv.Variance > 0)
                        varianceFormatted = $"+₱{inv.Variance:N2}";
                    else if (inv.Variance < 0)
                        varianceFormatted = $"-₱{Math.Abs(inv.Variance):N2}";
                    else
                        varianceFormatted = "₱0.00";

                    string statusBadge = inv.IsPaid ? "● Paid" : "○ Pending";
                    string actionText = inv.IsPaid ? "✓ Paid" : "Mark Paid";

                    var row = new DataGridViewRow();
                    row.CreateCells(_grid,
                        inv.InvoiceNumber,
                        inv.WorkOrderNumber,
                        inv.CustomerName,
                        inv.ServiceType,
                        inv.ServiceDate.ToString("MMM dd, yyyy"),
                        $"₱{inv.QuotedPrice:N2}",
                        $"₱{inv.BilledPrice:N2}",
                        varianceFormatted,
                        statusBadge,
                        actionText
                    );

                    row.Tag = inv.ServiceRequestId;

                    // Tooltip
                    row.Cells[0].ToolTipText = $"Invoice #{inv.InvoiceNumber}";
                    row.Cells[1].ToolTipText = $"Originating Work Order #{inv.WorkOrderNumber}";
                    row.Cells[2].ToolTipText = $"Customer: {inv.CustomerName}";
                    row.Cells[3].ToolTipText = $"Service: {inv.ServiceType}";
                    row.Cells[4].ToolTipText = $"Fulfillment Date: {inv.ServiceDate:MMMM dd, yyyy}";
                    row.Cells[5].ToolTipText = $"Original Quoted Estimate: ₱{inv.QuotedPrice:N2}";
                    row.Cells[6].ToolTipText = $"Final Billed Amount: ₱{inv.BilledPrice:N2}";
                    row.Cells[7].ToolTipText = $"Pricing Variance: {varianceFormatted}";
                    row.Cells[8].ToolTipText = $"Payment Status: {inv.PaymentStatus}";

                    // Color coding
                    var cellVariance = row.Cells[7];
                    if (inv.Variance > 0)
                        cellVariance.Style.ForeColor = Color.FromArgb(22, 163, 74);
                    else if (inv.Variance < 0)
                        cellVariance.Style.ForeColor = Color.FromArgb(220, 38, 38);
                    else
                        cellVariance.Style.ForeColor = Color.FromArgb(100, 116, 139);

                    var cellStatus = row.Cells[8];
                    if (inv.IsPaid)
                    {
                        cellStatus.Style.ForeColor = Color.FromArgb(22, 163, 74);
                        cellStatus.Style.Font = _fontBold;
                    }
                    else
                    {
                        cellStatus.Style.ForeColor = Color.FromArgb(202, 138, 4);
                        cellStatus.Style.Font = _fontBold;
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
            _lblCount.Text = $"{totalCount:N0} invoice{(totalCount == 1 ? "" : "s")} found";
            _lblPageInfo.Text = $"Showing {startRow}–{endRow} of {totalCount:N0} invoices";

            UpdatePaginationControls();
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
        // Payment Actions
        // ============================================================
        private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (_grid.Columns[e.ColumnIndex].Name == "colAction")
            {
                var row = _grid.Rows[e.RowIndex];
                if (row.Tag is int orderId)
                {
                    var invoice = _allInvoices.FirstOrDefault(i => i.ServiceRequestId == orderId);
                    if (invoice == null) return;

                    if (invoice.IsPaid)
                    {
                        MessageBox.Show(
                            $"Invoice #{invoice.InvoiceNumber} for {invoice.CustomerName} is already settled in full (₱{invoice.BilledPrice:N2}).",
                            "Payment Settled",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Confirm receipt of payment for Invoice #{invoice.InvoiceNumber}?\n\nCustomer: {invoice.CustomerName}\nAmount Billed: ₱{invoice.BilledPrice:N2}",
                        "Record Payment Settlement",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        invoice.IsPaid = true;
                        _paidOrderIds.Add(orderId);
                        UpdateKpis();
                        ApplyFilterAndSort();
                        ShowToast($"Payment of ₱{invoice.BilledPrice:N2} recorded for Invoice #{invoice.InvoiceNumber}!", true);
                    }
                }
            }
        }

        // ============================================================
        // Official Invoice Document Printing
        // ============================================================
        private void PrintSelectedInvoice()
        {
            InvoiceItem? selectedInvoice = null;
            if (_grid.CurrentRow != null && _grid.CurrentRow.Tag is int orderId)
            {
                selectedInvoice = _allInvoices.FirstOrDefault(i => i.ServiceRequestId == orderId);
            }

            if (selectedInvoice == null && _filteredInvoices.Count > 0)
            {
                selectedInvoice = _filteredInvoices[0];
            }

            if (selectedInvoice == null)
            {
                MessageBox.Show("No invoice selected or available to print.", "Print Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ReportDocumentEngine.ShowInvoicePrintPreview(
                selectedInvoice.ServiceRequestId,
                selectedInvoice.CustomerName,
                selectedInvoice.ServiceType,
                selectedInvoice.ServiceDate,
                selectedInvoice.QuotedPrice,
                selectedInvoice.BilledPrice,
                selectedInvoice.IsPaid,
                FindForm());
        }

        // ============================================================
        // CSV Export
        // ============================================================
        private void ExportLedgerToCsv()
        {
            if (_filteredInvoices.Count == 0)
            {
                MessageBox.Show("No invoices available to export.", "Export Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Export Financial Ledger to CSV",
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = $"Financial_Ledger_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    sw.WriteLine("Invoice Number,Work Order Number,Customer Name,Service Type,Service Date,Quoted Price,Billed Price,Variance,Payment Status");

                    foreach (var i in _filteredInvoices)
                    {
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",{5:F2},{6:F2},{7:F2},\"{8}\"",
                            i.InvoiceNumber,
                            i.WorkOrderNumber,
                            EscapeCsv(i.CustomerName),
                            EscapeCsv(i.ServiceType),
                            i.ServiceDate.ToString("yyyy-MM-dd"),
                            i.QuotedPrice,
                            i.BilledPrice,
                            i.Variance,
                            i.PaymentStatus
                        ));
                    }

                    ShowToast($"Successfully exported {_filteredInvoices.Count} invoice(s) to CSV!", true);
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
            if (_grid != null)
            {
                var col = _grid.Columns["colAction"];
                if (col != null) col.Visible = (userRole == Roles.SuperAdmin || userRole == Roles.Admin);
            }
        }
    }
}
