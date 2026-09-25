using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Reporting;

namespace App.WinForms.Views
{
    /// <summary>
    /// Modal dialog showing all ServiceRequests linked to a specific Customer.
    ///
    /// Grid columns:
    ///   [ Work Order # | Service Type | Scheduled Date | Staff Assigned | Status ]
    ///
    /// Opened from CustomersView when a customer row is double-clicked
    /// or the "View History" button is clicked.
    /// </summary>
    public class CustomerServiceHistoryDialog : Form
    {
        private readonly ApiClient _api = new();
        private readonly int _customerId;
        private readonly string _customerName;
        private List<WorkOrderDto> _orders = new();

        private DataGridView _grid = null!;
        private Label _lblLoading = null!;
        private Button _btnFeedback = null!;
        private Button _btnPrint = null!;

        public CustomerServiceHistoryDialog(int customerId, string customerName)
        {
            _customerId = customerId;
            _customerName = customerName;

            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = LoadAsync();
        }

        private void BuildUI()
        {
            Text = $"Service History — {_customerName}";
            Size = new Size(820, 500);
            MinimumSize = new Size(680, 380);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = $"Service History for {_customerName}",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Dock = DockStyle.Left,
                Width = 500,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            var btnClose = new Button
            {
                Text = "✕  Close",
                Dock = DockStyle.Right,
                Width = 95
            };
            Theme.ApplySecondaryButtonStyle(btnClose);
            btnClose.Click += (s, e) => Close();
            pnlHeader.Controls.Add(btnClose);

            var spacerH = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Color.White };
            pnlHeader.Controls.Add(spacerH);

            _btnFeedback = new Button
            {
                Text = "⭐ Feedback / QA",
                Dock = DockStyle.Right,
                Width = 155,
                Enabled = false
            };
            Theme.ApplyPrimaryButtonStyle(_btnFeedback);
            _btnFeedback.Click += OnFeedbackClick;
            pnlHeader.Controls.Add(_btnFeedback);

            var spacerH2 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Color.White };
            pnlHeader.Controls.Add(spacerH2);

            _btnPrint = new Button
            {
                Text = "🖨️  Print Order",
                Dock = DockStyle.Right,
                Width = 140,
                Enabled = false
            };
            Theme.ApplySecondaryButtonStyle(_btnPrint);
            _btnPrint.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnPrint.Click += (s, e) =>
            {
                var sel = GetSelectedOrder();
                if (sel != null) ReportDocumentEngine.ShowWorkOrderPrintPreview(sel, this);
            };
            pnlHeader.Controls.Add(_btnPrint);

            // Divider
            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            Controls.Add(pnlDivider);

            // Loading label
            _lblLoading = new Label
            {
                Text = "Loading service history...",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_lblLoading);

            // DataGridView
            _grid = new DataGridView
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
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 36 },
                Font = new Font("Segoe UI", 9F),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
                EnableHeadersVisualStyles = false,
                Visible = false
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            _grid.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId",         HeaderText = "Work Order #",    Width = 100, MinimumWidth = 80  },
                new DataGridViewTextBoxColumn { Name = "colType",       HeaderText = "Service Type",    Width = 180, MinimumWidth = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colDate",       HeaderText = "Scheduled Date",  Width = 120, MinimumWidth = 95  },
                new DataGridViewTextBoxColumn { Name = "colStaff",      HeaderText = "Staff Assigned",  Width = 130, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colStatus",     HeaderText = "Status",          Width = 100, MinimumWidth = 80  },
                new DataGridViewTextBoxColumn { Name = "colRating",     HeaderText = "Rating",          Width = 95,  MinimumWidth = 80  },
                new DataGridViewTextBoxColumn { Name = "colInspection", HeaderText = "QA Verdict",      Width = 95,  MinimumWidth = 80  }
            });

            _grid.SelectionChanged += (s, e) => UpdateFeedbackButton();
            _grid.CellDoubleClick += OnGridCellDoubleClick;

            Controls.Add(_grid);

            // Z-order: grid fills bottom, loading label above it, divider, header on top
            Controls.SetChildIndex(_grid, 0);
            Controls.SetChildIndex(_lblLoading, 1);
            Controls.SetChildIndex(pnlDivider, 2);
            Controls.SetChildIndex(pnlHeader, 3);
        }

        private async Task LoadAsync()
        {
            try
            {
                var orders = await _api.GetWorkOrdersByCustomerAsync(_customerId);
                _orders = orders;

                _grid.Rows.Clear();

                foreach (var wo in orders)
                {
                    if (string.IsNullOrEmpty(wo.CustomerName))
                        wo.CustomerName = _customerName;

                    string ratingDisplay = wo.Rating.HasValue && wo.Rating.Value > 0
                        ? new string('★', Math.Clamp(wo.Rating.Value, 1, 5))
                        : "—";
                    string inspectionDisplay = !string.IsNullOrWhiteSpace(wo.InspectionStatus)
                        ? wo.InspectionStatus
                        : "—";

                    int rowIdx = _grid.Rows.Add(
                        $"WO-{wo.ServiceRequestId:D4}",
                        wo.ServiceType,
                        wo.PreferredDate.ToString("MMM dd, yyyy"),
                        wo.AssignedStaff,
                        wo.Status,
                        ratingDisplay,
                        inspectionDisplay
                    );

                    // Colour-code status
                    var statusCell = _grid.Rows[rowIdx].Cells["colStatus"];
                    statusCell.Style.ForeColor = wo.Status switch
                    {
                        "Completed"   => Color.FromArgb(22, 163, 74),
                        "Cancelled"   => Color.FromArgb(220, 38, 38),
                        "Requested"   => Color.FromArgb(217, 119, 6),
                        "InProgress"  => Color.FromArgb(37, 99, 235),
                        "In Progress" => Color.FromArgb(37, 99, 235),
                        "Rescheduled" => Color.FromArgb(234, 88, 12),
                        "Scheduled"   => Color.FromArgb(71, 85, 105),
                        "Pending"     => Color.FromArgb(71, 85, 105),
                        _             => Color.FromArgb(71, 85, 105)
                    };
                    statusCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                    if (wo.Rating.HasValue && wo.Rating.Value > 0)
                    {
                        var ratingCell = _grid.Rows[rowIdx].Cells["colRating"];
                        ratingCell.Style.ForeColor = Color.FromArgb(234, 179, 8); // Gold
                        ratingCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }

                    if (!string.IsNullOrWhiteSpace(wo.InspectionStatus))
                    {
                        var inspCell = _grid.Rows[rowIdx].Cells["colInspection"];
                        inspCell.Style.ForeColor = wo.InspectionStatus == "Passed"
                            ? Color.FromArgb(22, 163, 74)
                            : (wo.InspectionStatus == "NeedsRework" ? Color.FromArgb(220, 38, 38) : Color.FromArgb(217, 119, 6));
                        inspCell.Style.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    }
                }

                _lblLoading.Visible = false;
                _grid.Visible = true;
                UpdateFeedbackButton();

                if (orders.Count == 0)
                {
                    _lblLoading.Text = "No service history found for this customer.";
                    _lblLoading.Visible = true;
                }
            }
            catch (Exception ex)
            {
                _lblLoading.Text = $"Failed to load: {ex.Message}";
            }
        }

        private void UpdateFeedbackButton()
        {
            var selected = GetSelectedOrder();
            _btnFeedback.Enabled = selected != null && string.Equals(selected.Status, "Completed", StringComparison.OrdinalIgnoreCase);
            _btnPrint.Enabled = selected != null;
        }

        private WorkOrderDto? GetSelectedOrder()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _orders.Count)
                return _orders[_grid.CurrentRow.Index];

            return null;
        }

        private void OnFeedbackClick(object? sender, EventArgs e)
        {
            var order = GetSelectedOrder();
            if (order == null) return;

            if (!string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Feedback and Quality Inspections can only be recorded for 'Completed' work orders.", "QA & Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new ServiceFeedbackDialog(order);
            dialog.FeedbackSaved += async (updated) =>
            {
                await LoadAsync();
            };
            dialog.ShowDialog(this);
        }

        private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _orders.Count) return;

            var order = _orders[e.RowIndex];
            using var dialog = new DispatchWorkOrderDialog(order);
            dialog.WorkOrderUpdated += async (updated) =>
            {
                await LoadAsync();
            };
            dialog.ShowDialog(this);
        }
    }
}
