using CRM.winforms.Helpers;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class LoginForm : Form
    {
        private TextBox _email = null!;
        private TextBox _password = null!;
        private TextBox _tenantId = null!;
        private CheckBox _showPassword = null!;
        private Button _loginBtn = null!;
        private Label _error = null!;

        public LoginForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"{BrandHelper.ProductName} — Sign In";
            ClientSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = AppTheme.Paper;
            Font = AppTheme.FontBody;

            // ─── HERO PANEL (left) ─────────────────────────────
            var hero = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = AppTheme.Ink
            };

            // Signature dot next to wordmark
            var heroDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 16F),
                ForeColor = AppTheme.Signal,
                Location = new Point(44, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            hero.Controls.Add(heroDot);

            var brandName = new Label
            {
                Text = BrandHelper.ProductName,
                Font = AppTheme.FontWordmark,
                ForeColor = Color.White,
                Location = new Point(66, 44),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            hero.Controls.Add(brandName);

            var brandSub = new Label
            {
                Text = "C R M   S Y S T E M",
                Font = new Font("Segoe UI Variable Text", 7.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0x7E, 0x8F, 0xB3),
                Location = new Point(68, 72),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            hero.Controls.Add(brandSub);

            // Logo mark in circle
            var logoImage = BrandHelper.LoadLogo();
            if (logoImage != null)
            {
                var logoBox = new PictureBox
                {
                    Image = logoImage,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(72, 72),
                    Location = new Point(44, 130),
                    BackColor = Color.Transparent
                };
                hero.Controls.Add(logoBox);
            }

            // Hero copy
            var heroTitle = new Label
            {
                Text = "Connect.\r\nSpot. Deliver.",
                Font = new Font("Bahnschrift SemiBold", 26F),
                ForeColor = Color.White,
                Location = new Point(44, 240),
                Size = new Size(340, 100),
                BackColor = Color.Transparent
            };
            hero.Controls.Add(heroTitle);

            var heroSub = new Label
            {
                Text = "Manage leads, projects, and client relationships in one place. Every screen is scoped to your role.",
                Font = AppTheme.FontBodySmall,
                ForeColor = Color.FromArgb(0x9A, 0xA9, 0xC5),
                Location = new Point(44, 350),
                Size = new Size(330, 90),
                BackColor = Color.Transparent
            };
            hero.Controls.Add(heroSub);

            var heroFooter = new Label
            {
                Text = BrandHelper.ProductFooter,
                Font = new Font("Segoe UI Variable Text", 8F),
                ForeColor = Color.FromArgb(0x7E, 0x8F, 0xB3),
                Location = new Point(44, 570),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            hero.Controls.Add(heroFooter);

            // ─── FORM PANEL (right) ────────────────────────────
            var form = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Paper
            };

            // Signature dot beside "Welcome back"
            var formDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 12F),
                ForeColor = AppTheme.Signal,
                Location = new Point(80, 78),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(formDot);

            var welcome = new Label
            {
                Text = "Welcome back",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(98, 72),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(welcome);

            var welcomeSub = new Label
            {
                Text = "Sign in with your account credentials.",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(82, 118),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(welcomeSub);

            // Fields
            int fieldX = 82;
            int fieldW = 400;
            int y = 180;

            var tenantLabel = MakeLabel("Company ID", fieldX, y);
            _tenantId = MakeInput(fieldX, y + 22, fieldW);
            _tenantId.PlaceholderText = "11111111-1111-1111-1111-111111111111";
            form.Controls.Add(tenantLabel);
            form.Controls.Add(_tenantId);
            y += 78;

            var emailLabel = MakeLabel("Email address", fieldX, y);
            _email = MakeInput(fieldX, y + 22, fieldW);
            _email.PlaceholderText = "you@company.com";
            _email.Text = "admin@crm.local";
            form.Controls.Add(emailLabel);
            form.Controls.Add(_email);
            y += 78;

            var passwordLabel = MakeLabel("Password", fieldX, y);
            _password = MakeInput(fieldX, y + 22, fieldW);
            _password.PlaceholderText = "••••••••";
            _password.UseSystemPasswordChar = true;
            _password.Text = "Admin123!";
            form.Controls.Add(passwordLabel);
            form.Controls.Add(_password);
            y += 46;

            _showPassword = new CheckBox
            {
                Text = "Show password",
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(fieldX, y),
                AutoSize = true
            };
            _showPassword.CheckedChanged += (_, _) =>
                _password.UseSystemPasswordChar = !_showPassword.Checked;
            form.Controls.Add(_showPassword);
            y += 34;

            _error = new Label
            {
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.DangerText,
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 20)
            };
            form.Controls.Add(_error);
            y += 26;

            _loginBtn = new Button
            {
                Text = "Sign in",
                Location = new Point(fieldX, y),
                Size = new Size(fieldW, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 11F),
                Cursor = Cursors.Hand
            };
            _loginBtn.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(_loginBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_loginBtn, AppTheme.Signal, AppTheme.SignalHover);
            _loginBtn.Click += async (_, _) => await OnLoginClicked();
            form.Controls.Add(_loginBtn);

            Controls.Add(form);
            Controls.Add(hero);

            AcceptButton = _loginBtn;
        }

        private Label MakeLabel(string text, int x, int y) => new()
        {
            Text = text,
            Font = AppTheme.FontLabel,
            ForeColor = AppTheme.Trace,
            Location = new Point(x, y),
            AutoSize = true
        };

        private TextBox MakeInput(int x, int y, int width) => new()
        {
            Location = new Point(x, y),
            Size = new Size(width, 30),
            Font = AppTheme.FontBody,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary
        };

        private async Task OnLoginClicked()
        {
            _error.Text = "";
            _loginBtn.Enabled = false;
            _loginBtn.Text = "Signing in…";
            _loginBtn.BackColor = AppTheme.SignalHover;
            _loginBtn.Invalidate();

            try
            {
                if (!Guid.TryParse(_tenantId.Text.Trim(), out var tenantId))
                {
                    _error.Text = "Company ID must be a valid GUID.";
                    return;
                }

                var client = new AuthApiClient();
                await client.LoginAsync(_email.Text.Trim(), _password.Text, tenantId);

                Hide();
                using var main = new MainForm();
                main.ShowDialog();
                Close();
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Connection error: {ex.Message}"; }
            finally
            {
                _loginBtn.Enabled = true;
                _loginBtn.Text = "Sign in";
                _loginBtn.BackColor = AppTheme.Signal;
                _loginBtn.Invalidate();
            }
        }
    }
}