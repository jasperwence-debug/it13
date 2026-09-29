using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.Domain.Models.Email;
using App.Infrastructure.Services.Email;

namespace App.WinForms.Views
{
    /// <summary>
    /// Professional Executive Dialog for triggering personalized win-back emails via SMTP.
    /// Provides live template preview, HTML browser preview, customizable promo voucher,
    /// and real-time delivery diagnostics.
    /// </summary>
    public class WinBackEmailDialog : Form
    {
        private readonly ApiClient _api = new();
        private readonly int? _customerId;
        private readonly string _customerName;
        private readonly int _daysInactive;
        private readonly string _lastService;
        private readonly string _serviceLocation;

        // UI Controls
        private TextBox _txtRecipientEmail = null!;
        private ComboBox _cmbTemplate = null!;
        private TextBox _txtSubject = null!;
        private TextBox _txtPromoCode = null!;
        private NumericUpDown _numDiscount = null!;
        private TextBox _txtCustomNotes = null!;
        private TextBox _txtPreview = null!;
        private Button _btnPreviewBrowser = null!;
        private Button _btnSend = null!;
        private Button _btnCancel = null!;
        private Label _lblStatusFeedback = null!;

        public EmailResult? DispatchResult { get; private set; }

        public WinBackEmailDialog(
            int? customerId,
            string customerName,
            string? email,
            int daysInactive = 30,
            string? lastService = "General Cleaning",
            string? serviceLocation = "")
        {
            _customerId = customerId;
            _customerName = string.IsNullOrWhiteSpace(customerName) ? "Valued Client" : customerName;
            _daysInactive = daysInactive;
            _lastService = string.IsNullOrWhiteSpace(lastService) ? "Professional Cleaning" : lastService;
            _serviceLocation = serviceLocation ?? string.Empty;

            InitializeComponent(email);
            UpdateTemplateFields();
        }

        private void InitializeComponent(string? initialEmail)
        {
            Text = "CleanPro Retention — Send Win-Back Email";
            Size = new Size(740, 660);
            MinimumSize = new Size(700, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // ── Top Header Panel ─────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(15, 23, 42), // Slate 900
                Padding = new Padding(24, 14, 24, 14)
            };

            var lblHeaderTitle = new Label
            {
                Text = "★  Client Win-Back & Retention Outreach",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 12)
            };

            var lblHeaderSubtitle = new Label
            {
                Text = $"Automated SMTP re-engagement workflow for {_customerName} • {_daysInactive} days inactive",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(24, 38)
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSubtitle);
            Controls.Add(pnlHeader);

            // ── Main Content Area ────────────────────────────────────
            var pnlBody = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(676, 490),
                BackColor = Color.White
            };
            pnlBody.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240));
                e.Graphics.DrawRectangle(pen, 0, 0, pnlBody.Width - 1, pnlBody.Height - 1);
            };

            int y = 16;
            const int labelX = 20;
            const int inputX = 160;
            const int inputW = 490;

            // Recipient Email
            var lblEmail = new Label { Text = "Recipient Email:", Location = new Point(labelX, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _txtRecipientEmail = new TextBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                Text = initialEmail?.Trim() ?? string.Empty
            };
            _txtRecipientEmail.TextChanged += (s, e) => UpdatePreview();
            pnlBody.Controls.Add(lblEmail);
            pnlBody.Controls.Add(_txtRecipientEmail);
            y += 36;

            // Campaign Template
            var lblTpl = new Label { Text = "Retention Flow:", Location = new Point(labelX, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _cmbTemplate = new ComboBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbTemplate.Items.AddRange(new object[]
            {
                "30-Day Lapsed — 'We Miss You' Friendly Check-in",
                "60-Day Inactive — 10% Discount Promo Voucher",
                "90-Day Lapsed — VIP Re-engagement Perk (15% Off)",
                "Contract Renewal Notice (30-Day Term Notice)",
                "Special Service Loyalty Bonus Offer"
            });
            _cmbTemplate.SelectedIndex = _daysInactive >= 90 ? 2 : (_daysInactive >= 60 ? 1 : 0);
            _cmbTemplate.SelectedIndexChanged += (s, e) => UpdateTemplateFields();
            pnlBody.Controls.Add(lblTpl);
            pnlBody.Controls.Add(_cmbTemplate);
            y += 36;

            // Promo Code & Discount %
            var lblPromo = new Label { Text = "Promo Voucher:", Location = new Point(labelX, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _txtPromoCode = new TextBox
            {
                Location = new Point(inputX, y),
                Width = 220,
                Text = "WINBACK10",
                CharacterCasing = CharacterCasing.Upper
            };
            _txtPromoCode.TextChanged += (s, e) => UpdatePreview();

            var lblDisc = new Label { Text = "Discount %:", Location = new Point(inputX + 240, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _numDiscount = new NumericUpDown
            {
                Location = new Point(inputX + 330, y),
                Width = 80,
                Minimum = 5,
                Maximum = 50,
                Value = 10,
                Increment = 5
            };
            _numDiscount.ValueChanged += (s, e) => UpdatePreview();

            pnlBody.Controls.Add(lblPromo);
            pnlBody.Controls.Add(_txtPromoCode);
            pnlBody.Controls.Add(lblDisc);
            pnlBody.Controls.Add(_numDiscount);
            y += 36;

            // Email Subject
            var lblSubj = new Label { Text = "Email Subject:", Location = new Point(labelX, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85) };
            _txtSubject = new TextBox
            {
                Location = new Point(inputX, y),
                Width = inputW
            };
            pnlBody.Controls.Add(lblSubj);
            pnlBody.Controls.Add(_txtSubject);
            y += 36;

            // Custom Note
            var lblNote = new Label { Text = "Custom Note:", Location = new Point(labelX, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139) };
            _txtCustomNotes = new TextBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                Height = 44,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _txtCustomNotes.TextChanged += (s, e) => UpdatePreview();
            pnlBody.Controls.Add(lblNote);
            pnlBody.Controls.Add(_txtCustomNotes);
            y += 54;

            // Preview Section
            var lblPrev = new Label
            {
                Text = "Email Live Preview (Delivered via SMTP):",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            pnlBody.Controls.Add(lblPrev);
            y += 22;

            _txtPreview = new TextBox
            {
                Location = new Point(labelX, y),
                Size = new Size(inputW + inputX - labelX, 230),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(51, 65, 85),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5F)
            };
            pnlBody.Controls.Add(_txtPreview);

            Controls.Add(pnlBody);

            // ── Bottom Action Footer ─────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 12, 24, 12)
            };

            _lblStatusFeedback = new Label
            {
                Text = "⚡ Ready to send via SMTP transport",
                AutoSize = true,
                Location = new Point(24, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            _btnPreviewBrowser = new Button
            {
                Text = "🌐 Preview HTML",
                Height = 34,
                Width = 130,
                Location = new Point(310, 12),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPreviewBrowser.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnPreviewBrowser.Click += OnPreviewBrowserClick;

            _btnSend = new Button
            {
                Text = "✉️ Send Win-Back Email",
                Height = 34,
                Width = 175,
                Location = new Point(448, 12),
                BackColor = Color.FromArgb(2, 132, 199), // CleanPro Ocean Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += async (s, e) => await OnSendClickAsync();

            _btnCancel = new Button
            {
                Text = "Cancel",
                Height = 34,
                Width = 80,
                Location = new Point(630, 12),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => Close();

            pnlFooter.Controls.Add(_lblStatusFeedback);
            pnlFooter.Controls.Add(_btnPreviewBrowser);
            pnlFooter.Controls.Add(_btnSend);
            pnlFooter.Controls.Add(_btnCancel);
            Controls.Add(pnlFooter);
        }

        private void UpdateTemplateFields()
        {
            switch (_cmbTemplate.SelectedIndex)
            {
                case 0: // 30-day
                    _txtSubject.Text = $"We Miss You at CleanPro! Enjoy {_numDiscount.Value}% Off Your Next Clean 🎁";
                    _txtPromoCode.Text = "WINBACK10";
                    _numDiscount.Value = 10;
                    break;
                case 1: // 60-day
                    _txtSubject.Text = $"Exclusive {_numDiscount.Value}% Loyalty Discount: Welcome Back to CleanPro!";
                    _txtPromoCode.Text = "SAVE10NOW";
                    _numDiscount.Value = 10;
                    break;
                case 2: // 90-day
                    _txtSubject.Text = $"We Want You Back! Special VIP 15% Care Voucher for {_customerName} ✨";
                    _txtPromoCode.Text = "VIPRETURN15";
                    _numDiscount.Value = 15;
                    break;
                case 3: // Contract renewal
                    _txtSubject.Text = "Notice: Service Agreement Renewal & Preferred Loyalty Rates";
                    _txtPromoCode.Text = "RENEW2026";
                    _numDiscount.Value = 10;
                    break;
                case 4: // Special loyalty
                    _txtSubject.Text = "CleanPro Client Appreciation Milestone Perk Inside";
                    _txtPromoCode.Text = "LOYALTY20";
                    _numDiscount.Value = 20;
                    break;
            }
            UpdatePreview();
        }

        private WinBackEmailRequest BuildRequest()
        {
            return new WinBackEmailRequest
            {
                CustomerId = _customerId,
                RecipientName = _customerName,
                RecipientEmail = _txtRecipientEmail.Text.Trim(),
                Subject = _txtSubject.Text.Trim(),
                PromoCode = _txtPromoCode.Text.Trim(),
                DiscountPercentage = (int)_numDiscount.Value,
                DaysInactive = _daysInactive,
                LastServiceType = _lastService,
                CampaignType = _cmbTemplate.SelectedItem?.ToString() ?? "Win-Back Outreach",
                CustomMessage = string.IsNullOrWhiteSpace(_txtCustomNotes.Text) ? null : _txtCustomNotes.Text.Trim(),
                ServiceLocation = _serviceLocation
            };
        }

        private void UpdatePreview()
        {
            var req = BuildRequest();
            var (_, plainText) = WinBackEmailTemplate.Render(req, "CleanPro Operations");
            _txtPreview.Text = plainText.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        private void OnPreviewBrowserClick(object? sender, EventArgs e)
        {
            try
            {
                var req = BuildRequest();
                var (html, _) = WinBackEmailTemplate.Render(req, "CleanPro Operations & Customer Care");
                var tempPath = Path.Combine(Path.GetTempPath(), $"CleanPro_WinBack_Preview_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, html);

                var psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to launch HTML browser preview: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task OnSendClickAsync()
        {
            var email = _txtRecipientEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid recipient email address (e.g. name@domain.com).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtRecipientEmail.Focus();
                return;
            }

            // Set loading state
            _btnSend.Enabled = false;
            _btnPreviewBrowser.Enabled = false;
            _btnCancel.Enabled = false;
            _btnSend.Text = "⏳ Sending via SMTP...";
            _lblStatusFeedback.Text = $"Connecting to SMTP server and transmitting to {email}...";
            _lblStatusFeedback.ForeColor = Color.FromArgb(2, 132, 199);
            Cursor = Cursors.WaitCursor;

            try
            {
                var req = BuildRequest();
                var result = await _api.SendWinBackEmailAsync(req);
                DispatchResult = result;

                if (result.IsSuccess)
                {
                    _lblStatusFeedback.Text = "✓ Email successfully delivered via SMTP!";
                    _lblStatusFeedback.ForeColor = Color.FromArgb(16, 149, 93);

                    MessageBox.Show(
                        $"Win-back re-engagement email successfully delivered via SMTP!\n\n" +
                        $"Recipient: {result.RecipientEmail}\n" +
                        $"Timestamp: {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n" +
                        $"Promo Code: {req.PromoCode} ({req.DiscountPercentage}% off)\n\n" +
                        $"Message: {result.Message}",
                        "SMTP Delivery Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblStatusFeedback.Text = "⚠️ SMTP delivery notice — see guidance";
                    _lblStatusFeedback.ForeColor = Color.FromArgb(220, 38, 38);

                    var builder = new System.Text.StringBuilder();
                    builder.AppendLine(result.Message);
                    builder.AppendLine();

                    if (!string.IsNullOrWhiteSpace(result.Diagnostics))
                    {
                        builder.AppendLine("💡 Configuration Guide:");
                        builder.AppendLine(result.Diagnostics);
                    }
                    else if (!string.IsNullOrWhiteSpace(result.ErrorDetails) && !result.ErrorDetails.Contains("at System."))
                    {
                        builder.AppendLine(result.ErrorDetails);
                    }

                    MessageBox.Show(
                        builder.ToString().TrimEnd(),
                        "CleanPro SMTP Delivery Notice",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error dispatching email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSend.Enabled = true;
                _btnPreviewBrowser.Enabled = true;
                _btnCancel.Enabled = true;
                _btnSend.Text = "✉️ Send Win-Back Email";
                Cursor = Cursors.Default;
            }
        }
    }
}
