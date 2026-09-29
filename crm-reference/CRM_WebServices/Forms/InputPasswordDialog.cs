using CRM.winforms.Helpers;

namespace CRM.winforms.Forms
{
    /// <summary>Small dialog to input a password.</summary>
    public class InputPasswordDialog : Form
    {
        public string Password { get; private set; } = "";

        private TextBox _password = null!;
        private TextBox _confirm = null!;
        private Label _error = null!;

        public InputPasswordDialog(string title)
        {
            Text = title;
            ClientSize = new Size(440, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBody;

            var dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppTheme.Signal,
                Location = new Point(24, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var header = new Label
            {
                Text = title,
                Font = new Font("Bahnschrift SemiBold", 14F),
                ForeColor = AppTheme.Ink,
                Location = new Point(42, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var div = new Panel
            {
                Location = new Point(24, 70),
                Size = new Size(392, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            AddPasswordField("New password *", 90, out _password);
            AddPasswordField("Confirm password *", 146, out _confirm);

            _error = new Label
            {
                Location = new Point(24, 196),
                Size = new Size(392, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);

            var save = new Button
            {
                Text = "Save",
                Location = new Point(24, 218),
                Size = new Size(190, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(save, 8);
            UiRadiusHelper.AttachHoverFeedback(save, AppTheme.Signal, AppTheme.SignalHover);
            save.Click += (_, _) =>
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
                Password = _password.Text;
                DialogResult = DialogResult.OK;
            };

            var cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(226, 218),
                Size = new Size(190, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            cancel.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(cancel, 8);
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            Controls.Add(save);
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private void AddPasswordField(string label, int y, out TextBox field)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(24, y),
                Size = new Size(392, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);

            field = new TextBox
            {
                Location = new Point(24, y + 22),
                Size = new Size(392, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                UseSystemPasswordChar = true
            };
            Controls.Add(field);
        }
    }
}