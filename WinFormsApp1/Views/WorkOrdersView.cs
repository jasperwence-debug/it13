using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;
using App.WinForms.Reporting;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 3 — Work Orders View (Schedule / Dispatch).
    ///
    /// Grid columns:
    ///   [ Work Order # | Customer Name | Service Type | Scheduled Date | Staff Assigned | Status ]
    ///
    /// Top-right button opens NewWorkOrderDialog for creating new bookings.
    /// Requires an existing Customer — never creates customers inline.
    /// </summary>
    public class WorkOrdersView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<WorkOrderDto> _workOrders = new();

        // Header controls
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnDispatch = null!;
        private Button _btnFeedback = null!;
        private Button _btnPrintOrder = null!;
        private Button _btnNewOrder = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cmbStatusFilter = null!;

        // Grid
        private DataGridView _grid = null!;

        // Pagination state (default: 25 per page, newest first)
        private int _pageSize = 25;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<WorkOrderDto> _filteredWorkOrders = new();

        // Pagination controls
        private Panel _pnlPagination = null!;
        private Label _lblPageInfo = null!;
        private ComboBox _cmbPageSize = null!;
        private Button _btnFirstPage = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Button _btnLastPage = null!;
        private FlowLayoutPanel _pnlPageNumbers = null!;

        public WorkOrdersView()
        {
            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = LoadAsync();
        }

        // ============================================================
        // UI Construction
        // ============================================================
        private void BuildUI()
        {
            SuspendLayout();
            BackColor = Theme.Background;
            Dock = DockStyle.Fill;

            // Card wrapper
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            ApplyCardStyle(card);
            Controls.Add(card);

            // ── Card Header ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 0, 20, 0)
            };
            card.Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "Work Orders",
                Font = Theme.SubHeaderFont,
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 180,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            _lblCount = new Label
            {
                Text = "Loading...",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 120,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(_lblCount);

            _btnNewOrder = new Button
            {
                Text = "+ New Booking Request",
                Dock = DockStyle.Right,
                Width = 190
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewOrder);
            _btnNewOrder.Click += OnNewOrderClick;
            pnlHeader.Controls.Add(_btnNewOrder);

            _btnDispatch = new Button
            {
                Text = "⚡ Dispatch & Manage",
                Dock = DockStyle.Right,
                Width = 170,
                Enabled = false,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnDispatch);
            _btnDispatch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnDispatch.Click += OnDispatchClick;
            pnlHeader.Controls.Add(_btnDispatch);

            _btnFeedback = new Button
            {
                Text = "⭐ Feedback / QA",
                Dock = DockStyle.Right,
                Width = 145,
                Enabled = false,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnFeedback);
            _btnFeedback.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnFeedback.Click += OnFeedbackClick;
            pnlHeader.Controls.Add(_btnFeedback);

            _btnPrintOrder = new Button
            {
                Text = "🖨️  Print Work Order",
                Dock = DockStyle.Right,
                Width = 165,
                Enabled = false,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnPrintOrder);
            _btnPrintOrder.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnPrintOrder.Click += OnPrintOrderClick;
            pnlHeader.Controls.Add(_btnPrintOrder);

            _btnRefresh = new Button
            {
                Text = "↻  Refresh",
                Dock = DockStyle.Right,
                Width = 95,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Click += async (s, e) => await LoadAsync();
            pnlHeader.Controls.Add(_btnRefresh);

            // ── Filter Bar ───────────────────────────────────────────
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 6, 20, 6)
            };
            card.Controls.Add(pnlFilter);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 300,
                Font = Theme.BodyFont,
                PlaceholderText = "🔍  Search by customer, service, or staff..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            pnlFilter.Controls.Add(_txtSearch);

            var lblSep = new Label
            {
                Text = "  Status:",
                Dock = DockStyle.Left,
                Width = 60,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlFilter.Controls.Add(lblSep);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 140,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[]
            {
                "All Statuses", "Requested", "Scheduled", "In Progress", "Completed", "Cancelled", "Rescheduled"
            });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlFilter.Controls.Add(_cmbStatusFilter);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── DataGridView ─────────────────────────────────────────
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
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 38 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                AutoGenerateColumns = false
            };

            // DataError handler as safety net
            _grid.DataError += (s, e) =>
            {
                e.ThrowException = false;
            };

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

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId",          HeaderText = "Work Order #",   DataPropertyName = "ServiceRequestId", Width = 110, MinimumWidth = 90  },
                new DataGridViewTextBoxColumn { Name = "colCustomer",    HeaderText = "Customer Name",  DataPropertyName = "CustomerName",     Width = 180, MinimumWidth = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colService",     HeaderText = "Service Type",   DataPropertyName = "ServiceType",      Width = 150, MinimumWidth = 110 },
                new DataGridViewTextBoxColumn { Name = "colQuotedPrice", HeaderText = "Quoted Price",   DataPropertyName = "QuotedPrice",      Width = 105, MinimumWidth = 85  },
                new DataGridViewTextBoxColumn { Name = "colActualPrice", HeaderText = "Billed Price",   DataPropertyName = "ActualPrice",      Width = 105, MinimumWidth = 85  },
                new DataGridViewTextBoxColumn { Name = "colDate",        HeaderText = "Scheduled Date", DataPropertyName = "PreferredDate",    Width = 120, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colStaff",       HeaderText = "Staff Assigned", DataPropertyName = "AssignedStaff",     Width = 130, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "Status",         DataPropertyName = "Status",            Width = 105, MinimumWidth = 85  },
                new DataGridViewTextBoxColumn { Name = "colRating",      HeaderText = "Rating",         DataPropertyName = "Rating",           Width = 95,  MinimumWidth = 80  },
                new DataGridViewTextBoxColumn { Name = "colInspection",  HeaderText = "QA Verdict",     DataPropertyName = "InspectionStatus", Width = 95,  MinimumWidth = 80  }
            });

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

                var colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "colId" && e.Value is int id)
                {
                    e.Value = $"WO-{id:D4}";
                    e.FormattingApplied = true;
                }
                else if (colName == "colDate" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("MMM dd, yyyy");
                    e.FormattingApplied = true;
                }
                else if (colName == "colQuotedPrice" || colName == "colActualPrice")
                {
                    if (e.Value is decimal price)
                    {
                        e.Value = $"₱{price:N2}";
                        e.FormattingApplied = true;
                    }
                    else if (e.Value == null || e.Value == DBNull.Value)
                    {
                        e.Value = "--";
                        e.FormattingApplied = true;
                    }
                }
                else if (colName == "colStatus" && e.Value is string status)
                {
                    e.CellStyle.ForeColor = status switch
                    {
                        "Completed"   => Color.FromArgb(22, 163, 74),  // #16A34A Green
                        "Cancelled"   => Color.FromArgb(220, 38, 38),  // #DC2626 Red
                        "Requested"   => Color.FromArgb(217, 119, 6),  // #D97706 Amber
                        "In Progress" => Color.FromArgb(37, 99, 235),  // #2563EB Blue
                        "InProgress"  => Color.FromArgb(37, 99, 235),  // #2563EB Blue
                        "Rescheduled" => Color.FromArgb(234, 88, 12),  // #EA580C Orange
                        "Scheduled"   => Color.FromArgb(71, 85, 105),  // #475569 Slate
                        "Pending"     => Color.FromArgb(71, 85, 105),  // Legacy fallback -> Slate
                        _             => Color.FromArgb(71, 85, 105)
                    };
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                else if (colName == "colRating")
                {
                    if (e.Value is int r && r > 0)
                    {
                        e.Value = new string('★', Math.Clamp(r, 1, 5));
                        e.CellStyle.ForeColor = Color.FromArgb(234, 179, 8); // Gold #EAB308
                        e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        e.FormattingApplied = true;
                    }
                    else
                    {
                        e.Value = "—";
                        e.CellStyle.ForeColor = Theme.TextSubtle;
                        e.FormattingApplied = true;
                    }
                }
                else if (colName == "colInspection")
                {
                    if (e.Value is string insp && !string.IsNullOrWhiteSpace(insp))
                    {
                        e.CellStyle.ForeColor = insp == "Passed"
                            ? Color.FromArgb(22, 163, 74)
                            : (insp == "NeedsRework" ? Color.FromArgb(220, 38, 38) : Color.FromArgb(217, 119, 6));
                        e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    }
                    else
                    {
                        e.Value = "—";
                        e.CellStyle.ForeColor = Theme.TextSubtle;
                        e.FormattingApplied = true;
                    }
                }
            };

            _grid.SelectionChanged += (s, e) => UpdateDispatchButtonState();
            _grid.CellDoubleClick += OnGridCellDoubleClick;

            // ── Bottom Pagination Bar ──────────────────────────────
            _pnlPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 6, 20, 6)
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

            card.Controls.Add(_grid);

            // Enforce correct z-order (fill panel must be index 0)
            card.Controls.SetChildIndex(_grid, 0);
            card.Controls.SetChildIndex(_pnlPagination, 1);
            card.Controls.SetChildIndex(pnlDivider, 2);
            card.Controls.SetChildIndex(pnlFilter, 3);
            card.Controls.SetChildIndex(pnlHeader, 4);

            ResumeLayout(false);
        }

        // ============================================================
        // Data Loading
        // ============================================================
        private async Task LoadAsync()
        {
            _btnRefresh.Enabled = false;
            _btnRefresh.Text = "⏳";
            _lblCount.Text = "Loading...";

            try
            {
                _workOrders = await _api.GetWorkOrdersAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to load work orders: {ex.Message}", false);
            }
            finally
            {
                _btnRefresh.Enabled = true;
                _btnRefresh.Text = "↻  Refresh";
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string search = _txtSearch.Text.Trim();
                string statusFilter = _cmbStatusFilter.SelectedIndex > 0
                    ? _cmbStatusFilter.SelectedItem?.ToString() ?? string.Empty
                    : string.Empty;

                var filtered = _workOrders.FindAll(wo =>
                {
                    bool matchSearch = string.IsNullOrEmpty(search) ||
                        (wo.CustomerName != null && wo.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (wo.ServiceType != null && wo.ServiceType.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (wo.AssignedStaff != null && wo.AssignedStaff.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        $"wo-{wo.ServiceRequestId:d4}".Contains(search, StringComparison.OrdinalIgnoreCase);

                    bool matchStatus = string.IsNullOrEmpty(statusFilter) ||
                        (wo.Status != null && wo.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

                    return matchSearch && matchStatus;
                });

                // Newest first
                _filteredWorkOrders = filtered.OrderByDescending(w => w.PreferredDate).ThenByDescending(w => w.ServiceRequestId).ToList();
                CalculatePagination();
                RenderPage();
            }
            catch (Exception ex)
            {
                ShowToast($"Error filtering work orders: {ex.Message}", false);
            }
        }

        private void CalculatePagination()
        {
            int total = _filteredWorkOrders.Count;
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

            int total = _filteredWorkOrders.Count;
            if (total == 0)
            {
                _grid.DataSource = null;
                _lblCount.Text = "0 work orders";
                _lblPageInfo.Text = "No work orders to display";
                _pnlPageNumbers.Controls.Clear();
                _btnFirstPage.Enabled = _btnPrevPage.Enabled = _btnNextPage.Enabled = _btnLastPage.Enabled = false;
                UpdateDispatchButtonState();
                return;
            }

            int startIndex = (_currentPage - 1) * _pageSize;
            var pageRecords = _filteredWorkOrders.Skip(startIndex).Take(_pageSize).ToList();
            int endIndex = startIndex + pageRecords.Count;

            _grid.DataSource = null;
            _grid.DataSource = pageRecords;

            _lblCount.Text = $"{total} work order{(total == 1 ? "" : "s")}";
            _lblPageInfo.Text = $"Showing {startIndex + 1} to {endIndex} of {total} records.";

            _btnFirstPage.Enabled = _btnPrevPage.Enabled = (_currentPage > 1);
            _btnNextPage.Enabled = _btnLastPage.Enabled = (_currentPage < _totalPages);
            UpdatePageNumberButtons();
            UpdateDispatchButtonState();
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

        private void UpdateDispatchButtonState()
        {
            var selected = GetSelectedWorkOrder();
            _btnDispatch.Enabled = selected != null;
            _btnFeedback.Enabled = selected != null && string.Equals(selected.Status, "Completed", StringComparison.OrdinalIgnoreCase);
            _btnPrintOrder.Enabled = selected != null;
        }

        private void OnPrintOrderClick(object? sender, EventArgs e)
        {
            var selected = GetSelectedWorkOrder();
            if (selected == null)
            {
                ShowToast("Please select a work order to print.", false);
                return;
            }

            ReportDocumentEngine.ShowWorkOrderPrintPreview(selected, FindForm());
        }

        private void OnFeedbackClick(object? sender, EventArgs e)
        {
            var selected = GetSelectedWorkOrder();
            if (selected == null)
            {
                ShowToast("Please select a work order first.", false);
                return;
            }

            if (!string.Equals(selected.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                ShowToast("Feedback and Quality Inspections can only be recorded for 'Completed' work orders.", false);
                return;
            }

            using var dialog = new ServiceFeedbackDialog(selected);
            dialog.FeedbackSaved += async (updated) =>
            {
                ShowToast($"Feedback & QA saved for WO-{updated.ServiceRequestId:D4}!", true);
                await LoadAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        private WorkOrderDto? GetSelectedWorkOrder()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.DataBoundItem is WorkOrderDto wo)
                return wo;

            return null;
        }

        private void OnDispatchClick(object? sender, EventArgs e)
        {
            var selected = GetSelectedWorkOrder();
            if (selected == null)
            {
                ShowToast("Please select a work order to dispatch or manage.", false);
                return;
            }

            OpenDispatchDialog(selected);
        }

        private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

            if (_grid.Rows[e.RowIndex].DataBoundItem is WorkOrderDto wo)
            {
                OpenDispatchDialog(wo);
            }
        }

        private void OpenDispatchDialog(WorkOrderDto order)
        {
            using var dialog = new DispatchWorkOrderDialog(order);
            dialog.WorkOrderUpdated += async (updated) =>
            {
                ShowToast("Work order updated successfully!", true);
                await LoadAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        // ============================================================
        // New Booking Request button
        // ============================================================
        private void OnNewOrderClick(object? sender, EventArgs e)
        {
            using var dialog = new NewBookingRequestDialog();
            dialog.BookingRequestSaved += async (wo) =>
            {
                ShowToast("Booking request submitted successfully!", true);
                await LoadAsync();
            };
            dialog.ShowDialog(FindForm());
        }

        // ============================================================
        // Role-Based Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            // Use Case 5: Work Order Management
            // Manager: FULL (Owns work orders, dispatch, and new orders)
            // Super Admin: VIEW (Read-only work orders audit)
            // Admin: VIEW (Read-only work orders audit)
            // Sales Staff: — (Hidden from navigation)
            bool isManager = (userRole == Roles.Manager);

            _btnNewOrder.Visible = isManager;

            if (isManager)
            {
                _btnDispatch.Text = "⚡ Dispatch & Manage";
                _btnDispatch.BackColor = Color.FromArgb(79, 70, 229); // Indigo
            }
            else
            {
                _btnDispatch.Text = "🔍 View Details";
                _btnDispatch.BackColor = Color.FromArgb(100, 116, 139); // Slate
            }
        }
    }
}
