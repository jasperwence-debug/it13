using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms;

namespace App.WinForms.Views
{
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
        private Panel _pnlCardWrapper = null!;
        private Panel _pnlStepIndicator = null!;
        private Panel _pnlHeader = null!;
        private Label _lblSectionTitle = null!;
        private Label _lblSectionSubtitle = null!;
        private Panel _pnlStepsHost = null!;
        private Panel _pnlNav = null!;

        // ============================================================
        // Wizard Step Panels
        // ============================================================
        private Panel _pnlStep1 = null!;
        private Panel _pnlStep2 = null!;
        private Panel _pnlStep3 = null!;

        // Step 1: Client Profile (Who)
        private RadioButton _rdoIndividual = null!;
        private RadioButton _rdoCompany = null!;
        private Label _lblFullNameTitle = null!;
        private TextBox _txtFullName = null!;
        private TextBox _txtContactInfo = null!;
        private ComboBox _cmbLeadSource = null!;

        private Label _lblErrorFullName = null!;
        private Label _lblErrorContactInfo = null!;
        private Label _lblErrorLeadSource = null!;

        // Step 2: Service & Location (What & Where)
        private ComboBox _cmbServiceRequested = null!;
        private TextBox _txtStreet = null!;
        private ComboBox _cmbCity = null!;
        private TextBox _txtLandmark = null!;
        private TextBox _txtSpecialRequests = null!;

        private Label _lblErrorServiceRequested = null!;
        private Label _lblErrorStreet = null!;
        private Label _lblErrorCity = null!;

        // Step 3: Schedule & Dispatch (When)
        private DateTimePicker _dtpPreferredDate = null!;
        private DateTimePicker _dtpFollowUpDate = null!;
        private ComboBox _cmbAssignedStaff = null!;
        private TextBox _txtNotes = null!;

        private Label _lblErrorPreferredDate = null!;
        private Label _lblErrorAssignedStaff = null!;

        // Navigation Buttons
        private Button _btnCancel = null!;
        private Button _btnBack = null!;
        private Button _btnNext = null!;
        private Button _btnSave = null!;

        // Toast Notification
        private Panel _pnlToast = null!;
        private Label _lblToast = null!;
        private System.Windows.Forms.Timer _toastTimer = null!;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: Prevents repaint bleed & ghosting
                return cp;
            }
        }

        public DataCollectionView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            _apiClient = new ApiClient();
            InitializeComponent();
            ShowStep(1);
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 252); // #F8FAFC
            Dock = DockStyle.Fill;
            DoubleBuffered = true;

            // Toast Floating Notification (Top-right corner)
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
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 20, 24, 20)
            };
            Controls.Add(_pnlScrollWrapper);
            _pnlToast.BringToFront();

            // Centered Card Container (Max 800px width)
            _pnlCardWrapper = new Panel
            {
                BackColor = Color.White,
                Width = 800,
                Height = 700
            };
            _pnlCardWrapper.Paint += (s, e) =>
            {
                e.Graphics.Clear(_pnlCardWrapper.BackColor);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlCardWrapper.Width - 1, _pnlCardWrapper.Height - 1);
            };
            _pnlScrollWrapper.Controls.Add(_pnlCardWrapper);

            _pnlScrollWrapper.Resize += (s, e) => LayoutCenteredCard();
            Resize += (s, e) => LayoutToast();

            // -------------------------------------------------------------
            // Step Indicator Header (Top)
            // -------------------------------------------------------------
            _pnlStepIndicator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.White
            };
            _pnlStepIndicator.Paint += PaintStepIndicator;
            _pnlCardWrapper.Controls.Add(_pnlStepIndicator);

            var pnlDiv1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            _pnlCardWrapper.Controls.Add(pnlDiv1);

            // -------------------------------------------------------------
            // Section Title & Subtitle Header
            // -------------------------------------------------------------
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(28, 12, 28, 4)
            };

            _lblSectionTitle = new Label
            {
                Text = "Client Profile",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28
            };
            _pnlHeader.Controls.Add(_lblSectionTitle);

            _lblSectionSubtitle = new Label
            {
                Text = "Select client type and provide contact details",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22
            };
            _pnlHeader.Controls.Add(_lblSectionSubtitle);
            _lblSectionSubtitle.BringToFront();
            _pnlCardWrapper.Controls.Add(_pnlHeader);

            var pnlDiv2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            _pnlCardWrapper.Controls.Add(pnlDiv2);

            // -------------------------------------------------------------
            // Bottom Navigation Bar
            // -------------------------------------------------------------
            _pnlNav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(28, 15, 28, 15)
            };
            _pnlNav.Paint += (s, e) =>
            {
                e.Graphics.Clear(_pnlNav.BackColor);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(pen, 0, 0, _pnlNav.Width, 0);
            };
            _pnlCardWrapper.Controls.Add(_pnlNav);

            // Left Navigation Buttons
            _btnCancel = CreateNavButton("Cancel", Color.FromArgb(226, 232, 240), Color.FromArgb(51, 65, 85), 120);
            _btnCancel.Location = new Point(28, 15);
            _btnCancel.Click += (s, e) => HandleCancel();
            _pnlNav.Controls.Add(_btnCancel);

            _btnBack = CreateNavButton("← Back", Color.FromArgb(226, 232, 240), Color.FromArgb(51, 65, 85), 120);
            _btnBack.Location = new Point(28, 15);
            _btnBack.Visible = false;
            _btnBack.Click += (s, e) => ShowStep(_currentStep - 1);
            _pnlNav.Controls.Add(_btnBack);

            // Right Navigation Buttons
            _btnNext = CreateNavButton("Next →", Color.FromArgb(59, 130, 246), Color.White, 120);
            _btnNext.Click += (s, e) => OnNextClick();
            _pnlNav.Controls.Add(_btnNext);

            _btnSave = CreateNavButton("✓ Save Customer", Color.FromArgb(34, 197, 94), Color.White, 150);
            _btnSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnSave.Visible = false;
            _btnSave.Click += async (s, e) => await OnSaveClickAsync();
            _pnlNav.Controls.Add(_btnSave);

            _pnlNav.Resize += (s, e) =>
            {
                _btnNext.Location = new Point(_pnlNav.Width - _btnNext.Width - 28, 15);
                _btnSave.Location = new Point(_pnlNav.Width - _btnSave.Width - 28, 15);
            };

            // -------------------------------------------------------------
            // Step Content Host Panel
            // -------------------------------------------------------------
            _pnlStepsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pnlCardWrapper.Controls.Add(_pnlStepsHost);
            _pnlStepsHost.BringToFront();

            BuildStep1();
            BuildStep2();
            BuildStep3();

            LayoutCenteredCard();
        }

        private void LayoutCenteredCard()
        {
            if (_pnlScrollWrapper == null || _pnlCardWrapper == null) return;

            int clientW = _pnlScrollWrapper.ClientSize.Width;
            int clientH = _pnlScrollWrapper.ClientSize.Height;

            int cardW = Math.Min(800, clientW - 48);
            if (cardW < 440) cardW = 440;

            int cardH = Math.Max(660, clientH - 40);

            _pnlCardWrapper.Size = new Size(cardW, cardH);
            _pnlCardWrapper.Location = new Point(Math.Max(24, (clientW - cardW) / 2), 20);

            _btnNext.Location = new Point(_pnlNav.Width - _btnNext.Width - 28, 15);
            _btnSave.Location = new Point(_pnlNav.Width - _btnSave.Width - 28, 15);
        }

        private void LayoutToast()
        {
            if (_pnlToast != null)
            {
                _pnlToast.Location = new Point(Math.Max(10, Width - _pnlToast.Width - 28), 16);
            }
        }

        private static Button CreateNavButton(string text, Color bg, Color fg, int width)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ============================================================
        // STEP 1: Client Profile (Who)
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
                RowCount = 4,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 1. Customer Type Segmented Toggle [ Individual ] [ Company ]
            var pnlToggle = CreateSegmentedToggle();
            tlp.Controls.Add(pnlToggle, 0, 0);

            // 2. Full Name
            _lblFullNameTitle = new Label
            {
                Text = "Customer Name *",
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
                PlaceholderText = "Customer Name"
            };
            _txtFullName.TextChanged += (s, e) => { ClearError(_txtFullName, _lblErrorFullName); _isModified = true; };
            var pnlFullName = CreateFieldGroup("Customer Name", true, _txtFullName, out _lblErrorFullName, 30, Color.White, _lblFullNameTitle);
            tlp.Controls.Add(pnlFullName, 0, 1);

            // 3. Contact Info
            _txtContactInfo = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 150,
                PlaceholderText = "Mobile number or work email"
            };
            _txtContactInfo.TextChanged += (s, e) => { ClearError(_txtContactInfo, _lblErrorContactInfo); _isModified = true; };
            var pnlContact = CreateFieldGroup("Contact Information", true, _txtContactInfo, out _lblErrorContactInfo, 30);
            tlp.Controls.Add(pnlContact, 0, 2);

            // 4. Lead Source
            _cmbLeadSource = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbLeadSource.Items.AddRange(new object[]
            {
                "Facebook", "Website", "Referral", "Walk-in", "Google Ads", "Other"
            });
            _cmbLeadSource.SelectedIndexChanged += (s, e) => { ClearError(_cmbLeadSource, _lblErrorLeadSource); _isModified = true; };
            var pnlSource = CreateFieldGroup("Lead Source", true, _cmbLeadSource, out _lblErrorLeadSource, 30);
            tlp.Controls.Add(pnlSource, 0, 3);

            _pnlStep1.Controls.Add(tlp);
            _pnlStepsHost.Controls.Add(_pnlStep1);
        }

        private Panel CreateSegmentedToggle()
        {
            var pnlField = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 66,
                Margin = new Padding(0, 4, 0, 10),
                BackColor = Color.White
            };

            var lblToggleTitle = new Label
            {
                Text = "Customer Type *",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.White,
                Location = new Point(0, 0),
                Size = new Size(pnlField.Width, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlField.Controls.Add(lblToggleTitle);

            var container = new Panel
            {
                Location = new Point(0, 22),
                Size = new Size(pnlField.Width, 38),
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(3),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            container.Paint += (s, e) =>
            {
                e.Graphics.Clear(container.BackColor);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, container.Width - 1, container.Height - 1);
            };

            _rdoIndividual = new RadioButton
            {
                Text = "👤  Individual",
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = true,
                Cursor = Cursors.Hand
            };
            _rdoIndividual.FlatAppearance.BorderSize = 0;

            _rdoCompany = new RadioButton
            {
                Text = "🏢  Company",
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = false,
                Cursor = Cursors.Hand
            };
            _rdoCompany.FlatAppearance.BorderSize = 0;

            void UpdateToggleStyles()
            {
                if (_rdoIndividual.Checked)
                {
                    _rdoIndividual.BackColor = Color.FromArgb(59, 130, 246);
                    _rdoIndividual.ForeColor = Color.White;
                    _rdoCompany.BackColor = Color.FromArgb(241, 245, 249);
                    _rdoCompany.ForeColor = Color.FromArgb(71, 85, 105);

                    if (_txtFullName != null) _txtFullName.PlaceholderText = "Customer Name";
                    if (_lblFullNameTitle != null) _lblFullNameTitle.Text = "Customer Name *";
                }
                else
                {
                    _rdoCompany.BackColor = Color.FromArgb(59, 130, 246);
                    _rdoCompany.ForeColor = Color.White;
                    _rdoIndividual.BackColor = Color.FromArgb(241, 245, 249);
                    _rdoIndividual.ForeColor = Color.FromArgb(71, 85, 105);

                    if (_txtFullName != null) _txtFullName.PlaceholderText = "Company / Org Name";
                    if (_lblFullNameTitle != null) _lblFullNameTitle.Text = "Company / Org Name *";
                }
            }

            _rdoIndividual.CheckedChanged += (s, e) => { UpdateToggleStyles(); _isModified = true; };
            _rdoCompany.CheckedChanged += (s, e) => { UpdateToggleStyles(); _isModified = true; };

            container.Resize += (s, e) =>
            {
                int halfWidth = (container.ClientSize.Width - 6) / 2;
                _rdoIndividual.Size = new Size(halfWidth, container.ClientSize.Height - 6);
                _rdoIndividual.Location = new Point(3, 3);
                _rdoCompany.Size = new Size(halfWidth, container.ClientSize.Height - 6);
                _rdoCompany.Location = new Point(3 + halfWidth, 3);
            };

            container.Controls.Add(_rdoIndividual);
            container.Controls.Add(_rdoCompany);
            pnlField.Controls.Add(container);

            UpdateToggleStyles();
            return pnlField;
        }

        // ============================================================
        // STEP 2: Service & Location (What & Where)
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
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 1. Service Requested
            _cmbServiceRequested = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbServiceRequested.Items.AddRange(new object[]
            {
                "General Cleaning", "Deep Cleaning", "Office Cleaning", "Move-in Cleaning", "Move-out Cleaning"
            });
            _cmbServiceRequested.SelectedIndexChanged += (s, e) => { ClearError(_cmbServiceRequested, _lblErrorServiceRequested); _isModified = true; };
            var pnlService = CreateFieldGroup("Service Requested", true, _cmbServiceRequested, out _lblErrorServiceRequested, 30);
            tlp.Controls.Add(pnlService, 0, 0);

            // 2. Service Location Container (Structured Card Layout)
            var pnlLocationCard = CreateLocationSubCard();
            tlp.Controls.Add(pnlLocationCard, 0, 1);

            // 3. Special Cleaning Requests (replaces redundant inquiry fields)
            _txtSpecialRequests = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500,
                PlaceholderText = "Specific instructions, special areas to focus on, or crew requirements"
            };
            _txtSpecialRequests.TextChanged += (s, e) => _isModified = true;
            var pnlRequests = CreateFieldGroup("Special Cleaning Requests", false, _txtSpecialRequests, out _, 75);
            tlp.Controls.Add(pnlRequests, 0, 2);

            _pnlStep2.Controls.Add(tlp);
            _pnlStepsHost.Controls.Add(_pnlStep2);
        }

        private Panel CreateLocationSubCard()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 12, 16, 14),
                Margin = new Padding(0, 6, 0, 12)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.Clear(card.BackColor);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblCardHeader = new Label
            {
                Text = "📍  SERVICE LOCATION DETAILS",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock = DockStyle.Top,
                Height = 22
            };
            card.Controls.Add(lblCardHeader);

            var tlpLoc = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(0, 4, 0, 0)
            };
            tlpLoc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpLoc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Street / Unit / Building (Full width across 2 columns)
            _txtStreet = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 150,
                PlaceholderText = "Street address, building, or unit no."
            };
            _txtStreet.TextChanged += (s, e) => { ClearError(_txtStreet, _lblErrorStreet); _isModified = true; };
            var pnlStreet = CreateFieldGroup("Street / Unit / Building", true, _txtStreet, out _lblErrorStreet, 30, Color.FromArgb(248, 250, 252));
            tlpLoc.Controls.Add(pnlStreet, 0, 0);
            tlpLoc.SetColumnSpan(pnlStreet, 2);

            // City / Region (Left column)
            _cmbCity = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCity.Items.AddRange(new object[]
            {
                "Davao City", "Cebu City", "Metro Manila", "Baguio", "Iloilo"
            });
            _cmbCity.SelectedIndexChanged += (s, e) => { ClearError(_cmbCity, _lblErrorCity); _isModified = true; };
            var pnlCity = CreateFieldGroup("City / Region", true, _cmbCity, out _lblErrorCity, 30, Color.FromArgb(248, 250, 252));
            pnlCity.Margin = new Padding(0, 4, 8, 4);
            tlpLoc.Controls.Add(pnlCity, 0, 1);

            // Landmark / Gate Notes (Right column, optional)
            _txtLandmark = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 150,
                PlaceholderText = "Landmark, gate access, or entrance details"
            };
            _txtLandmark.TextChanged += (s, e) => _isModified = true;
            var pnlLandmark = CreateFieldGroup("Landmark / Gate Notes", false, _txtLandmark, out _, 30, Color.FromArgb(248, 250, 252));
            pnlLandmark.Margin = new Padding(8, 4, 0, 4);
            tlpLoc.Controls.Add(pnlLandmark, 1, 1);

            card.Controls.Add(tlpLoc);
            tlpLoc.BringToFront();

            return card;
        }

        // ============================================================
        // STEP 3: Schedule & Dispatch (When)
        // ============================================================
        private void BuildStep3()
        {
            _pnlStep3 = new Panel
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
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(28, 16, 28, 16)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Preferred Date (Left)
            _dtpPreferredDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9.5F),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1)
            };
            _dtpPreferredDate.ValueChanged += (s, e) => { ClearError(_dtpPreferredDate, _lblErrorPreferredDate); _isModified = true; };
            var pnlPrefDate = CreateFieldGroup("Preferred Date", true, _dtpPreferredDate, out _lblErrorPreferredDate, 30);
            pnlPrefDate.Margin = new Padding(0, 4, 8, 8);
            tlp.Controls.Add(pnlPrefDate, 0, 0);

            // Follow-Up Date (Right)
            _dtpFollowUpDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9.5F),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(7)
            };
            _dtpFollowUpDate.ValueChanged += (s, e) => _isModified = true;
            var pnlFollowUp = CreateFieldGroup("Follow-Up Date (Optional)", false, _dtpFollowUpDate, out _, 30);
            pnlFollowUp.Margin = new Padding(8, 4, 0, 8);
            tlp.Controls.Add(pnlFollowUp, 1, 0);

            // Assigned Sales Staff (Full width)
            _cmbAssignedStaff = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbAssignedStaff.Items.AddRange(new object[]
            {
                "Juan Dela Cruz", "Maria Santos", "Pedro Reyes", "Ana Lopez"
            });
            _cmbAssignedStaff.SelectedIndexChanged += (s, e) => { ClearError(_cmbAssignedStaff, _lblErrorAssignedStaff); _isModified = true; };
            var pnlStaff = CreateFieldGroup("Assigned Sales Staff", true, _cmbAssignedStaff, out _lblErrorAssignedStaff, 30);
            tlp.Controls.Add(pnlStaff, 0, 1);
            tlp.SetColumnSpan(pnlStaff, 2);

            // Internal Notes (Full width)
            _txtNotes = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 1000,
                PlaceholderText = "Additional operational context, pricing notes, or dispatch instructions"
            };
            _txtNotes.TextChanged += (s, e) => _isModified = true;
            var pnlNotes = CreateFieldGroup("Internal Notes", false, _txtNotes, out _, 80);
            tlp.Controls.Add(pnlNotes, 0, 2);
            tlp.SetColumnSpan(pnlNotes, 2);

            _pnlStep3.Controls.Add(tlp);
            _pnlStepsHost.Controls.Add(_pnlStep3);
        }

        // ============================================================
        // Field Group Helper
        // ============================================================
        private static Panel CreateFieldGroup(string labelText, bool isRequired, Control inputControl, out Label errorLabel, int inputHeight, Color? bg = null, Label? externalTitleLabel = null)
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

            panel.Controls.Add(lbl);
            panel.Controls.Add(inputControl);
            panel.Controls.Add(errorLabel);

            return panel;
        }

        // ============================================================
        // Step Indicator Painting
        // ============================================================
        private void PaintStepIndicator(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_pnlStepIndicator.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _pnlStepIndicator.Width;
            int circleSize = 32;
            int circleRadius = circleSize / 2;
            int circleY = 14;
            int centerY = circleY + circleRadius;

            int[] xCenters = { w / 6, w / 2, 5 * w / 6 };
            string[] labels = { "Profile", "Service", "Schedule" };

            Color activeColor = Color.FromArgb(59, 130, 246);    // #3B82F6
            Color completedColor = Color.FromArgb(34, 197, 94);  // #22C55E
            Color inactiveColor = Color.FromArgb(203, 213, 225); // #CBD5E1
            Color lineColor = Color.FromArgb(226, 232, 240);     // #E2E8F0

            // Draw connecting lines between nodes
            using var penCompleted = new Pen(completedColor, 3);
            using var penInactive = new Pen(lineColor, 3);

            // Line 1 to 2
            int x1Right = xCenters[0] + circleRadius + 6;
            int x2Left = xCenters[1] - circleRadius - 6;
            g.DrawLine(_currentStep > 1 ? penCompleted : penInactive, x1Right, centerY, x2Left, centerY);

            // Line 2 to 3
            int x2Right = xCenters[1] + circleRadius + 6;
            int x3Left = xCenters[2] - circleRadius - 6;
            g.DrawLine(_currentStep > 2 ? penCompleted : penInactive, x2Right, centerY, x3Left, centerY);

            using var fontNumber = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var fontLabel = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var brushWhite = new SolidBrush(Color.White);
            using var brushLabel = new SolidBrush(Color.FromArgb(51, 65, 85));
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < 3; i++)
            {
                int stepNum = i + 1;
                int cx = xCenters[i];
                var circleRect = new Rectangle(cx - circleRadius, circleY, circleSize, circleSize);

                Color circleColor;
                if (stepNum < _currentStep)
                    circleColor = completedColor;
                else if (stepNum == _currentStep)
                    circleColor = activeColor;
                else
                    circleColor = inactiveColor;

                using (var brushCircle = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brushCircle, circleRect);
                }

                string nodeText = stepNum < _currentStep ? "✓" : stepNum.ToString();
                g.DrawString(nodeText, fontNumber, brushWhite, circleRect, sfCenter);

                var labelRect = new Rectangle(cx - 70, circleY + circleSize + 5, 140, 20);
                g.DrawString(labels[i], fontLabel, brushLabel, labelRect, sfCenter);
            }
        }

        // ============================================================
        // Navigation & Step Control
        // ============================================================
        private void ShowStep(int step)
        {
            if (step < 1 || step > 3) return;

            _currentStep = step;
            ClearValidationErrors();

            _pnlStep1.Visible = (_currentStep == 1);
            _pnlStep2.Visible = (_currentStep == 2);
            _pnlStep3.Visible = (_currentStep == 3);

            switch (_currentStep)
            {
                case 1:
                    _lblSectionTitle.Text = "Client Profile";
                    _lblSectionSubtitle.Text = "Select client type and provide contact details";
                    _btnCancel.Visible = true;
                    _btnBack.Visible = false;
                    _btnNext.Visible = true;
                    _btnSave.Visible = false;
                    break;

                case 2:
                    _lblSectionTitle.Text = "Service & Location";
                    _lblSectionSubtitle.Text = "Specify service requirements and physical site location";
                    _btnCancel.Visible = false;
                    _btnBack.Visible = true;
                    _btnNext.Visible = true;
                    _btnSave.Visible = false;
                    break;

                case 3:
                    _lblSectionTitle.Text = "Schedule & Dispatch";
                    _lblSectionSubtitle.Text = "Set preferred appointment dates and assign staff";
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
            {
                ShowStep(_currentStep + 1);
            }
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
        }

        // ============================================================
        // Validation (Inline red borders & helper labels, no popups)
        // ============================================================
        private bool ValidateCurrentStep()
        {
            bool isValid = true;
            ClearValidationErrors();

            if (_currentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(_txtFullName.Text))
                {
                    string nameField = _rdoCompany.Checked ? "Company / Org Name" : "Customer Name";
                    SetError(_txtFullName, _lblErrorFullName, $"⚠ {nameField} is required.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(_txtContactInfo.Text))
                {
                    SetError(_txtContactInfo, _lblErrorContactInfo, "⚠ Contact Information is required.");
                    isValid = false;
                }

                if (_cmbLeadSource.SelectedIndex < 0)
                {
                    SetError(_cmbLeadSource, _lblErrorLeadSource, "⚠ Please select a Lead Source.");
                    isValid = false;
                }
            }
            else if (_currentStep == 2)
            {
                if (_cmbServiceRequested.SelectedIndex < 0)
                {
                    SetError(_cmbServiceRequested, _lblErrorServiceRequested, "⚠ Please select Service Requested.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(_txtStreet.Text))
                {
                    SetError(_txtStreet, _lblErrorStreet, "⚠ Street address / unit is required.");
                    isValid = false;
                }

                if (_cmbCity.SelectedIndex < 0)
                {
                    SetError(_cmbCity, _lblErrorCity, "⚠ Please select a City / Region.");
                    isValid = false;
                }
            }
            else if (_currentStep == 3)
            {
                if (_dtpPreferredDate.Value.Date < DateTime.Today)
                {
                    SetError(_dtpPreferredDate, _lblErrorPreferredDate, "⚠ Preferred date cannot be in the past.");
                    isValid = false;
                }

                if (_cmbAssignedStaff.SelectedIndex < 0)
                {
                    SetError(_cmbAssignedStaff, _lblErrorAssignedStaff, "⚠ Please assign a Sales Staff.");
                    isValid = false;
                }
            }

            return isValid;
        }

        private static void SetError(Control control, Label errorLabel, string message)
        {
            control.BackColor = Color.FromArgb(254, 226, 226); // #FEE2E2
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
            if (_cmbLeadSource != null) ClearError(_cmbLeadSource, _lblErrorLeadSource);
            if (_cmbServiceRequested != null) ClearError(_cmbServiceRequested, _lblErrorServiceRequested);
            if (_txtStreet != null) ClearError(_txtStreet, _lblErrorStreet);
            if (_cmbCity != null) ClearError(_cmbCity, _lblErrorCity);
            if (_dtpPreferredDate != null) ClearError(_dtpPreferredDate, _lblErrorPreferredDate);
            if (_cmbAssignedStaff != null) ClearError(_cmbAssignedStaff, _lblErrorAssignedStaff);
        }

        // ============================================================
        // Save Execution
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
                // Format location cleanly: Street, City, Ref: Landmark
                string street = _txtStreet.Text.Trim();
                string city = _cmbCity.SelectedItem?.ToString() ?? string.Empty;
                string landmark = _txtLandmark.Text.Trim();

                string serviceLocation = string.IsNullOrWhiteSpace(landmark)
                    ? $"{street}, {city}".TrimEnd(',', ' ')
                    : $"{street}, {city}, Ref: {landmark}".TrimEnd(',', ' ');

                string specialReq = string.IsNullOrWhiteSpace(_txtSpecialRequests.Text) ? null! : _txtSpecialRequests.Text.Trim();
                string notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null! : _txtNotes.Text.Trim();

                var dto = new DataCollectionDto
                {
                    // Lead & Customer Shared Mappings
                    LeadName = _txtFullName.Text.Trim(),
                    CustomerName = _txtFullName.Text.Trim(),
                    ContactInfo = _txtContactInfo.Text.Trim(),
                    ContactDetails = _txtContactInfo.Text.Trim(),
                    LeadSource = _cmbLeadSource.SelectedItem?.ToString() ?? string.Empty,
                    RequestedService = _cmbServiceRequested.SelectedItem?.ToString() ?? string.Empty,
                    ServiceOfInterest = _cmbServiceRequested.SelectedItem?.ToString() ?? string.Empty,
                    InquiryDetails = specialReq,

                    // Customer Details
                    CustomerType = _rdoCompany.Checked ? "Company" : "Individual",
                    ServiceLocation = serviceLocation,

                    // Service Schedule
                    PreferredDate = _dtpPreferredDate.Value.Date,
                    FollowUpDate = _dtpFollowUpDate.Checked ? _dtpFollowUpDate.Value.Date : null,
                    SpecialRequests = specialReq,
                    Notes = notes,
                    AssignedSalesStaff = _cmbAssignedStaff.SelectedItem?.ToString() ?? string.Empty
                };

                var (success, message) = await _apiClient.SaveAsync(dto);

                if (success)
                {
                    ShowToast("Record saved successfully!", true);
                    ClearForm();
                    ShowStep(1);
                    RecordSaved?.Invoke();
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
                _btnSave.Text = "✓ Save Customer";
            }
        }

        // ============================================================
        // Clear Form
        // ============================================================
        public void ClearForm()
        {
            if (_rdoIndividual != null) _rdoIndividual.Checked = true;
            _txtFullName.Clear();
            _txtContactInfo.Clear();
            _cmbLeadSource.SelectedIndex = -1;

            _cmbServiceRequested.SelectedIndex = -1;
            _txtStreet.Clear();
            _cmbCity.SelectedIndex = -1;
            _txtLandmark.Clear();
            _txtSpecialRequests.Clear();

            _dtpPreferredDate.Value = DateTime.Today.AddDays(1);
            _dtpFollowUpDate.Checked = false;
            _cmbAssignedStaff.SelectedIndex = -1;
            _txtNotes.Clear();

            ClearValidationErrors();
            _isModified = false;
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
        // Keyboard Shortcuts (Enter -> Next/Save, Escape -> Cancel)
        // ============================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                var focused = FindActiveControl(this);
                if (focused is TextBox tb && tb.Multiline)
                {
                    return base.ProcessCmdKey(ref msg, keyData);
                }

                if (_currentStep < 3)
                {
                    if (_btnNext.Visible && _btnNext.Enabled)
                    {
                        OnNextClick();
                        return true;
                    }
                }
                else if (_currentStep == 3)
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
            {
                return FindActiveControl(cc.ActiveControl);
            }
            return root;
        }
    }
}