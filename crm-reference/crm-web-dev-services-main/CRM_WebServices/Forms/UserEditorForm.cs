using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class UserEditorForm : Form
    {
        private readonly UserListDto? _existing;
        private readonly bool _readOnly;

        private TextBox _name = null!;
        private TextBox _email = null!;
        private TextBox _password = null!;
        private TextBox _confirm = null!;
        private ComboBox _role = null!;
        private Label _error = null!;

        public UserEditorForm(UserListDto? existing, bool readOnly = false)
        {
            _existing = existing;
            _readOnly = readOnly;
            InitializeUI();
        }

        private void InitializeUI()
        {
            bool isView = _readOnly;
            bool isEdit = !_readOnly && _existing != null;
            bool isCreate = _existing == null && !_readOnly;

            Text = isView ? "View User" : (isEdit ? "Edit User" : "New User");
            ClientSize = new Size(540, isCreate ? 560 : 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBody;

            // Header
            var dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppTheme.Signal,
                Location = new Point(28, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var header = new Label
            {
                Text = isView ? "User Details" : (isEdit ? "Edit User" : "New User"),
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(46, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var sub = new Label
            {
                Text = isView ? "Read-only view."
                     : (isEdit ? "Update the user below."
                              : "Create a new user in your agency."),
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(48, 52),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            var div = new Panel
            {
                Location = new Point(28, 84),
                Size = new Size(484, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            int y = 104;
            _name = AddField("Name *", ref y, _existing?.Name);
            _email = AddField("Email *", ref y, _existing?.Email);

            if (isCreate)
            {
                _password = AddField("Password *", ref y, "", isPassword: true);
                _confirm = AddField("Confirm Password *", ref y, "", isPassword: true);
            }

            _role = AddCombo("Role *", ref y,
                new object[] { "SalesStaff", "Manager", "Admin", "SuperAdmin" },
                _existing?.Role ?? "SalesStaff");

            // Only SuperAdmin can pick SuperAdmin
            if (!SessionManager.Current.IsSuperAdmin)
            {
                _role.Items.Remove("SuperAdmin");
            }

            _error = new Label
            {
                Location = new Point(28, y),
                Size = new Size(484, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);
            y += 30;

            var save = new Button
            {
                Text = "Save",
                Location = new Point(28, y),
                Size = new Size(232, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(save, 8);
            UiRadiusHelper.AttachHoverFeedback(save, AppTheme.Signal, AppTheme.SignalHover);
            save.Click += async (_, _) => await SaveAsync();

            var cancel = new Button
            {
                Text = isView ? "Close" : "Cancel",
                Location = new Point(280, y),
                Size = new Size(232, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            cancel.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(cancel, 8);
            UiRadiusHelper.AttachHoverFeedback(cancel, AppTheme.Surface, Color.FromArgb(0xF0, 0xF3, 0xF9));
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            if (isView)
            {
                save.Visible = false;
                cancel.Location = new Point(28, y);
                cancel.Size = new Size(484, 42);
                cancel.BackColor = AppTheme.Signal;
                cancel.ForeColor = Color.White;
                cancel.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(cancel, 8);
                UiRadiusHelper.AttachHoverFeedback(cancel, AppTheme.Signal, AppTheme.SignalHover);
            }

            Controls.Add(save);
            Controls.Add(cancel);

            if (isView)
            {
                foreach (Control c in Controls)
                {
                    if (c is TextBox tb)
                    {
                        tb.ReadOnly = true;
                        tb.BackColor = Color.FromArgb(0xF7, 0xF8, 0xFB);
                    }
                    else if (c is ComboBox cb)
                    {
                        cb.Enabled = false;
                        cb.BackColor = Color.FromArgb(0xF7, 0xF8, 0xFB);
                    }
                }
            }

            int maxY = 0;
            foreach (Control c in Controls)
                maxY = Math.Max(maxY, c.Bottom);
            ClientSize = new Size(ClientSize.Width, maxY + 32);

            if (!isView) AcceptButton = save;
            CancelButton = cancel;
        }

        private TextBox AddField(string label, ref int y, string? value, bool isPassword = false)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(484, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var tb = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(484, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Text = value ?? "",
                UseSystemPasswordChar = isPassword
            };
            Controls.Add(lbl);
            Controls.Add(tb);
            y += 72;
            return tb;
        }

        private ComboBox AddCombo(string label, ref int y, object[] items, string selected)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(484, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var cb = new ComboBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(484, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            cb.Items.AddRange(items);
            cb.SelectedItem = selected;
            Controls.Add(lbl);
            Controls.Add(cb);
            y += 72;
            return cb;
        }

        private async Task SaveAsync()
        {
            _error.Text = "";

            if (string.IsNullOrWhiteSpace(_name.Text) ||
                string.IsNullOrWhiteSpace(_email.Text))
            {
                _error.Text = "Name and email are required.";
                return;
            }

            if (_existing == null)
            {
                if (string.IsNullOrWhiteSpace(_password.Text) || _password.Text.Length < 6)
                {
                    _error.Text = "Password must be at least 6 characters.";
                    return;
                }
                if (_password.Text != _confirm.Text)
                {
                    _error.Text = "Passwords do not match.";
                    return;
                }
            }

            try
            {
                var client = new UserApiClient();

                if (_existing == null)
                {
                    await client.CreateAsync(new CreateUserRequest
                    {
                        Name = _name.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Password = _password.Text,
                        Role = _role.SelectedItem?.ToString() ?? "SalesStaff"
                    });
                }
                else
                {
                    await client.UpdateAsync(_existing.Id, new UpdateUserRequest
                    {
                        Name = _name.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Role = _role.SelectedItem?.ToString() ?? "SalesStaff"
                    });
                }

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}