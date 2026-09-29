using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class LeadsControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private TextBox _search = null!;
        private ComboBox _statusFilter = null!;
        private Button _addBtn = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;

        public LeadsControl()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Paper;
            Build();
            _ = LoadAsync();
        }

        private void Build()
        {
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
                Text = "Leads",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _subtitle = new Label
            {
                Text = "0 total",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(22, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_title);
            Controls.Add(_subtitle);

            _addBtn = new Button
            {
                Text = "+  New Lead",
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _addBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_addBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_addBtn, AppTheme.Signal, AppTheme.SignalHover);
            _addBtn.Click += async (_, _) => await OpenEditor(null);
            Controls.Add(_addBtn);

            _search = new TextBox
            {
                Location = new Point(0, 92),
                Size = new Size(320, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                PlaceholderText = "🔍   Search leads…"
            };
            _search.TextChanged += async (_, _) => await LoadAsync();
            Controls.Add(_search);

            var filterLbl = new Label
            {
                Text = "Status",
                Location = new Point(340, 96),
                Size = new Size(50, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(filterLbl);

            _statusFilter = new ComboBox
            {
                Location = new Point(390, 92),
                Size = new Size(160, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _statusFilter.Items.AddRange(new object[]
            {
                "(All)", "New", "Contacted", "Qualified", "ProposalSent",
                "Negotiation", "Won", "Lost"
            });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += async (_, _) => await LoadAsync();
            Controls.Add(_statusFilter);

            _card = new Panel
            {
                Location = new Point(0, 140),
                BackColor = AppTheme.Surface,
                Padding = new Padding(1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            UiRadiusHelper.StyleCard(_card, 12);

            _grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false };
            AppTheme.ApplyGridStyle(_grid);
            _grid.RowTemplate.Height = 52;

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Name", HeaderText = "NAME", FillWeight = 160 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Email", HeaderText = "EMAIL", FillWeight = 200 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Status", HeaderText = "STATUS", FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Priority", HeaderText = "PRIORITY", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExpectedValue",
                HeaderText = "VALUE",
                FillWeight = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C0" }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "AssignedUserName", HeaderText = "ASSIGNED", FillWeight = 130 });

            UiGridHelper.AddActionsColumn(_grid, 56);

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellContentClick += Grid_CellContentClick;
            _grid.CellMouseDown += Grid_CellMouseDown;

            _grid.CellMouseMove += (_, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                    _grid.Columns[e.ColumnIndex].Name == "Actions")
                    _grid.Cursor = Cursors.Hand;
                else
                    _grid.Cursor = Cursors.Default;
            };

            _grid.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is LeadDto l)
                    await OpenEditor(l);
            };

            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No leads yet. Create your first with the button above.",
                Font = new Font("Segoe UI Variable Text", 10.5F, FontStyle.Italic),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                BackColor = Color.Transparent
            };
            _card.Controls.Add(_emptyLabel);

            Controls.Add(_card);
            Resize += (_, _) => LayoutChildren();
        }

        private void LayoutChildren()
        {
            int w = ClientSize.Width;
            _addBtn.Location = new Point(w - _addBtn.Width - 4, 8);
            _card.Width = w;
            _card.Height = Math.Max(200, ClientSize.Height - _card.Top - 8);
        }

        private async Task LoadAsync()
        {
            try
            {
                var client = new LeadApiClient();
                var status = _statusFilter.SelectedItem?.ToString();
                var statusParam = status == "(All)" ? null : status;

                var resp = await client.ListAsync(
                    search: _search.Text.Trim(),
                    status: statusParam,
                    pageSize: 100);

                var items = resp?.Items ?? new List<LeadDto>();
                _grid.DataSource = items;
                _subtitle.Text = $"{resp?.Total ?? 0} lead(s)";
                _emptyLabel.Visible = items.Count == 0;
                _grid.Visible = items.Count > 0;
            }
            catch (Exception ex)
            {
                _emptyLabel.Visible = true;
                _emptyLabel.Text = $"⚠ {ex.Message}";
                _grid.Visible = false;
            }
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;

            string colName = _grid.Columns[e.ColumnIndex].Name;
            string val = e.Value?.ToString() ?? "";

            if (colName == "Status")
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = StatusColors(val);
                DrawPill(e.Graphics, e.CellBounds, val.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
            else if (colName == "Priority")
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = PriorityColors(val);
                DrawPill(e.Graphics, e.CellBounds, val.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
        }

        private static (Color bg, Color fg) StatusColors(string status)
        {
            var s = status.ToLowerInvariant();
            if (s.Contains("won")) return (AppTheme.SuccessBg, AppTheme.SuccessText);
            if (s.Contains("lost")) return (AppTheme.DangerBg, AppTheme.DangerText);
            if (s.Contains("new")) return (AppTheme.InfoBg, AppTheme.InfoText);
            if (s.Contains("qualified") || s.Contains("proposal") || s.Contains("negotiation"))
                return (AppTheme.WarningBg, AppTheme.WarningText);
            return (AppTheme.NeutralBg, AppTheme.NeutralText);
        }

        private static (Color bg, Color fg) PriorityColors(string priority)
        {
            var p = priority.ToLowerInvariant();
            if (p.Contains("high")) return (AppTheme.DangerBg, AppTheme.DangerText);
            if (p.Contains("medium")) return (AppTheme.WarningBg, AppTheme.WarningText);
            return (AppTheme.InfoBg, AppTheme.InfoText);
        }

        private void DrawPill(Graphics g, Rectangle bounds, string text, Color bg, Color fg)
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

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "Actions") return;

            var lead = _grid.Rows[e.RowIndex].DataBoundItem as LeadDto;
            if (lead == null) return;

            ShowRowMenu(lead, e.RowIndex);
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;

            var lead = _grid.Rows[e.RowIndex].DataBoundItem as LeadDto;
            if (lead == null) return;

            ShowRowMenu(lead, e.RowIndex);
        }

        private void ShowRowMenu(LeadDto lead, int rowIndex)
        {
            var menu = new ContextMenuStrip
            {
                Font = AppTheme.FontBody,
                BackColor = AppTheme.Surface,
                ShowImageMargin = false,
                Padding = new Padding(4)
            };

            menu.Items.Add("View / Edit", null, async (_, _) => await OpenEditor(lead));
            menu.Items.Add("Change Status", null, async (_, _) => await ChangeStatus(lead));

            if (string.Equals(lead.Status, "Won", StringComparison.OrdinalIgnoreCase))
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Convert to Customer", null, async (_, _) => await Convert(lead));
            }

            if (RoleHelper.CanAssign)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Assign Owner…", null, async (_, _) => await AssignLead(lead));
            }

            var r = _grid.GetCellDisplayRectangle(_grid.Columns["Actions"].Index, rowIndex, true);
            menu.Show(_grid, r.Left - menu.Width + 60, r.Bottom);
        }

        private async Task OpenEditor(LeadDto? existing)
        {
            using var dlg = new LeadEditorForm(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var msg = existing == null ? "Lead created" : "Lead updated";
                CRM.winforms.Controls.ToastHost.Show(this, msg, CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }

        private async Task ChangeStatus(LeadDto lead)
        {
            using var dlg = new LeadStatusForm(lead);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                CRM.winforms.Controls.ToastHost.Show(this, "Status updated", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }

        private async Task Convert(LeadDto lead)
        {
            if (lead.Status != "Won")
            {
                MessageBox.Show("Only leads with status 'Won' can be converted.",
                    "Cannot Convert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show($"Convert '{lead.Name}' to a customer?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                var client = new LeadApiClient();
                var result = await client.ConvertAsync(lead.Id);
                MessageBox.Show($"Created customer: {result?.CustomerName}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CRM.winforms.Controls.ToastHost.Show(this, "Lead converted to customer", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex) { MessageBox.Show(ex.Message, "Error"); }
        }

        private async Task AssignLead(LeadDto lead)
        {
            using var dlg = new AssignOwnerDialog(
                $"{lead.Name} · {lead.Email}",
                lead.AssignedUserId);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (dlg.SelectedUserId == null) return;

            try
            {
                var client = new LeadApiClient();
                await client.AssignAsync(lead.Id, dlg.SelectedUserId.Value);
                CRM.winforms.Controls.ToastHost.Show(this, "Lead reassigned", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Assign Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}