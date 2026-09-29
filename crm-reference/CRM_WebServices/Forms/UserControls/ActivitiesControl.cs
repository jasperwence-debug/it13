using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class ActivitiesControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private ComboBox _typeFilter = null!;
        private Button _addBtn = null!;
        private Button _refreshBtn = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;

        public ActivitiesControl()
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
                Text = "Activities",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _subtitle = new Label
            {
                Text = "0 activity(ies)",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(22, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_title);
            Controls.Add(_subtitle);

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

            _addBtn = new Button
            {
                Text = "+  Log Activity",
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
            _addBtn.Click += async (_, _) => await OpenEditor();
            Controls.Add(_addBtn);

            var filterLbl = new Label
            {
                Text = "Type",
                Location = new Point(0, 96),
                Size = new Size(36, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(filterLbl);

            _typeFilter = new ComboBox
            {
                Location = new Point(38, 92),
                Size = new Size(180, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _typeFilter.Items.AddRange(new object[]
            {
                "(All)", "Call", "Email", "Meeting", "Note", "Task"
            });
            _typeFilter.SelectedIndex = 0;
            _typeFilter.SelectedIndexChanged += async (_, _) => await LoadAsync();
            Controls.Add(_typeFilter);

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
            { DataPropertyName = "ActivityType", HeaderText = "TYPE", FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Description", HeaderText = "DESCRIPTION", FillWeight = 280 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "CustomerName", HeaderText = "CUSTOMER", FillWeight = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "LeadName", HeaderText = "LEAD", FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "UserName", HeaderText = "LOGGED BY", FillWeight = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ActivityDate",
                HeaderText = "WHEN",
                FillWeight = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, HH:mm" }
            });

            _grid.CellPainting += Grid_CellPainting;
            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No activities yet. Log one with the button above.",
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
            _refreshBtn.Location = new Point(w - _addBtn.Width - _refreshBtn.Width - 14, 8);
            _card.Width = w;
            _card.Height = Math.Max(200, ClientSize.Height - _card.Top - 8);
        }

        private async Task LoadAsync()
        {
            try
            {
                var client = new ActivityApiClient();
                var type = _typeFilter.SelectedItem?.ToString();
                var typeParam = type == "(All)" ? null : type;

                var resp = await client.ListAsync(type: typeParam, pageSize: 100);
                var items = resp?.Items ?? new List<ActivityDto>();

                _grid.DataSource = items;
                _subtitle.Text = $"{resp?.Total ?? 0} activity(ies)";
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

            if (_grid.Columns[e.ColumnIndex].Name == "ActivityType" && e.Value != null)
            {
                e.PaintBackground(e.CellBounds, true);
                string type = e.Value.ToString() ?? "";
                var (bg, fg) = TypeColors(type);
                DrawPill(e.Graphics, e.CellBounds, type.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
        }

        private static (Color bg, Color fg) TypeColors(string type)
        {
            var t = type.ToLowerInvariant();
            return t switch
            {
                "call" => (AppTheme.InfoBg, AppTheme.InfoText),
                "email" => (AppTheme.WarningBg, AppTheme.WarningText),
                "meeting" => (AppTheme.SuccessBg, AppTheme.SuccessText),
                "task" => (Color.FromArgb(0xF3, 0xE8, 0xFF), Color.FromArgb(0x6B, 0x21, 0xA8)),
                _ => (AppTheme.NeutralBg, AppTheme.NeutralText)
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

        private async Task OpenEditor()
        {
            using var dlg = new ActivityEditorForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                CRM.winforms.Controls.ToastHost.Show(this, "Activity logged", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }
    }
}