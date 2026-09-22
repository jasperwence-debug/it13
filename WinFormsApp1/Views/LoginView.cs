using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using App.Domain.Entities;
using App.Infrastructure;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Standalone Form for user authentication.
    /// Manages the single-instance sign-in flow with zero UI duplication.
    /// Sets DialogResult.OK and closes upon successful authentication.
    /// </summary>
    public class LoginView : Form
    {
        private Panel _pnlCenter = null!;
        private Panel _pnlCard = null!;
        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private Button _btnLogin = null!;
        private Label _lblStatus = null!;

        public event EventHandler? LoginSuccess;
        public Button LoginButton => _btnLogin;

        // WS_EX_COMPOSITED — prevents ghosting, flickering, and repaint artifacts
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        public LoginView()
        {
            // Desktop Form Configuration
            Text = "Cleaning Services CRM - Sign In";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(800, 600);
            ClientSize = new Size(960, 680);
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(15, 23, 42); // #0F172A Deep Navy
            DoubleBuffered = true;

            InitializeFigmaLoginUI();
            AcceptButton = _btnLogin;
        }

        private void InitializeFigmaLoginUI()
        {
            AutoScroll = true;

            // Centered master container hosting Header + White Card
            _pnlCenter = new Panel
            {
                Size = new Size(460, 500),
                BackColor = Color.Transparent
            };
            Controls.Add(_pnlCenter);

            // 1. Top Header: Icon badge + Title + Subtitle
            var pnlHeader = CreateTopHeader();
            pnlHeader.Location = new Point(0, 0);
            _pnlCenter.Controls.Add(pnlHeader);

            // 2. Centered White Card
            _pnlCard = CreateLoginCard();
            _pnlCard.Location = new Point(0, 116);
            _pnlCenter.Controls.Add(_pnlCard);

            // Re-center on window resize
            Resize += (s, e) => CenterContainer();
            CenterContainer();
        }

        private void CenterContainer()
        {
            if (_pnlCenter == null) return;
            int x = Math.Max(20, (ClientSize.Width - _pnlCenter.Width) / 2);
            int y = Math.Max(20, (ClientSize.Height - _pnlCenter.Height) / 2);
            _pnlCenter.Location = new Point(x, y);
        }

        // ============================================================
        // 1. TOP HEADER (Icon Badge, Title, Subtitle)
        // ============================================================
        private static Panel CreateTopHeader()
        {
            var pnl = new Panel
            {
                Size = new Size(460, 104),
                BackColor = Color.Transparent
            };

            // Blue Square Badge (44x44, rounded, with white cloud/download vector icon)
            var pnlBadge = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point((460 - 44) / 2, 0),
                BackColor = Color.Transparent
            };
            pnlBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(37, 99, 235)); // Vibrant Blue #2563EB
                using var path = CreateRoundedPath(new Rectangle(0, 0, 44, 44), 10);
                e.Graphics.FillPath(brush, path);

                // Cloud + arrow vector in crisp white
                using var pen = new Pen(Color.White, 2.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };

                // Cloud body
                e.Graphics.DrawArc(pen, 13, 14, 10, 10, 150, 180);
                e.Graphics.DrawArc(pen, 20, 11, 12, 12, 180, 160);
                e.Graphics.DrawLine(pen, 12, 23, 32, 23);

                // Down arrow in center of cloud
                e.Graphics.DrawLine(pen, 22, 17, 22, 28);
                e.Graphics.DrawLine(pen, 19, 25, 22, 28);
                e.Graphics.DrawLine(pen, 25, 25, 22, 28);
            };
            pnl.Controls.Add(pnlBadge);

            // Main Title
            var lblTitle = new Label
            {
                Text = "CLEANING SERVICES CRM",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(460, 26),
                Location = new Point(0, 52)
            };
            pnl.Controls.Add(lblTitle);

            // Subtitle
            var lblSub = new Label
            {
                Text = "Operations & Client Management Platform",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate-400
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(460, 20),
                Location = new Point(0, 80)
            };
            pnl.Controls.Add(lblSub);

            return pnl;
        }

        // ============================================================
        // 2. WHITE CARD (Form Controls & Demo Credentials)
        // ============================================================
        private Panel CreateLoginCard()
        {
            var card = new Panel
            {
                Size = new Size(460, 384),
                BackColor = Color.White
            };

            // Clip corners to 12px smooth radius and draw subtle border
            card.Resize += (s, e) =>
            {
                using var path = CreateRoundedPath(new Rectangle(0, 0, card.Width, card.Height), 12);
                card.Region = new Region(path);
            };
            using (var initialPath = CreateRoundedPath(new Rectangle(0, 0, card.Width, card.Height), 12))
            {
                card.Region = new Region(initialPath);
            }

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                using var path = CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                e.Graphics.DrawPath(borderPen, path);
            };

            int top = 28;
            const int left = 32;
            const int width = 396; // 460 - 64

            // Section Title: "Sign in"
            var lblSectionTitle = new Label
            {
                Text = "Sign in",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Dark Slate
                Location = new Point(left, top),
                Size = new Size(width, 28)
            };
            card.Controls.Add(lblSectionTitle);
            top += 30;

            // Subtext: "Enter your credentials to access the platform."
            var lblSectionSub = new Label
            {
                Text = "Enter your credentials to access the platform.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate-500
                Location = new Point(left, top),
                Size = new Size(width, 20)
            };
            card.Controls.Add(lblSectionSub);
            top += 28;

            // USERNAME Label
            var lblUser = new Label
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(left, top),
                Size = new Size(width, 16)
            };
            card.Controls.Add(lblUser);
            top += 18;

            // USERNAME Flat Input Box
            var pnlUser = CreateFlatInput(out _txtUsername, isPassword: false, placeholder: "admin, superadmin, or sales");
            pnlUser.Location = new Point(left, top);
            pnlUser.Size = new Size(width, 40);
            card.Controls.Add(pnlUser);
            top += 48;

            // PASSWORD Label
            var lblPass = new Label
            {
                Text = "PASSWORD",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(left, top),
                Size = new Size(width, 16)
            };
            card.Controls.Add(lblPass);
            top += 18;

            // PASSWORD Flat Input Box
            var pnlPass = CreateFlatInput(out _txtPassword, isPassword: true, placeholder: "");
            pnlPass.Location = new Point(left, top);
            pnlPass.Size = new Size(width, 40);
            card.Controls.Add(pnlPass);
            top += 52;

            // Primary Action Button: "Sign In"
            _btnLogin = new Button
            {
                Text = "Sign In",
                Location = new Point(left, top),
                Size = new Size(width, 42),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235), // Primary Blue #2563EB
                ForeColor = Color.White
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
            _btnLogin.Resize += (s, e) =>
            {
                using var btnPath = CreateRoundedPath(new Rectangle(0, 0, _btnLogin.Width, _btnLogin.Height), 6);
                _btnLogin.Region = new Region(btnPath);
            };
            using (var btnPath = CreateRoundedPath(new Rectangle(0, 0, _btnLogin.Width, _btnLogin.Height), 6))
            {
                _btnLogin.Region = new Region(btnPath);
            }
            _btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(_btnLogin);
            top += 46;

            // Status message label (subtle, non-intrusive)
            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(left, top),
                Size = new Size(width, 16)
            };
            card.Controls.Add(_lblStatus);
            top += 18;

            // 3. DEMO CREDENTIALS HELPER SECTION
            var lblDemo = new Label
            {
                Text = "Demo credentials:",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate-400
                Location = new Point(left, top),
                Size = new Size(width, 16)
            };
            card.Controls.Add(lblDemo);
            top += 18;

            // 3 Side-by-Side Demo Cards
            var pnlDemoCards = CreateDemoCredentialsRow(width);
            pnlDemoCards.Location = new Point(left, top);
            card.Controls.Add(pnlDemoCards);

            // Tab navigation order
            _txtUsername.TabIndex = 0;
            _txtPassword.TabIndex = 1;
            _btnLogin.TabIndex = 2;

            // Enter key handling
            _txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _txtPassword.Focus();
                }
            };

            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _btnLogin.PerformClick();
                }
            };

            return card;
        }

        // ============================================================
        // INPUT FIELD BUILDER (Flat, subtle border, placeholder)
        // ============================================================
        private static Panel CreateFlatInput(out TextBox txt, bool isPassword, string placeholder)
        {
            var pnl = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252), // Light slate #F8FAFC
                Padding = new Padding(12, 10, 12, 8)
            };

            pnl.Resize += (s, e) =>
            {
                using var path = CreateRoundedPath(new Rectangle(0, 0, pnl.Width, pnl.Height), 6);
                pnl.Region = new Region(path);
            };
            using (var path = CreateRoundedPath(new Rectangle(0, 0, 396, 40), 6))
            {
                pnl.Region = new Region(path);
            }

            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = isPassword,
                PlaceholderText = placeholder
            };
            pnl.Controls.Add(tb);

            bool isFocused = false;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(isFocused ? Color.FromArgb(37, 99, 235) : Color.FromArgb(226, 232, 240), isFocused ? 1.5f : 1f);
                using var roundPath = CreateRoundedPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 6);
                e.Graphics.DrawPath(pen, roundPath);
            };

            tb.Enter += (s, e) => { isFocused = true; pnl.Invalidate(); };
            tb.Leave += (s, e) => { isFocused = false; pnl.Invalidate(); };

            txt = tb;
            return pnl;
        }

        // ============================================================
        // 3. DEMO CREDENTIALS HELPER ROW (Admin, SuperAdmin, Sales Staff)
        // ============================================================
        private Panel CreateDemoCredentialsRow(int totalWidth)
        {
            var pnl = new Panel
            {
                Size = new Size(totalWidth, 50),
                BackColor = Color.Transparent
            };

            int gap = 8;
            int cardWidth = (totalWidth - (gap * 2)) / 3;

            var demoRoles = new[]
            {
                ("Admin", "admin", "admin"),
                ("SuperAdmin", "superadmin", "superadmin"),
                ("Sales Staff", "sales", "sales")
            };

            for (int i = 0; i < demoRoles.Length; i++)
            {
                var role = demoRoles[i];
                var btnCard = CreateDemoCard(role.Item1, role.Item2, role.Item3, cardWidth, 48);
                btnCard.Location = new Point(i * (cardWidth + gap), 0);
                pnl.Controls.Add(btnCard);
            }

            return pnl;
        }

        private Panel CreateDemoCard(string roleTitle, string username, string password, int width, int height)
        {
            var card = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand
            };

            card.Resize += (s, e) =>
            {
                using var path = CreateRoundedPath(new Rectangle(0, 0, card.Width, card.Height), 6);
                card.Region = new Region(path);
            };
            using (var path = CreateRoundedPath(new Rectangle(0, 0, width, height), 6))
            {
                card.Region = new Region(path);
            }

            bool isHover = false;
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(isHover ? Color.FromArgb(148, 163, 184) : Color.FromArgb(226, 232, 240), 1f);
                using var roundPath = CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6);
                e.Graphics.DrawPath(pen, roundPath);
            };

            var lblTitle = new Label
            {
                Text = roleTitle,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85), // Slate-700
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 18),
                Location = new Point(0, 7),
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = username,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate-500
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 16),
                Location = new Point(0, 25),
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblSub);

            Action clickAction = () =>
            {
                _txtUsername.Text = username;
                _txtPassword.Text = password;
                _lblStatus.Text = "";
                _btnLogin.Focus();
            };

            card.Click += (s, e) => clickAction();
            lblTitle.Click += (s, e) => clickAction();
            lblSub.Click += (s, e) => clickAction();

            card.MouseEnter += (s, e) => { isHover = true; card.BackColor = Color.FromArgb(241, 245, 249); card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHover = false; card.BackColor = Color.FromArgb(248, 250, 252); card.Invalidate(); };
            lblTitle.MouseEnter += (s, e) => { isHover = true; card.BackColor = Color.FromArgb(241, 245, 249); card.Invalidate(); };
            lblSub.MouseEnter += (s, e) => { isHover = true; card.BackColor = Color.FromArgb(241, 245, 249); card.Invalidate(); };

            return card;
        }

        // ============================================================
        // ROUNDED RECTANGLE PATH HELPER
        // ============================================================
        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_txtUsername != null)
            {
                _txtUsername.Focus();
            }
        }

        // ============================================================
        // AUTHENTICATION LOGIC & ROUTING
        // Sets DialogResult = DialogResult.OK and closes form cleanly.
        // Never instantiates duplicate MainForm or hides parents.
        // ============================================================
        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            var username = _txtUsername.Text.Trim();
            var password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter both username and password.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            _lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            _lblStatus.Text = "Authenticating with database...";
            _btnLogin.Enabled = false;

            User? user = null;

            try
            {
                using var db = new AppDbContext();
                AppDbContext.EnsureSeedData(db);

                // 1. Direct database match
                user = db.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

                // 2. Demo aliases match (supports demo shorthand: admin/admin, superadmin/superadmin, sales/sales)
                if (user == null)
                {
                    user = CheckDemoAlias(db, username, password);
                }
            }
            catch
            {
                user = GetFallbackSeedUser(username, password);
            }

            _btnLogin.Enabled = true;

            if (user != null)
            {
                _lblStatus.Text = "";
                SessionManager.Login(user);
                LoginSuccess?.Invoke(this, EventArgs.Empty);

                // Clean single-instance termination: return DialogResult.OK to caller
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = "Invalid username or password.";

                MessageBox.Show(
                    "Invalid username or password. Please try again.",
                    "Authentication Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private static User? CheckDemoAlias(AppDbContext db, string username, string password)
        {
            var u = username.Trim().ToLowerInvariant();
            var p = password.Trim().ToLowerInvariant();

            if (u == "admin" && (p == "admin" || p == "admin123"))
                return db.Users.FirstOrDefault(x => x.Username == "admin");

            if (u == "superadmin" && (p == "superadmin" || p == "super123"))
                return db.Users.FirstOrDefault(x => x.Username == "superadmin");

            if ((u == "sales" || u == "staff") && (p == "sales" || p == "staff" || p == "staff123"))
                return db.Users.FirstOrDefault(x => x.Username == "staff" || x.Username == "sales");

            if (u == "manager" && (p == "manager" || p == "manager123"))
                return db.Users.FirstOrDefault(x => x.Username == "manager");

            return null;
        }

        private static User? GetFallbackSeedUser(string username, string password)
        {
            var u = username.Trim().ToLowerInvariant();
            var p = password.Trim().ToLowerInvariant();

            if (u == "admin" && (p == "admin" || p == "admin123"))
                return new User { Id = 2, Username = "admin", PasswordHash = "admin123", Role = Roles.Admin };

            if (u == "superadmin" && (p == "superadmin" || p == "super123"))
                return new User { Id = 1, Username = "superadmin", PasswordHash = "super123", Role = Roles.SuperAdmin };

            if ((u == "sales" || u == "staff") && (p == "sales" || p == "staff" || p == "staff123"))
                return new User { Id = 4, Username = "staff", PasswordHash = "staff123", Role = Roles.SalesStaff };

            if (u == "manager" && (p == "manager" || p == "manager123"))
                return new User { Id = 3, Username = "manager", PasswordHash = "manager123", Role = Roles.Manager };

            return null;
        }
    }

    /// <summary>
    /// Backward-compatible alias for LoginView.
    /// </summary>
    public class LoginForm : LoginView
    {
    }
}
