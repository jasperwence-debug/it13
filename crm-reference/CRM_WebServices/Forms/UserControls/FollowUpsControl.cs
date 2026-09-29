using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class FollowUpsControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private ComboBox _statusFilter = null!;
        private Button _overdueBtn = null!;
        private Button _addBtn = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;
        private bool _overdueOnly = false;

        public FollowUpsControl()
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
                Text = "Follow-ups",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _subtitle = new Label
            {
                Text = "0 follow-up(s)",
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
                Text = "+  New Follow-up",
                Size = new Size(160, 40),
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
            _addBtn.Click += async (_, _) => await OpenEditor();
            Controls.Add(_addBtn);

            var filterLbl = new Label
            {
                Text = "Status",
                Location = new Point(0, 96),
                Size = new Size(50, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(filterLbl);

            _statusFilter = new ComboBox
            {
                Location = new Point(52, 92),
                Size = new Size(170, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _statusFilter.Items.AddRange(new object[]
            {
                "(All)", "Pending", "InProgress", "Completed", "Cancelled"
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
                Location = new Point(234, 92),
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
            { DataPropertyName = "Title", HeaderText = "TITLE", FillWeight = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "CustomerName", HeaderText = "CUSTOMER", FillWeight = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "LeadName", HeaderText = "LEAD", FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DueDate",
                HeaderText = "DUE",
                FillWeight = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, HH:mm" }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Status", HeaderText = "STATUS", FillWeight = 120 });

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                var f = _grid.Rows[e.RowIndex].DataBoundItem as FollowUpDto;
                if (f == null) return;
                if (f.IsOverdue) e.CellStyle.ForeColor = AppTheme.DangerText;
                else if (f.Status == "Completed") e.CellStyle.ForeColor = AppTheme.SuccessText;
            };

            _grid.CellContentClick += Grid_CellContentClick;

            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No follow-ups yet. Create one with the button above.",
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
                var client = new FollowUpApiClient();

                if (_overdueOnly)
                {
                    var overdue = await client.ListAsync(overdue: true, pageSize: 100);
                    var items = overdue?.Items ?? new List<FollowUpDto>();
                    _grid.DataSource = items;
                    _subtitle.Text = $"{overdue?.Total ?? 0} overdue follow-up(s)";
                    _emptyLabel.Visible = items.Count == 0;
                    _grid.Visible = items.Count > 0;
                    return;
                }

                var status = _statusFilter.SelectedItem?.ToString();
                var statusParam = status == "(All)" ? null : status;

                var resp = await client.ListAsync(status: statusParam, pageSize: 100);
                var list = resp?.Items ?? new List<FollowUpDto>();

                _grid.DataSource = list;
                _subtitle.Text = $"{resp?.Total ?? 0} follow-up(s)";
                _emptyLabel.Visible = list.Count == 0;
                _grid.Visible = list.Count > 0;
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

            if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                e.PaintBackground(e.CellBounds, true);
                string status = e.Value.ToString() ?? "";
                var (bg, fg) = StatusColors(status);
                DrawPill(e.Graphics, e.CellBounds, status.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
        }

        private static (Color bg, Color fg) StatusColors(string status)
        {
            var s = status.ToLowerInvariant();
            return s switch
            {
                "completed" => (AppTheme.SuccessBg, AppTheme.SuccessText),
                "cancelled" => (AppTheme.NeutralBg, AppTheme.NeutralText),
                "inprogress" => (AppTheme.InfoBg, AppTheme.InfoText),
                _ => (AppTheme.WarningBg, AppTheme.WarningText)
            };
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
            if (e.RowIndex < 0) return;
            var f = _grid.Rows[e.RowIndex].DataBoundItem as FollowUpDto;
            if (f == null) return;

            var menu = new ContextMenuStrip
            {
                Font = AppTheme.FontBody,
                BackColor = AppTheme.Surface,
                ShowImageMargin = false,
                Padding = new Padding(4)
            };

            menu.Items.Add("Mark Complete", null, async (_, _) => await ChangeStatus(f, "Completed"));
            menu.Items.Add("Mark In Progress", null, async (_, _) => await ChangeStatus(f, "InProgress"));
            menu.Items.Add("Reopen (Pending)", null, async (_, _) => await ChangeStatus(f, "Pending"));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Cancel", null, async (_, _) => await ChangeStatus(f, "Cancelled"));

            var r = _grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            menu.Show(_grid, r.Left - menu.Width + 60, r.Bottom);
        }

        private async Task ChangeStatus(FollowUpDto f, string newStatus)
        {
            try
            {
                var client = new FollowUpApiClient();
                await client.UpdateStatusAsync(f.Id, newStatus);
                CRM.winforms.Controls.ToastHost.Show(this, $"Marked {newStatus.ToLower()}", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex) { MessageBox.Show(ex.Message, "Error"); }
        }

        private async Task OpenEditor()
        {
            using var dlg = new FollowUpEditorForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                CRM.winforms.Controls.ToastHost.Show(this, "Follow-up created", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }

        /// <summary>
        /// Public entry point used by navigation to auto-enable the Overdue filter.
        /// Called from MainForm when the user clicks the "Overdue" KPI.
        /// </summary>
        public void ActivateOverdueFilter()
        {
            if (_overdueOnly) return;
            _overdueOnly = true;
            UpdateOverdueStyle();
            _ = LoadAsync();
        }
    }
}