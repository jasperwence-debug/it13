using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using App.Domain.Entities;
using App.Infrastructure;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Modal Dialog for Super Admin to provision new system user accounts.
    /// </summary>
    public class NewUserDialog : Form
    {
        public event Action? UserCreated;

        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private TextBox _txtConfirmPassword = null!;
        private ComboBox _cmbRole = null!;
        private Label _lblStatus = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public NewUserDialog()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "Provision New System User";
            Size = new Size(480, 480);
            MinimumSize = new Size(440, 440);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // ── Header Panel ─────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "Create User Account",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Left,
                Width = 240,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblBadge = new Label
            {
                Text = "Super Admin Only",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(202, 138, 4),
                BackColor = Color.FromArgb(254, 252, 232),
                Dock = DockStyle.Right,
                Width = 130,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 18, 0, 18)
            };
            pnlHeader.Controls.Add(lblBadge);

            var pnlDivTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDivTop);

            // ── Footer Panel ─────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 10, 24, 10)
            };
            Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Left,
                Width = 100
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            _btnSave = new Button
            {
                Text = "✓  Create User",
                Dock = DockStyle.Right,
                Width = 160
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += (s, e) => OnSaveUser();
            pnlFooter.Controls.Add(_btnSave);

            var pnlDivBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDivBottom);

            // ── Form Body ────────────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(24, 16, 24, 16)
            };
            Controls.Add(pnlBody);

            int top = 16;
            int width = 415;

            // Username
            var lblUsername = new Label { Text = "Username *", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark, Location = new Point(0, top), Size = new Size(width, 20) };
            pnlBody.Controls.Add(lblUsername);
            top += 22;
            _txtUsername = new TextBox { Font = new Font("Segoe UI", 9.5F), Location = new Point(0, top), Size = new Size(width, 28) };
            pnlBody.Controls.Add(_txtUsername);
            top += 38;

            // Role
            var lblRole = new Label { Text = "System Role *", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark, Location = new Point(0, top), Size = new Size(width, 20) };
            pnlBody.Controls.Add(lblRole);
            top += 22;
            _cmbRole = new ComboBox { Font = new Font("Segoe UI", 9.5F), DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(0, top), Size = new Size(width, 28) };
            _cmbRole.Items.AddRange(new object[]
            {
                Roles.SalesStaff,
                Roles.Manager,
                Roles.Admin,
                Roles.SuperAdmin
            });
            _cmbRole.SelectedIndex = 0;
            pnlBody.Controls.Add(_cmbRole);
            top += 38;

            // Password
            var lblPassword = new Label { Text = "Password *", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark, Location = new Point(0, top), Size = new Size(width, 20) };
            pnlBody.Controls.Add(lblPassword);
            top += 22;
            _txtPassword = new TextBox { Font = new Font("Segoe UI", 9.5F), UseSystemPasswordChar = true, Location = new Point(0, top), Size = new Size(width, 28) };
            pnlBody.Controls.Add(_txtPassword);
            top += 38;

            // Confirm Password
            var lblConfirm = new Label { Text = "Confirm Password *", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark, Location = new Point(0, top), Size = new Size(width, 20) };
            pnlBody.Controls.Add(lblConfirm);
            top += 22;
            _txtConfirmPassword = new TextBox { Font = new Font("Segoe UI", 9.5F), UseSystemPasswordChar = true, Location = new Point(0, top), Size = new Size(width, 28) };
            pnlBody.Controls.Add(_txtConfirmPassword);
            top += 38;

            // Status message
            _lblStatus = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(0, top),
                Size = new Size(width, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBody.Controls.Add(_lblStatus);
        }

        private void OnSaveUser()
        {
            var username = _txtUsername.Text.Trim();
            var role = _cmbRole.SelectedItem?.ToString() ?? Roles.SalesStaff;
            var password = _txtPassword.Text;
            var confirm = _txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                _lblStatus.Text = "⚠ Username must be at least 3 characters long.";
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                _lblStatus.Text = "⚠ Password must be at least 4 characters long.";
                return;
            }

            if (password != confirm)
            {
                _lblStatus.Text = "⚠ Passwords do not match.";
                return;
            }

            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);

                if (db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
                {
                    _lblStatus.Text = $"⚠ Username '{username}' already exists in the system.";
                    return;
                }

                var newUser = new User
                {
                    Username = username,
                    Role = role,
                    PasswordHash = password
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                UserCreated?.Invoke();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Database Error: {ex.Message}";
            }
        }
    }
}
