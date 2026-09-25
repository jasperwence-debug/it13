using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Search dialog pattern for selecting a Customer from a large dataset.
    /// Features:
    ///   - Real-time search filtering across Name, Contact Details, and Location.
    ///   - Multi-column grid showing Name, Contact, Service Location, Type, and Bookings.
    ///   - Column header sorting (Ascending / Descending).
    ///   - Double-click or 'Select Customer' button to confirm.
    ///   - Total count display.
    /// </summary>
    public class CustomerSearchDialog : Form
    {
        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _allCustomers = new();
        private List<CustomerSummaryDto> _filteredCustomers = new();

        // Sort tracking
        private string _sortColumn = "CustomerName";
        private bool _sortAscending = true;

        // UI Controls
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private DataGridView _grid = null!;
        private Label _lblCount = null!;
        private Button _btnSelect = null!;
        private Button _btnCancel = null!;
        private Label _lblLoading = null!;

        public CustomerSummaryDto? SelectedCustomer { get; private set; }

        public CustomerSearchDialog(List<CustomerSummaryDto>? preloadedCustomers = null)
        {
            if (preloadedCustomers != null && preloadedCustomers.Count > 0)
            {
                _allCustomers = preloadedCustomers;
                _filteredCustomers = new List<CustomerSummaryDto>(_allCustomers);
            }

            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (_allCustomers.Count == 0)
            {
                _ = LoadCustomersAsync();
            }
            else
            {
                ApplySortAndFilter();
                _txtSearch.Focus();
            }
        }

        private void BuildUI()
        {
            Text = "Select Customer";
            Size = new Size(820, 560);
            MinimumSize = new Size(680, 440);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // ── Top Header & Search Bar (80px) ──────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 12, 20, 10)
            };
            Controls.Add(pnlTop);

            var lblTitle = new Label
            {
                Text = "Customer Search & Selection",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlTop.Controls.Add(lblTitle);

            var pnlSearchWrap = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Theme.Surface
            };
            pnlTop.Controls.Add(pnlSearchWrap);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "🔍  Type customer name, phone number, email, or service location to filter..."
            };
            _txtSearch.TextChanged += (s, e) => ApplySortAndFilter();
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && _grid.SelectedRows.Count > 0)
                {
                    ConfirmSelection();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Down && _grid.Rows.Count > 0)
                {
                    _grid.Focus();
                    e.Handled = true;
                }
            };
            pnlSearchWrap.Controls.Add(_txtSearch);

            _btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClearSearch.FlatAppearance.BorderSize = 0;
            _btnClearSearch.Click += (s, e) =>
            {
                _txtSearch.Text = string.Empty;
                _txtSearch.Focus();
            };
            pnlSearchWrap.Controls.Add(_btnClearSearch);

            var pnlTopDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            Controls.Add(pnlTopDivider);

            // ── Footer Bar (56px) ──────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 10, 20, 10)
            };
            Controls.Add(pnlFooter);

            _lblCount = new Label
            {
                Dock = DockStyle.Left,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 300
            };
            pnlFooter.Controls.Add(_lblCount);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 100
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            var pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 10 };
            pnlFooter.Controls.Add(pnlSpacer);

            _btnSelect = new Button
            {
                Text = "✓ Select Customer",
                Dock = DockStyle.Right,
                Width = 175,
                Enabled = false
            };
            Theme.ApplyPrimaryButtonStyle(_btnSelect);
            _btnSelect.Click += (s, e) => ConfirmSelection();
            pnlFooter.Controls.Add(_btnSelect);

            var pnlBottomDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.Border
            };
            Controls.Add(pnlBottomDivider);

            // ── DataGridView ──────────────────────────────────────────
            var pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface
            };
            Controls.Add(pnlGridWrapper);

            _lblLoading = new Label
            {
                Text = "Loading customers from database...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                Visible = false
            };
            pnlGridWrapper.Controls.Add(_lblLoading);

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
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 36 },
                Font = Theme.BodyFont,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                AutoGenerateColumns = false
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
            _grid.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "colName",
                    HeaderText = "Customer Name",
                    DataPropertyName = "CustomerName",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    MinimumWidth = 160,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colContact",
                    HeaderText = "Phone / Contact",
                    DataPropertyName = "ContactDetails",
                    Width = 160,
                    MinimumWidth = 130,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colLocation",
                    HeaderText = "Service Location",
                    DataPropertyName = "ServiceLocation",
                    Width = 210,
                    MinimumWidth = 150,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colType",
                    HeaderText = "Type",
                    DataPropertyName = "CustomerType",
                    Width = 110,
                    MinimumWidth = 90,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colBookings",
                    HeaderText = "Bookings",
                    DataPropertyName = "TotalBookings",
                    Width = 85,
                    MinimumWidth = 70,
                    SortMode = DataGridViewColumnSortMode.Programmatic
                }
            });

            if (_grid.Columns["colBookings"] is { } colBookings)
            {
                colBookings.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
                var col = _grid.Columns[e.ColumnIndex].Name;

                if (col == "colType" && e.Value is string type)
                {
                    if (string.Equals(type, "Commercial", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(79, 70, 229); // Indigo
                        e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(2, 132, 199); // Sky
                        e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    }
                }
                else if (col == "colName")
                {
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            };

            _grid.SelectionChanged += (s, e) =>
            {
                _btnSelect.Enabled = (_grid.CurrentRow != null && _grid.CurrentRow.DataBoundItem is CustomerSummaryDto);
            };

            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    ConfirmSelection();
                }
            };

            _grid.ColumnHeaderMouseClick += (s, e) =>
            {
                var clickedCol = _grid.Columns[e.ColumnIndex];
                if (clickedCol.DataPropertyName == _sortColumn)
                {
                    _sortAscending = !_sortAscending;
                }
                else
                {
                    _sortColumn = clickedCol.DataPropertyName;
                    _sortAscending = true;
                }
                ApplySortAndFilter();
            };

            _grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.Handled = true;
                }
            };

            pnlGridWrapper.Controls.Add(_grid);

            // Z-Order layout inside Form
            Controls.SetChildIndex(pnlGridWrapper, 0);
            Controls.SetChildIndex(pnlBottomDivider, 1);
            Controls.SetChildIndex(pnlFooter, 2);
            Controls.SetChildIndex(pnlTopDivider, 3);
            Controls.SetChildIndex(pnlTop, 4);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private async Task LoadCustomersAsync()
        {
            _lblLoading.Visible = true;
            _grid.Visible = false;

            try
            {
                _allCustomers = await _api.GetCustomersAsync();
                _filteredCustomers = new List<CustomerSummaryDto>(_allCustomers);
                ApplySortAndFilter();
            }
            catch (Exception ex)
            {
                _lblCount.Text = $"Error loading customers: {ex.Message}";
            }
            finally
            {
                _lblLoading.Visible = false;
                _grid.Visible = true;
                _txtSearch.Focus();
            }
        }

        private void ApplySortAndFilter()
        {
            string query = _txtSearch.Text.Trim();

            // Filter
            if (string.IsNullOrWhiteSpace(query))
            {
                _filteredCustomers = new List<CustomerSummaryDto>(_allCustomers);
            }
            else
            {
                _filteredCustomers = _allCustomers.Where(c =>
                    (!string.IsNullOrEmpty(c.CustomerName) && c.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.ContactDetails) && c.ContactDetails.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.ServiceLocation) && c.ServiceLocation.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.CustomerType) && c.CustomerType.Contains(query, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Sort
            _filteredCustomers = _sortColumn switch
            {
                "CustomerName" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.CustomerName).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.CustomerName).ToList(),
                "ContactDetails" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.ContactDetails).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.ContactDetails).ToList(),
                "ServiceLocation" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.ServiceLocation).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.ServiceLocation).ToList(),
                "CustomerType" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.CustomerType).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.CustomerType).ToList(),
                "TotalBookings" => _sortAscending
                    ? _filteredCustomers.OrderBy(c => c.TotalBookings).ToList()
                    : _filteredCustomers.OrderByDescending(c => c.TotalBookings).ToList(),
                _ => _filteredCustomers
            };

            // Bind to grid
            _grid.DataSource = null;
            _grid.DataSource = _filteredCustomers;

            _lblCount.Text = $"Showing {_filteredCustomers.Count} of {_allCustomers.Count} customer{(_allCustomers.Count == 1 ? "" : "s")}";

            // Re-select first row if available
            if (_filteredCustomers.Count > 0 && _grid.Rows.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                _btnSelect.Enabled = true;
            }
            else
            {
                _btnSelect.Enabled = false;
            }
        }

        private void ConfirmSelection()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.DataBoundItem is CustomerSummaryDto selected)
            {
                SelectedCustomer = selected;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
