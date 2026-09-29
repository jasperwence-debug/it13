using CRM.winforms.Helpers;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class ProfileControl : UserControl
    {
        public ProfileControl()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Paper;
            Build();
        }

        private void Build()
        {
            // Signature dot + title
            var dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 13F),
                ForeColor = AppTheme.Signal,
                Location = new Point(0, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var title = new Label
            {
                Text = "Profile",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = "Your account details and current session.",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(22, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            // Main card
            var card = new Panel
            {
                Location = new Point(0, 100),
                Size = new Size(660, 520),
                BackColor = AppTheme.Surface,
                Padding = new Padding(36)
            };
            UiRadiusHelper.StyleCard(card, 12);

            // ─── 96px avatar with hairline ring ─────────────────
            int avatarSize = 96;
            var avatarRing = new Panel
            {
                Location = new Point(36, 36),
                Size = new Size(avatarSize, avatarSize),
                BackColor = Color.Transparent
            };
            avatarRing.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fill = new SolidBrush(AppTheme.Signal);
                e.Graphics.FillEllipse(fill, 4, 4, avatarSize - 8, avatarSize - 8);
                using var ring = new Pen(Color.FromArgb(30, 0x14, 0x47, 0xE6), 2);
                e.Graphics.DrawEllipse(ring, 1, 1, avatarSize - 3, avatarSize - 3);
            };
            card.Controls.Add(avatarRing);

            var initials = new Label
            {
                Text = UiRadiusHelper.GetInitials(SessionManager.Current.Name),
                Font = new Font("Bahnschrift SemiBold", 30F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(avatarSize, avatarSize),
                Location = new Point(36, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(initials);
            initials.BringToFront();

            // Name + role
            var nameLbl = new Label
            {
                Text = SessionManager.Current.Name,
                Font = new Font("Bahnschrift SemiBold", 20F),
                ForeColor = AppTheme.Ink,
                Location = new Point(160, 44),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(nameLbl);

            var roleLbl = new Label
            {
                Text = SessionManager.Current.Role,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(162, 82),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(roleLbl);

            // Divider
            var divider = new Panel
            {
                Location = new Point(36, 164),
                Size = new Size(588, 1),
                BackColor = AppTheme.Line
            };
            card.Controls.Add(divider);

            // KV rows
            int y = 194;
            AddRow(card, "Email address", SessionManager.Current.Email, ref y);
            AddRow(card, "Role", SessionManager.Current.Role, ref y);
            AddRow(card, "Company ID (tenant)", SessionManager.Current.TenantId.ToString(), ref y);
            AddRow(card, "User ID", SessionManager.Current.UserId.ToString(), ref y);
            AddRow(card, "Session expires", SessionManager.Current.ExpiresAt.ToLocalTime().ToString("MMM dd, yyyy HH:mm"), ref y);

            // Logout button
            var logoutBtn = new Button
            {
                Text = "Logout",
                Location = new Point(36, y + 20),
                Size = new Size(160, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.DangerText,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            logoutBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(logoutBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(logoutBtn, AppTheme.DangerText, Color.FromArgb(0x87, 0x1C, 0x1C));
            logoutBtn.Click += (_, _) =>
            {
                var confirm = MessageBox.Show("Log out?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                SessionManager.Current.Clear();
                FindForm()?.Close();
            };
            card.Controls.Add(logoutBtn);

            Controls.Add(card);
        }

        private void AddRow(Panel parent, string label, string value, ref int y)
        {
            var lblKey = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(36, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblVal = new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Ink,
                Location = new Point(36, y + 20),
                AutoSize = false,
                Size = new Size(588, 22),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblKey);
            parent.Controls.Add(lblVal);
            y += 52;
        }
    }
}