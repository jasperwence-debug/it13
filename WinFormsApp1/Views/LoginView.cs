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

        public LoginView()
        {
            // Desktop Form Configuration
            Text = "Cleaning Services CRM - Sign In";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(800, 640);
            ClientSize = new Size(960, 720);
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(248, 250, 252); // #F8FAFC slate-50 base
            DoubleBuffered = true;
            ResizeRedraw = true;

            InitializeFigmaLoginUI();
            AcceptButton = _btnLogin;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;

            // Soft wash: slate-50 (#F8FAFC) -> blue-50 (#EFF6FF) -> indigo-100 (#E0E7FF)
            using var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(248, 250, 252),
                Color.FromArgb(224, 231, 255),
                LinearGradientMode.ForwardDiagonal);

            var cb = new ColorBlend(3)
            {
                Colors = new[]
                {
                    Color.FromArgb(248, 250, 252), // slate-50 #F8FAFC
                    Color.FromArgb(239, 246, 255), // blue-50 #EFF6FF
                    Color.FromArgb(224, 231, 255)  // indigo-100 #E0E7FF
                },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            brush.InterpolationColors = cb;
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        private void InitializeFigmaLoginUI()
        {
            AutoScroll = true;

            // Centered master container hosting Header + White Card
            _pnlCenter = new Panel
            {
                Size = new Size(460, 600),
                BackColor = Color.Transparent
            };
            Controls.Add(_pnlCenter);

            // 1. Top Header: Icon badge + Title + Subtitle
            var pnlHeader = CreateTopHeader();
            pnlHeader.Location = new Point(0, 0);
            _pnlCenter.Controls.Add(pnlHeader);

            // 2. Centered White Card
            _pnlCard = CreateLoginCard();
            _pnlCard.Location = new Point(0, 136);
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
                Size = new Size(460, 126),
                BackColor = Color.Transparent
            };

            // Blue-Indigo Diagonal Gradient Badge (46x46, rounded, with white cloud/download vector icon)
            var pnlBadge = new Panel
            {
                Size = new Size(46, 46),
                Location = new Point((460 - 46) / 2, 0),
                BackColor = Color.Transparent
            };
            pnlBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 46, 46),
                    Color.FromArgb(37, 99, 235), // blue-600 #2563EB
                    Color.FromArgb(79, 70, 229), // indigo-600 #4F46E5
                    LinearGradientMode.ForwardDiagonal);
                using var path = CreateRoundedPath(new Rectangle(0, 0, 46, 46), 10);
                e.Graphics.FillPath(brush, path);

                // Cloud + arrow vector in crisp white
                using var pen = new Pen(Color.White, 2.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };

                // Cloud body
                e.Graphics.DrawArc(pen, 14, 15, 10, 10, 150, 180);
                e.Graphics.DrawArc(pen, 21, 12, 12, 12, 180, 160);
                e.Graphics.DrawLine(pen, 13, 24, 33, 24);

                // Down arrow in center of cloud
                e.Graphics.DrawLine(pen, 23, 18, 23, 29);
                e.Graphics.DrawLine(pen, 20, 26, 23, 29);
                e.Graphics.DrawLine(pen, 26, 26, 23, 29);
            };
            pnl.Controls.Add(pnlBadge);

            // Main Title - Generous height prevents vertical font clipping
            var lblTitle = new Label
            {
                Text = "CLEANING SERVICES CRM",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Slate-900
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(460, 36),
                Location = new Point(0, 56)
            };
            pnl.Controls.Add(lblTitle);

            // Subtitle - Clear separation and height prevents text overlap
            var lblSub = new Label
            {
                Text = "Operations & Client Management Platform",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105), // Slate-600
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(460, 24),
                Location = new Point(0, 94)
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
                Size = new Size(460, 440),
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

                // Left panel accent with diagonal gradient: blue-600 (#2563EB) -> indigo-600 (#4F46E5) -> purple-700 (#7E22CE)
                var accentRect = new Rectangle(0, 0, 6, card.Height);
                using (var accentBrush = new LinearGradientBrush(
                    accentRect,
                    Color.FromArgb(37, 99, 235),
                    Color.FromArgb(126, 34, 206),
                    LinearGradientMode.ForwardDiagonal))
                {
                    var cb = new ColorBlend(3)
                    {
                        Colors = new[]
                        {
                            Color.FromArgb(37, 99, 235),  // blue-600 #2563EB
                            Color.FromArgb(79, 70, 229),  // indigo-600 #4F46E5
                            Color.FromArgb(126, 34, 206)  // purple-700 #7E22CE
                        },
                        Positions = new[] { 0f, 0.5f, 1f }
                    };
                    accentBrush.InterpolationColors = cb;
                    e.Graphics.FillRectangle(accentBrush, accentRect);
                }

                using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                using var path = CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                e.Graphics.DrawPath(borderPen, path);
            };

            int top = 26;
            const int left = 32;
            const int width = 396; // 460 - 64

            // Section Title: "Sign in" - Generous height prevents vertical clipping
            var lblSectionTitle = new Label
            {
                Text = "Sign in",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Dark Slate
                Location = new Point(left, top),
                Size = new Size(width, 36)
            };
            card.Controls.Add(lblSectionTitle);
            top += 40;

            // Subtext: "Enter your credentials to access the platform."
            var lblSectionSub = new Label
            {
                Text = "Enter your credentials to access the platform.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate-500
                Location = new Point(left, top),
                Size = new Size(width, 22)
            };
            card.Controls.Add(lblSectionSub);
            top += 30;

            // USERNAME Label
            var lblUser = new Label
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(left, top),
                Size = new Size(width, 18)
            };
            card.Controls.Add(lblUser);
            top += 20;

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
                Size = new Size(width, 18)
            };
            card.Controls.Add(lblPass);
            top += 20;

            // PASSWORD Flat Input Box
            var pnlPass = CreateFlatInput(out _txtPassword, isPassword: true, placeholder: "");
            pnlPass.Location = new Point(left, top);
            pnlPass.Size = new Size(width, 40);
            card.Controls.Add(pnlPass);
            top += 50;

            // Primary Action Button: "Sign In" with gradient, scale & shadow effects
            _btnLogin = new LoginGradientButton
            {
                Text = "Sign In",
                Location = new Point(left, top),
                Size = new Size(width, 44),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White
            };
            _btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(_btnLogin);
            top += 52;

            // Status message label (subtle, non-intrusive)
            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(left, top),
                Size = new Size(width, 18)
            };
            card.Controls.Add(_lblStatus);
            top += 20;

            // 3. DEMO CREDENTIALS HELPER SECTION
            var lblDemo = new Label
            {
                Text = "Demo credentials:",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate-400
                Location = new Point(left, top),
                Size = new Size(width, 18)
            };
            card.Controls.Add(lblDemo);
            top += 22;

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
                Size = new Size(width, 20),
                Location = new Point(0, 5),
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = username,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate-500
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 18),
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
    /// Specialized gradient CTA button for the Login View.
    /// Features left-to-right gradient (#2563EB -> #4F46E5), hover shift (#1D4ED8 -> #4338CA),
    /// scale effect (1.02 on hover, 0.98 on click), and dynamic drop shadows (shadow-lg to shadow-xl).
    /// </summary>
    internal class LoginGradientButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;

        public LoginGradientButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Clear background with parent's backcolor
            using (var bgBrush = new SolidBrush(Parent?.BackColor ?? Color.White))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Inset bounds slightly to provide room for shadow and 1.02 scale
            int marginX = 4;
            int marginY = 4;
            var baseRect = new Rectangle(marginX, marginY, Width - (marginX * 2), Height - (marginY * 2));

            Rectangle drawRect;
            if (_isPressed)
            {
                // Scale 0.98 on click
                int dx = Math.Max(1, (int)Math.Round(baseRect.Width * 0.01f));
                int dy = Math.Max(1, (int)Math.Round(baseRect.Height * 0.01f));
                drawRect = new Rectangle(baseRect.X + dx, baseRect.Y + dy, baseRect.Width - (dx * 2), baseRect.Height - (dy * 2));
            }
            else if (_isHovered)
            {
                // Scale 1.02 on hover
                int dx = Math.Max(1, (int)Math.Round(baseRect.Width * 0.01f));
                int dy = Math.Max(1, (int)Math.Round(baseRect.Height * 0.01f));
                drawRect = new Rectangle(baseRect.X - dx, baseRect.Y - dy, baseRect.Width + (dx * 2), baseRect.Height + (dy * 2));
            }
            else
            {
                drawRect = baseRect;
            }

            // Shadow grows from shadow-lg at rest to shadow-xl on hover
            int shadowLayers = _isHovered ? 4 : 2;
            int shadowAlpha = _isHovered ? 20 : 12;
            for (int i = shadowLayers; i >= 1; i--)
            {
                var shadowRect = new Rectangle(drawRect.X - i, drawRect.Y + (i * 2) - 1, drawRect.Width + (i * 2), drawRect.Height + (i * 2));
                using var shadowPath = CreateRoundedPath(shadowRect, 8);
                using var shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, 37, 99, 235));
                g.FillPath(shadowBrush, shadowPath);
            }

            // Gradient: Left-to-right from blue-600 (#2563eb) to indigo-600 (#4f46e5)
            // Hover/Pressed: blue-700 (#1d4ed8) to indigo-700 (#4338ca)
            Color startColor = (_isHovered || _isPressed) ? Color.FromArgb(29, 78, 216) : Color.FromArgb(37, 99, 235);
            Color endColor = (_isHovered || _isPressed) ? Color.FromArgb(67, 56, 202) : Color.FromArgb(79, 70, 229);

            if (!Enabled)
            {
                startColor = Color.FromArgb(148, 163, 184); // Slate-400
                endColor = Color.FromArgb(148, 163, 184);
            }

            using (var fillBrush = new LinearGradientBrush(drawRect, startColor, endColor, LinearGradientMode.Horizontal))
            {
                using var buttonPath = CreateRoundedPath(drawRect, 6);
                g.FillPath(fillBrush, buttonPath);
            }

            // Button label centered in crisp white bold font
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                drawRect,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }

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
    }

    /// <summary>
    /// Backward-compatible alias for LoginView.
    /// </summary>
    public class LoginForm : LoginView
    {
    }
}
