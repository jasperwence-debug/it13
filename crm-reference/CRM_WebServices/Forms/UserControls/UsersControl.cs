using CRM.winforms.Controls;
using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class UsersControl : UserControl
    {
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private TextBox _search = null!;
        private ComboBox _roleFilter = null!;
        private CheckBox _includeInactive = null!;
        private Button _addBtn = null!;
        private Panel _card = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;

        public UsersControl()
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
                Text = "Users",
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
                Text = "+  New User",
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
                Size = new Size(300, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.TextPrimary,
                PlaceholderText = "🔍   Search name or email…"
            };
            _search.TextChanged += async (_, _) => await LoadAsync();
            Controls.Add(_search);

            var roleLbl = new Label
            {
                Text = "Role",
                Location = new Point(320, 96),
                Size = new Size(36, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(roleLbl);

            _roleFilter = new ComboBox
            {
                Location = new Point(358, 92),
                Size = new Size(160, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface
            };
            _roleFilter.Items.AddRange(new object[]
            {
                "(All Roles)", "SuperAdmin", "Admin", "Manager", "SalesStaff"
            });
            _roleFilter.SelectedIndex = 0;
            _roleFilter.SelectedIndexChanged += async (_, _) => await LoadAsync();
            Controls.Add(_roleFilter);

            _includeInactive = new CheckBox
            {
                Text = "Include inactive",
                Location = new Point(536, 96),
                Size = new Size(140, 24),
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                BackColor = Color.Transparent
            };
            _includeInactive.CheckedChanged += async (_, _) => await LoadAsync();
            Controls.Add(_includeInactive);

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
            { DataPropertyName = "Name", HeaderText = "NAME", FillWeight = 170 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Email", HeaderText = "EMAIL", FillWeight = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Role", HeaderText = "ROLE", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "IsActive", HeaderText = "STATUS", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LastLoginAt",
                HeaderText = "LAST LOGIN",
                FillWeight = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, HH:mm" }
            });

            UiGridHelper.AddActionsColumn(_grid, 56);

            _grid.CellPainting += Grid_CellPainting;
            _grid.CellContentClick += Grid_CellContentClick;
            _grid.CellMouseDown += Grid_CellMouseDown;

            _grid.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is UserListDto u)
                    await ViewUser(u);
            };

            _card.Controls.Add(_grid);

            _emptyLabel = new Label
            {
                Text = "No users found.",
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
                var client = new UserApiClient();
                var role = _roleFilter.SelectedItem?.ToString();
                var roleParam = role == "(All Roles)" ? null : role;

                var rows = await client.ListAsync(
                    _search.Text.Trim(),
                    roleParam,
                    _includeInactive.Checked) ?? new List<UserListDto>();

                _grid.DataSource = rows;
                _subtitle.Text = $"{rows.Count} user(s)";
                _emptyLabel.Visible = rows.Count == 0;
                _grid.Visible = rows.Count > 0;
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

            if (colName == "Role")
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = RoleColors(val);
                DrawPill(e.Graphics, e.CellBounds, val.ToUpperInvariant(), bg, fg);
                e.Handled = true;
            }
            else if (colName == "IsActive" && e.Value is bool b)
            {
                e.PaintBackground(e.CellBounds, true);
                var (bg, fg) = b
                    ? (AppTheme.SuccessBg, AppTheme.SuccessText)
                    : (AppTheme.NeutralBg, AppTheme.NeutralText);
                DrawPill(e.Graphics, e.CellBounds, b ? "ACTIVE" : "INACTIVE", bg, fg);
                e.Handled = true;
            }
        }

        private static (Color bg, Color fg) RoleColors(string role)
        {
            var r = role.ToLowerInvariant();
            if (r == "superadmin") return (Color.FromArgb(0xF3, 0xE8, 0xFF), Color.FromArgb(0x6B, 0x21, 0xA8));
            if (r == "admin") return (AppTheme.DangerBg, AppTheme.DangerText);
            if (r == "manager") return (AppTheme.WarningBg, AppTheme.WarningText);
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

            var user = _grid.Rows[e.RowIndex].DataBoundItem as UserListDto;
            if (user == null) return;

            ShowRowMenu(user, e.RowIndex);
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;

            var user = _grid.Rows[e.RowIndex].DataBoundItem as UserListDto;
            if (user == null) return;

            ShowRowMenu(user, e.RowIndex);
        }

        private void ShowRowMenu(UserListDto user, int rowIndex)
        {
            var menu = new ContextMenuStrip
            {
                Font = AppTheme.FontBody,
                BackColor = AppTheme.Surface,
                ShowImageMargin = false,
                Padding = new Padding(4)
            };

            menu.Items.Add("View Details", null, async (_, _) => await ViewUser(user));
            menu.Items.Add("Edit User", null, async (_, _) => await OpenEditor(user));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reset Password…", null, async (_, _) => await ResetPassword(user));

            if (user.IsActive)
            {
                menu.Items.Add(new ToolStripSeparator());
                var deactivate = new ToolStripMenuItem("Deactivate User");
                deactivate.ForeColor = AppTheme.DangerText;
                deactivate.Click += async (_, _) => await DeactivateUser(user);
                menu.Items.Add(deactivate);
            }
            else
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Reactivate User", null, async (_, _) => await ReactivateUser(user));
            }

            var r = _grid.GetCellDisplayRectangle(_grid.Columns["Actions"].Index, rowIndex, true);
            menu.Show(_grid, r.Left - menu.Width + 60, r.Bottom);
        }

        private async Task ViewUser(UserListDto user)
        {
            using var dlg = new UserDetailForm(user);
            dlg.ShowDialog(this);
        }

        private async Task OpenEditor(UserListDto? existing)
        {
            using var dlg = new UserEditorForm(existing, readOnly: false);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var msg = existing == null ? "User created" : "User updated";
                ToastHost.Show(this, msg, ToastKind.Success);
                await LoadAsync();
            }
        }

        private async Task ResetPassword(UserListDto user)
        {
            using var dlg = new InputPasswordDialog($"Reset password for {user.Name}");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (string.IsNullOrWhiteSpace(dlg.Password)) return;

            try
            {
                var client = new UserApiClient();
                await client.ChangePasswordAsync(user.Id, dlg.Password);
                ToastHost.Show(this, "Password reset", ToastKind.Success);
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task DeactivateUser(UserListDto user)
        {
            var confirm = MessageBox.Show(
                $"Deactivate {user.Name}?\n\nThey will no longer be able to log in.",
                "Confirm Deactivate",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var client = new UserApiClient();
                await client.DeactivateAsync(user.Id);
                ToastHost.Show(this, "User deactivated", ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task ReactivateUser(UserListDto user)
        {
            try
            {
                var client = new UserApiClient();
                await client.ReactivateAsync(user.Id);
                ToastHost.Show(this, "User reactivated", ToastKind.Success);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}   