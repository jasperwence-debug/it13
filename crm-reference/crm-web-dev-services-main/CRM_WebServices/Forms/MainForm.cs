    using CRM.winforms.Helpers;
    using CRM.winforms.Services;
    using CRM.winforms.Forms.UserControls;

    namespace CRM.winforms.Forms
    {
        public class MainForm : Form
        {
            private Panel _sidebar = null!;
            private Panel _content = null!;
            private Panel _topBar = null!;
            private Label _pageTitle = null!;
            private Label _pageDot = null!;
            private Label _userLabel = null!;
            private readonly Dictionary<string, Button> _navButtons = new();

            public MainForm()
            {
                InitializeUI();
                ShowPage("Dashboard");
            }

            private void InitializeUI()
            {
                Text = $"{BrandHelper.ProductName} — CRM";
                Size = new Size(1400, 860);
                StartPosition = FormStartPosition.CenterScreen;
                MinimumSize = new Size(1200, 720);
                BackColor = AppTheme.Paper;
                Font = AppTheme.FontBody;

                // ─── TOP BAR ────────────────────────────────────────
                _topBar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 64,
                    BackColor = AppTheme.Surface
                };
                _topBar.Paint += (_, e) =>
                {
                    using var pen = new Pen(AppTheme.Line, 1);
                    e.Graphics.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
                };

                // Logo
                var logoImage = BrandHelper.LoadLogo();
                if (logoImage != null)
                {
                    var pic = new PictureBox
                    {
                        Image = logoImage,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Size = new Size(36, 36),
                        Location = new Point(24, 14),
                        BackColor = Color.Transparent
                    };
                    _topBar.Controls.Add(pic);
                }

                // Signature dot + product wordmark
                var brandDot = new Label
                {
                    Text = "●",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = AppTheme.Signal,
                    Location = new Point(70, 22),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _topBar.Controls.Add(brandDot);

                var productLabel = new Label
                {
                    Text = BrandHelper.ProductName,
                    Font = AppTheme.FontWordmark,
                    ForeColor = AppTheme.Ink,
                    AutoSize = true,
                    Location = new Point(88, 22),
                    BackColor = Color.Transparent
                };
                _topBar.Controls.Add(productLabel);

                // Hairline divider
                var divider = new Panel
                {
                    Location = new Point(180, 20),
                    Size = new Size(1, 26),
                    BackColor = AppTheme.Line
                };
                _topBar.Controls.Add(divider);

                // Page dot (signature) + page title
                _pageDot = new Label
                {
                    Text = "●",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = AppTheme.Signal,
                    Location = new Point(200, 22),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _topBar.Controls.Add(_pageDot);

                _pageTitle = new Label
                {
                    Text = "Dashboard",
                    Font = AppTheme.FontSubheading,
                    ForeColor = AppTheme.Ink,
                    AutoSize = true,
                    Location = new Point(218, 22),
                    BackColor = Color.Transparent
                };
                _topBar.Controls.Add(_pageTitle);

                // User label (right)
                _userLabel = new Label
                {
                    Text = $"{SessionManager.Current.Name}  •  {SessionManager.Current.Role}",
                    Font = AppTheme.FontBodySmall,
                    ForeColor = AppTheme.Trace,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                _topBar.Controls.Add(_userLabel);

                // Logout
                var logoutBtn = new Button
                {
                    Text = "Logout",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = AppTheme.DangerText,
                    Font = AppTheme.FontLabel,
                    Size = new Size(80, 32),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Cursor = Cursors.Hand
                };
                logoutBtn.FlatAppearance.BorderSize = 0;
                logoutBtn.FlatAppearance.MouseOverBackColor = AppTheme.DangerBg;
                logoutBtn.Click += (_, _) => ConfirmLogout();
                _topBar.Controls.Add(logoutBtn);

                _topBar.Resize += (_, _) =>
                {
                    _userLabel.Location = new Point(_topBar.Width - _userLabel.Width - 110, 22);
                    logoutBtn.Location = new Point(_topBar.Width - 90, 16);
                };

                // ─── SIDEBAR ────────────────────────────────────────
                _sidebar = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 220,
                    BackColor = AppTheme.Ink
                };

                // Sidebar brand block
                var sbDot = new Label
                {
                    Text = "●",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = AppTheme.Signal,
                    Location = new Point(24, 24),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _sidebar.Controls.Add(sbDot);

                var sbTitle = new Label
                {
                    Text = BrandHelper.ProductName,
                    Font = AppTheme.FontSubheading,
                    ForeColor = Color.White,
                    Location = new Point(42, 24),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _sidebar.Controls.Add(sbTitle);

                var sbSub = new Label
                {
                    Text = "C R M   S Y S T E M",
                    Font = new Font("Segoe UI Variable Text", 7.5F),
                    ForeColor = Color.FromArgb(0x7E, 0x8F, 0xB3),
                    Location = new Point(44, 48),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _sidebar.Controls.Add(sbSub);

                var sidebarDivider = new Panel
                {
                    Location = new Point(24, 80),
                    Size = new Size(172, 1),
                    BackColor = Color.FromArgb(30, 255, 255, 255)
                };
                _sidebar.Controls.Add(sidebarDivider);

                var navItems = new List<(string Name, string Icon)>
                {
                    ("Dashboard",  "📊"),
                    ("Customers",  "👥"),
                    ("Leads",      "🎯"),
                    ("Inquiries",  "📩"),
                    ("Activities", "📞"),
                    ("Follow-ups", "⏰"),
                    ("Reports",    "📈"),
                };

                // Users management — only for Admin+
                if (SessionManager.Current.IsElevated)
                {
                    navItems.Add(("Users", "🧑\u200d💼"));
                }

                // Retention Action Center — Manager+ only
                if (SessionManager.Current.CanManageTeam)
                {
                    navItems.Add(("Retention", "🔄"));
                }

            navItems.Add(("Profile", "👤"));

                int y = 104;
                foreach (var (name, icon) in navItems)
                {
                    var btn = new Button
                    {
                        Text = $"   {icon}    {name}",
                        TextAlign = ContentAlignment.MiddleLeft,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = AppTheme.Ink,
                        ForeColor = AppTheme.SidebarText,
                        Font = AppTheme.FontBody,
                        Size = new Size(188, 42),
                        Location = new Point(16, y),
                        Cursor = Cursors.Hand,
                        Padding = new Padding(12, 0, 0, 0),
                        Tag = name
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.SidebarHoverOverlay;
                    btn.Click += (_, _) => ShowPage((string)btn.Tag);

                    _navButtons[name] = btn;
                    _sidebar.Controls.Add(btn);
                    y += 46;
                }

                // Footer version
                var sidebarFooter = new Label
                {
                    Text = "v1.0",
                    Font = new Font("Segoe UI Variable Text", 8F),
                    ForeColor = Color.FromArgb(0x7E, 0x8F, 0xB3),
                    Location = new Point(24, _sidebar.Height - 40),
                    AutoSize = true,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                    BackColor = Color.Transparent
                };
                _sidebar.Controls.Add(sidebarFooter);

                // ─── CONTENT ────────────────────────────────────────
                _content = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(28, 24, 28, 24),
                    BackColor = AppTheme.Paper,
                    AutoScroll = true
                };

                Controls.Add(_content);
                Controls.Add(_sidebar);
                Controls.Add(_topBar);
            }

            private void HighlightNav(string name)
            {
                foreach (var (key, btn) in _navButtons)
                {
                    if (key == name)
                    {
                        btn.BackColor = AppTheme.Signal;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = AppTheme.Ink;
                        btn.ForeColor = AppTheme.SidebarText;
                    }
                    btn.Invalidate();
                }
            }

            private void ConfirmLogout()
            {
                var result = MessageBox.Show(
                    "Log out?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                SessionManager.Current.Clear();
                Hide();
                using var login = new LoginForm();
                login.ShowDialog();
                Close();
            }

        private void ShowPage(string name)
        {
            // Handle "Follow-ups?Overdue" special case
            bool showOverdue = false;
            if (name.Contains('?'))
            {
                var parts = name.Split('?', 2);
                name = parts[0];
                showOverdue = parts.Length > 1 &&
                              parts[1].Contains("Overdue", StringComparison.OrdinalIgnoreCase);
            }

            HighlightNav(name);
            _pageTitle.Text = name;
            _content.Controls.Clear();

            UserControl page;

            if (name == "Dashboard")
            {
                var dashboard = new DashboardControl();
                dashboard.NavigationRequested += (dest) => ShowPage(dest);
                page = dashboard;
            }
            else if (name == "Customers")
            {
                page = new CustomersControl();
            }
            else if (name == "Leads")
            {
                page = new LeadsControl();
            }
            else if (name == "Inquiries")
            {
                page = new InquiriesControl();
            }
            else if (name == "Activities")
            {
                page = new ActivitiesControl();
            }
            else if (name == "Follow-ups")
            {
                var followUps = new FollowUpsControl();
                page = followUps;

                // If the Overdue KPI was clicked, enable the toggle AFTER the
                // control is attached to a parent (i.e. it has a window handle).
                if (showOverdue)
                {
                    followUps.HandleCreated += (_, _) =>
                    {
                        followUps.BeginInvoke(new Action(() =>
                        {
                            followUps.ActivateOverdueFilter();
                        }));
                    };
                }
            }
            else if (name == "Reports")
            {
                page = new ReportsControl();
            }
            else if (name == "Retention")
            {
                page = new RetentionControl();
            }
            else if (name == "Users")
            {
                page = new UsersControl();
            }
            else if (name == "Profile")
            {
                page = new ProfileControl();
            }
            else
            {
                var dashboard = new DashboardControl();
                dashboard.NavigationRequested += (dest) => ShowPage(dest);
                page = dashboard;
            }

            page.Dock = DockStyle.Fill;
            _content.Controls.Add(page);
        }
    }
    }