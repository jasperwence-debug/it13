using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    /// <summary>
    /// Generic "assign owner" dialog for Customers and Leads.
    /// Manager+ can pick a user; SalesStaff can't open this.
    /// </summary>
    public class AssignOwnerDialog : Form
    {
        private readonly string _recordTitle;
        private readonly Guid? _currentAssigneeId;

        private ComboBox _userCombo = null!;
        private Label _error = null!;

        /// <summary>Set by the dialog when the caller picks a user.</summary>
        public Guid? SelectedUserId { get; private set; }

        public AssignOwnerDialog(string recordTitle, Guid? currentAssigneeId)
        {
            _recordTitle = recordTitle;
            _currentAssigneeId = currentAssigneeId;
            InitializeUI();
            _ = LoadUsersAsync();
        }

        private void InitializeUI()
        {
            Text = "Assign Owner";
            ClientSize = new Size(520, 320);
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
                Location = new Point(24, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var title = new Label
            {
                Text = "Assign Owner",
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(42, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = _recordTitle,
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(44, 48),
                Size = new Size(456, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            var div = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(472, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            var lbl = new Label
            {
                Text = "Assign to *",
                Location = new Point(24, 104),
                Size = new Size(472, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);

            _userCombo = new ComboBox
            {
                Location = new Point(24, 126),
                Size = new Size(472, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            Controls.Add(_userCombo);

            _error = new Label
            {
                Location = new Point(24, 168),
                Size = new Size(472, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);

            var save = new Button
            {
                Text = "Assign",
                Location = new Point(24, 210),
                Size = new Size(230, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(save, 8);
            UiRadiusHelper.AttachHoverFeedback(save, AppTheme.Signal, AppTheme.SignalHover);
            save.Click += (_, _) => OnAssign();

            var cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(266, 210),
                Size = new Size(230, 42),
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

            Controls.Add(save);
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var client = new UserApiClient();
                var users = await client.ListAssignableAsync() ?? new List<UserSummary>();

                _userCombo.Items.Clear();
                foreach (var u in users)
                    _userCombo.Items.Add(u);

                if (_userCombo.Items.Count == 0)
                {
                    _error.Text = "No assignable users found.";
                    return;
                }

                // Preselect current assignee if any
                if (_currentAssigneeId.HasValue)
                {
                    var match = users.FirstOrDefault(u => u.Id == _currentAssigneeId.Value);
                    if (match != null)
                        _userCombo.SelectedItem = match;
                    else
                        _userCombo.SelectedIndex = 0;
                }
                else
                {
                    _userCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _error.Text = $"Failed to load users: {ex.Message}";
            }
        }

        private void OnAssign()
        {
            _error.Text = "";

            if (_userCombo.SelectedItem is not UserSummary u)
            {
                _error.Text = "Please pick a user.";
                return;
            }

            SelectedUserId = u.Id;
            DialogResult = DialogResult.OK;
        }
    }
}