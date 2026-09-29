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
    /// Modal dialog allowing Managers, Admins, and SuperAdmins to assign or reassign
    /// account ownership for Leads and Customers to a Sales Staff or Manager.
    /// Modeled after CRM reference architecture.
    /// </summary>
    public class AssignOwnerDialog : Form
    {
        public class AssigneeItem
        {
            public int? UserId { get; set; }
            public string? Username { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public override string ToString() => DisplayText;
        }

        private readonly string _recordTitle;
        private readonly int? _currentAssigneeId;
        private readonly string? _currentAssigneeName;

        private ComboBox _cmbAssignee = null!;
        private Label _lblStatus = null!;
        private Button _btnAssign = null!;
        private Button _btnCancel = null!;

        public int? SelectedUserId { get; private set; }
        public string? SelectedUsername { get; private set; }

        public AssignOwnerDialog(string recordTitle, int? currentAssigneeId, string? currentAssigneeName = null)
        {
            _recordTitle = recordTitle;
            _currentAssigneeId = currentAssigneeId;
            _currentAssigneeName = currentAssigneeName;

            BuildUI();
            LoadUsers();
        }

        private void BuildUI()
        {
            Text = "Assign Record Owner";
            Size = new Size(480, 320);
            MinimumSize = new Size(480, 320);
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
                Height = 65,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitle = new Label
            {
                Text = "Assign Owner",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(20, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = _recordTitle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                Location = new Point(20, 34),
                Size = new Size(430, 20),
                AutoEllipsis = true
            };
            pnlHeader.Controls.Add(lblSub);

            var pnlDivTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            // ── Footer Panel ─────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 10, 20, 10)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Left,
                Width = 90
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            _btnAssign = new Button
            {
                Text = "✓  Confirm Assignment",
                Dock = DockStyle.Right,
                Width = 170
            };
            Theme.ApplyPrimaryButtonStyle(_btnAssign);
            _btnAssign.Click += (s, e) => OnAssign();
            pnlFooter.Controls.Add(_btnAssign);

            var pnlDivBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };

            // ── Body Panel ───────────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(24, 20, 24, 10)
            };

            var lblPrompt = new Label
            {
                Text = "SELECT SALES REPRESENTATIVE OR ACCOUNT OWNER:",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Location = new Point(24, 20),
                Size = new Size(430, 18)
            };
            pnlBody.Controls.Add(lblPrompt);

            _cmbAssignee = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(24, 42),
                Size = new Size(415, 30),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextDark
            };
            pnlBody.Controls.Add(_cmbAssignee);

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Theme.Danger,
                Location = new Point(24, 82),
                Size = new Size(415, 40)
            };
            pnlBody.Controls.Add(_lblStatus);

            Controls.Add(pnlBody);
            Controls.Add(pnlDivBottom);
            Controls.Add(pnlFooter);
            Controls.Add(pnlDivTop);
            Controls.Add(pnlHeader);

            AcceptButton = _btnAssign;
            CancelButton = _btnCancel;
        }

        private void LoadUsers()
        {
            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);

                var users = db.Users
                    .Where(u => u.Role == Roles.SalesStaff || u.Role == Roles.Manager || u.Role == Roles.Admin)
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.Username)
                    .ToList();

                _cmbAssignee.Items.Clear();

                // Add unassigned option
                var unassigned = new AssigneeItem
                {
                    UserId = null,
                    Username = null,
                    DisplayText = "— Unassigned —"
                };
                _cmbAssignee.Items.Add(unassigned);

                AssigneeItem? matchToSelect = null;

                foreach (var u in users)
                {
                    var item = new AssigneeItem
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        DisplayText = $"{u.Username}  ({u.Role})"
                    };
                    _cmbAssignee.Items.Add(item);

                    if (_currentAssigneeId.HasValue && u.Id == _currentAssigneeId.Value)
                    {
                        matchToSelect = item;
                    }
                    else if (matchToSelect == null && !string.IsNullOrWhiteSpace(_currentAssigneeName) &&
                             string.Equals(u.Username, _currentAssigneeName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchToSelect = item;
                    }
                }

                if (matchToSelect != null)
                {
                    _cmbAssignee.SelectedItem = matchToSelect;
                }
                else if (_cmbAssignee.Items.Count > 0)
                {
                    _cmbAssignee.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Error loading staff list: {ex.Message}";
            }
        }

        private void OnAssign()
        {
            if (_cmbAssignee.SelectedItem is not AssigneeItem item)
            {
                _lblStatus.Text = "Please select an assignee.";
                return;
            }

            SelectedUserId = item.UserId;
            SelectedUsername = item.Username;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
