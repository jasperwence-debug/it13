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
        private Button _btnAvailService = null!;
        private Button _btnMarkLost = null!;
        private ContextMenuStrip _gridContextMenu = null!;

        // Filter & Search controls
        private TextBox _txtSearch = null!;
        private ComboBox _cmbStatusFilter = null!;

        // Grid
        private DataGridView _grid = null!;

        // Pagination state (default: 25 per page, newest first)
        private int _pageSize = 25;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<LeadDto> _filteredLeads = new();

        // Pagination controls
        private Panel _pnlPagination = null!;
        private Label _lblPageInfo = null!;
        private ComboBox _cmbPageSize = null!;
        private Button _btnFirstPage = null!;
        private Button _btnPrevPage = null!;
        private Button _btnNextPage = null!;
        private Button _btnLastPage = null!;
        private FlowLayoutPanel _pnlPageNumbers = null!;

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

            // Right-aligned header buttons (order: Refresh, Lost/Not Interested, Avail Service, New Lead)
            _btnNewLead = new Button
            {
                Text = "+ New Lead",
                Dock = DockStyle.Right,
                Width = 120,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnNewLead);
            _btnNewLead.Click += OnNewLeadClick;
            pnlHeader.Controls.Add(_btnNewLead);

            var pnlSpacerH1 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpacerH1);

            _btnAvailService = new Button
            {
                Text = "⚡  Avail Service / Book",
                Dock = DockStyle.Right,
                Width = 185,
                Height = 36,
                Enabled = false
            };
            Theme.ApplyPrimaryButtonStyle(_btnAvailService);
            _btnAvailService.Click += async (s, e) => await OnAvailServiceClickAsync();
            pnlHeader.Controls.Add(_btnAvailService);

            var pnlSpacerH2 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpacerH2);

            _btnMarkLost = new Button
            {
                Text = "❌  Not Interested",
                Dock = DockStyle.Right,
                Width = 140,
                Height = 36,
                Enabled = false
            };
            Theme.ApplySecondaryButtonStyle(_btnMarkLost);
            _btnMarkLost.Click += async (s, e) => await OnMarkLostClickAsync();
            pnlHeader.Controls.Add(_btnMarkLost);

            var pnlSpacerH3 = new Panel { Dock = DockStyle.Right, Width = 8, BackColor = Theme.Surface };
            pnlHeader.Controls.Add(pnlSpacerH3);

            _btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Dock = DockStyle.Right,
                Width = 90,
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
                        e.Value = $"₱{price:N2}";
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

            // Context menu for row actions
            _gridContextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };

            var mnuAvail = new ToolStripMenuItem("⚡  Avail Service & Book...", null, async (s, e) => await OnAvailServiceClickAsync());
            var mnuContacted = new ToolStripMenuItem("📞  Mark as Contacted", null, async (s, e) => await OnMarkContactedClickAsync());
            var mnuLost = new ToolStripMenuItem("❌  Mark as Lost / Not Interested...", null, async (s, e) => await OnMarkLostClickAsync());
            var mnuRefresh = new ToolStripMenuItem("↻  Refresh", null, async (s, e) => await LoadAsync());

            _gridContextMenu.Items.AddRange(new ToolStripItem[]
            {
                mnuAvail,
                new ToolStripSeparator(),
                mnuContacted,
                mnuLost,
                new ToolStripSeparator(),
                mnuRefresh
            });

            _gridContextMenu.Opening += (s, e) =>
            {
                var sel = GetSelectedLead();
                if (sel == null || SessionManager.IsManager)
                {
                    mnuAvail.Enabled = false;
                    mnuContacted.Enabled = false;
                    mnuLost.Enabled = false;
                    return;
                }

                bool isConverted = string.Equals(sel.Status, "Converted", StringComparison.OrdinalIgnoreCase);
                bool isLost = string.Equals(sel.Status, "Lost", StringComparison.OrdinalIgnoreCase);

                mnuAvail.Enabled = !isConverted;
                mnuContacted.Enabled = !isConverted && !isLost && sel.Status == "New";
                mnuLost.Enabled = !isConverted && !isLost;
            };

            _grid.ContextMenuStrip = _gridContextMenu;
            _grid.CellDoubleClick += async (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    await OnAvailServiceClickAsync();
                }
            };
            _grid.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    _grid.ClearSelection();
                    _grid.Rows[e.RowIndex].Selected = true;
                    UpdateActionButtonState();
                }
            };

            _grid.SelectionChanged += (s, e) => UpdateActionButtonState();

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
                _btnAvailService.Visible = false;
                _btnMarkLost.Visible = false;
            }
            else
            {
                _btnNewLead.Visible = true;
                _btnAvailService.Visible = true;
                _btnMarkLost.Visible = true;
                UpdateActionButtonState();
            }
        }

        private void UpdateActionButtonState()
        {
            if (SessionManager.IsManager)
            {
                _btnAvailService.Enabled = false;
                _btnMarkLost.Enabled = false;
                return;
            }

            var selectedLead = GetSelectedLead();
            if (selectedLead == null)
            {
                _btnAvailService.Enabled = false;
                _btnMarkLost.Enabled = false;
                return;
            }

            bool isConverted = string.Equals(selectedLead.Status, "Converted", StringComparison.OrdinalIgnoreCase);
            bool isLost = string.Equals(selectedLead.Status, "Lost", StringComparison.OrdinalIgnoreCase);

            // A lead can avail a service as long as they are not already converted!
            _btnAvailService.Enabled = !isConverted;
            _btnMarkLost.Enabled = !isConverted && !isLost;
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
                var lower = search.ToLowerInvariant();
                query = query.Where(l =>
                    (!string.IsNullOrEmpty(l.LeadName) && l.LeadName.ToLowerInvariant().Contains(lower)) ||
                    (!string.IsNullOrEmpty(l.ContactInfo) && l.ContactInfo.ToLowerInvariant().Contains(lower)) ||
                    (!string.IsNullOrEmpty(l.LeadSource) && l.LeadSource.ToLowerInvariant().Contains(lower)) ||
                    (!string.IsNullOrEmpty(l.ServiceOfInterest) && l.ServiceOfInterest.ToLowerInvariant().Contains(lower)) ||
                    (!string.IsNullOrEmpty(l.ServiceAddress) && l.ServiceAddress.ToLowerInvariant().Contains(lower)) ||
                    $"ld-{l.LeadId:d4}".Contains(lower));
            }

            // Client-side sorting: newest first (CreatedAt DESC, then LeadId DESC)
            query = query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LeadId);

            _filteredLeads = query.ToList();
            CalculatePagination();
            RenderPage();
        }

        private void CalculatePagination()
        {
            int total = _filteredLeads.Count;
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

            int total = _filteredLeads.Count;
            if (total == 0)
            {
                _grid.DataSource = null;
                _lblCount.Text = "0 leads matching";
                _lblPageInfo.Text = "No leads to display";
                _pnlPageNumbers.Controls.Clear();
                _btnFirstPage.Enabled = _btnPrevPage.Enabled = _btnNextPage.Enabled = _btnLastPage.Enabled = false;
                UpdateActionButtonState();
                return;
            }

            int startIndex = (_currentPage - 1) * _pageSize;
            var pageRecords = _filteredLeads.Skip(startIndex).Take(_pageSize).ToList();
            int endIndex = startIndex + pageRecords.Count;

            _grid.DataSource = pageRecords;
            _lblCount.Text = $"{total} of {_allLeads.Count} leads";
            _lblPageInfo.Text = $"Showing {startIndex + 1} to {endIndex} of {total} records.";

            _btnFirstPage.Enabled = _btnPrevPage.Enabled = (_currentPage > 1);
            _btnNextPage.Enabled = _btnLastPage.Enabled = (_currentPage < _totalPages);
            UpdatePageNumberButtons();
            UpdateActionButtonState();
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

        // ============================================================
        // Actions: New Lead
        // ============================================================
        private async void OnNewLeadClick(object? sender, EventArgs e)
        {
            using var dialog = new NewLeadDialog();
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                ShowToast("New lead captured successfully!", true);
                _currentPage = 1;
                await LoadAsync();
            }
        }

        // ============================================================
        // Actions: Avail Service / Book
        // ============================================================
        private async Task OnAvailServiceClickAsync()
        {
            var lead = GetSelectedLead();
            if (lead == null)
            {
                ShowToast("Please select a lead to book service.", false);
                return;
            }

            if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
            {
                ShowToast($"Lead LD-{lead.LeadId:D4} has already availed a service and is an active Customer.", false);
                return;
            }

            using var dialog = new AvailServiceDialog(lead);
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                ShowToast($"Lead LD-{lead.LeadId:D4} successfully availed service and converted to Customer!", true);
                await LoadAsync();
            }
        }

        private async Task OnMarkContactedClickAsync()
        {
            var lead = GetSelectedLead();
            if (lead == null) return;

            var (success, message, _) = await _api.UpdateLeadStatusAsync(lead.LeadId, new LeadStatusUpdateDto
            {
                Status = "Contacted"
            });

            if (success)
            {
                ShowToast($"Lead LD-{lead.LeadId:D4} marked as Contacted.", true);
                await LoadAsync();
            }
            else
            {
                ShowToast(message, false);
            }
        }

        private async Task OnMarkLostClickAsync()
        {
            var lead = GetSelectedLead();
            if (lead == null) return;

            using var dlg = new LeadLostDialog(lead);
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                var (success, message, _) = await _api.UpdateLeadStatusAsync(lead.LeadId, new LeadStatusUpdateDto
                {
                    Status = "Lost",
                    LostReason = dlg.LostReason
                });

                if (success)
                {
                    ShowToast($"Lead LD-{lead.LeadId:D4} marked as Lost.", true);
                    await LoadAsync();
                }
                else
                {
                    ShowToast(message, false);
                }
            }
        }
    }
}
