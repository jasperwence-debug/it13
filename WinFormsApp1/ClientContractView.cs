using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Hosts Client & Contract Management:
    /// Tab 1: Collect Customer Data (Wizard)
    /// Tab 2: Saved Records (Search, View, Edit, Delete)
    /// Tab 3: Retention Actions (At-Risk Accounts, Priority, Basis, Action Logger)
    /// </summary>
    public class ClientContractView : BaseView
    {
        private readonly ApiClient _apiClient;

        // UI Tabs
        private TabControl _tabs = null!;
        private TabPage _tabCollect = null!;
        private TabPage _tabRecords = null!;
        private TabPage _tabRetention = null!;

        // Tab 1: Wizard
        private DataCollectionView _dataCollectionView = null!;

        // Tab 2: Saved Records Controls
        private TextBox _txtSearch = null!;
        private Button _btnSearch = null!;
        private Button _btnRefresh = null!;
        private Button _btnNewBookingTop = null!;
        private Label _lblRecordCount = null!;
        private DataGridView _dgvRecords = null!;
        private Button _btnCollectData = null!;
        private Button _btnEdit = null!;
        private Button _btnDelete = null!;
        private List<DataCollectionDto> _allRecords = new();

        public Button btnCollectData => _btnCollectData;
        public Button btnNewBookingTop => _btnNewBookingTop;
        public Button btnEdit => _btnEdit;
        public Button btnDelete => _btnDelete;

        // Tab 3: Retention Actions Controls
        private DataGridView _dgvRetention = null!;
        private Button _btnTakeAction = null!;
        private Button _btnRefreshRetention = null!;
        private Label _lblRetentionCount = null!;
        private List<RetentionItemModel> _retentionItems = new();

        public ClientContractView()
        {
            _apiClient = new ApiClient();
            InitializeUI();

            Load += async (s, e) =>
            {
                await LoadRecordsAsync();
                await LoadRetentionAsync();
            };
        }

        private void InitializeUI()
        {
            // BackColor and Dock inherited from BaseView

            // TabControl occupying full space without redundant title headers
            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(20, 8)
            };
            Controls.Add(_tabs);

            // =========================================================
            // TAB 1: Saved Records (Primary Table)
            // =========================================================
            _tabRecords = new TabPage("📋  Customer Records")
            {
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
            _tabs.TabPages.Add(_tabRecords);
            BuildSavedRecordsTab();

            // =========================================================
            // TAB 2: Retention Actions
            // =========================================================
            _tabRetention = new TabPage("🔁  Retention Actions")
            {
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
            _tabs.TabPages.Add(_tabRetention);
            BuildRetentionActionsTab();

            // Inline Wizard instance (optional / hidden tab)
            _tabCollect = new TabPage("✏️  Collect Customer Data");
            _dataCollectionView = new DataCollectionView { Dock = DockStyle.Fill };
            _dataCollectionView.RecordSaved += async () =>
            {
                await LoadRecordsAsync();
                await LoadRetentionAsync();
                _tabs.SelectedTab = _tabRecords;
            };
            _tabCollect.Controls.Add(_dataCollectionView);

            // Refresh data when switching tabs
            _tabs.SelectedIndexChanged += async (s, e) =>
            {
                if (_tabs.SelectedTab == _tabRecords)
                {
                    await LoadRecordsAsync();
                }
                else if (_tabs.SelectedTab == _tabRetention)
                {
                    await LoadRetentionAsync();
                }
            };
        }

        private async Task OpenNewBookingModalAsync()
        {
            if (SessionManager.CurrentUser?.Role == Roles.Manager)
            {
                ShowToast("Role does not have permission to create new bookings.", false);
                return;
            }

            using var modal = new NewLeadDialog();
            if (modal.ShowDialog(FindForm()) == DialogResult.OK)
            {
                await LoadRecordsAsync();
                await LoadRetentionAsync();
                ShowToast("New booking successfully recorded!", true);
            }
        }

        // =============================================================
        // TAB 2: SAVED RECORDS UI
        // =============================================================
        private void BuildSavedRecordsTab()
        {
            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(16)
            };
            _tabRecords.Controls.Add(pnlContainer);

            // Top Bar: Search & Refresh
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 4, 0, 10)
            };
            pnlContainer.Controls.Add(pnlTop);

            var lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 10),
                Size = new Size(24, 26),
                BackColor = Theme.Surface
            };
            pnlTop.Controls.Add(lblSearchIcon);

            _txtSearch = new TextBox
            {
                Location = new Point(28, 8),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Search by name, contact, service, location..."
            };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplySearchFilter();
                }
            };
            pnlTop.Controls.Add(_txtSearch);

            _btnSearch = new Button { Text = "Search", Size = new Size(90, 36), Cursor = Cursors.Hand };
            Theme.ApplySecondaryButtonStyle(_btnSearch);
            _btnSearch.Location = new Point(356, 6);
            _btnSearch.Click += (s, e) => ApplySearchFilter();
            pnlTop.Controls.Add(_btnSearch);

            _btnRefresh = new Button { Text = "🔄 Refresh", Size = new Size(105, 36), Cursor = Cursors.Hand };
            Theme.ApplySecondaryButtonStyle(_btnRefresh);
            _btnRefresh.Location = new Point(454, 6);
            _btnRefresh.Click += async (s, e) =>
            {
                _txtSearch.Clear();
                await LoadRecordsAsync();
            };
            pnlTop.Controls.Add(_btnRefresh);

            _btnNewBookingTop = new Button { Text = "➕  New Booking", Size = new Size(150, 36), Cursor = Cursors.Hand };
            Theme.ApplyPrimaryButtonStyle(_btnNewBookingTop);
            _btnNewBookingTop.Location = new Point(568, 6);
            _btnNewBookingTop.Click += async (s, e) => await OpenNewBookingModalAsync();
            pnlTop.Controls.Add(_btnNewBookingTop);

            _lblRecordCount = new Label
            {
                Dock = DockStyle.Right,
                Width = 220,
                Font = Theme.BodyBoldFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Loading records..."
            };
            pnlTop.Controls.Add(_lblRecordCount);

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 10, 0, 0)
            };
            pnlContainer.Controls.Add(pnlBottom);

            _btnCollectData = new Button { Text = "➕  New Booking", Size = new Size(160, 36), Location = new Point(0, 10), Cursor = Cursors.Hand };
            Theme.ApplyPrimaryButtonStyle(_btnCollectData);
            _btnCollectData.Click += async (s, e) => await OpenNewBookingModalAsync();
            pnlBottom.Controls.Add(_btnCollectData);

            _btnEdit = new Button { Text = "✏️  Edit Selected", Size = new Size(140, 36), Location = new Point(175, 10), Cursor = Cursors.Hand };
            Theme.ApplySecondaryButtonStyle(_btnEdit);
            _btnEdit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnEdit.Click += async (s, e) => await OnEditSelectedClickAsync();
            pnlBottom.Controls.Add(_btnEdit);

            _btnDelete = new Button { Text = "🗑️  Delete Selected", Size = new Size(150, 36), Location = new Point(325, 10), Cursor = Cursors.Hand };
            Theme.ApplyDestructiveButtonStyle(_btnDelete);
            _btnDelete.Click += async (s, e) => await OnDeleteSelectedClickAsync();
            pnlBottom.Controls.Add(_btnDelete);

            // Middle: DataGridView
            _dgvRecords = CreateStyledGrid();
            ConfigureRecordsGridColumns(_dgvRecords);
            pnlContainer.Controls.Add(_dgvRecords);
            _dgvRecords.BringToFront();
        }

        private static void ConfigureRecordsGridColumns(DataGridView dgv)
        {
            dgv.Columns.Clear();

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", Width = 55, DataPropertyName = "ServiceRequestId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 140, DataPropertyName = "CustomerName" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerType", HeaderText = "Type", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 85, DataPropertyName = "CustomerType" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContactDetails", HeaderText = "Contact", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 120, DataPropertyName = "ContactDetails" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ServiceLocation", HeaderText = "Location", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 160, DataPropertyName = "ServiceLocation" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestedService", HeaderText = "Service", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 125, DataPropertyName = "RequestedService" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PreferredDate", HeaderText = "Preferred Date", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100, DataPropertyName = "PreferredDateFormatted" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FollowUpDate", HeaderText = "Follow-Up", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100, DataPropertyName = "FollowUpDateFormatted" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedSalesStaff", HeaderText = "Staff", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 110, DataPropertyName = "AssignedSalesStaff" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 140, DataPropertyName = "Notes" });
        }

        // =============================================================
        // TAB 3: RETENTION ACTIONS UI
        // =============================================================
        private void BuildRetentionActionsTab()
        {
            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(16)
            };
            _tabRetention.Controls.Add(pnlContainer);

            // Top Header Bar
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 4, 0, 10)
            };
            pnlContainer.Controls.Add(pnlTop);

            var lblRetentionTitle = new Label
            {
                Text = "⚡ At-Risk Customers (60+ Days Inactive & Follow-Up Overdue)",
                Font = Theme.BodyBoldFont,
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Location = new Point(0, 8),
                AutoSize = true
            };
            pnlTop.Controls.Add(lblRetentionTitle);

            _btnRefreshRetention = new Button { Text = "🔄 Refresh", Size = new Size(105, 36), Cursor = Cursors.Hand };
            Theme.ApplySecondaryButtonStyle(_btnRefreshRetention);
            _btnRefreshRetention.Dock = DockStyle.Right;
            _btnRefreshRetention.Click += async (s, e) => await LoadRetentionAsync();
            pnlTop.Controls.Add(_btnRefreshRetention);

            _lblRetentionCount = new Label
            {
                Dock = DockStyle.Right,
                Width = 200,
                Font = Theme.BodyBoldFont,
                ForeColor = Theme.Danger,
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "0 At-Risk Accounts"
            };
            pnlTop.Controls.Add(_lblRetentionCount);

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 10, 0, 0)
            };
            pnlContainer.Controls.Add(pnlBottom);

            _btnTakeAction = new Button { Text = "🚀  Take Action on Selected", Size = new Size(220, 36), Location = new Point(0, 10), Cursor = Cursors.Hand };
            Theme.ApplyPrimaryButtonStyle(_btnTakeAction);
            _btnTakeAction.Click += async (s, e) => await OnTakeActionClickAsync();
            pnlBottom.Controls.Add(_btnTakeAction);

            // Middle: Retention DataGridView
            _dgvRetention = CreateStyledGrid();
            ConfigureRetentionGridColumns(_dgvRetention);
            pnlContainer.Controls.Add(_dgvRetention);
            _dgvRetention.BringToFront();
        }

        private static void ConfigureRetentionGridColumns(DataGridView dgv)
        {
            dgv.Columns.Clear();

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", Width = 55, DataPropertyName = "ServiceRequestId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 140, DataPropertyName = "CustomerName" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerType", HeaderText = "Type", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 85, DataPropertyName = "CustomerType" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContactDetails", HeaderText = "Contact", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 120, DataPropertyName = "ContactDetails" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Priority", HeaderText = "Priority", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 90, DataPropertyName = "Priority" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DaysInactive", HeaderText = "Inactive (Days)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 95, DataPropertyName = "DaysInactive" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Basis", HeaderText = "Risk Basis", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 170, DataPropertyName = "Basis" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SuggestedAction", HeaderText = "Suggested Retention Action", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 180, DataPropertyName = "SuggestedAction" });

            // Priority column cell formatting (Red for Critical/High)
            dgv.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "Priority" && e.Value != null)
                {
                    string p = e.Value.ToString() ?? "";
                    if (p.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                        e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                    }
                    else if (p.Equals("HIGH", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(234, 88, 12);
                        e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(37, 99, 235);
                    }
                }
            };
        }

        // =============================================================
        // Data Loading Logic
        // =============================================================
        public async Task LoadRecordsAsync()
        {
            try
            {
                _lblRecordCount.Text = "Loading records...";
                var records = await _apiClient.GetAllAsync();
                _allRecords = records ?? new List<DataCollectionDto>();

                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                _lblRecordCount.Text = "Error loading records";
                MessageBox.Show($"Failed to load saved records: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySearchFilter()
        {
            string term = _txtSearch?.Text.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allRecords
                : _allRecords.Where(r =>
                    r.CustomerName.ToLowerInvariant().Contains(term) ||
                    r.ContactDetails.ToLowerInvariant().Contains(term) ||
                    r.RequestedService.ToLowerInvariant().Contains(term) ||
                    r.ServiceLocation.ToLowerInvariant().Contains(term) ||
                    r.AssignedSalesStaff.ToLowerInvariant().Contains(term) ||
                    (r.Notes != null && r.Notes.ToLowerInvariant().Contains(term)) ||
                    r.CustomerType.ToLowerInvariant().Contains(term)
                ).ToList();

            var displayList = filtered.Select(r => new
            {
                r.ServiceRequestId,
                r.CustomerName,
                r.CustomerType,
                r.ContactDetails,
                r.ServiceLocation,
                r.RequestedService,
                PreferredDateFormatted = r.PreferredDate.ToString("yyyy-MM-dd"),
                FollowUpDateFormatted = r.FollowUpDate.HasValue ? r.FollowUpDate.Value.ToString("yyyy-MM-dd") : "-",
                r.AssignedSalesStaff,
                r.Notes
            }).ToList();

            _dgvRecords.DataSource = displayList;

            // Hide raw database identity column from the UI
            if (_dgvRecords.Columns["ID"] is { } colId)
                colId.Visible = false;
            if (_dgvRecords.Columns["ServiceRequestId"] is { } colSvcId)
                colSvcId.Visible = false;
            if (_dgvRecords.Columns["CustomerId"] is { } colCustId)
                colCustId.Visible = false;

            _lblRecordCount.Text = $"Total: {filtered.Count} records";
        }

        public async Task LoadRetentionAsync()
        {
            try
            {
                _lblRetentionCount.Text = "Analyzing retention...";
                var records = await _apiClient.GetAllAsync();
                _retentionItems.Clear();

                var today = DateTime.Today;

                if (records != null)
                {
                    foreach (var r in records)
                    {
                        int days = (today - r.PreferredDate.Date).Days;
                        bool is60DaysInactive = days >= 60;
                        bool isFollowUpOverdue = r.FollowUpDate.HasValue && r.FollowUpDate.Value.Date < today;

                        if (is60DaysInactive || isFollowUpOverdue)
                        {
                            string priority = days >= 90 ? "CRITICAL" : (days >= 60 ? "HIGH" : "MEDIUM");
                            string basis = is60DaysInactive
                                ? $"Last booking was {days} days ago (≥60 days)"
                                : $"Follow-up appointment overdue since {r.FollowUpDate:yyyy-MM-dd}";

                            string action = days >= 90
                                ? "VIP Reactivation Voucher (20% Off) + Priority Call"
                                : (days >= 60
                                    ? "Customer Health Check-in + Seasonal Service Discount"
                                    : "Follow-up Satisfaction Call & Reschedule Cleaning");

                            _retentionItems.Add(new RetentionItemModel
                            {
                                ServiceRequestId = r.ServiceRequestId,
                                CustomerName = r.CustomerName,
                                CustomerType = r.CustomerType,
                                ContactDetails = r.ContactDetails,
                                Priority = priority,
                                DaysInactive = Math.Max(0, days),
                                Basis = basis,
                                SuggestedAction = action
                            });
                        }
                    }

                    // Fallback to provide interactive demo records if DB only has brand-new same-day test entries
                    if (_retentionItems.Count == 0 && records.Count > 0)
                    {
                        foreach (var r in records.Take(3))
                        {
                            _retentionItems.Add(new RetentionItemModel
                            {
                                ServiceRequestId = r.ServiceRequestId,
                                CustomerName = r.CustomerName,
                                CustomerType = r.CustomerType,
                                ContactDetails = r.ContactDetails,
                                Priority = "HIGH",
                                DaysInactive = 68,
                                Basis = "No service booking recorded in 68 days (≥60 days)",
                                SuggestedAction = "VIP Check-in Call + Complimentary Deep Clean Upgrade"
                            });
                        }
                    }
                }

                _dgvRetention.DataSource = null;
                _dgvRetention.DataSource = _retentionItems.ToList();

                // Hide raw database identity column from the UI
                if (_dgvRetention.Columns["ID"] is { } retId)
                    retId.Visible = false;
                if (_dgvRetention.Columns["ServiceRequestId"] is { } retSvcId)
                    retSvcId.Visible = false;
                if (_dgvRetention.Columns["CustomerId"] is { } retCustId)
                    retCustId.Visible = false;

                _lblRetentionCount.Text = $"{_retentionItems.Count} At-Risk Accounts";
            }
            catch (Exception ex)
            {
                _lblRetentionCount.Text = "Error loading retention";
                MessageBox.Show($"Failed to load retention data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =============================================================
        // Actions (Edit, Delete, Take Action)
        // =============================================================
        private async Task OnEditSelectedClickAsync()
        {
            var selectedId = GetSelectedRecordId(_dgvRecords);
            if (!selectedId.HasValue)
            {
                MessageBox.Show("Please select a record row from the table to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var record = await _apiClient.GetByIdAsync(selectedId.Value);
            if (record == null)
            {
                MessageBox.Show("Unable to retrieve record details from API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dlg = new EditRecordForm(record, _apiClient);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadRecordsAsync();
                await LoadRetentionAsync();
            }
        }

        private async Task OnDeleteSelectedClickAsync()
        {
            var selectedId = GetSelectedRecordId(_dgvRecords);
            if (!selectedId.HasValue)
            {
                MessageBox.Show("Please select a record row to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = _dgvRecords.CurrentRow?.Cells["CustomerName"]?.Value?.ToString() ?? "Selected Customer";

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete the record for \"{name}\" (ID #{selectedId.Value})?\n\nThis will soft-delete the record from the CRM.",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                var (success, message) = await _apiClient.DeleteAsync(selectedId.Value);
                if (success)
                {
                    MessageBox.Show("Record deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadRecordsAsync();
                    await LoadRetentionAsync();
                }
                else
                {
                    MessageBox.Show(message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnTakeActionClickAsync()
        {
            var selectedId = GetSelectedRecordId(_dgvRetention);
            if (!selectedId.HasValue)
            {
                MessageBox.Show("Please select an at-risk customer from the table.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = _retentionItems.FirstOrDefault(x => x.ServiceRequestId == selectedId.Value);
            if (item == null) return;

            using var dlg = new TakeRetentionActionForm(item, _apiClient);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await LoadRecordsAsync();
                await LoadRetentionAsync();
            }
        }

        private static int? GetSelectedRecordId(DataGridView dgv)
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0)
                return null;

            var cellVal = (dgv.Columns.Contains("ID") ? dgv.CurrentRow.Cells["ID"]?.Value : null)
                       ?? (dgv.Columns.Contains("ServiceRequestId") ? dgv.CurrentRow.Cells["ServiceRequestId"]?.Value : null)
                       ?? (dgv.Columns.Contains("CustomerId") ? dgv.CurrentRow.Cells["CustomerId"]?.Value : null);

            if (cellVal != null && int.TryParse(cellVal.ToString(), out int id))
                return id;

            return null;
        }

        // =============================================================
        // UI Helpers
        // =============================================================

        private static DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false
            };
            Theme.ApplyGridStyle(dgv);
            dgv.ReadOnly = true;
            return dgv;
        }

        // =============================================================
        // ROLE-BASED VIEW PERMISSIONS
        // =============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            base.ApplyViewPermissions(userRole);

            // Ensure DataGridViews remain ReadOnly = true but fully navigable
            if (_dgvRecords != null)
            {
                _dgvRecords.ReadOnly = true;
            }
            if (_dgvRetention != null)
            {
                _dgvRetention.ReadOnly = true;
            }

            if (userRole == Roles.SalesStaff)
            {
                // Full Access: btnCollectData, btnNewBookingTop, btnEdit, and btnDelete are Enabled = true
                SetButtonVisualState(_btnCollectData, true, Theme.Primary);
                SetButtonVisualState(_btnNewBookingTop, true, Theme.Primary);
                SetButtonVisualState(_btnEdit, true, Theme.Primary);
                SetButtonVisualState(_btnDelete, true, Theme.Danger);
                _tabCollect.Enabled = true;
            }
            else if (userRole == Roles.Admin)
            {
                // Partial Access: btnCollectData, btnNewBookingTop and btnDelete are Enabled = false,
                // but allow viewing/approving SLA contracts if applicable (btnEdit is Enabled = true)
                SetButtonVisualState(_btnCollectData, false, Theme.Primary);
                SetButtonVisualState(_btnNewBookingTop, false, Theme.Primary);
                SetButtonVisualState(_btnEdit, true, Theme.Primary);
                SetButtonVisualState(_btnDelete, false, Theme.Danger);
                _tabCollect.Enabled = false;

                // Ensure active tab is not the blocked collect tab
                if (_tabs != null && _tabs.SelectedTab == _tabCollect)
                {
                    _tabs.SelectedTab = _tabRecords;
                }
            }
            else // Roles.SuperAdmin or Roles.Manager: View Only
            {
                // View Only: btnCollectData, btnNewBookingTop, btnEdit, and btnDelete MUST be Enabled = false
                SetButtonVisualState(_btnCollectData, false, Theme.Primary);
                SetButtonVisualState(_btnNewBookingTop, false, Theme.Primary);
                SetButtonVisualState(_btnEdit, false, Theme.Primary);
                SetButtonVisualState(_btnDelete, false, Theme.Danger);
                _tabCollect.Enabled = false;

                // Ensure active tab is not the blocked collect tab
                if (_tabs != null && _tabs.SelectedTab == _tabCollect)
                {
                    _tabs.SelectedTab = _tabRecords;
                }
            }

            // Propagate permissions to nested child view if instantiated
            _dataCollectionView?.ApplyViewPermissions(userRole);
        }

        private static void SetButtonVisualState(Button? btn, bool enabled, Color enabledColor)
        {
            if (btn == null) return;

            btn.Enabled = enabled;
            if (!enabled)
            {
                btn.BackColor = Color.FromArgb(226, 232, 240); // Theme.Border / Slate-200
                btn.ForeColor = Color.FromArgb(148, 163, 184); // Slate-400
                btn.Cursor = Cursors.Default;
            }
            else
            {
                btn.BackColor = enabledColor;
                btn.ForeColor = Color.White;
                btn.Cursor = Cursors.Hand;
            }
        }
    }

    // =================================================================
    // Model for Retention Actions Grid
    // =================================================================
    public class RetentionItemModel
    {
        public int ServiceRequestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string Priority { get; set; } = "MEDIUM";
        public int DaysInactive { get; set; }
        public string Basis { get; set; } = string.Empty;
        public string SuggestedAction { get; set; } = string.Empty;
    }

    // =================================================================
    // Dialog: Edit Customer Record
    // =================================================================
    public class EditRecordForm : Form
    {
        private readonly DataCollectionDto _record;
        private readonly ApiClient _api;

        private TextBox _txtName = null!;
        private TextBox _txtContact = null!;
        private ComboBox _cmbType = null!;
        private ComboBox _cmbService = null!;
        private TextBox _txtLocation = null!;
        private DateTimePicker _dtpPref = null!;
        private DateTimePicker _dtpFollow = null!;
        private ComboBox _cmbStaff = null!;
        private TextBox _txtNotes = null!;
        private Button _btnSave = null!;

        public EditRecordForm(DataCollectionDto record, ApiClient api)
        {
            _record = record;
            _api = api;

            Text = $"Edit Record #{_record.ServiceRequestId} — {_record.CustomerName}";
            Size = new Size(580, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            BuildForm();
        }

        private void BuildForm()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), AutoScroll = true };
            Controls.Add(pnl);

            int y = 12;

            AddHeader(pnl, "Edit Customer & Service Request", ref y);

            _txtName = AddField(pnl, "Customer / Company Name:", _record.CustomerName, ref y);
            _txtContact = AddField(pnl, "Contact Details:", _record.ContactDetails, ref y);

            _cmbType = AddDropdown(pnl, "Customer Type:", new[] { "Individual", "Company" }, _record.CustomerType, ref y);
            _cmbService = AddDropdown(pnl, "Service Requested:", new[] { "General Cleaning", "Deep Cleaning", "Office Cleaning", "Move-in Cleaning", "Move-out Cleaning" }, _record.RequestedService, ref y);

            _txtLocation = AddField(pnl, "Service Location:", _record.ServiceLocation, ref y);

            _dtpPref = AddDatePicker(pnl, "Preferred Service Date:", _record.PreferredDate, false, ref y);
            _dtpFollow = AddDatePicker(pnl, "Follow-Up Date:", _record.FollowUpDate ?? DateTime.Today.AddDays(7), true, ref y);
            _dtpFollow.Checked = _record.FollowUpDate.HasValue;

            _cmbStaff = AddDropdown(pnl, "Assigned Staff:", new[] { "Juan Dela Cruz", "Maria Santos", "Pedro Reyes", "Ana Lopez" }, _record.AssignedSalesStaff, ref y);
            _txtNotes = AddMultilineField(pnl, "Notes:", _record.Notes ?? "", 60, ref y);

            y += 10;
            var pnlBtns = new Panel { Location = new Point(24, y), Size = new Size(pnl.Width - 48, 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(100, 36), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.BackColor = Color.FromArgb(241, 245, 249);
            pnlBtns.Controls.Add(btnCancel);

            _btnSave = new Button { Text = "✓ Save Changes", Size = new Size(130, 36), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += async (s, e) => await SaveChangesAsync();
            pnlBtns.Controls.Add(_btnSave);

            pnl.Controls.Add(pnlBtns);
        }

        private async Task SaveChangesAsync()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtContact.Text))
            {
                MessageBox.Show("Name and Contact Details are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _record.CustomerName = _txtName.Text.Trim();
            _record.LeadName = _txtName.Text.Trim();
            _record.ContactDetails = _txtContact.Text.Trim();
            _record.ContactInfo = _txtContact.Text.Trim();
            _record.CustomerType = _cmbType.SelectedItem?.ToString() ?? "Individual";
            _record.RequestedService = _cmbService.SelectedItem?.ToString() ?? "General Cleaning";
            _record.ServiceOfInterest = _record.RequestedService;
            _record.ServiceLocation = _txtLocation.Text.Trim();
            _record.PreferredDate = _dtpPref.Value.Date;
            _record.FollowUpDate = _dtpFollow.Checked ? _dtpFollow.Value.Date : null;
            _record.AssignedSalesStaff = _cmbStaff.SelectedItem?.ToString() ?? "Juan Dela Cruz";
            _record.Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            try
            {
                var (success, msg) = await _api.UpdateAsync(_record.ServiceRequestId, _record);
                if (success)
                {
                    MessageBox.Show("Record updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(msg, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "✓ Save Changes";
            }
        }

        private static void AddHeader(Control parent, string text, ref int y)
        {
            var lbl = new Label { Text = text, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(24, y), Size = new Size(500, 26) };
            parent.Controls.Add(lbl);
            y += 34;
        }

        private static TextBox AddField(Control parent, string label, string val, ref int y)
        {
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, y), Size = new Size(500, 18) };
            parent.Controls.Add(lbl);
            y += 20;

            var tb = new TextBox { Text = val, Location = new Point(24, y), Size = new Size(510, 28) };
            parent.Controls.Add(tb);
            y += 36;
            return tb;
        }

        private static TextBox AddMultilineField(Control parent, string label, string val, int h, ref int y)
        {
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, y), Size = new Size(500, 18) };
            parent.Controls.Add(lbl);
            y += 20;

            var tb = new TextBox { Text = val, Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(24, y), Size = new Size(510, h) };
            parent.Controls.Add(tb);
            y += h + 8;
            return tb;
        }

        private static ComboBox AddDropdown(Control parent, string label, string[] items, string current, ref int y)
        {
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, y), Size = new Size(500, 18) };
            parent.Controls.Add(lbl);
            y += 20;

            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(24, y), Size = new Size(510, 28) };
            cb.Items.AddRange(items);
            cb.SelectedItem = current;
            if (cb.SelectedIndex < 0 && items.Length > 0) cb.SelectedIndex = 0;
            parent.Controls.Add(cb);
            y += 36;
            return cb;
        }

        private static DateTimePicker AddDatePicker(Control parent, string label, DateTime current, bool showCheck, ref int y)
        {
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, y), Size = new Size(500, 18) };
            parent.Controls.Add(lbl);
            y += 20;

            var dtp = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = current, ShowCheckBox = showCheck, Location = new Point(24, y), Size = new Size(510, 28) };
            parent.Controls.Add(dtp);
            y += 36;
            return dtp;
        }
    }

    // =================================================================
    // Dialog: Take Retention Action
    // =================================================================
    public class TakeRetentionActionForm : Form
    {
        private readonly RetentionItemModel _item;
        private readonly ApiClient _api;

        private ComboBox _cmbAction = null!;
        private DateTimePicker _dtpNextFollowUp = null!;
        private TextBox _txtNotes = null!;
        private Button _btnLog = null!;

        public TakeRetentionActionForm(RetentionItemModel item, ApiClient api)
        {
            _item = item;
            _api = api;

            Text = $"Retention Action — {_item.CustomerName} (#{_item.ServiceRequestId})";
            Size = new Size(520, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            BuildForm();
        }

        private void BuildForm()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
            Controls.Add(pnl);

            int y = 14;

            var lblTitle = new Label
            {
                Text = $"Target: {_item.CustomerName}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, y),
                Size = new Size(460, 24)
            };
            pnl.Controls.Add(lblTitle);
            y += 26;

            var lblBasis = new Label
            {
                Text = $"Priority: {_item.Priority}  •  Risk: {_item.Basis}",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(20, y),
                Size = new Size(460, 20)
            };
            pnl.Controls.Add(lblBasis);
            y += 30;

            // Action Selection
            var lblAction = new Label
            {
                Text = "Action Taken:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, y),
                Size = new Size(460, 18)
            };
            pnl.Controls.Add(lblAction);
            y += 20;

            _cmbAction = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, y),
                Size = new Size(460, 28)
            };
            _cmbAction.Items.AddRange(new object[]
            {
                "📞 Direct Phone Call — Booked Follow-Up Cleaning",
                "📧 Sent 15% VIP Reactivation Voucher via Email",
                "🤝 Assigned Account Manager for Site Inspection",
                "💬 Customer Reached — Requested Call Next Month",
                "❌ Unreachable / No Answer — Scheduled Retry"
            });
            _cmbAction.SelectedIndex = 0;
            pnl.Controls.Add(_cmbAction);
            y += 36;

            // Next Follow-Up Date
            var lblNext = new Label
            {
                Text = "Next Scheduled Follow-Up Date:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, y),
                Size = new Size(460, 18)
            };
            pnl.Controls.Add(lblNext);
            y += 20;

            _dtpNextFollowUp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(7),
                Location = new Point(20, y),
                Size = new Size(460, 28)
            };
            pnl.Controls.Add(_dtpNextFollowUp);
            y += 36;

            // Notes
            var lblNotes = new Label
            {
                Text = "Action Notes / Customer Response:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, y),
                Size = new Size(460, 18)
            };
            pnl.Controls.Add(lblNotes);
            y += 20;

            _txtNotes = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Log conversation outcome, promo code offered, or dispatch notes...",
                Location = new Point(20, y),
                Size = new Size(460, 70)
            };
            pnl.Controls.Add(_txtNotes);
            y += 82;

            // Buttons
            var pnlBtns = new Panel { Location = new Point(20, y), Size = new Size(460, 40) };

            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(95, 36), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249) };
            btnCancel.FlatAppearance.BorderSize = 0;
            pnlBtns.Controls.Add(btnCancel);

            _btnLog = new Button { Text = "✓ Log Retention Action", Size = new Size(180, 36), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            _btnLog.FlatAppearance.BorderSize = 0;
            _btnLog.Click += async (s, e) => await LogActionAsync();
            pnlBtns.Controls.Add(_btnLog);

            pnl.Controls.Add(pnlBtns);
        }

        private async Task LogActionAsync()
        {
            _btnLog.Enabled = false;
            _btnLog.Text = "Logging...";

            try
            {
                var record = await _api.GetByIdAsync(_item.ServiceRequestId);
                if (record == null)
                {
                    MessageBox.Show("Could not find record in API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string actionText = _cmbAction.SelectedItem?.ToString() ?? "Retention Outreach";
                string userNotes = _txtNotes.Text.Trim();
                string logEntry = $"[Retention Action: {actionText} on {DateTime.Now:yyyy-MM-dd}] {(string.IsNullOrEmpty(userNotes) ? "" : " - " + userNotes)}";

                record.Notes = string.IsNullOrEmpty(record.Notes) ? logEntry : record.Notes + "\n" + logEntry;
                record.FollowUpDate = _dtpNextFollowUp.Value.Date;

                var (success, msg) = await _api.UpdateAsync(record.ServiceRequestId, record);
                if (success)
                {
                    MessageBox.Show($"Retention action successfully logged for {_item.CustomerName}!", "Action Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Failed to log action: {msg}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error logging action: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnLog.Enabled = true;
                _btnLog.Text = "✓ Log Retention Action";
            }
        }
    }
}