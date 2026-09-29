using System.ComponentModel;
using CRM.winforms.Controls;
using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class RetentionControl : UserControl
    {
        // ─── Header ──────────────────────────────────────────
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;

        // ─── Summary cards ───────────────────────────────────
        private Panel _kpiRow = null!;
        private Panel _cardAtRisk = null!;
        private Panel _cardContacted = null!;
        private Panel _cardRecovered = null!;

        // ─── Filters row ─────────────────────────────────────
        private ComboBox _thresholdCombo = null!;
        private CheckBox _onlyUnassigned = null!;
        private Button _refreshBtn = null!;

        // ─── Tabs ────────────────────────────────────────────
        private Panel _tabBar = null!;
        private Button _tabAtRisk = null!;
        private Button _tabRecovered = null!;
        private string _activeTab = "AtRisk";

        // ─── Grids ───────────────────────────────────────────
        private Panel _gridHost = null!;
        private DataGridView _atRiskGrid = null!;
        private DataGridView _recoveredGrid = null!;
        private Label _emptyLabel = null!;

        // ─── Action bar ──────────────────────────────────────
        private Panel _actionBar = null!;
        private Label _selectionLabel = null!;
        private Button _createWinBackBtn = null!;
        private Button _bulkAssignBtn = null!;

        // ─── State ───────────────────────────────────────────
        private RetentionSummaryDto? _summary;
        private List<AtRiskActionCustomerDto> _atRiskData = new();
        private List<RecoveredActionCustomerDto> _recoveredData = new();
        private int _thresholdDays = 60;

        public RetentionControl()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Paper;
            Build();
            _ = LoadAsync();
        }

        // ═══════════════════════════════════════════════════════
        // BUILD
        // ═══════════════════════════════════════════════════════
        private void Build()
        {
            // ─── Header ────────────────────────────────────────
            _dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 13F),
                ForeColor = AppTheme.Signal,
                Location = new Point(0, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_dot);

            _title = new Label
            {
                Text = "Retention Action Center",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_title);

            _subtitle = new Label
            {
                Text = "Re-engage at-risk customers before they churn.",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(22, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_subtitle);

            // ─── Filters ────────────────────────────────────────
            var thresholdLbl = new Label
            {
                Text = "Inactivity threshold",
                Location = new Point(0, 92),
                Size = new Size(140, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(thresholdLbl);

            _thresholdCombo = new ComboBox
            {
                Location = new Point(142, 88),
                Size = new Size(120, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            _thresholdCombo.Items.AddRange(new object[]
            {
                "30 days", "60 days", "90 days"
            });
            _thresholdCombo.SelectedIndex = 1;
            _thresholdCombo.SelectedIndexChanged += async (_, _) =>
            {
                _thresholdDays = _thresholdCombo.SelectedIndex switch
                {
                    0 => 30,
                    2 => 90,
                    _ => 60
                };
                await LoadAsync();
            };
            Controls.Add(_thresholdCombo);

            _onlyUnassigned = new CheckBox
            {
                Text = "Only unassigned",
                Location = new Point(280, 92),
                Size = new Size(140, 24),
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                BackColor = Color.Transparent
            };
            _onlyUnassigned.CheckedChanged += async (_, _) => await LoadAsync();
            Controls.Add(_onlyUnassigned);

            _refreshBtn = new Button
            {
                Text = "Refresh",
                Size = new Size(96, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Trace,
                Font = new Font("Segoe UI Variable Text Semibold", 9.5F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _refreshBtn.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(_refreshBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_refreshBtn, AppTheme.Surface, Color.FromArgb(0xF0, 0xF3, 0xF9));
            _refreshBtn.Click += async (_, _) => await LoadAsync();
            Controls.Add(_refreshBtn);

            // ─── Summary row (3 KPI cards) ──────────────────────
            _kpiRow = new Panel
            {
                Location = new Point(0, 140),
                Height = 110,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_kpiRow);

            _cardAtRisk = MakeStatCard("AT-RISK CUSTOMERS", "0",
                Color.FromArgb(0xA3, 0x24, 0x24), "⚠");
            _cardContacted = MakeStatCard("CONTACTED", "0",
                Color.FromArgb(0xC1, 0x7B, 0x12), "📩");
            _cardRecovered = MakeStatCard("RECOVERED", "0",
                AppTheme.SuccessText, "✓");

            _kpiRow.Controls.Add(_cardAtRisk);
            _kpiRow.Controls.Add(_cardContacted);
            _kpiRow.Controls.Add(_cardRecovered);

            // ─── Tabs ───────────────────────────────────────────
            _tabBar = new Panel
            {
                Location = new Point(0, 268),
                Height = 42,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_tabBar);

            _tabAtRisk = MakeTab("At-Risk", 0, true);
            _tabRecovered = MakeTab("Recovered", 110, false);
            _tabAtRisk.Click += async (_, _) =>
            {
                _activeTab = "AtRisk";
                UpdateTabStyles();
                await LoadAsync();
            };
            _tabRecovered.Click += async (_, _) =>
            {
                _activeTab = "Recovered";
                UpdateTabStyles();
                await LoadAsync();
            };
            _tabBar.Controls.Add(_tabAtRisk);
            _tabBar.Controls.Add(_tabRecovered);

            // ─── Grid host ──────────────────────────────────────
            _gridHost = new Panel
            {
                Location = new Point(0, 320),
                BackColor = AppTheme.Surface,
                Padding = new Padding(1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            UiRadiusHelper.StyleCard(_gridHost, 12);
            Controls.Add(_gridHost);

            _atRiskGrid = BuildAtRiskGrid();
            _recoveredGrid = BuildRecoveredGrid();
            _recoveredGrid.Visible = false;

            _gridHost.Controls.Add(_atRiskGrid);
            _gridHost.Controls.Add(_recoveredGrid);

            _emptyLabel = new Label
            {
                Text = "No customers match the current filters.",
                Font = new Font("Segoe UI Variable Text", 10.5F, FontStyle.Italic),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                BackColor = Color.Transparent
            };
            _gridHost.Controls.Add(_emptyLabel);

            // ─── Action bar ─────────────────────────────────────
            _actionBar = new Panel
            {
                Height = 56,
                BackColor = AppTheme.Surface,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(16, 0, 16, 0)
            };
            UiRadiusHelper.StyleCard(_actionBar, 12);

            _selectionLabel = new Label
            {
                Text = "0 selected",
                Location = new Point(16, 18),
                Size = new Size(200, 20),
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                BackColor = Color.Transparent
            };
            _actionBar.Controls.Add(_selectionLabel);

            _createWinBackBtn = new Button
            {
                Text = "Create Win-back Follow-ups",
                Size = new Size(230, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false
            };
            _createWinBackBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_createWinBackBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_createWinBackBtn, AppTheme.Signal, AppTheme.SignalHover);
            _createWinBackBtn.Click += async (_, _) => await CreateWinBacksAsync();
            _actionBar.Controls.Add(_createWinBackBtn);

            _bulkAssignBtn = new Button
            {
                Text = "Bulk Assign",
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Enabled = false
            };
            _bulkAssignBtn.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(_bulkAssignBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_bulkAssignBtn, AppTheme.Surface, Color.FromArgb(0xF0, 0xF3, 0xF9));
            _bulkAssignBtn.Click += async (_, _) => await BulkAssignAsync();
            _actionBar.Controls.Add(_bulkAssignBtn);

            Controls.Add(_actionBar);

            // ─── Layout & events ────────────────────────────────
            Resize += (_, _) => LayoutChildren();
            LayoutChildren();
            UpdateTabStyles();
        }

        private Panel MakeStatCard(string title, string initialValue, Color accent, string icon)
        {
            var card = new Panel { BackColor = AppTheme.Surface };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = UiRadiusHelper.CreateRoundedPath(
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                using var pen = new Pen(AppTheme.Line, 1);
                g.DrawPath(pen, path);
                using var brush = new SolidBrush(accent);
                g.FillRectangle(brush, 0, 12, 3, card.Height - 24);
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = AppTheme.FontKpiLabel,
                ForeColor = AppTheme.Trace,
                Location = new Point(20, 18),
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "title"
            };
            card.Controls.Add(titleLbl);

            var valueLbl = new Label
            {
                Text = initialValue,
                Font = AppTheme.FontKpiNumber,
                ForeColor = AppTheme.Ink,
                Location = new Point(16, 42),
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "value"
            };
            card.Controls.Add(valueLbl);

            var iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 14F),
                ForeColor = accent,
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Name = "icon"
            };
            card.Controls.Add(iconLbl);

            card.Resize += (_, _) =>
            {
                iconLbl.Location = new Point(
                    card.Width - iconLbl.Width - 20, 20);
            };

            return card;
        }

        private Button MakeTab(string text, int x, bool active)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 4),
                Size = new Size(100, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? AppTheme.Signal : AppTheme.Surface,
                ForeColor = active ? Color.White : AppTheme.Trace,
                Font = new Font("Segoe UI Variable Text Semibold", 9.5F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(btn, 8);
            return btn;
        }

        private DataGridView BuildAtRiskGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            AppTheme.ApplyGridStyle(grid);

            // ⚠️ ApplyGridStyle sets grid.ReadOnly = true. We need it OFF for the
            // checkbox column to be clickable. We re-enable read-only per-column below.
            grid.ReadOnly = false;

            grid.RowTemplate.Height = 52;

            // Checkbox column — the ONLY writable column
            var checkCol = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "",
                Width = 44,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = false,
                DataPropertyName = "Selected",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = AppTheme.Surface,
                    ForeColor = AppTheme.Signal,
                    SelectionBackColor = AppTheme.InfoBg,
                    SelectionForeColor = AppTheme.Signal
                }
            };
            grid.Columns.Add(checkCol);

            // ─── All other columns: READ-ONLY ──────────────────────
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "CUSTOMER",
                FillWeight = 150,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Company",
                HeaderText = "COMPANY",
                FillWeight = 130,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Email",
                HeaderText = "EMAIL",
                FillWeight = 180,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DaysSinceLastActivity",
                HeaderText = "DAYS INACTIVE",
                FillWeight = 100,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AssignedUserName",
                HeaderText = "ASSIGNED",
                FillWeight = 120,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "HasOpenWinBack",
                HeaderText = "WIN-BACK",
                FillWeight = 100,
                ReadOnly = true
            });

            // Commit checkbox toggles immediately so CellValueChanged fires
            grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (grid.Columns[e.ColumnIndex].Name != "Selected") return;

                var row = grid.Rows[e.RowIndex].DataBoundItem as AtRiskActionCustomerDto;
                if (row == null) return;

                row.Selected = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is bool b && b;
                UpdateSelectionState();
            };

            grid.CellPainting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics is null) return;

                string col = grid.Columns[e.ColumnIndex].Name;

                if (col == "HasOpenWinBack" && e.Value is bool hasWinBack)
                {
                    e.PaintBackground(e.CellBounds, true);
                    var (bg, fg) = hasWinBack
                        ? (AppTheme.WarningBg, AppTheme.WarningText)
                        : (AppTheme.NeutralBg, AppTheme.NeutralText);
                    DrawPill(e.Graphics, e.CellBounds, hasWinBack ? "PENDING" : "—", bg, fg);
                    e.Handled = true;
                }
                else if (col == "DaysSinceLastActivity" && e.Value is int days)
                {
                    e.PaintBackground(e.CellBounds, true);
                    var (bg, fg) = days >= 90
                        ? (AppTheme.DangerBg, AppTheme.DangerText)
                        : days >= 60
                            ? (Color.FromArgb(0xFF, 0xED, 0xD5), Color.FromArgb(0x9A, 0x34, 0x12))
                            : (AppTheme.WarningBg, AppTheme.WarningText);
                    DrawPill(e.Graphics, e.CellBounds, $"{days} days", bg, fg);
                    e.Handled = true;
                }
            };

            return grid;
        }

        private DataGridView BuildRecoveredGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            AppTheme.ApplyGridStyle(grid);
            grid.RowTemplate.Height = 52;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Name", HeaderText = "CUSTOMER", FillWeight = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Company", HeaderText = "COMPANY", FillWeight = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Email", HeaderText = "EMAIL", FillWeight = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WinBackCreatedAt",
                HeaderText = "WIN-BACK SENT",
                FillWeight = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, yyyy" }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FirstActivityAfterWinBack",
                HeaderText = "FIRST RESPONSE",
                FillWeight = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, yyyy" }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DaysToRecover",
                HeaderText = "DAYS TO RECOVER",
                FillWeight = 110
            });

            return grid;
        }

        private void LayoutChildren()
        {
            int w = ClientSize.Width;

            _refreshBtn.Location = new Point(w - _refreshBtn.Width - 4, 88);

            int cardW = (w - 24) / 3;
            int cardH = 110;
            int gap = 12;

            _cardAtRisk.Location = new Point(0, 0);
            _cardAtRisk.Size = new Size(cardW, cardH);

            _cardContacted.Location = new Point(cardW + gap, 0);
            _cardContacted.Size = new Size(cardW, cardH);

            _cardRecovered.Location = new Point(2 * (cardW + gap), 0);
            _cardRecovered.Size = new Size(cardW, cardH);

            _kpiRow.Width = w;
            _tabBar.Width = w;

            int actionBarHeight = 56;
            int bottomMargin = 8;
            int gridBottom = ClientSize.Height - actionBarHeight - bottomMargin - 8;
            int gridHeight = Math.Max(200, gridBottom - _gridHost.Top);

            _gridHost.Width = w;
            _gridHost.Height = gridHeight;

            _actionBar.Location = new Point(0, _gridHost.Bottom + 8);
            _actionBar.Width = w;

            int rightEdge = _actionBar.Width - 16;

            _createWinBackBtn.Location = new Point(
                rightEdge - _createWinBackBtn.Width, 8);
            rightEdge -= _createWinBackBtn.Width + 8;

            _bulkAssignBtn.Location = new Point(
                rightEdge - _bulkAssignBtn.Width, 8);
        }

        private void UpdateTabStyles()
        {
            bool atRiskActive = _activeTab == "AtRisk";
            _tabAtRisk.BackColor = atRiskActive ? AppTheme.Signal : AppTheme.Surface;
            _tabAtRisk.ForeColor = atRiskActive ? Color.White : AppTheme.Trace;
            _tabAtRisk.Invalidate();

            _tabRecovered.BackColor = !atRiskActive ? AppTheme.Signal : AppTheme.Surface;
            _tabRecovered.ForeColor = !atRiskActive ? Color.White : AppTheme.Trace;
            _tabRecovered.Invalidate();

            _atRiskGrid.Visible = atRiskActive;
            _recoveredGrid.Visible = !atRiskActive;

            _actionBar.Visible = atRiskActive;
        }

        private void UpdateSelectionState()
        {
            int count = _atRiskData.Count(c => c.Selected);
            _selectionLabel.Text = $"{count} selected";

            _createWinBackBtn.Enabled = count > 0;
            _bulkAssignBtn.Enabled = count > 0 && RoleHelper.CanAssign;

            _createWinBackBtn.BackColor = count > 0 ? AppTheme.Signal : Color.FromArgb(0xB0, 0xB8, 0xC6);
            _createWinBackBtn.Invalidate();
            _bulkAssignBtn.Invalidate();
        }

        // ═══════════════════════════════════════════════════════
        // DATA
        // ═══════════════════════════════════════════════════════
        private async Task LoadAsync()
        {
            try
            {
                var client = new RetentionApiClient();

                _summary = await client.GetSummaryAsync(_thresholdDays);
                if (_summary != null)
                {
                    SetStatCardValue(_cardAtRisk, _summary.AtRiskCount.ToString());
                    SetStatCardValue(_cardContacted, _summary.ContactedCount.ToString());
                    SetStatCardValue(_cardRecovered, _summary.RecoveredCount.ToString());
                }

                if (_activeTab == "AtRisk")
                {
                    _atRiskData = await client.GetAtRiskAsync(
                        _thresholdDays, _onlyUnassigned.Checked) ?? new();

                    _atRiskGrid.DataSource = new BindingList<AtRiskActionCustomerDto>(_atRiskData);
                    _emptyLabel.Visible = _atRiskData.Count == 0;
                    _atRiskGrid.Visible = _atRiskData.Count > 0;
                    _selectionLabel.Text = "0 selected";
                    UpdateSelectionState();
                }
                else
                {
                    _recoveredData = await client.GetRecoveredAsync() ?? new();
                    _recoveredGrid.DataSource = _recoveredData;
                    _emptyLabel.Visible = _recoveredData.Count == 0;
                    _recoveredGrid.Visible = _recoveredData.Count > 0;
                }
            }
            catch (Exception ex)
            {
                _emptyLabel.Visible = true;
                _emptyLabel.Text = $"⚠ {ex.Message}";
                _atRiskGrid.Visible = false;
                _recoveredGrid.Visible = false;
            }
        }

        private void SetStatCardValue(Panel card, string value)
        {
            var lbl = card.Controls["value"] as Label;
            if (lbl != null) lbl.Text = value;
        }

        // ═══════════════════════════════════════════════════════
        // ACTIONS
        // ═══════════════════════════════════════════════════════
        private List<Guid> GetSelectedCustomerIds()
            => _atRiskData.Where(c => c.Selected).Select(c => c.CustomerId).ToList();

        private async Task CreateWinBacksAsync()
        {
            var ids = GetSelectedCustomerIds();
            if (ids.Count == 0) return;

            var confirm = MessageBox.Show(
                $"Create {ids.Count} win-back follow-up(s)?\n\n" +
                "Each will be assigned to the customer's existing owner " +
                "(or to you if unassigned).\n" +
                "Due date: 7 days from now.",
                "Confirm Win-back",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var client = new RetentionApiClient();
                var result = await client.CreateWinBacksAsync(ids);

                if (result != null)
                {
                    var msg = result.SkippedExisting > 0
                        ? $"Created {result.Created}. Skipped {result.SkippedExisting} (already have open win-back)."
                        : $"Created {result.Created} win-back follow-up(s).";

                    ToastHost.Show(this, msg, ToastKind.Success);
                }

                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Win-back Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task BulkAssignAsync()
        {
            var ids = GetSelectedCustomerIds();
            if (ids.Count == 0) return;

            using var dlg = new AssignOwnerDialog(
                $"{ids.Count} at-risk customer(s)",
                null);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (dlg.SelectedUserId == null) return;

            try
            {
                var client = new RetentionApiClient();
                await client.BulkAssignAsync(ids, dlg.SelectedUserId.Value);

                ToastHost.Show(this, $"Assigned {ids.Count} customer(s)", ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Assign Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void DrawPill(Graphics g, Rectangle bounds, string text, Color bg, Color fg)
        {
            using var font = AppTheme.FontPill;
            var sz = TextRenderer.MeasureText(text, font);
            int w = sz.Width + 20;
            int h = 22;
            int x = bounds.X + (bounds.Width - w) / 2;
            int y = bounds.Y + (bounds.Height - h) / 2;
            var rect = new Rectangle(x, y, w, h);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = UiRadiusHelper.CreateRoundedPath(rect, h / 2);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
            TextRenderer.DrawText(g, text, font, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}