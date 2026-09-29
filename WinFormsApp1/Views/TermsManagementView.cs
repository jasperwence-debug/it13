using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// MODULE 8 — Terms & Management Control Center.
    ///
    /// Industry Standards:
    ///   - Super Admin: SaaS Master Subscription Agreement (MSA), Platform SLAs (99.9% Uptime),
    ///     Data Privacy & Multi-Tenant Addendum (DPA), Acceptable Use Policy (AUP).
    ///   - Tenant Admin (Cleaning Company): Client Master Service Agreements, 24-hr Cancellation
    ///     & Rescheduling Policy, Invoicing & Net Payment Terms, Property Damage & Breakage Disclaimer,
    ///     Staff Non-Solicitation Covenants.
    ///   - Manager & Sales Staff: Read-only contractual reference with 1-click clipboard copying,
    ///     searchable clause lookup, and agreement document export for quoting/client emails.
    ///   - Local JSON persistence in terms_and_policies.json.
    /// </summary>
    public class TermsManagementView : BaseView
    {
        public class TermSection
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string TargetScope { get; set; } = "Tenant"; // "SuperAdmin" or "Tenant"
            public string Version { get; set; } = "v1.0";
            public DateTime EffectiveDate { get; set; } = DateTime.Today;
            public string Status { get; set; } = "Active"; // "Active" or "Draft"
            public string Summary { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string LastModifiedBy { get; set; } = "System";
            public DateTime LastModifiedDate { get; set; } = DateTime.Now;
            public bool IsCustom { get; set; } = false;
        }

        private List<TermSection> _allSections = new();
        private List<TermSection> _filteredSections = new();
        private TermSection? _selectedSection;

        // UI Controls - Header
        private Label _lblHeaderTitle = null!;
        private Label _lblHeaderSubtitle = null!;
        private Label _lblRoleBadge = null!;
        private Button _btnExportAll = null!;
        private Button _btnAddClause = null!;

        // Filter / Search
        private TextBox _txtSearch = null!;
        private ComboBox _cmbCategory = null!;
        private readonly FlowLayoutPanel _pnlCategoryPills = new();

        // Left Pane: Clause List
        private DataGridView _gridClauses = null!;
        private Label _lblClauseCount = null!;

        // Right Pane: Clause Details & Editor
        private Panel _pnlRight = null!;
        private Label _lblClauseIdBadge = null!;
        private Label _lblClauseStatusBadge = null!;
        private TextBox _txtClauseTitle = null!;
        private Label _lblClauseMeta = null!;
        private TextBox _txtClauseSummary = null!;
        private TextBox _txtClauseContent = null!;
        private Button _btnSave = null!;
        private Button _btnCopyClause = null!;
        private Button _btnResetClause = null!;
        private Button _btnExportClause = null!;
        private Button _btnDeleteClause = null!;
        private Panel _pnlReadOnlyNotice = null!;

        private bool _canEdit = true;
        private string _jsonFilePath = string.Empty;

        public TermsManagementView()
        {
            DetermineStoragePath();
            BuildUI();
            LoadTermsData();
            ApplyFilter();
        }

        private void DetermineStoragePath()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _jsonFilePath = Path.Combine(appDir, "terms_and_policies.json");
            }
            catch
            {
                _jsonFilePath = "terms_and_policies.json";
            }
        }

        // ============================================================
        // UI Construction
        // ============================================================
        private void BuildUI()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;

            // Main outer card
            var pnlMainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 20, 24, 20)
            };
            ApplyCardStyle(pnlMainCard);
            Controls.Add(pnlMainCard);

            // ── TOP HEADER BAR ──────────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Theme.Surface
            };
            pnlMainCard.Controls.Add(pnlTop);

            var pnlHeaderLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 580,
                BackColor = Theme.Surface
            };
            pnlTop.Controls.Add(pnlHeaderLeft);

            _lblHeaderTitle = new Label
            {
                Text = "📜  Terms & Management",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeaderLeft.Controls.Add(_lblHeaderTitle);

            _lblHeaderSubtitle = new Label
            {
                Text = SessionManager.IsSuperAdmin
                    ? "Master SaaS Subscription Agreements (MSA), Platform SLAs & Data Protection Governance"
                    : "Client Service Contracts, Cancellation Policy, Billing Terms & Property Liability Disclaimers",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeaderLeft.Controls.Add(_lblHeaderSubtitle);

            // Header Action Buttons (Dock Right)
            var pnlHeaderRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Theme.Surface,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };
            pnlTop.Controls.Add(pnlHeaderRight);

            _btnExportAll = new Button
            {
                Text = "📄  Export Agreement Package",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                BackColor = Color.FromArgb(241, 245, 249),
                Height = 38,
                Width = 210,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnExportAll.FlatAppearance.BorderColor = Theme.Border;
            _btnExportAll.Click += (s, e) => ExportFullAgreement();
            pnlHeaderRight.Controls.Add(_btnExportAll);

            _btnAddClause = new Button
            {
                Text = "➕  Add Custom Policy",
                Font = Theme.BodyFont,
                ForeColor = Color.White,
                BackColor = Theme.Primary,
                Height = 38,
                Width = 175,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnAddClause);
            _btnAddClause.Click += (s, e) => AddCustomClauseDialog();
            pnlHeaderRight.Controls.Add(_btnAddClause);

            _lblRoleBadge = new Label
            {
                Text = SessionManager.IsSuperAdmin ? "PLATFORM MSA TIER" : "TENANT POLICIES",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SessionManager.IsSuperAdmin ? Color.FromArgb(147, 51, 234) : Color.FromArgb(37, 99, 235),
                BackColor = SessionManager.IsSuperAdmin ? Color.FromArgb(243, 232, 255) : Color.FromArgb(239, 246, 255),
                Height = 38,
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(8, 0, 0, 0)
            };
            pnlHeaderRight.Controls.Add(_lblRoleBadge);

            // ── READ-ONLY BANNER FOR STAFF ──────────────────────────────
            _pnlReadOnlyNotice = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(254, 243, 199), // Amber-100
                Padding = new Padding(14, 0, 14, 0),
                Visible = false
            };
            var lblNotice = new Label
            {
                Text = "ℹ️  Staff Reference Mode: You are viewing company standard agreements in read-only mode. Use 'Copy Clause' to insert standard text into customer quotes or emails.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14), // Amber-800
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            _pnlReadOnlyNotice.Controls.Add(lblNotice);
            pnlMainCard.Controls.Add(_pnlReadOnlyNotice);

            // ── FILTER & SEARCH BAR ──────────────────────────────────────
            var pnlFilterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 8, 0, 8)
            };
            pnlMainCard.Controls.Add(pnlFilterBar);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 280,
                Font = Theme.BodyFont,
                PlaceholderText = "🔍  Search clauses, terms, keywords..."
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            pnlFilterBar.Controls.Add(_txtSearch);

            var lblCategory = new Label
            {
                Text = "Category:",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Left,
                Width = 75,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlFilterBar.Controls.Add(lblCategory);

            _cmbCategory = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.BodyFont
            };
            _cmbCategory.SelectedIndexChanged += (s, e) => ApplyFilter();
            pnlFilterBar.Controls.Add(_cmbCategory);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.Border
            };
            pnlMainCard.Controls.Add(pnlDivider);

            // ── SPLIT CONTAINER: CLAUSE LIST (LEFT) & EDITOR (RIGHT) ─────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 12, 0, 0)
            };
            pnlMainCard.Controls.Add(pnlBody);

            // Left Pane: Clause Directory
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 0, 16, 0)
            };
            pnlBody.Controls.Add(pnlLeft);

            var pnlLeftHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Theme.Surface
            };
            pnlLeft.Controls.Add(pnlLeftHeader);

            _lblClauseCount = new Label
            {
                Text = "Clauses (0)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlLeftHeader.Controls.Add(_lblClauseCount);

            _gridClauses = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 48 },
                Font = Theme.BodyFont,
                GridColor = Color.FromArgb(241, 245, 249)
            };
            _gridClauses.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Code",
                Width = 75,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _gridClauses.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Section Title & Category",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _gridClauses.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _filteredSections.Count) return;
                var sec = _filteredSections[e.RowIndex];

                if (e.ColumnIndex == 0)
                {
                    e.Value = sec.Id;
                    e.CellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                    e.CellStyle.ForeColor = Theme.Primary;
                }
                else if (e.ColumnIndex == 1)
                {
                    e.Value = $"{sec.Title}\n[{sec.Category}]";
                }
            };
            _gridClauses.SelectionChanged += (s, e) =>
            {
                if (_gridClauses.CurrentRow != null && _gridClauses.CurrentRow.Index >= 0 && _gridClauses.CurrentRow.Index < _filteredSections.Count)
                {
                    SelectSection(_filteredSections[_gridClauses.CurrentRow.Index]);
                }
            };
            pnlLeft.Controls.Add(_gridClauses);

            // Right Pane: Detail & Editor
            _pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 16, 20, 16)
            };
            _pnlRight.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlRight.Width - 1, _pnlRight.Height - 1);
            };
            pnlBody.Controls.Add(_pnlRight);

            BuildRightEditorPane();

            // Set Z-Order so Fill is at index 0
            pnlBody.Controls.SetChildIndex(_pnlRight, 0);
            pnlBody.Controls.SetChildIndex(pnlLeft, 1);

            pnlMainCard.Controls.SetChildIndex(pnlBody, 0);
            pnlMainCard.Controls.SetChildIndex(pnlDivider, 1);
            pnlMainCard.Controls.SetChildIndex(pnlFilterBar, 2);
            pnlMainCard.Controls.SetChildIndex(_pnlReadOnlyNotice, 3);
            pnlMainCard.Controls.SetChildIndex(pnlTop, 4);

            ResumeLayout(false);
        }

        private void BuildRightEditorPane()
        {
            // Right Pane Header: Badge + Title + Status
            var pnlClauseHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent
            };
            _pnlRight.Controls.Add(pnlClauseHeader);

            var pnlBadges = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 24,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false
            };
            pnlClauseHeader.Controls.Add(pnlBadges);

            _lblClauseIdBadge = new Label
            {
                Text = "SECTION-ID",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 41, 59),
                Height = 22,
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlBadges.Controls.Add(_lblClauseIdBadge);

            _lblClauseStatusBadge = new Label
            {
                Text = "ACTIVE POLICY",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(5, 150, 105),
                BackColor = Color.FromArgb(236, 253, 245),
                Height = 22,
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlBadges.Controls.Add(_lblClauseStatusBadge);

            _txtClauseTitle = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlClauseHeader.Controls.Add(_txtClauseTitle);

            // Metadata bar
            _lblClauseMeta = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Version v1.0 • Effective: 2026-01-01 • Last Revised by System"
            };
            _pnlRight.Controls.Add(_lblClauseMeta);

            // Summary box (Key highlights)
            var pnlSummaryWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 6)
            };
            _pnlRight.Controls.Add(pnlSummaryWrapper);

            var lblSummaryTitle = new Label
            {
                Text = "KEY PROVISIONS & EXECUTIVE SUMMARY:",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Dock = DockStyle.Top,
                Height = 18
            };
            pnlSummaryWrapper.Controls.Add(lblSummaryTitle);

            _txtClauseSummary = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = Theme.CaptionFont,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(30, 58, 138),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            pnlSummaryWrapper.Controls.Add(_txtClauseSummary);

            // Bottom Action Bar
            var pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };
            _pnlRight.Controls.Add(pnlActions);

            var pnlActionsRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                WrapContents = false
            };
            pnlActions.Controls.Add(pnlActionsRight);

            _btnSave = new Button
            {
                Text = "💾  Save Changes",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Theme.Primary,
                Height = 36,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += (s, e) => SaveCurrentClause();
            pnlActionsRight.Controls.Add(_btnSave);

            _btnCopyClause = new Button
            {
                Text = "📋  Copy Clause",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                BackColor = Color.White,
                Height = 36,
                Width = 125,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnCopyClause.FlatAppearance.BorderColor = Theme.Border;
            _btnCopyClause.Click += (s, e) => CopyClauseToClipboard();
            pnlActionsRight.Controls.Add(_btnCopyClause);

            _btnExportClause = new Button
            {
                Text = "📄  Export Clause",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark,
                BackColor = Color.White,
                Height = 36,
                Width = 125,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnExportClause.FlatAppearance.BorderColor = Theme.Border;
            _btnExportClause.Click += (s, e) => ExportSingleClause();
            pnlActionsRight.Controls.Add(_btnExportClause);

            _btnResetClause = new Button
            {
                Text = "↺  Reset Template",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                BackColor = Color.White,
                Height = 36,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnResetClause.FlatAppearance.BorderColor = Theme.Border;
            _btnResetClause.Click += (s, e) => ResetCurrentClauseToDefault();
            pnlActionsRight.Controls.Add(_btnResetClause);

            _btnDeleteClause = new Button
            {
                Text = "🗑️  Delete",
                Font = Theme.BodyFont,
                ForeColor = Theme.Danger,
                BackColor = Color.FromArgb(254, 242, 242),
                Height = 36,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
                Visible = false
            };
            _btnDeleteClause.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            _btnDeleteClause.Click += (s, e) => DeleteCurrentClause();
            pnlActionsRight.Controls.Add(_btnDeleteClause);

            // Full Legal Text Editor
            var pnlEditorWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 8)
            };
            _pnlRight.Controls.Add(pnlEditorWrapper);

            var lblLegalLabel = new Label
            {
                Text = "FULL CONTRACTUAL & LEGAL TEXT:",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlEditorWrapper.Controls.Add(lblLegalLabel);

            _txtClauseContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = new Font("Consolas", 9.5F, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true
            };
            pnlEditorWrapper.Controls.Add(_txtClauseContent);

            // Set Z-Order inside _pnlRight
            _pnlRight.Controls.SetChildIndex(pnlEditorWrapper, 0);
            _pnlRight.Controls.SetChildIndex(pnlActions, 1);
            _pnlRight.Controls.SetChildIndex(pnlSummaryWrapper, 2);
            _pnlRight.Controls.SetChildIndex(_lblClauseMeta, 3);
            _pnlRight.Controls.SetChildIndex(pnlClauseHeader, 4);
        }

        // ============================================================
        // Data Management & Default Pre-Seeding
        // ============================================================
        private void LoadTermsData()
        {
            try
            {
                if (File.Exists(_jsonFilePath))
                {
                    var json = File.ReadAllText(_jsonFilePath);
                    var loaded = JsonSerializer.Deserialize<List<TermSection>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        _allSections = loaded;
                    }
                }
            }
            catch
            {
                // Fallback to defaults
            }

            if (_allSections.Count == 0)
            {
                _allSections = GetDefaultIndustryTerms();
                SaveAllSectionsToFile();
            }

            PopulateCategoryDropdown();
        }

        private void SaveAllSectionsToFile()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_allSections, options);
                File.WriteAllText(_jsonFilePath, json);
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to persist changes: {ex.Message}", false);
            }
        }

        private void PopulateCategoryDropdown()
        {
            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add("All Categories");

            var scope = SessionManager.IsSuperAdmin ? "SuperAdmin" : "Tenant";
            var cats = _allSections
                .Where(s => s.TargetScope == scope || SessionManager.IsSuperAdmin)
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var c in cats)
            {
                _cmbCategory.Items.Add(c);
            }

            _cmbCategory.SelectedIndex = 0;
        }

        private void ApplyFilter()
        {
            string query = _txtSearch.Text.Trim().ToLower();
            string selectedCat = _cmbCategory.SelectedItem?.ToString() ?? "All Categories";
            string currentScope = SessionManager.IsSuperAdmin ? "SuperAdmin" : "Tenant";

            _filteredSections = _allSections.Where(s =>
            {
                // Scope matching
                bool matchScope = SessionManager.IsSuperAdmin
                    ? (s.TargetScope == "SuperAdmin" || s.TargetScope == "Tenant")
                    : (s.TargetScope == "Tenant");

                if (!matchScope) return false;

                // Category matching
                if (selectedCat != "All Categories" && !s.Category.Equals(selectedCat, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Query matching
                if (!string.IsNullOrEmpty(query))
                {
                    bool matchQuery = s.Id.ToLower().Contains(query) ||
                                     s.Title.ToLower().Contains(query) ||
                                     s.Category.ToLower().Contains(query) ||
                                     s.Summary.ToLower().Contains(query) ||
                                     s.Content.ToLower().Contains(query);
                    if (!matchQuery) return false;
                }

                return true;
            }).OrderBy(s => s.Id).ToList();

            _lblClauseCount.Text = $"Clauses ({_filteredSections.Count})";
            _gridClauses.RowCount = _filteredSections.Count;
            _gridClauses.Invalidate();

            if (_filteredSections.Count > 0)
            {
                SelectSection(_filteredSections[0]);
            }
            else
            {
                ClearEditor();
            }
        }

        private void SelectSection(TermSection sec)
        {
            _selectedSection = sec;
            _lblClauseIdBadge.Text = sec.Id;
            _lblClauseStatusBadge.Text = sec.Status.ToUpper();
            _lblClauseStatusBadge.BackColor = sec.Status == "Active" ? Color.FromArgb(236, 253, 245) : Color.FromArgb(254, 243, 199);
            _lblClauseStatusBadge.ForeColor = sec.Status == "Active" ? Color.FromArgb(5, 150, 105) : Color.FromArgb(146, 64, 14);

            _txtClauseTitle.Text = sec.Title;
            _lblClauseMeta.Text = $"Version: {sec.Version} • Category: {sec.Category} • Effective: {sec.EffectiveDate:yyyy-MM-dd} • Last Revised by: {sec.LastModifiedBy} ({sec.LastModifiedDate:yyyy-MM-dd HH:mm})";
            _txtClauseSummary.Text = sec.Summary;
            _txtClauseContent.Text = sec.Content;

            _btnDeleteClause.Visible = sec.IsCustom && _canEdit;
        }

        private void ClearEditor()
        {
            _selectedSection = null;
            _lblClauseIdBadge.Text = "NONE";
            _txtClauseTitle.Text = string.Empty;
            _lblClauseMeta.Text = "No section selected";
            _txtClauseSummary.Text = string.Empty;
            _txtClauseContent.Text = string.Empty;
            _btnDeleteClause.Visible = false;
        }

        // ============================================================
        // Actions: Save, Copy, Export, Reset, Add, Delete
        // ============================================================
        private void SaveCurrentClause()
        {
            if (!_canEdit || _selectedSection == null)
            {
                ShowToast("Read-only reference: cannot save changes.", false);
                return;
            }

            var title = _txtClauseTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a valid clause title.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedSection.Title = title;
            _selectedSection.Summary = _txtClauseSummary.Text.Trim();
            _selectedSection.Content = _txtClauseContent.Text;
            _selectedSection.LastModifiedBy = SessionManager.CurrentUser?.Username ?? "Admin";
            _selectedSection.LastModifiedDate = DateTime.Now;

            SaveAllSectionsToFile();
            _gridClauses.Invalidate();
            SelectSection(_selectedSection);

            ShowToast($"Clause '{_selectedSection.Id}' updated successfully!", true);
        }

        private void CopyClauseToClipboard()
        {
            if (_selectedSection == null)
            {
                ShowToast("Please select a clause first.", false);
                return;
            }

            try
            {
                var copyText = $"=== {_selectedSection.Id}: {_selectedSection.Title} ({_selectedSection.Category}) ===\n" +
                               $"Version: {_selectedSection.Version} | Effective: {_selectedSection.EffectiveDate:yyyy-MM-dd}\n\n" +
                               $"SUMMARY:\n{_selectedSection.Summary}\n\n" +
                               $"LEGAL PROVISIONS:\n{_selectedSection.Content}\n" +
                               $"==================================================";

                Clipboard.SetText(copyText);
                ShowToast($"Copied clause '{_selectedSection.Id}' to clipboard!", true);
            }
            catch (Exception ex)
            {
                ShowToast($"Clipboard error: {ex.Message}", false);
            }
        }

        private void ExportSingleClause()
        {
            if (_selectedSection == null)
            {
                ShowToast("Please select a clause to export.", false);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = $"Export Clause {_selectedSection.Id}",
                Filter = "Text Document (*.txt)|*.txt|Markdown File (*.md)|*.md",
                FileName = $"{_selectedSection.Id}_{_selectedSection.Title.Replace(" ", "_")}.txt"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var text = $"# {_selectedSection.Id} - {_selectedSection.Title}\n\n" +
                               $"**Category:** {_selectedSection.Category}  \n" +
                               $"**Version:** {_selectedSection.Version}  \n" +
                               $"**Effective Date:** {_selectedSection.EffectiveDate:yyyy-MM-dd}  \n" +
                               $"**Last Revised:** {_selectedSection.LastModifiedDate:yyyy-MM-dd HH:mm} by {_selectedSection.LastModifiedBy}  \n\n" +
                               $"## Key Highlights\n{_selectedSection.Summary}\n\n" +
                               $"## Terms & Legal Text\n{_selectedSection.Content}\n";

                    File.WriteAllText(sfd.FileName, text);
                    ShowToast($"Clause exported successfully to {Path.GetFileName(sfd.FileName)}!", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportFullAgreement()
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Export Master Agreement Package",
                Filter = "Text Document (*.txt)|*.txt|Markdown Document (*.md)|*.md",
                FileName = SessionManager.IsSuperAdmin
                    ? $"SaaS_Master_Agreement_Package_{DateTime.Now:yyyyMMdd}.txt"
                    : $"Cleaning_Services_Master_Agreement_{DateTime.Now:yyyyMMdd}.txt"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    var header = SessionManager.IsSuperAdmin
                        ? "========================================================================\n" +
                          "          SaaS PLATFORM MASTER SUBSCRIPTION & SLA AGREEMENT             \n" +
                          $"               Generated: {DateTime.Now:yyyy-MM-dd HH:mm}               \n" +
                          "========================================================================\n\n"
                        : "========================================================================\n" +
                          "       CLEANING SERVICES MASTER CUSTOMER AGREEMENT & POLICIES           \n" +
                          $"               Generated: {DateTime.Now:yyyy-MM-dd HH:mm}               \n" +
                          "========================================================================\n\n";

                    sw.WriteLine(header);

                    foreach (var s in _filteredSections)
                    {
                        sw.WriteLine($"------------------------------------------------------------------------");
                        sw.WriteLine($"SECTION {s.Id}: {s.Title.ToUpper()}");
                        sw.WriteLine($"Category: {s.Category} | Version: {s.Version} | Status: {s.Status}");
                        sw.WriteLine($"------------------------------------------------------------------------");
                        sw.WriteLine($"[KEY SUMMARY]");
                        sw.WriteLine(s.Summary);
                        sw.WriteLine();
                        sw.WriteLine($"[TERMS & CONDITIONS]");
                        sw.WriteLine(s.Content);
                        sw.WriteLine("\n\n");
                    }

                    ShowToast($"Master Agreement Package exported ({_filteredSections.Count} sections)!", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ResetCurrentClauseToDefault()
        {
            if (!_canEdit || _selectedSection == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to reset clause '{_selectedSection.Id}' back to the industry standard template?",
                "Confirm Template Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var defaultMatch = GetDefaultIndustryTerms().FirstOrDefault(d => d.Id == _selectedSection.Id);
            if (defaultMatch != null)
            {
                _selectedSection.Title = defaultMatch.Title;
                _selectedSection.Summary = defaultMatch.Summary;
                _selectedSection.Content = defaultMatch.Content;
                _selectedSection.LastModifiedBy = "Default Template";
                _selectedSection.LastModifiedDate = DateTime.Now;

                SaveAllSectionsToFile();
                SelectSection(_selectedSection);
                ShowToast($"Reset clause '{_selectedSection.Id}' to default standard.", true);
            }
            else
            {
                ShowToast("No default template available for this custom section.", false);
            }
        }

        private void AddCustomClauseDialog()
        {
            if (!_canEdit) return;

            using var form = new Form
            {
                Text = "Add Custom Policy / Rider",
                Size = new Size(520, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Theme.Surface,
                Font = Theme.BodyFont
            };

            var lblCode = new Label { Text = "Section Code (e.g. POL-02):", Location = new Point(24, 20), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextMuted };
            var txtCode = new TextBox { Location = new Point(24, 42), Width = 450, Font = Theme.BodyFont };

            var lblTitle = new Label { Text = "Policy Title:", Location = new Point(24, 80), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextMuted };
            var txtTitle = new TextBox { Location = new Point(24, 102), Width = 450, Font = Theme.BodyFont };

            var lblCat = new Label { Text = "Category:", Location = new Point(24, 140), AutoSize = true, Font = Theme.CaptionFont, ForeColor = Theme.TextMuted };
            var cmbCat = new ComboBox { Location = new Point(24, 162), Width = 450, DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.BodyFont };
            cmbCat.Items.AddRange(new object[] { "Operations & Service Delivery", "Health & Safety", "Cancellation & Rescheduling", "Billing & Payment Terms", "Property & Liability" });
            cmbCat.SelectedIndex = 0;

            var btnSubmit = new Button { Text = "Create Policy", DialogResult = DialogResult.OK, Location = new Point(340, 240), Width = 134, Height = 38, BackColor = Theme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(230, 240), Width = 100, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249) };

            form.Controls.AddRange(new Control[] { lblCode, txtCode, lblTitle, txtTitle, lblCat, cmbCat, btnSubmit, btnCancel });
            form.AcceptButton = btnSubmit;
            form.CancelButton = btnCancel;

            if (form.ShowDialog(FindForm()) == DialogResult.OK)
            {
                var code = txtCode.Text.Trim().ToUpper();
                var title = txtTitle.Text.Trim();
                var cat = cmbCat.Text.Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Please enter both Section Code and Title.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var newSec = new TermSection
                {
                    Id = code,
                    Title = title,
                    Category = cat,
                    TargetScope = SessionManager.IsSuperAdmin ? "SuperAdmin" : "Tenant",
                    Version = "v1.0",
                    EffectiveDate = DateTime.Today,
                    Status = "Active",
                    Summary = "Custom organizational policy enacted by management.",
                    Content = $"1. PURPOSE\nThis clause governs {title.ToLower()} in compliance with operational standards.\n\n2. ENFORCEMENT\nAll operational personnel and clientele are expected to adhere to this guideline upon execution.",
                    LastModifiedBy = SessionManager.CurrentUser?.Username ?? "Admin",
                    LastModifiedDate = DateTime.Now,
                    IsCustom = true
                };

                _allSections.Add(newSec);
                SaveAllSectionsToFile();
                PopulateCategoryDropdown();
                ApplyFilter();
                SelectSection(newSec);

                ShowToast($"Custom policy '{code}' created!", true);
            }
        }

        private void DeleteCurrentClause()
        {
            if (!_canEdit || _selectedSection == null || !_selectedSection.IsCustom) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to permanently delete custom clause '{_selectedSection.Id}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            _allSections.Remove(_selectedSection);
            SaveAllSectionsToFile();
            PopulateCategoryDropdown();
            ApplyFilter();

            ShowToast("Custom clause deleted.", true);
        }

        // ============================================================
        // Role-Based Permissions
        // ============================================================
        public override void ApplyViewPermissions(string userRole)
        {
            // Super Admin: Can edit SaaS policies
            // Admin: Can edit Tenant customer policies
            // Manager & Sales Staff: Read-only reference mode
            _canEdit = (userRole == Roles.SuperAdmin || userRole == Roles.Admin);

            _btnSave.Visible = _canEdit;
            _btnResetClause.Visible = _canEdit;
            _btnAddClause.Visible = _canEdit;
            _btnDeleteClause.Visible = _canEdit && (_selectedSection?.IsCustom ?? false);

            _txtClauseTitle.ReadOnly = !_canEdit;
            _txtClauseSummary.ReadOnly = !_canEdit;
            _txtClauseContent.ReadOnly = !_canEdit;

            _pnlReadOnlyNotice.Visible = !_canEdit;

            if (!_canEdit)
            {
                _lblRoleBadge.Text = "STAFF REFERENCE MODE";
                _lblRoleBadge.ForeColor = Color.FromArgb(146, 64, 14);
                _lblRoleBadge.BackColor = Color.FromArgb(254, 243, 199);
            }
        }

        // ============================================================
        // Default Industry Standard Terms & Conditions
        // ============================================================
        private static List<TermSection> GetDefaultIndustryTerms()
        {
            return new List<TermSection>
            {
                // ── SUPER ADMIN / SAAS VENDOR TERMS ────────────────────────
                new TermSection
                {
                    Id = "MSA-01",
                    Title = "Master Subscription Agreement (MSA)",
                    Category = "SaaS Licensing & Governance",
                    TargetScope = "SuperAdmin",
                    Version = "v2.5",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    Status = "Active",
                    Summary = "Governs platform software licensing, user seat allocation, tenant data ownership, subscription tier upgrades, and automated billing cycles for CRM subscribers.",
                    Content = @"1. LICENSE GRANT & SUBSCRIPTION SCOPE
The Vendor grants Tenant Company a non-exclusive, non-transferable, subscription-based license to access and utilize the Cleaning Services CRM platform according to the selected Subscription Tier (Standard, Premium, or Enterprise).

2. SEAT ALLOCATION & AUTHORIZED USAGE
Access credentials are provisioned strictly on a per-named-user basis. Concurrent sharing of logins between multiple staff members is prohibited. Tenant Company may provision additional staff seats within their configured tier limits.

3. DATA OWNERSHIP & PORTABILITY
The Tenant retains full, exclusive proprietary ownership of all customer records, service histories, booking schedules, and financial transactions stored in the CRM database. Upon termination, Tenant is entitled to export complete raw datasets in standard CSV/JSON format within 30 calendar days.

4. SUBSCRIPTION FEES & AUTOMATED RENEWAL
Platform licensing fees are billed on an automated monthly or annual recurring cycle. Non-payment after a 7-day grace period results in temporary account suspension under automated dunning protocols.",
                    LastModifiedBy = "Platform Legal Counsel"
                },
                new TermSection
                {
                    Id = "SLA-01",
                    Title = "Platform Service Level Agreement (SLA)",
                    Category = "Service Level & Reliability",
                    TargetScope = "SuperAdmin",
                    Version = "v2.1",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    Status = "Active",
                    Summary = "Guarantees 99.9% platform availability, scheduled maintenance notification windows, P1-P4 incident response times, and financial credit remediation for unscheduled downtime.",
                    Content = @"1. HIGH AVAILABILITY GUARANTEE
The Vendor guarantees 99.9% system availability ('Uptime') during each calendar month, excluding scheduled maintenance windows announced with at least 48 hours advance notice.

2. INCIDENT SEVERITY & RESPONSE TIMES
- Severity 1 (Critical Platform Outage / Database Unreachable): Initial response within 15 minutes; active remediation 24/7 until resolved.
- Severity 2 (Major Module Impairment / Dispatch Offline): Response within 1 hour; resolution target under 4 hours.
- Severity 3 (Minor Operational Glitch / Report Formatting): Response within 4 business hours.
- Severity 4 (General Feature Request / Cosmetic Adjustment): Evaluated in regular product sprint cycles.

3. DOWNTIME REMEDIATION CREDITS
If monthly availability drops below 99.9%, Tenant is eligible for service fee credits:
- 99.0% - 99.89%: 10% credit applied to subsequent invoice.
- 95.0% - 98.99%: 25% credit applied to subsequent invoice.
- Below 95.0%: 50% credit applied to subsequent invoice.",
                    LastModifiedBy = "VP of Infrastructure"
                },
                new TermSection
                {
                    Id = "DPA-01",
                    Title = "Data Processing Addendum (DPA) & Privacy",
                    Category = "Data Privacy & Compliance",
                    TargetScope = "SuperAdmin",
                    Version = "v3.0",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    Status = "Active",
                    Summary = "Enforces strict tenant isolation, AES-256 cryptographic encryption at rest and in transit, multi-tenant row security per CompanyId, and GDPR/CCPA compliance.",
                    Content = @"1. MULTI-TENANT ISOLATION ARCHITECTURE
All database queries and transactions are cryptographically bounded and logically segregated by CompanyId at the Entity Framework and SQL command level. Under no circumstances may tenant cross-boundary data leakage occur.

2. ENCRYPTION PROTOCOLS
All customer PII (Personally Identifiable Information), contact numbers, physical addresses, and financial transaction amounts are transmitted via TLS 1.3 encryption and stored with AES-256 database-level encryption.

3. BACKUP RETENTION & DISASTER RECOVERY
Automated point-in-time database snapshots are executed every 6 hours and replicated to geo-redundant storage. Recovery Point Objective (RPO) is 6 hours; Recovery Time Objective (RTO) is 2 hours.",
                    LastModifiedBy = "Data Protection Officer"
                },

                // ── TENANT CLEANING COMPANY CLIENT POLICIES ────────────────
                new TermSection
                {
                    Id = "CSA-01",
                    Title = "Customer Master Service Agreement",
                    Category = "Service Contracts",
                    TargetScope = "Tenant",
                    Version = "v2.0",
                    EffectiveDate = new DateTime(2026, 2, 1),
                    Status = "Active",
                    Summary = "Standard terms of service for residential and commercial cleaning clients, covering scope of work, property access permissions, utility requirements, and satisfaction guarantees.",
                    Content = @"1. SCOPE OF CLEANING SERVICES
The Company agrees to provide professional cleaning services as itemized in the confirmed Work Order or Service Booking. Any additional tasks outside the approved service scope must be confirmed via a formal Change Order or Supplemental Booking.

2. PROPERTY ACCESS & UTILITIES
Client shall provide company cleaning personnel with unobstructed access to the service premises during the scheduled operational window. Client must ensure operational running water and electrical power are available at the site.

3. QUALITY SATISFACTION GUARANTEE
If the Client is dissatisfied with any aspect of the cleaning service, Client must notify the Company within 24 hours of job completion. The Company will dispatch an inspection crew to re-clean the disputed area at zero additional charge.",
                    LastModifiedBy = "Director of Operations"
                },
                new TermSection
                {
                    Id = "CAN-01",
                    Title = "Cancellation & Rescheduling Policy",
                    Category = "Operations & Cancellation",
                    TargetScope = "Tenant",
                    Version = "v1.8",
                    EffectiveDate = new DateTime(2026, 2, 1),
                    Status = "Active",
                    Summary = "Enforces minimum 24-hour advance notice for appointment cancellations or rescheduling. Outlines late fee schedule and lock-out charges.",
                    Content = @"1. NOTICE REQUIREMENTS
Clients may reschedule or cancel scheduled appointments without penalty by providing written or verbal notice at least 24 hours prior to the scheduled booking start time.

2. LATE CANCELLATION CHARGES
- Cancellations made less than 24 hours before the scheduled window incur a 50% service cancellation fee to compensate dispatched cleaning personnel.
- Same-day cancellations within 3 hours of arrival incur a 75% fee.

3. LOCK-OUT & ACCESS FAILURE POLICY
If cleaning crew arrives at the client premises and cannot gain access within 20 minutes of arrival due to incorrect lockbox codes, un-restrained pets, or client absence, the service will be marked as 'Client Lock-Out' and billed at full rate.",
                    LastModifiedBy = "Head of Scheduling"
                },
                new TermSection
                {
                    Id = "PAY-01",
                    Title = "Billing, Invoicing & Payment Terms",
                    Category = "Billing & Payment Terms",
                    TargetScope = "Tenant",
                    Version = "v2.2",
                    EffectiveDate = new DateTime(2026, 1, 15),
                    Status = "Active",
                    Summary = "Governs residential payment upon completion, Net-15/Net-30 corporate billing accounts, late interest fees (1.5%/month), and disputed invoice handling.",
                    Content = @"1. RESIDENTIAL ACCOUNTS
Payment for residential cleaning services is due upon completion of each service visit. We accept credit cards, debit cards, and authorized bank transfers processed through the CRM payment ledger.

2. COMMERCIAL & CORPORATE BILLING
Commercial accounts with pre-approved credit lines are invoiced on Net-15 or Net-30 terms from the date of invoice issuance.

3. LATE PAYMENTS & RECOVERY
Overdue balances past 30 days accrue interest at the rate of 1.5% per month (or the maximum allowed by law). Accounts overdue past 60 days will have ongoing recurring cleanings paused until settled.",
                    LastModifiedBy = "Chief Financial Officer"
                },
                new TermSection
                {
                    Id = "LIA-01",
                    Title = "Property Damage & Liability Disclaimer",
                    Category = "Liability & Breakage",
                    TargetScope = "Tenant",
                    Version = "v2.0",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    Status = "Active",
                    Summary = "Outlines accidental breakage reporting protocols within 24 hours, comprehensive commercial general liability insurance, and maximum claim limitations.",
                    Content = @"1. REPORTING PROTOCOL
In the rare event of accidental property damage during a service visit, our cleaning personnel are required to immediately photograph and document the item and notify management. Client must report any un-reported damage within 24 hours of job completion.

2. INSURANCE & COVERAGE
The Company maintains active Commercial General Liability insurance and bonded staff coverage. Approved claims will be repaired, replaced, or reimbursed up to fair market depreciated value.

3. EXCLUSIONS & LIMITATIONS
The Company is not liable for:
- Pre-existing structural damage, unstable shelving, or improperly hung wall art.
- Extreme high-value irreplaceable heirlooms or unsecured jewelry exceeding $1,000 unless declared in writing prior to service.
- Indoor pet welfare if pets are left unrestricted on the premises during equipment operation.",
                    LastModifiedBy = "Risk Management Counsel"
                },
                new TermSection
                {
                    Id = "NON-01",
                    Title = "Staff Non-Solicitation Covenant",
                    Category = "Staff Protection",
                    TargetScope = "Tenant",
                    Version = "v1.4",
                    EffectiveDate = new DateTime(2026, 1, 1),
                    Status = "Active",
                    Summary = "Prohibits clients from directly soliciting, contracting, or hiring company cleaning staff outside the CRM platform for 12 months following service termination.",
                    Content = @"1. NON-SOLICITATION RESTRICTIONS
Client acknowledges that the Company invests substantial resources into vetting, background-checking, and training its professional cleaning workforce. Client agrees not to directly solicit, employ, or contract with any company cleaner for private cleaning services outside the CRM platform.

2. LIQUIDATED DAMAGES
In the event of a breach of this non-solicitation covenant, Client agrees to pay the Company a placement and training liquidated damages fee of $2,500 per hired individual.",
                    LastModifiedBy = "HR & Operations Director"
                }
            };
        }
    }
}
