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
    /// LAYER 1 — Leads Management View.
    ///
    /// Responsibilities:
    ///   - Displays active or filtered lead inquiries.
    ///   - Allows creating new inquiries via NewLeadDialog.
    ///   - Allows updating lead status (Quoted, Won, Lost).
    ///   - Allows promoting qualified leads (Quoted or Won) to Customers.
    ///   - Supports deduplication check and conflict resolution dialog.
    ///   - Enforces RBAC permissions: Manager is view-only; SalesStaff/Admin/SuperAdmin can create & convert.
    /// </summary>
    public class LeadsView : BaseView
    {
        private readonly ApiClient _api = new();
        private List<LeadDto> _allLeads = new();

        // Header controls
        private Label _lblCount = null!;
        private Button _btnRefresh = null!;
        private Button _btnNewLead = null!;
        private Button _btnConvert = null!;

        // Filter & Search controls
        private TextBox _txtSearch = null!;
        private ComboBox _cmbStatusFilter = null!;

        // Grid
        private DataGridView _grid = null!;

        public LeadsView()
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

            // ── Main Card Panel ─────────────────────────────────────
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Theme.Surface
            };
            ApplyCardStyle(card);
            Controls.Add(card);

            // ── Card Header (64px) ──────────────────────────────────
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
                Text = "Leads Management",
                Font = Theme.SubHeaderFont,
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 220,
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
                Width = 140,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(_lblCount);

            // Right-aligned header buttons (order: Refresh, Convert, New Lead)
            _btnNewLead = new Button
            {
                Text = "+ New Lead",
                Dock = DockStyle.Right,
                Width = 135,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewLead);
            _btnNewLead.Click += OnNewLeadClick;
            pnlHeader.Controls.Add(_btnNewLead);

            var pnlSpacerH1 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpacerH1);

            _btnConvert = new Button
            {
                Text = "★  Convert to Customer",
                Dock = DockStyle.Right,
                Width = 180,
                Height = 36,
                Enabled = false
            };
            Theme.ApplySecondaryButtonStyle(_btnConvert);
            _btnConvert.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnConvert.Click += async (s, e) => await OnConvertClickAsync();
            pnlHeader.Controls.Add(_btnConvert);

            var pnlSpacerH2 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpacerH2);

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

            // ── Search & Filter Toolbar (44px) ──────────────────────
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 6, 20, 6)
            };
            card.Controls.Add(pnlToolbar);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 320,
                Font = Theme.BodyFont,
                PlaceholderText = "🔍  Search by name, contact, source, or address..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            pnlToolbar.Controls.Add(_txtSearch);

            var lblStatusLabel = new Label
            {
                Text = "  Status:",
                Dock = DockStyle.Left,
                Width = 60,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlToolbar.Controls.Add(lblStatusLabel);

            _cmbStatusFilter = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 200,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbStatusFilter.Items.AddRange(new object[]
            {
                "All Active (Excl. Converted)",
                "All Leads (Incl. Converted)",
                "New",
                "Contacted",
                "Quoted",
                "Won",
                "Lost",
                "Converted"
            });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += async (s, e) => await LoadAsync();
            pnlToolbar.Controls.Add(_cmbStatusFilter);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            card.Controls.Add(pnlDivider);

            // ── DataGridView ────────────────────────────────────────
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

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId",          HeaderText = "Lead #",        DataPropertyName = "LeadId",            Width = 95,  MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { Name = "colName",        HeaderText = "Lead Name",     DataPropertyName = "LeadName",          Width = 160, MinimumWidth = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { Name = "colContact",     HeaderText = "Contact Info",  DataPropertyName = "ContactInfo",       Width = 180, MinimumWidth = 140 },
                new DataGridViewTextBoxColumn { Name = "colSource",      HeaderText = "Source",        DataPropertyName = "LeadSource",        Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colService",     HeaderText = "Interest",      DataPropertyName = "ServiceOfInterest", Width = 130, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colQuotedPrice", HeaderText = "Quoted Price",  DataPropertyName = "QuotedPrice",       Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "Status",        DataPropertyName = "Status",            Width = 110, MinimumWidth = 90 },
                new DataGridViewTextBoxColumn { Name = "colAddress",     HeaderText = "Address",       DataPropertyName = "ServiceAddress",    Width = 150, MinimumWidth = 100 },
                new DataGridViewTextBoxColumn { Name = "colDate",        HeaderText = "Created Date",  DataPropertyName = "CreatedAt",         Width = 120, MinimumWidth = 100 }
            });

            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

                var colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "colId" && e.Value is int id)
                {
                    e.Value = $"LD-{id:D4}";
                    e.FormattingApplied = true;
                }
                else if (colName == "colQuotedPrice")
                {
                    if (e.Value is decimal price)
                    {
                        e.Value = $"${price:N2}";
                    }
                    else
                    {
                        e.Value = "—";
                    }
                    e.FormattingApplied = true;
                }
                else if (colName == "colDate" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("MMM dd, yyyy");
                    e.FormattingApplied = true;
                }
                else if (colName == "colStatus" && e.Value is string status)
                {
                    e.CellStyle.ForeColor = LeadStatusHelper.GetStatusColor(status);
                    e.CellStyle.BackColor = LeadStatusHelper.GetStatusBgColor(status);
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
            };

            _grid.SelectionChanged += (s, e) => UpdateConvertButtonState();

            card.Controls.Add(_grid);
            _grid.BringToFront();

            ResumeLayout(true);
        }

        // ============================================================
        // RBAC Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            base.ApplyViewPermissions(userRole);

            bool isManager = string.Equals(userRole, Roles.Manager, StringComparison.OrdinalIgnoreCase);

            if (isManager)
            {
                // Manager is view-only on Leads
                _btnNewLead.Visible = false;
                _btnConvert.Visible = false;
            }
            else
            {
                _btnNewLead.Visible = true;
                _btnConvert.Visible = true;
                UpdateConvertButtonState();
            }
        }

        private void UpdateConvertButtonState()
        {
            if (SessionManager.IsManager)
            {
                _btnConvert.Enabled = false;
                return;
            }

            var selectedLead = GetSelectedLead();
            if (selectedLead == null)
            {
                _btnConvert.Enabled = false;
                return;
            }

            // Only Quoted or Won leads can be converted to Customers
            bool canConvert = string.Equals(selectedLead.Status, "Won", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(selectedLead.Status, "Quoted", StringComparison.OrdinalIgnoreCase);

            _btnConvert.Enabled = canConvert;
        }

        private LeadDto? GetSelectedLead()
        {
            if (_grid.SelectedRows.Count == 0) return null;
            return _grid.SelectedRows[0].DataBoundItem as LeadDto;
        }

        // ============================================================
        // Data Loading & Filtering
        // ============================================================
        public async Task LoadAsync()
        {
            _lblCount.Text = "Loading...";

            string? statusFilter = null;
            bool includeConverted = false;

            var selected = _cmbStatusFilter.SelectedItem?.ToString() ?? "";
            if (selected == "All Leads (Incl. Converted)")
            {
                includeConverted = true;
            }
            else if (selected != "All Active (Excl. Converted)")
            {
                statusFilter = selected;
                includeConverted = true;
            }

            _allLeads = await _api.GetLeadsAsync(statusFilter, includeConverted);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allLeads.AsEnumerable();

            var search = _txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(l =>
                    l.LeadName.ToLower().Contains(lower) ||
                    l.ContactInfo.ToLower().Contains(lower) ||
                    l.LeadSource.ToLower().Contains(lower) ||
                    l.ServiceOfInterest.ToLower().Contains(lower) ||
                    (l.ServiceAddress != null && l.ServiceAddress.ToLower().Contains(lower)) ||
                    $"ld-{l.LeadId:d4}".Contains(lower));
            }

            var filtered = query.ToList();
            _grid.DataSource = filtered;
            _lblCount.Text = $"{filtered.Count} of {_allLeads.Count} leads";
            UpdateConvertButtonState();
        }

        // ============================================================
        // Actions: New Lead
        // ============================================================
        private void OnNewLeadClick(object? sender, EventArgs e)
        {
            using var dialog = new NewLeadDialog();
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                ShowToast("New lead captured successfully!", true);
                _ = LoadAsync();
            }
        }

        // ============================================================
        // Actions: Convert to Customer
        // ============================================================
        private async Task OnConvertClickAsync()
        {
            var lead = GetSelectedLead();
            if (lead == null) return;

            if (!string.Equals(lead.Status, "Won", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lead.Status, "Quoted", StringComparison.OrdinalIgnoreCase))
            {
                ShowToast("Only leads with status 'Quoted' or 'Won' can be converted.", false);
                return;
            }

            // Attempt conversion
            _btnConvert.Enabled = false;
            var response = await _api.ConvertLeadAsync(lead.LeadId);

            if (response.Success)
            {
                ShowToast(response.Message, true);
                await LoadAsync();
                return;
            }

            // Handle potential duplicate conflict (409)
            if (response.DuplicateConflict && response.DuplicateInfo != null)
            {
                using var resDialog = new LeadDuplicateResolutionDialog(lead, response.DuplicateInfo);
                var dialogResult = resDialog.ShowDialog(FindForm());

                if (dialogResult == DialogResult.OK)
                {
                    if (resDialog.UserChoice == DuplicateResolutionChoice.UseExisting)
                    {
                        var linkResponse = await _api.ConvertLeadAsync(lead.LeadId, useExistingCustomerId: response.DuplicateInfo.ExistingCustomerId);
                        if (linkResponse.Success)
                        {
                            ShowToast(linkResponse.Message, true);
                            await LoadAsync();
                        }
                        else
                        {
                            ShowToast(linkResponse.Message, false);
                        }
                    }
                    else if (resDialog.UserChoice == DuplicateResolutionChoice.CreateAnyway)
                    {
                        var createResponse = await _api.ConvertLeadAsync(lead.LeadId, forceCreate: true);
                        if (createResponse.Success)
                        {
                            ShowToast(createResponse.Message, true);
                            await LoadAsync();
                        }
                        else
                        {
                            ShowToast(createResponse.Message, false);
                        }
                    }
                }
            }
            else
            {
                ShowToast(response.Message, false);
            }

            UpdateConvertButtonState();
        }
    }
}
