using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class UserDetailForm : Form
    {
        private readonly Guid _userId;
        private readonly UserListDto _summary;
        private UserDetailDto? _detail;

        private Label _error = null!;
        private Panel _content = null!;

        public UserDetailForm(UserListDto summary)
        {
            _userId = summary.Id;
            _summary = summary;
            InitializeUI();
            _ = LoadAsync();
        }

        private void InitializeUI()
        {
            Text = $"User — {_summary.Name}";
            ClientSize = new Size(620, 560);
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
                Location = new Point(28, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var header = new Label
            {
                Text = "User Details",
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(46, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            _error = new Label
            {
                Location = new Point(28, 60),
                Size = new Size(560, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);

            _content = new Panel
            {
                Location = new Point(28, 92),
                Size = new Size(560, 400),
                BackColor = Color.Transparent
            };
            Controls.Add(_content);

            var close = new Button
            {
                Text = "Close",
                Location = new Point(28, 508),
                Size = new Size(560, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            close.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(close, 8);
            UiRadiusHelper.AttachHoverFeedback(close, AppTheme.Signal, AppTheme.SignalHover);
            Controls.Add(close);

            CancelButton = close;
        }

        private async Task LoadAsync()
        {
            try
            {
                var client = new UserApiClient();
                _detail = await client.GetAsync(_userId);
                if (_detail == null)
                {
                    _error.Text = "User not found.";
                    return;
                }
                RenderDetail(_detail);
            }
            catch (Exception ex)
            {
                _error.Text = $"Error: {ex.Message}";
            }
        }

        private void RenderDetail(UserDetailDto d)
        {
            _content.Controls.Clear();
            int y = 0;

            // Avatar + name
            var avatar = new Label
            {
                Text = UiRadiusHelper.GetInitials(d.Name),
                Font = new Font("Bahnschrift SemiBold", 22F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(72, 72),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(AppTheme.Signal);
                e.Graphics.FillEllipse(brush, 0, 0, 71, 71);
                TextRenderer.DrawText(e.Graphics, avatar.Text, avatar.Font,
                    new Rectangle(0, 0, 72, 72), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            _content.Controls.Add(avatar);

            var nameLbl = new Label
            {
                Text = d.Name,
                Font = new Font("Bahnschrift SemiBold", 18F),
                ForeColor = AppTheme.Ink,
                Location = new Point(92, 4),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _content.Controls.Add(nameLbl);

            var roleLbl = new Label
            {
                Text = d.Role,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(94, 40),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _content.Controls.Add(roleLbl);

            var statusLbl = new Label
            {
                Text = d.IsActive ? "ACTIVE" : "INACTIVE",
                Font = AppTheme.FontPill,
                ForeColor = d.IsActive ? AppTheme.SuccessText : AppTheme.NeutralText,
                BackColor = d.IsActive ? AppTheme.SuccessBg : AppTheme.NeutralBg,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true,
                Location = new Point(94, 62)
            };
            UiRadiusHelper.ApplyPillShape(statusLbl);
            _content.Controls.Add(statusLbl);

            y = 100;
            AddRow("Email", d.Email, ref y);
            AddRow("Created", d.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy HH:mm"), ref y);
            AddRow("Last login", d.LastLoginAt?.ToLocalTime().ToString("MMM dd, yyyy HH:mm") ?? "Never", ref y);

            y += 12;

            // Stats
            var statsLabel = new Label
            {
                Text = "ACTIVITY SNAPSHOT",
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(0, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _content.Controls.Add(statsLabel);
            y += 24;

            int cardW = 130;
            int cardH = 70;
            int gap = 10;
            AddStatCard("CUSTOMERS", d.AssignedCustomers.ToString(), AppTheme.Signal, new Point(0, y), new Size(cardW, cardH));
            AddStatCard("LEADS", d.AssignedLeads.ToString(), Color.FromArgb(0xC1, 0x7B, 0x12), new Point(cardW + gap, y), new Size(cardW, cardH));
            AddStatCard("ACTIVITIES", d.LoggedActivities.ToString(), AppTheme.SuccessText, new Point(2 * (cardW + gap), y), new Size(cardW, cardH));
            AddStatCard("OPEN F/U", d.OpenFollowUps.ToString(), AppTheme.DangerText, new Point(3 * (cardW + gap), y), new Size(cardW, cardH));
        }

        private void AddRow(string label, string value, ref int y)
        {
            var keyLbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(0, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var valLbl = new Label
            {
                Text = value,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Ink,
                Location = new Point(0, y + 18),
                Size = new Size(560, 22),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            _content.Controls.Add(keyLbl);
            _content.Controls.Add(valLbl);
            y += 50;
        }

        private void AddStatCard(string label, string value, Color accent, Point loc, Size size)
        {
            var card = new Panel
            {
                Location = loc,
                Size = size,
                BackColor = AppTheme.Surface
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = UiRadiusHelper.CreateRoundedPath(
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                using var pen = new Pen(AppTheme.Line, 1);
                e.Graphics.DrawPath(pen, path);
                using var brush = new SolidBrush(accent);
                e.Graphics.FillRectangle(brush, 0, 10, 3, card.Height - 20);
            };

            var lblTitle = new Label
            {
                Text = label,
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                Location = new Point(14, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Bahnschrift SemiBold", 20F),
                ForeColor = AppTheme.Ink,
                Location = new Point(12, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            _content.Controls.Add(card);
        }
    }
}