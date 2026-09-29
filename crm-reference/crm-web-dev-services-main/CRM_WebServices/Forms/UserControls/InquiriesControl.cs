using CRM.winforms.Controls;
using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class InquiriesControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private TextBox _search = null!;
        private ComboBox _typeFilter = null!;
        private ComboBox _statusFilter = null!;
        private Button _overdueBtn = null!;
        private Button _addBtn = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;

        private bool _overdueOnly = false;

        public InquiriesControl()
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
                Text = "Inquiries, Complaints && Feedback",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _subtitle = new Label
            {
                Text = "0 total · 0 open · 0 overdue",
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
                Text = "+  New Inquiry",
                Size = new Size(150, 40),
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
                Size = new Size(260, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                PlaceholderText = "🔍   Search subject or customer…"
            };
            _search.TextChanged += async (_, _) => await LoadAsync();
            Controls.Add(_search);

            var typeLbl = new Label
            {
                Text = "Type",
                Location = new Point(280, 96),
                Size = new Size(36, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(typeLbl);

            _typeFilter = new ComboBox
            {
                Location = new Point(318, 92),
                Size = new Size(130, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _typeFilter.Items.AddRange(new object[]
            {
                "(All Types)", "Inquiry", "Complaint", "Feedback"
            });
            _typeFilter.SelectedIndex = 0;
            _typeFilter.SelectedIndexChanged += async (_, _) => await LoadAsync();
            Controls.Add(_typeFilter);

            var statusLbl = new Label
            {
                Text = "Status",
                Location = new Point(458, 96),
                Size = new Size(50, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(statusLbl);

            _statusFilter = new ComboBox
            {
                Location = new Point(510, 92),
                Size = new Size(130, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _statusFilter.Items.AddRange(new object[]
            {
                "(All Status)", "Open", "InProgress", "Resolved", "Closed"
            });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += async (_, _) =>
            {
                _overdueOnly = false;
                UpdateOverdueStyle();
                await LoadAsync();
            };
            Controls.Add(_statusFilter);

            _overdueBtn = new Button
            {
                Text = "Overdue Only",
                Location = new Point(650, 92),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.DangerText,
                Font = new Font("Segoe UI Variable Text Semibold", 9F),
                Cursor = Cursors.Hand
            };
            _overdueBtn.FlatAppearance.BorderColor = AppTheme.DangerText;
            UiRadiusHelper.StyleButton(_overdueBtn, 6);
            _overdueBtn.Click += async (_, _) =>
            {
                _overdueOnly = !_overdueOnly;
                UpdateOverdueStyle();
                await LoadAsync();
            };
            Controls.Add(_overdueBtn);

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
            { DataPropertyName = "CustomerName", HeaderText = "CUSTOMER", FillWeight = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Type", HeaderText = "TYPE", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Subject", HeaderText = "SUBJECT", FillWeight = 240 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Priority", HeaderText = "PRIORITY", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Status", HeaderText = "STATUS", FillWeight = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "AssignedUserName", HeaderText = "ASSIGNED", FillWeight = 130 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAt",
                HeaderText = "CREATED",
                FillWeight = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, HH:mm" }
            });

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
                if (_grid.Rows[e.RowIndex].DataBoundItem is CustomerInquiryDto i)
                    await OpenEditor(i);
            };

            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var item = _grid.Rows[e.RowIndex].DataBoundItem as CustomerInquiryDto;
                if (item == null) return;
                if (item.IsOverdue) e.CellStyle.ForeColor = AppTheme.DangerText;
                else if (item.Status == "Resolved") e.CellStyle.ForeColor = AppTheme.SuccessText;
            };

            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No inquiries yet. Create your first with the button above.",
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

        private void UpdateOverdueStyle()
        {
            if (_overdueOnly)
            {
                _overdueBtn.BackColor = AppTheme.DangerText;
                _overdueBtn.ForeColor = Color.White;
            }
            else
            {
                _overdueBtn.BackColor = AppTheme.Surface;
                _overdueBtn.ForeColor = AppTheme.DangerText;
            }
            _overdueBtn.Invalidate();
        }

        private async Task LoadAsync()
        {
            try
            {
                var client = new CustomerInquiryApiClient();

                var typeSel = _typeFilter.SelectedItem?.ToString();
                var typeParam = typeSel == "(All Types)" ? null : typeSel;

                var statusSel = _statusFilter.SelectedItem?.ToString();
                var statusParam = statusSel == "(All Status)" ? null : statusSel;

                var resp = await client.ListAsync(
                    type: typeParam,
                    status: statusParam,
                    pageSize: 100);

                var items = resp?.Items ?? new List<CustomerInquiryDto>();

                var search = _search.Text.Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    items = items.Where(i =>
                        (i.Subject ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (i.CustomerName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (i.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (_overdueOnly)
                    items = items.Where(i => i.IsOverdue).ToList();

                _grid.DataSource = items;

                int total = resp?.Total ?? 0;
                int open = items.Count(i => i.Status == "Open" || i.Status == "InProgress");
                int overdue = items.Count(i => i.IsOverdue);

                _subtitle.Text = $"{total} total · {open} open · {overdue} overdue";
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

            if (colName == "Type")
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = TypeColors(val);
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
            else if (colName == "Status")
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = StatusColors(val);
                DrawPill(e.Graphics, e.CellBounds, val.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
        }

        private static (Color bg, Color fg) TypeColors(string t)
        {
            var s = t.ToLowerInvariant();
            if (s == "complaint") return (AppTheme.DangerBg, AppTheme.DangerText);
            if (s == "feedback") return (AppTheme.SuccessBg, AppTheme.SuccessText);
            return (AppTheme.InfoBg, AppTheme.InfoText);
        }

        private static (Color bg, Color fg) PriorityColors(string p)
        {
            var s = p.ToLowerInvariant();
            if (s == "urgent") return (AppTheme.DangerBg, AppTheme.DangerText);
            if (s == "high") return (Color.FromArgb(0xFF, 0xED, 0xD5), Color.FromArgb(0x9A, 0x34, 0x12));
            if (s == "medium") return (AppTheme.WarningBg, AppTheme.WarningText);
            return (AppTheme.NeutralBg, AppTheme.NeutralText);
        }

        private static (Color bg, Color fg) StatusColors(string s)
        {
            var t = s.ToLowerInvariant();
            if (t == "resolved") return (AppTheme.SuccessBg, AppTheme.SuccessText);
            if (t == "closed") return (AppTheme.NeutralBg, AppTheme.NeutralText);
            if (t == "inprogress") return (AppTheme.WarningBg, AppTheme.WarningText);
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

            var item = _grid.Rows[e.RowIndex].DataBoundItem as CustomerInquiryDto;
            if (item == null) return;

            ShowRowMenu(item, e.RowIndex);
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;

            var item = _grid.Rows[e.RowIndex].DataBoundItem as CustomerInquiryDto;
            if (item == null) return;

            ShowRowMenu(item, e.RowIndex);
        }

        private void ShowRowMenu(CustomerInquiryDto item, int rowIndex)
        {
            var menu = new ContextMenuStrip
            {
                Font = AppTheme.FontBody,
                BackColor = AppTheme.Surface,
                ShowImageMargin = false,
                Padding = new Padding(4)
            };

            menu.Items.Add("View Details", null, async (_, _) => await ViewInquiry(item));

            bool canEdit = item.Status != "Resolved" && item.Status != "Closed";
            if (canEdit)
                menu.Items.Add("Edit Inquiry", null, async (_, _) => await OpenEditor(item));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Change Status", null, async (_, _) => await ChangeStatus(item));

            var r = _grid.GetCellDisplayRectangle(_grid.Columns["Actions"].Index, rowIndex, true);
            menu.Show(_grid, r.Left - menu.Width + 60, r.Bottom);
        }

        private async Task ViewInquiry(CustomerInquiryDto item)
        {
            using var dlg = new InquiryEditorForm(item, readOnly: true);
            dlg.ShowDialog(this);
        }

        private async Task OpenEditor(CustomerInquiryDto? existing)
        {
            using var dlg = new InquiryEditorForm(existing, readOnly: false);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var msg = existing == null ? "Inquiry created" : "Inquiry updated";
                CRM.winforms.Controls.ToastHost.Show(this, msg, CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }

        private async Task ChangeStatus(CustomerInquiryDto item)
        {
            using var dlg = new InquiryStatusForm(item);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                CRM.winforms.Controls.ToastHost.Show(this, "Status updated", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }
    }
}