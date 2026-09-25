using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Lead Capture Wizard — 2-Step form.
    ///
    /// Step 1: Contact Information (Name, Phone, Email, Lead Source)
    /// Step 2: Inquiry Details (Optional notes / service of interest)
    ///
    /// RULE: This form ONLY creates Lead records via POST /api/leads.
    ///       No DateTimePicker, no Service Type, no Staff Assignment.
    /// </summary>
    public partial class DataCollectionView : UserControl
    {
        // ============================================================
        // Cross-View Event Hook
        // ============================================================
        public event Action? RecordSaved;

        // ============================================================
        // State & API Client
        // ============================================================
        private readonly ApiClient _apiClient;
        private int _currentStep = 1;
        private bool _isModified;

        // ============================================================
        // UI Layout Containers
        // ============================================================
        private Panel _pnlScrollWrapper = null!;
        private Panel _pnlPageCenter = null!;
        private Panel _pnlCardWrapper = null!;
        private Panel _pnlStepIndicator = null!;
        private Panel _pnlHeader = null!;
        private Label _lblSectionTitle = null!;
        private Label _lblSectionSubtitle = null!;
        private Panel _pnlStepsHost = null!;
        private Panel _pnlNav = null!;

        // ============================================================
        // Wizard Step Panels — 2 steps only (no scheduling step)
        // ============================================================
        private Panel _pnlStep1 = null!;
        private Panel _pnlStep2 = null!;

        // Step 1: Contact Information
        private Label _lblFullNameTitle = null!;
        private TextBox _txtFullName = null!;
        private TextBox _txtContactInfo = null!;   // Phone
        private TextBox _txtEmail = null!;
        private ComboBox _cmbLeadSource = null!;

        private Label _lblErrorFullName = null!;
        private Label _lblErrorContactInfo = null!;
        private Label _lblErrorEmail = null!;
        private Label _lblErrorLeadSource = null!;

        // Step 2: Inquiry Details (optional)
        private TextBox _txtInquiryDetails = null!;
        private TextBox _txtServiceAddress = null!;
        private TextBox _txtQuotedPrice = null!;
        private Label _lblErrorServiceAddress = null!;
        private Label _lblErrorQuotedPrice = null!;
        private Label _lblErrorInquiryDetails = null!;

        // Navigation Buttons
        private Button _btnCancel = null!;
        private Button _btnBack = null!;
        private Button _btnNext = null!;
        private Button _btnSave = null!;

        // Toast Notification
        private Panel _pnlToast = null!;
        private Label _lblToast = null!;
        private System.Windows.Forms.Timer _toastTimer = null!;

        public DataCollectionView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            _apiClient = new ApiClient();
            InitializeComponent();
            ShowStep(1);
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Dock = DockStyle.Fill;
            DoubleBuffered = true;

            // Toast
            _pnlToast = new Panel
            {
                Size = new Size(380, 48),
                BackColor = Color.FromArgb(34, 197, 94),
                Visible = false,
                Padding = new Padding(16, 0, 16, 0)
            };
            _pnlToast.Paint += (s, e) =>
            {
                e.Graphics.Clear(_pnlToast.BackColor);
                using var pen = new Pen(Color.FromArgb(0, 0, 0, 30), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlToast.Width - 1, _pnlToast.Height - 1);
            };

            _lblToast = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 197, 94),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlToast.Controls.Add(_lblToast);
            Controls.Add(_pnlToast);

            _toastTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _toastTimer.Tick += (s, e) =>
            {
                _toastTimer.Stop();
                _pnlToast.Visible = false;
            };

            // Outer Scroll Wrapper
            _pnlScrollWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Background,
                Padding = new Padding(32, 20, 32, 20)
            };
            Controls.Add(_pnlScrollWrapper);
            _pnlToast.BringToFront();

            // Centered Page Container
            _pnlPageCenter = new Panel
            {
                BackColor = Theme.Background,
                Width = 720
            };
            _pnlScrollWrapper.Controls.Add(_pnlPageCenter);

            _pnlScrollWrapper.Resize += (s, e) => LayoutCenteredCard();
            Resize += (s, e) => LayoutToast();

            // Page Header
            _pnlHeader = new Panel
            {
                Height = 54,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 0, 4)
            };

            _lblSectionTitle = new Label
            {
                Text = "New Lead",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Background,
                Dock = DockStyle.Top,
                Height = 28
            };
            _pnlHeader.Controls.Add(_lblSectionTitle);

            _lblSectionSubtitle = new Label
            {
                Text = "Capture inquiry details. No scheduling — that happens in Work Orders.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Background,
                Dock = DockStyle.Top,
                Height = 22
            };
            _pnlHeader.Controls.Add(_lblSectionSubtitle);
            _lblSectionSubtitle.BringToFront();
            _pnlPageCenter.Controls.Add(_pnlHeader);

            // Step Indicator
            _pnlStepIndicator = new Panel
            {
                Height = 62,
                BackColor = Theme.Background
            };
            _pnlStepIndicator.Paint += PaintStepIndicator;
            _pnlPageCenter.Controls.Add(_pnlStepIndicator);

            // Card
            _pnlCardWrapper = new Panel
            {
                BackColor = Theme.Surface,
                Height = 480,
                Padding = new Padding(28, 20, 28, 20)
            };
            _pnlCardWrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(_pnlCardWrapper.BackColor);
                using var pen = new Pen(Theme.Border, 1);
                var rect = new Rectangle(0, 0, _pnlCardWrapper.Width - 1, _pnlCardWrapper.Height - 1);
                e.Graphics.FillRectangle(brush, rect);
                e.Graphics.DrawRectangle(pen, rect);
            };
            _pnlPageCenter.Controls.Add(_pnlCardWrapper);

            // Nav Bar
            _pnlNav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };
            _pnlCardWrapper.Controls.Add(_pnlNav);

            _btnCancel = new Button { Text = "Cancel", Width = 95, Height = 38 };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Location = new Point(0, 10);
            _btnCancel.Click += (s, e) => HandleCancel();
            _pnlNav.Controls.Add(_btnCancel);

            _btnBack = new Button { Text = "← Back", Width = 95, Height = 38 };
            Theme.ApplySecondaryButtonStyle(_btnBack);
            _btnBack.Location = new Point(0, 10);
            _btnBack.Visible = false;
            _btnBack.Click += (s, e) => ShowStep(_currentStep - 1);
            _pnlNav.Controls.Add(_btnBack);

            _btnNext = new Button { Text = "Continue →", Width = 140, Height = 38 };
            Theme.ApplyPrimaryButtonStyle(_btnNext);
            _btnNext.Click += (s, e) => OnNextClick();
            _pnlNav.Controls.Add(_btnNext);

            _btnSave = new Button { Text = "✓  Save Lead", Width = 160, Height = 38 };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Visible = false;
            _btnSave.Click += async (s, e) => await OnSaveClickAsync();
            _pnlNav.Controls.Add(_btnSave);

            _pnlNav.Resize += (s, e) =>
            {
                _btnNext.Location = new Point(_pnlNav.Width - _btnNext.Width, 10);
                _btnSave.Location = new Point(_pnlNav.Width - _btnSave.Width, 10);
            };

            // Step Host
            _pnlStepsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pnlCardWrapper.Controls.Add(_pnlStepsHost);
            _pnlStepsHost.BringToFront();

            BuildStep1();
            BuildStep2();

            LayoutCenteredCard();
        }

        private void LayoutCenteredCard()
        {
            if (_pnlScrollWrapper == null || _pnlPageCenter == null || _pnlCardWrapper == null) return;

            int clientW = _pnlScrollWrapper.ClientSize.Width;
            int targetW = Math.Min(740, clientW - 64);
            if (targetW < 480) targetW = 480;

            _pnlPageCenter.Width = targetW;
            _pnlPageCenter.Location = new Point(Math.Max(24, (clientW - targetW) / 2), 16);

            _pnlHeader.Location = new Point(0, 0);
            _pnlHeader.Width = targetW;
            _pnlHeader.Height = 54;

            _pnlStepIndicator.Location = new Point(0, _pnlHeader.Bottom + 4);
            _pnlStepIndicator.Width = targetW;
            _pnlStepIndicator.Height = 62;

            int cardTop = _pnlStepIndicator.Bottom + 8;
            int cardH = Math.Max(380, _pnlScrollWrapper.ClientSize.Height - cardTop - 32);

            _pnlCardWrapper.Location = new Point(0, cardTop);
            _pnlCardWrapper.Width = targetW;
            _pnlCardWrapper.Height = cardH;

            _pnlPageCenter.Height = cardTop + cardH + 20;

            if (_btnNext != null && _pnlNav != null)
            {
                _btnNext.Location = new Point(_pnlNav.Width - _btnNext.Width, 10);
                _btnSave.Location = new Point(_pnlNav.Width - _btnSave.Width, 10);
            }
        }

        private void LayoutToast()
        {
            if (_pnlToast != null)
                _pnlToast.Location = new Point(Math.Max(10, Width - _pnlToast.Width - 28), 16);
        }

        private static Button CreateNavButton(string text, Color bg, Color fg, int width)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ============================================================
        // STEP 1: Contact Information
        // Fields: Customer Type, Full Name, Phone, Email, Lead Source
        // ============================================================
        private void BuildStep1()
        {
            _pnlStep1 = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            int row = 0;

            // SECTION: CONTACT INFORMATION
            var lblSecContact = new Label
            {
                Text = "CONTACT INFORMATION",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.White,
                Height = 22,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4)
            };
            tlp.Controls.Add(lblSecContact, 0, row++);

            // Phone Number
            _txtContactInfo = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 20,
                PlaceholderText = "e.g. 0917-123-4567 or +63 917 123 4567"
            };
            _txtContactInfo.TextChanged += (s, e) => { ClearError(_txtContactInfo, _lblErrorContactInfo); _isModified = true; };
            _txtContactInfo.KeyPress += (s, e) =>
            {
                // Disallow letters - allow control keys (backspace, delete) and valid phone characters
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-' && e.KeyChar != ' ' && e.KeyChar != '(' && e.KeyChar != ')')
                {
                    e.Handled = true;
                }
            };
            var pnlContact = CreateFieldGroup("Phone Number", true, _txtContactInfo, out _lblErrorContactInfo, 30, Color.White);
            tlp.Controls.Add(pnlContact, 0, row++);

            // Email Address
            _txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 120,
                PlaceholderText = "name@example.com"
            };
            _txtEmail.TextChanged += (s, e) => { ClearError(_txtEmail, _lblErrorEmail); _isModified = true; };
            var pnlEmail = CreateFieldGroup("Email Address", false, _txtEmail, out _lblErrorEmail, 30, Color.White);
            tlp.Controls.Add(pnlEmail, 0, row++);

            // SECTION: LEAD DETAILS
            var lblSecDetails = new Label
            {
                Text = "LEAD DETAILS",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.White,
                Height = 26,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 10, 0, 4)
            };
            tlp.Controls.Add(lblSecDetails, 0, row++);

            // Full Name
            _lblFullNameTitle = new Label
            {
                Text = "Lead Name *",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                Location = new Point(0, 0),
                Size = new Size(740, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _txtFullName = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 100,
                PlaceholderText = "e.g. Jane Doe"
            };
            _txtFullName.TextChanged += (s, e) => { ClearError(_txtFullName, _lblErrorFullName); _isModified = true; };
            var pnlFullName = CreateFieldGroup("Lead Name", true, _txtFullName, out _lblErrorFullName, 30, Color.White, _lblFullNameTitle);
            tlp.Controls.Add(pnlFullName, 0, row++);

            // Lead Source
            _cmbLeadSource = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbLeadSource.Items.AddRange(new object[]
            {
                "Website", "Facebook", "Referral", "Walk-in", "Google Ads", "Other"
            });
            _cmbLeadSource.SelectedIndex = 0;
            _cmbLeadSource.SelectedIndexChanged += (s, e) => { ClearError(_cmbLeadSource, _lblErrorLeadSource); _isModified = true; };
            var pnlSource = CreateFieldGroup("Lead Source", true, _cmbLeadSource, out _lblErrorLeadSource, 30);
            tlp.Controls.Add(pnlSource, 0, row++);

            _pnlStep1.Controls.Add(tlp);
            _pnlStepsHost.Controls.Add(_pnlStep1);
        }

        // ============================================================
        // STEP 2: Inquiry Details (optional notes)
        // ============================================================
        private void BuildStep2()
        {
            _pnlStep2 = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Visible = false
            };

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Info banner — clarify this is lead-only
            var pnlBanner = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 52,
                BackColor = Color.FromArgb(239, 246, 255),
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlBanner.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(147, 197, 253), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlBanner.Width - 1, pnlBanner.Height - 1);
            };
            var lblBanner = new Label
            {
                Text = "ℹ  Leads capture inquiry intent only. To schedule a service, convert this lead to a Customer first, then create a Work Order.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.FromArgb(239, 246, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };
            pnlBanner.Controls.Add(lblBanner);
            tlp.Controls.Add(pnlBanner, 0, 0);

            // Service Address (Optional)
            _txtServiceAddress = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 250,
                PlaceholderText = "e.g. 123 Main St, Suite 400"
            };
            _txtServiceAddress.TextChanged += (s, e) => { ClearError(_txtServiceAddress, _lblErrorServiceAddress); _isModified = true; };
            var pnlAddress = CreateFieldGroup("Service Location / Address (Optional)", false, _txtServiceAddress, out _lblErrorServiceAddress, 30);
            tlp.Controls.Add(pnlAddress, 0, 1);

            // Quoted Price (Optional)
            _txtQuotedPrice = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 15,
                PlaceholderText = "e.g. 2500.00"
            };
            _txtQuotedPrice.TextChanged += (s, e) => { ClearError(_txtQuotedPrice, _lblErrorQuotedPrice); _isModified = true; };
            _txtQuotedPrice.KeyPress += (s, e) =>
            {
                // Disallow non-numeric characters (allow digits, '.', and control keys)
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                }
                // Disallow multiple decimal points
                if (e.KeyChar == '.' && _txtQuotedPrice.Text.Contains('.'))
                {
                    e.Handled = true;
                }
            };
            var pnlPrice = CreateFieldGroup("Initial Quoted Price (₱, Optional)", false, _txtQuotedPrice, out _lblErrorQuotedPrice, 30);
            tlp.Controls.Add(pnlPrice, 0, 2);

            // Inquiry Notes
            _txtInquiryDetails = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500,
                PlaceholderText = "Optional — any specific requirements the lead mentioned during inquiry"
            };
            _txtInquiryDetails.TextChanged += (s, e) => { ClearError(_txtInquiryDetails, _lblErrorInquiryDetails); _isModified = true; };
            var pnlNotes = CreateFieldGroup("Inquiry Notes (Optional)", false, _txtInquiryDetails, out _lblErrorInquiryDetails, 90);
            tlp.Controls.Add(pnlNotes, 0, 3);

            _pnlStep2.Controls.Add(tlp);
            _pnlStepsHost.Controls.Add(_pnlStep2);
        }

        // ============================================================
        // Field Group Helper
        // ============================================================
        private static Panel CreateFieldGroup(string labelText, bool isRequired, Control inputControl, out Label errorLabel, int inputHeight, Color? bg = null, Label? externalTitleLabel = null, string? hintText = null)
        {
            Color backColor = bg ?? Color.White;
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                Height = 22 + inputHeight + 20,
                BackColor = backColor
            };

            var lbl = externalTitleLabel ?? new Label
            {
                Text = isRequired ? labelText + " *" : labelText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = backColor,
                Location = new Point(0, 0),
                Size = new Size(panel.Width, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(lbl);

            if (!string.IsNullOrEmpty(hintText))
            {
                var lblHint = new Label
                {
                    Text = hintText,
                    Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    BackColor = backColor,
                    TextAlign = ContentAlignment.MiddleRight,
                    Location = new Point(panel.Width - 240, 0),
                    Size = new Size(240, 20),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                panel.Controls.Add(lblHint);
                lblHint.BringToFront();
            }

            inputControl.Location = new Point(0, 22);
            inputControl.Size = new Size(panel.Width, inputHeight);
            inputControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            errorLabel = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 38, 38),
                BackColor = backColor,
                Location = new Point(0, 22 + inputHeight + 2),
                Size = new Size(panel.Width, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };

            panel.Controls.Add(inputControl);
            panel.Controls.Add(errorLabel);

            return panel;
        }

        // ============================================================
        // Step Indicator (2 steps: Contact, Inquiry)
        // ============================================================
        private void PaintStepIndicator(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_pnlStepIndicator.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _pnlStepIndicator.Width;
            int circleSize = 32;
            int circleRadius = circleSize / 2;
            int circleY = 6;
            int centerY = circleY + circleRadius;

            int[] xCenters = { w / 3, 2 * w / 3 };
            string[] labels = { "Contact Info", "Inquiry Details" };

            Color activeColor = Theme.Primary;
            Color completedColor = Color.FromArgb(34, 197, 94);
            Color inactiveBg = Color.FromArgb(241, 245, 249);
            Color inactiveBorder = Color.FromArgb(203, 213, 225);
            Color inactiveFg = Color.FromArgb(100, 116, 139);
            Color lineColor = Color.FromArgb(226, 232, 240);

            using var penCompleted = new Pen(completedColor, 2);
            using var penInactive = new Pen(lineColor, 2);

            int x1Right = xCenters[0] + circleRadius + 8;
            int x2Left = xCenters[1] - circleRadius - 8;
            var line1Pen = _currentStep > 1 ? penCompleted : penInactive;
            g.DrawLine(line1Pen, x1Right, centerY, x2Left, centerY);

            using var fontNumber = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var fontLabel = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var brushWhite = new SolidBrush(Color.White);
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < 2; i++)
            {
                int stepNum = i + 1;
                int cx = xCenters[i];
                var circleRect = new Rectangle(cx - circleRadius, circleY, circleSize, circleSize);

                if (stepNum < _currentStep)
                {
                    using var brush = new SolidBrush(completedColor);
                    g.FillEllipse(brush, circleRect);
                    g.DrawString("✓", fontNumber, brushWhite, circleRect, sfCenter);
                    using var brushLbl = new SolidBrush(Theme.TextDark);
                    g.DrawString(labels[i], fontLabel, brushLbl, new Rectangle(cx - 70, circleY + circleSize + 4, 140, 20), sfCenter);
                }
                else if (stepNum == _currentStep)
                {
                    using var brush = new SolidBrush(activeColor);
                    g.FillEllipse(brush, circleRect);
                    g.DrawString(stepNum.ToString(), fontNumber, brushWhite, circleRect, sfCenter);
                    using var brushLbl = new SolidBrush(activeColor);
                    g.DrawString(labels[i], fontLabel, brushLbl, new Rectangle(cx - 70, circleY + circleSize + 4, 140, 20), sfCenter);
                }
                else
                {
                    using var brush = new SolidBrush(inactiveBg);
                    using var pen = new Pen(inactiveBorder, 1);
                    using var brushText = new SolidBrush(inactiveFg);
                    g.FillEllipse(brush, circleRect);
                    g.DrawEllipse(pen, circleRect);
                    g.DrawString(stepNum.ToString(), fontNumber, brushText, circleRect, sfCenter);
                    g.DrawString(labels[i], fontLabel, brushText, new Rectangle(cx - 70, circleY + circleSize + 4, 140, 20), sfCenter);
                }
            }
        }

        // ============================================================
        // Navigation & Step Control
        // ============================================================
        private void ShowStep(int step)
        {
            if (step < 1 || step > 2) return;

            _currentStep = step;
            ClearValidationErrors();

            _pnlStep1.Visible = (_currentStep == 1);
            _pnlStep2.Visible = (_currentStep == 2);

            switch (_currentStep)
            {
                case 1:
                    _lblSectionTitle.Text = "Contact Information";
                    _lblSectionSubtitle.Text = "Provide lead name, phone, email, and source";
                    _btnCancel.Visible = true;
                    _btnBack.Visible = false;
                    _btnNext.Visible = true;
                    _btnSave.Visible = false;
                    break;

                case 2:
                    _lblSectionTitle.Text = "Inquiry Details";
                    _lblSectionSubtitle.Text = "Optional notes about what the lead is looking for";
                    _btnCancel.Visible = false;
                    _btnBack.Visible = true;
                    _btnNext.Visible = false;
                    _btnSave.Visible = true;
                    break;
            }

            _pnlStepIndicator.Invalidate();
        }

        private void OnNextClick()
        {
            if (ValidateCurrentStep())
                ShowStep(_currentStep + 1);
        }

        private void HandleCancel()
        {
            if (_isModified)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Discard Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            ClearForm();
            ShowStep(1);

            if (FindForm() is NewLeadDialog modal)
            {
                modal.DialogResult = DialogResult.Cancel;
                modal.Close();
            }
        }

        // ============================================================
        // Validation
        // ============================================================
        private bool ValidateCurrentStep()
        {
            bool isValid = true;
            ClearValidationErrors();

            if (_currentStep == 1)
            {
                // Full Name validation
                if (!ValidationHelper.IsValidName(_txtFullName.Text, 100, out var nameErr))
                {
                    SetError(_txtFullName, _lblErrorFullName, nameErr);
                    isValid = false;
                }

                // Phone & Email validation
                bool hasPhone = !string.IsNullOrWhiteSpace(_txtContactInfo.Text);
                bool hasEmail = !string.IsNullOrWhiteSpace(_txtEmail.Text);

                if (!hasPhone && !hasEmail)
                {
                    SetError(_txtContactInfo, _lblErrorContactInfo, "⚠ Please provide either a phone number or email address.");
                    SetError(_txtEmail, _lblErrorEmail, "⚠ Please provide either a phone number or email address.");
                    isValid = false;
                }
                else
                {
                    if (hasPhone && !ValidationHelper.IsValidPhoneNumber(_txtContactInfo.Text, false, out var phoneErr))
                    {
                        SetError(_txtContactInfo, _lblErrorContactInfo, phoneErr);
                        isValid = false;
                    }

                    if (hasEmail && !ValidationHelper.IsValidEmail(_txtEmail.Text, false, out var emailErr))
                    {
                        SetError(_txtEmail, _lblErrorEmail, emailErr);
                        isValid = false;
                    }
                }

                if (_cmbLeadSource.SelectedIndex < 0)
                {
                    SetError(_cmbLeadSource, _lblErrorLeadSource, "⚠ Please select a Lead Source.");
                    isValid = false;
                }
            }
            else if (_currentStep == 2)
            {
                // Service Address length validation
                if (!string.IsNullOrWhiteSpace(_txtServiceAddress?.Text) &&
                    !ValidationHelper.IsValidTextLength(_txtServiceAddress.Text, "Service Location", 250, false, out var addrErr))
                {
                    SetError(_txtServiceAddress, _lblErrorServiceAddress, addrErr);
                    isValid = false;
                }

                // Quoted Price validation
                if (!string.IsNullOrWhiteSpace(_txtQuotedPrice?.Text) &&
                    !ValidationHelper.IsValidPrice(_txtQuotedPrice.Text, false, out _, out var priceErr))
                {
                    SetError(_txtQuotedPrice, _lblErrorQuotedPrice, priceErr);
                    isValid = false;
                }

                // Inquiry Notes length validation
                if (!string.IsNullOrWhiteSpace(_txtInquiryDetails?.Text) &&
                    !ValidationHelper.IsValidTextLength(_txtInquiryDetails.Text, "Inquiry Notes", 500, false, out var notesErr))
                {
                    SetError(_txtInquiryDetails, _lblErrorInquiryDetails, notesErr);
                    isValid = false;
                }
            }

            return isValid;
        }

        private static void SetError(Control control, Label errorLabel, string message)
        {
            control.BackColor = Color.FromArgb(254, 226, 226);
            errorLabel.Text = message;
            errorLabel.Visible = true;
        }

        private static void ClearError(Control control, Label errorLabel)
        {
            control.BackColor = Color.White;
            errorLabel.Text = string.Empty;
            errorLabel.Visible = false;
        }

        private void ClearValidationErrors()
        {
            if (_txtFullName != null) ClearError(_txtFullName, _lblErrorFullName);
            if (_txtContactInfo != null) ClearError(_txtContactInfo, _lblErrorContactInfo);
            if (_txtEmail != null) ClearError(_txtEmail, _lblErrorEmail);
            if (_cmbLeadSource != null) ClearError(_cmbLeadSource, _lblErrorLeadSource);
            if (_txtServiceAddress != null) ClearError(_txtServiceAddress, _lblErrorServiceAddress);
            if (_txtQuotedPrice != null) ClearError(_txtQuotedPrice, _lblErrorQuotedPrice);
            if (_txtInquiryDetails != null) ClearError(_txtInquiryDetails, _lblErrorInquiryDetails);
        }

        // ============================================================
        // Save — POST /api/leads (inquiry only, no scheduling)
        // ============================================================
        private async Task OnSaveClickAsync()
        {
            if (!ValidateCurrentStep())
                return;

            _btnBack.Enabled = false;
            _btnSave.Enabled = false;
            _btnSave.Text = "⏳ Saving...";

            try
            {
                decimal? quotedPrice = null;
                if (!string.IsNullOrWhiteSpace(_txtQuotedPrice?.Text) && decimal.TryParse(_txtQuotedPrice.Text.Trim(), out var parsedPrice))
                {
                    quotedPrice = parsedPrice;
                }

                var dto = new LeadCreateDto
                {
                    LeadName       = _txtFullName.Text.Trim(),
                    Phone          = _txtContactInfo.Text.Trim(),
                    Email          = _txtEmail.Text.Trim(),
                    LeadSource     = _cmbLeadSource.SelectedItem?.ToString() ?? string.Empty,
                    ServiceAddress = string.IsNullOrWhiteSpace(_txtServiceAddress?.Text) ? null : _txtServiceAddress.Text.Trim(),
                    QuotedPrice    = quotedPrice,
                    InquiryDetails = string.IsNullOrWhiteSpace(_txtInquiryDetails?.Text)
                                       ? null
                                       : _txtInquiryDetails.Text.Trim()
                };

                var (success, message, _) = await _apiClient.CreateLeadAsync(dto);

                if (success)
                {
                    ShowToast("Lead captured successfully!", true);
                    ClearForm();
                    ShowStep(1);
                    RecordSaved?.Invoke();

                    if (FindForm() is NewLeadDialog modal)
                    {
                        modal.DialogResult = DialogResult.OK;
                        modal.Close();
                    }
                }
                else
                {
                    ShowToast(message, false);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Error: {ex.Message}", false);
            }
            finally
            {
                _btnBack.Enabled = true;
                _btnSave.Enabled = true;
                _btnSave.Text = "✓ Save Lead";
            }
        }

        // ============================================================
        // Clear Form
        // ============================================================
        public void ClearForm()
        {
            _txtFullName?.Clear();
            _txtContactInfo?.Clear();
            _txtEmail?.Clear();
            if (_cmbLeadSource?.Items.Count > 0) _cmbLeadSource.SelectedIndex = 0;
            _txtServiceAddress?.Clear();
            _txtQuotedPrice?.Clear();
            _txtInquiryDetails?.Clear();

            ClearValidationErrors();
            _isModified = false;
        }

        // ============================================================
        // Role-Based Permissions
        // ============================================================
        public void ApplyViewPermissions(string userRole)
        {
            bool canSave = (userRole == Roles.SalesStaff);
            if (_btnSave != null)
            {
                _btnSave.Enabled = canSave;
                if (!canSave)
                {
                    _btnSave.BackColor = Color.FromArgb(226, 232, 240);
                    _btnSave.ForeColor = Color.FromArgb(148, 163, 184);
                    _btnSave.Cursor = Cursors.Default;
                }
                else
                {
                    _btnSave.BackColor = Color.FromArgb(34, 197, 94);
                    _btnSave.ForeColor = Color.White;
                    _btnSave.Cursor = Cursors.Hand;
                }
            }
        }

        // ============================================================
        // Toast Notification
        // ============================================================
        private void ShowToast(string message, bool isSuccess)
        {
            _toastTimer.Stop();
            var bg = isSuccess ? Color.FromArgb(34, 197, 94) : Color.FromArgb(220, 38, 38);
            _pnlToast.BackColor = bg;
            _lblToast.BackColor = bg;
            _lblToast.Text = (isSuccess ? "✓  " : "⚠  ") + message;
            _pnlToast.Location = new Point(Math.Max(10, Width - _pnlToast.Width - 28), 16);
            _pnlToast.Visible = true;
            _pnlToast.BringToFront();
            _toastTimer.Start();
        }

        // ============================================================
        // Keyboard Shortcuts
        // ============================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                var focused = FindActiveControl(this);
                if (focused is TextBox tb && tb.Multiline)
                    return base.ProcessCmdKey(ref msg, keyData);

                if (_currentStep < 2)
                {
                    if (_btnNext.Visible && _btnNext.Enabled)
                    {
                        OnNextClick();
                        return true;
                    }
                }
                else if (_currentStep == 2)
                {
                    if (_btnSave.Visible && _btnSave.Enabled)
                    {
                        _ = OnSaveClickAsync();
                        return true;
                    }
                }
            }
            else if (keyData == Keys.Escape)
            {
                HandleCancel();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static Control? FindActiveControl(Control? root)
        {
            if (root is ContainerControl cc && cc.ActiveControl != null)
                return FindActiveControl(cc.ActiveControl);
            return root;
        }
    }
}