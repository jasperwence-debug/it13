using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class CustomersControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private TextBox _search = null!;
        private Button _addBtn = null!;
        private Button _filterAll = null!;
        private Button _filterActive = null!;
        private Button _filterInactive = null!;
        private Button _filterProspect = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;

        private string _filterStatus = "All";

        public CustomersControl()
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
                Text = "Customers",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _subtitle = new Label
            {
                Text = "0 total · 0 active",
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
                Text = "+  New Customer",
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
            _addBtn.Click += async (_, _) => await OpenEditor(null);
            Controls.Add(_addBtn);

            _search = new TextBox
            {
                Location = new Point(0, 92),
                Size = new Size(340, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                PlaceholderText = "🔍   Search name, email, or company…"
            };
            _search.TextChanged += async (_, _) => await LoadAsync();
            Controls.Add(_search);

            _filterAll = MakePill("All", 360);
            _filterActive = MakePill("Active", 418);
            _filterInactive = MakePill("Inactive", 496);
            _filterProspect = MakePill("Prospect", 592);

            _filterAll.Click += (_, _) => SetFilter("All");
            _filterActive.Click += (_, _) => SetFilter("Active");
            _filterInactive.Click += (_, _) => SetFilter("Inactive");
            _filterProspect.Click += (_, _) => SetFilter("Prospect");

            Controls.Add(_filterAll);
            Controls.Add(_filterActive);
            Controls.Add(_filterInactive);
            Controls.Add(_filterProspect);

            _card = new Panel
            {
                Location = new Point(0, 140),
                BackColor = AppTheme.Surface,
                Padding = new Padding(1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            UiRadiusHelper.StyleCard(_card, 12);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            AppTheme.ApplyGridStyle(_grid);
            _grid.RowTemplate.Height = 52;

            // ─── Columns (both Name and DataPropertyName) ───
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullName",
                DataPropertyName = "FullName",
                HeaderText = "NAME",
                FillWeight = 160
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Email",
                DataPropertyName = "Email",
                HeaderText = "EMAIL",
                FillWeight = 200
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Phone",
                DataPropertyName = "Phone",
                HeaderText = "PHONE",
                FillWeight = 130
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Company",
                DataPropertyName = "Company",
                HeaderText = "COMPANY",
                FillWeight = 160
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                DataPropertyName = "Status",
                HeaderText = "STATUS",
                FillWeight = 100
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AssignedUserName",
                DataPropertyName = "AssignedUserName",
                HeaderText = "ASSIGNED",
                FillWeight = 130
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
                if (_grid.Rows[e.RowIndex].DataBoundItem is CustomerDto c)
                    await OpenEditor(c);
            };

            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No customers yet. Create your first with the button above.",
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

        private Button MakePill(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 92),
                Size = new Size(text.Length * 10 + 28, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Trace,
                Font = new Font("Segoe UI Variable Text Semibold", 9F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.ApplyPillShape(btn);
            return btn;
        }

        private void LayoutChildren()
        {
            int w = ClientSize.Width;
            _addBtn.Location = new Point(w - _addBtn.Width - 4, 8);
            _card.Width = w;
            _card.Height = Math.Max(200, ClientSize.Height - _card.Top - 8);

            int px = _search.Right + 14;
            foreach (var pill in new[] { _filterAll, _filterActive, _filterInactive, _filterProspect })
            {
                pill.Location = new Point(px, 92);
                px += pill.Width + 6;
            }
        }

        private void SetFilter(string s)
        {
            _filterStatus = s;
            UpdatePillStyles();
            _ = LoadAsync();
        }

        private void UpdatePillStyles()
        {
            var pills = new (Button btn, string name)[]
            {
                (_filterAll, "All"),
                (_filterActive, "Active"),
                (_filterInactive, "Inactive"),
                (_filterProspect, "Prospect")
            };
            foreach (var (btn, name) in pills)
            {
                bool sel = string.Equals(_filterStatus, name, StringComparison.OrdinalIgnoreCase);
                btn.BackColor = sel ? AppTheme.Signal : AppTheme.Surface;
                btn.ForeColor = sel ? Color.White : AppTheme.Trace;
                btn.Font = new Font("Segoe UI Variable Text Semibold", 9F, sel ? FontStyle.Bold : FontStyle.Regular);
                btn.Invalidate();
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                var client = new CustomerApiClient();
                var resp = await client.ListAsync(_search.Text.Trim());
                var items = resp?.Items ?? new List<CustomerDto>();

                if (!string.Equals(_filterStatus, "All", StringComparison.OrdinalIgnoreCase))
                    items = items.Where(c => string.Equals(c.Status, _filterStatus, StringComparison.OrdinalIgnoreCase)).ToList();

                _grid.DataSource = items;
                _subtitle.Text = $"{resp?.Total ?? 0} total · {items.Count(i => i.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))} active";
                _emptyLabel.Visible = items.Count == 0;
                _grid.Visible = items.Count > 0;

                // Tooltips — in its own try/catch so a failure here never
                // blanks out the grid
                TryApplyTooltips();
            }
            catch (Exception ex)
            {
                _emptyLabel.Visible = true;
                _emptyLabel.Text = $"⚠ {ex.Message}";
                _grid.Visible = false;
            }
        }

        private void TryApplyTooltips()
        {
            try
            {
                if (!_grid.Columns.Contains("FullName")) return;

                _grid.ShowCellToolTips = true;

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.DataBoundItem is CustomerDto c)
                    {
                        var tooltip = c.ChurnRisk switch
                        {
                            "High" => $"⚠ High churn risk — {c.DaysSinceLastActivity} days since last activity",
                            "Medium" => $"◐ Medium churn risk — {c.DaysSinceLastActivity} days since last activity",
                            _ => $"✓ Active — {c.DaysSinceLastActivity} days since last activity"
                        };
                        row.Cells["FullName"].ToolTipText = tooltip;
                    }
                }
            }
            catch
            {
                // Silent — tooltips are non-essential
            }
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics is null) return;

            string col = _grid.Columns[e.ColumnIndex].Name;
            string val = e.Value?.ToString() ?? "";

            if (col == "Status" && e.Value != null)
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = StatusColors(val);
                DrawPill(e.Graphics, e.CellBounds, val.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
            else if (col == "FullName" && e.Value != null)
            {
                var row = _grid.Rows[e.RowIndex].DataBoundItem as CustomerDto;
                if (row == null) return;   // leave default rendering untouched

                e.PaintBackground(e.CellBounds, true);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Avatar
                int avatarSize = 28;
                int avatarX = e.CellBounds.X + 8;
                int avatarY = e.CellBounds.Y + (e.CellBounds.Height - avatarSize) / 2;
                var avatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

                using (var brush = new SolidBrush(AppTheme.Signal))
                    g.FillEllipse(brush, avatarRect);

                string initials = UiRadiusHelper.GetInitials(row.FullName);
                using (var font = new Font("Segoe UI Variable Text Semibold", 8.5F))
                {
                    TextRenderer.DrawText(g, initials, font, avatarRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Churn dot
                int dotSize = 8;
                int nameX = avatarX + avatarSize + 10;
                var (dotColor, _) = ChurnColor(row.ChurnRisk, row.DaysSinceLastActivity);

                int nameWidth;
                using (var nameFont = new Font("Segoe UI Variable Text Semibold", 9.5F))
                {
                    var measured = TextRenderer.MeasureText(row.FullName, nameFont);
                    nameWidth = Math.Min(measured.Width + 4,
                        e.CellBounds.Right - nameX - dotSize - 12);

                    var nameRect = new Rectangle(nameX, e.CellBounds.Y, nameWidth, e.CellBounds.Height);
                    TextRenderer.DrawText(g, row.FullName, nameFont, nameRect, AppTheme.Ink,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }

                int dotX = Math.Min(nameX + nameWidth + 4, e.CellBounds.Right - dotSize - 6);
                int dotY = e.CellBounds.Y + (e.CellBounds.Height - dotSize) / 2;
                using (var dotBrush = new SolidBrush(dotColor))
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);

                e.Handled = true;
            }
        }

        private static (Color dot, string text) ChurnColor(string churnRisk, int days)
        {
            var r = churnRisk?.ToLowerInvariant() ?? "low";
            return r switch
            {
                "high" => (AppTheme.DangerText, $"{days} days since last activity"),
                "medium" => (Color.FromArgb(0xC1, 0x7B, 0x12), $"{days} days since last activity"),
                _ => (AppTheme.SuccessText, $"{days} days since last activity")
            };
        }

        private static (Color bg, Color fg) StatusColors(string status)
        {
            var s = status.ToLowerInvariant();
            if (s.Contains("active")) return (AppTheme.SuccessBg, AppTheme.SuccessText);
            if (s.Contains("inactive")) return (AppTheme.NeutralBg, AppTheme.NeutralText);
            if (s.Contains("prospect")) return (AppTheme.WarningBg, AppTheme.WarningText);
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

            var customer = _grid.Rows[e.RowIndex].DataBoundItem as CustomerDto;
            if (customer == null) return;

            ShowRowMenu(customer, e.RowIndex);
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;

            var customer = _grid.Rows[e.RowIndex].DataBoundItem as CustomerDto;
            if (customer == null) return;

            ShowRowMenu(customer, e.RowIndex);
        }

        private void ShowRowMenu(CustomerDto customer, int rowIndex)
        {
            var menu = new ContextMenuStrip
            {
                Font = AppTheme.FontBody,
                BackColor = AppTheme.Surface,
                ShowImageMargin = false,
                Padding = new Padding(4)
            };

            menu.Items.Add("View Details", null, async (_, _) => await ViewCustomer(customer));
            menu.Items.Add("Edit Customer", null, async (_, _) => await OpenEditor(customer));

            if (RoleHelper.CanAssign)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Assign Owner…", null, async (_, _) => await AssignCustomer(customer));
            }

            var r = _grid.GetCellDisplayRectangle(_grid.Columns["Actions"].Index, rowIndex, true);
            menu.Show(_grid, r.Left - menu.Width + 60, r.Bottom);
        }

        private async Task ViewCustomer(CustomerDto customer)
        {
            using var dlg = new CustomerEditorForm(customer, readOnly: true);
            dlg.ShowDialog(this);
        }

        private async Task OpenEditor(CustomerDto? existing)
        {
            using var dlg = new CustomerEditorForm(existing, readOnly: false);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var msg = existing == null ? "Customer created" : "Customer updated";
                CRM.winforms.Controls.ToastHost.Show(this, msg, CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
        }

        private async Task AssignCustomer(CustomerDto customer)
        {
            using var dlg = new AssignOwnerDialog(
                $"{customer.FullName} · {customer.Company}",
                customer.AssignedUserId);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (dlg.SelectedUserId == null) return;

            try
            {
                var client = new CustomerApiClient();
                await client.AssignAsync(customer.Id, dlg.SelectedUserId.Value);
                CRM.winforms.Controls.ToastHost.Show(this, "Customer reassigned", CRM.winforms.Controls.ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Assign Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}