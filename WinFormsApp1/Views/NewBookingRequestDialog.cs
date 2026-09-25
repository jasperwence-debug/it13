using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 3 — New Booking Request Dialog (Sales Staff).
    ///
    /// Responsibilities:
    ///   - Allows Sales Staff to submit a booking request for an existing Customer.
    ///   - Automatically sets status to 'Requested' (Manager schedules/dispatches later).
    ///   - Captures Service Type, Preferred Date, Quoted Price, Special Requests, and Notes.
    ///   - Stamps AssignedSalesStaff with the logged-in user without assigning field technicians.
    /// </summary>
    public class NewBookingRequestDialog : Form
    {
        public event Action<WorkOrderDto?>? BookingRequestSaved;

        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _customers = new();

        private readonly int? _preselectedCustomerId;
        private readonly string? _preselectedCustomerName;
        private readonly string? _preselectedLocation;

        private int? _currentCustomerId;
        private string? _currentCustomerName;
        private string? _currentLocation;

        // UI Controls
        private CustomerSummaryDto? _selectedCustomer;
        private TextBox _txtCustomerDisplay = null!;
        private Button _btnSearchCustomer = null!;
        private Button _btnClearCustomer = null!;
        private Label _lblCustomerDetails = null!;
        private Label _lblFixedCustomer = null!;
        private ComboBox _cmbServiceType = null!;
        private DateTimePicker _dtpPreferredDate = null!;
        private TextBox _txtQuotedPrice = null!;
        private TextBox _txtCreatedBy = null!;
        private TextBox _txtSpecialRequests = null!;
        private TextBox _txtNotes = null!;
        private CheckBox _chkNotifyCustomer = null!;

        // Error & Status
        private Label _lblErrCustomer = null!;
        private Label _lblErrServiceType = null!;
        private Label _lblErrPrice = null!;
        private Label _lblStatus = null!;
        private Button _btnSubmit = null!;
        private Button _btnCancel = null!;

        public NewBookingRequestDialog()
        {
            BuildUI();
        }

        public NewBookingRequestDialog(int customerId, string customerName, string? location = null)
        {
            _preselectedCustomerId = customerId;
            _preselectedCustomerName = customerName;
            _preselectedLocation = location;
            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_preselectedCustomerId.HasValue)
            {
                _ = LoadCustomersAsync();
            }
        }

        private void BuildUI()
        {
            Text = "New Booking Request";
            Size = new Size(680, 680);
            MinimumSize = new Size(600, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
                else if (e.Control && e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _ = OnSubmitAsync();
                }
            };

            // ── Header Panel (64px) ──────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            Controls.Add(pnlHeader);

            var pnlTitleGroup = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Theme.Surface
            };
            pnlHeader.Controls.Add(pnlTitleGroup);

            var lblTitle = new Label
            {
                Text = !string.IsNullOrWhiteSpace(_preselectedCustomerName) ? $"Booking for {_preselectedCustomerName}" : "Create Booking Request",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlTitleGroup.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Submit new service order request for Manager dispatch and scheduling",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlTitleGroup.Controls.Add(lblSubtitle);

            // Right side badges: Status badge + User badge
            var pnlBadges = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 18, 0, 0)
            };
            pnlHeader.Controls.Add(pnlBadges);

            var lblStatusBadge = new Label
            {
                Text = "● Status: Requested",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(202, 138, 4), // Amber
                BackColor = Color.FromArgb(254, 252, 232),
                Height = 26,
                Width = 140,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlBadges.Controls.Add(lblStatusBadge);

            var lblRoleBadge = new Label
            {
                Text = $"Sales: {SessionManager.CurrentUser?.Username ?? "staff"}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Theme.Primary,
                BackColor = Color.FromArgb(239, 246, 255),
                Height = 26,
                Width = 130,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            pnlBadges.Controls.Add(lblRoleBadge);

            var pnlDividerTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDividerTop);

            // ── Footer Panel (56px) ──────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 10, 24, 10)
            };
            Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Left,
                Width = 100
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(220, 38, 38),
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };
            pnlFooter.Controls.Add(_lblStatus);

            _btnSubmit = new Button
            {
                Text = "✓  Submit Booking Request",
                Dock = DockStyle.Right,
                Width = 200
            };
            Theme.ApplyPrimaryButtonStyle(_btnSubmit);
            _btnSubmit.Click += async (s, e) => await OnSubmitAsync();
            pnlFooter.Controls.Add(_btnSubmit);

            var pnlDividerBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDividerBottom);

            // ── Scrollable Body Panel ────────────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Background,
                Padding = new Padding(24, 16, 24, 16)
            };
            Controls.Add(scroll);

            // Ensure z-order
            Controls.SetChildIndex(scroll, 0);
            Controls.SetChildIndex(pnlDividerBottom, 1);
            Controls.SetChildIndex(pnlFooter, 2);
            Controls.SetChildIndex(pnlDividerTop, 3);
            Controls.SetChildIndex(pnlHeader, 4);

            // Informational Notice Banner
            var pnlNotice = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(239, 246, 255), // Soft Blue
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 16)
            };
            var lblNotice = new Label
            {
                Dock = DockStyle.Fill,
                Text = "ℹ  Booking requests are submitted in 'Requested' status. Manager approval, technician assignment, and scheduling occur in the Dispatch stage.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(30, 64, 175),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlNotice.Controls.Add(lblNotice);
            scroll.Controls.Add(pnlNotice);

            // Form Layout
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Theme.Background,
                Padding = new Padding(0, 12, 0, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            scroll.Controls.Add(tlp);

            // Customer Field
            int row = 0;
            if (_preselectedCustomerId.HasValue)
            {
                var pnlFixedWrap = new Panel { Dock = DockStyle.Fill, Height = 34 };

                _lblFixedCustomer = new Label
                {
                    Text = $"{_currentCustomerName} (CUST-{(_currentCustomerId ?? 0):D4})" +
                           (!string.IsNullOrWhiteSpace(_currentLocation) ? $" — {_currentLocation}" : ""),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    BackColor = Color.FromArgb(241, 245, 249),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(8, 0, 8, 0)
                };
                pnlFixedWrap.Controls.Add(_lblFixedCustomer);

                var btnChangeCust = new Button
                {
                    Text = "⇄ Change Customer",
                    Dock = DockStyle.Right,
                    Width = 145,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(224, 231, 255),
                    ForeColor = Color.FromArgb(67, 56, 202),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnChangeCust.FlatAppearance.BorderSize = 0;
                btnChangeCust.Click += async (s, e) =>
                {
                    if (_customers == null || _customers.Count == 0)
                    {
                        btnChangeCust.Text = "⏳ Loading...";
                        btnChangeCust.Enabled = false;
                        await LoadCustomersAsync();
                        btnChangeCust.Text = "⇄ Change Customer";
                        btnChangeCust.Enabled = true;
                    }
                    OpenCustomerSearch();
                };
                pnlFixedWrap.Controls.Add(btnChangeCust);

                var pnlCustFixed = MakeField("Customer (Pre-selected) *", pnlFixedWrap, out _lblErrCustomer, 34);
                tlp.Controls.Add(pnlCustFixed, 0, row);
                tlp.SetColumnSpan(pnlCustFixed, 2);
            }
            else
            {
                var pnlCustField = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 4, 0, 8),
                    Height = 84,
                    BackColor = Theme.Background
                };

                var lblCustTitle = new Label
                {
                    Text = "Customer *",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(0, 0),
                    Size = new Size(200, 20),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                pnlCustField.Controls.Add(lblCustTitle);

                var pnlSearchInput = new Panel
                {
                    Location = new Point(0, 22),
                    Size = new Size(520, 32),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };
                pnlCustField.Controls.Add(pnlSearchInput);

                _btnSearchCustomer = new Button
                {
                    Text = "🔍  Search...",
                    Dock = DockStyle.Right,
                    Width = 120,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                _btnSearchCustomer.FlatAppearance.BorderSize = 0;
                _btnSearchCustomer.Click += (s, e) => OpenCustomerSearch();
                pnlSearchInput.Controls.Add(_btnSearchCustomer);

                _btnClearCustomer = new Button
                {
                    Text = "✕",
                    Dock = DockStyle.Right,
                    Width = 30,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Visible = false
                };
                _btnClearCustomer.FlatAppearance.BorderSize = 0;
                _btnClearCustomer.Click += (s, e) => ClearSelectedCustomer();
                pnlSearchInput.Controls.Add(_btnClearCustomer);

                var pnlSpcSearch = new Panel { Dock = DockStyle.Right, Width = 6 };
                pnlSearchInput.Controls.Add(pnlSpcSearch);

                _txtCustomerDisplay = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9.5F),
                    ReadOnly = true,
                    BackColor = Color.White,
                    PlaceholderText = "Click 'Search...' to find customer by name, phone, or location *",
                    Cursor = Cursors.Hand
                };
                _txtCustomerDisplay.Click += (s, e) => OpenCustomerSearch();
                _txtCustomerDisplay.DoubleClick += (s, e) => OpenCustomerSearch();
                pnlSearchInput.Controls.Add(_txtCustomerDisplay);

                // Quick-reference subtitle beneath customer field
                _lblCustomerDetails = new Label
                {
                    Text = "No customer selected. Click 'Search...' to select an existing account.",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Location = new Point(0, 58),
                    Size = new Size(520, 20),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                pnlCustField.Controls.Add(_lblCustomerDetails);

                _lblErrCustomer = new Label
                {
                    Text = string.Empty,
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(220, 38, 38),
                    Location = new Point(0, 78),
                    Size = new Size(520, 18),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Visible = false
                };
                pnlCustField.Controls.Add(_lblErrCustomer);

                tlp.Controls.Add(pnlCustField, 0, row);
                tlp.SetColumnSpan(pnlCustField, 2);
            }
            row++;

            // Service Type (Left) + Quoted Price (Right)
            _cmbServiceType = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbServiceType.Items.AddRange(new object[]
            {
                "Standard Residential Cleaning",
                "Deep Cleaning Service",
                "Move-In / Move-Out Sanitization",
                "Commercial Office Cleaning",
                "Post-Construction Detailed Clean",
                "Window Cleaning",
                "Carpet & Upholstery Deep Steam",
                "Specialty Disinfection"
            });
            _cmbServiceType.SelectedIndex = 0;
            _cmbServiceType.SelectedIndexChanged += (s, e) => ClearErr(_cmbServiceType, _lblErrServiceType);

            var pnlService = MakeField("Service Type *", _cmbServiceType, out _lblErrServiceType, 30);
            pnlService.Margin = new Padding(0, 4, 8, 8);
            tlp.Controls.Add(pnlService, 0, row);

            _txtQuotedPrice = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "e.g. 250.00"
            };
            _txtQuotedPrice.TextChanged += (s, e) => ClearErr(_txtQuotedPrice, _lblErrPrice);
            _txtQuotedPrice.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.' && _txtQuotedPrice.Text.Contains('.'))
                {
                    e.Handled = true;
                }
            };

            var pnlPrice = MakeField("Quoted Price (₱)", _txtQuotedPrice, out _lblErrPrice, 30);
            pnlPrice.Margin = new Padding(8, 4, 0, 8);
            tlp.Controls.Add(pnlPrice, 1, row);
            row++;

            // Preferred Date (Left) + Created By (Right)
            _dtpPreferredDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9.5F),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1)
            };
            var pnlDate = MakeField("Preferred Service Date *", _dtpPreferredDate, out _, 30);
            pnlDate.Margin = new Padding(0, 4, 8, 8);
            tlp.Controls.Add(pnlDate, 0, row);

            _txtCreatedBy = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Text = SessionManager.CurrentUser?.Username ?? "Sales Staff",
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            var pnlCreator = MakeField("Recorded By", _txtCreatedBy, out _, 30);
            pnlCreator.Margin = new Padding(8, 4, 0, 8);
            tlp.Controls.Add(pnlCreator, 1, row);
            row++;

            // Special Requests (Full width)
            _txtSpecialRequests = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500,
                PlaceholderText = "Client preferences, access details (gate code, key box), pets on premises, etc."
            };
            var pnlSpecial = MakeField("Special Requests & Instructions", _txtSpecialRequests, out _, 70);
            tlp.Controls.Add(pnlSpecial, 0, row);
            tlp.SetColumnSpan(pnlSpecial, 2);
            row++;

            // Internal Notes (Full width)
            _txtNotes = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 1000,
                PlaceholderText = "Add any internal notes for the dispatch team..."
            };
            var pnlNotes = MakeField("Internal Notes", _txtNotes, out _, 70);
            tlp.Controls.Add(pnlNotes, 0, row);
            tlp.SetColumnSpan(pnlNotes, 2);
            row++;

            // Confirmation & Notification Checkbox
            _chkNotifyCustomer = new CheckBox
            {
                Text = "Send booking confirmation notice to customer (Email / SMS)",
                Checked = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 28,
                Cursor = Cursors.Hand
            };
            tlp.Controls.Add(_chkNotifyCustomer, 0, row);
            tlp.SetColumnSpan(_chkNotifyCustomer, 2);
        }

        private static Panel MakeField(string labelText, Control ctrl, out Label errLabel, int inputH)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                Height = 22 + inputH + 20,
                BackColor = Theme.Background
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Background,
                Location = new Point(0, 0),
                Size = new Size(pnl.Width, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(lbl);

            ctrl.Location = new Point(0, 22);
            ctrl.Size = new Size(pnl.Width, inputH);
            ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnl.Controls.Add(ctrl);

            errLabel = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(220, 38, 38),
                BackColor = Theme.Background,
                Location = new Point(0, 22 + inputH + 2),
                Size = new Size(pnl.Width, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };
            pnl.Controls.Add(errLabel);

            return pnl;
        }

        private static void ClearErr(Control ctrl, Label lbl)
        {
            ctrl.BackColor = Color.White;
            lbl.Text = string.Empty;
            lbl.Visible = false;
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                _customers = await _api.GetCustomersAsync();
            }
            catch
            {
                // Preload fallback
            }
        }

        private void OpenCustomerSearch()
        {
            using var dlg = new CustomerSearchDialog(_customers);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedCustomer != null)
            {
                SelectCustomer(dlg.SelectedCustomer);
            }
        }

        private void SelectCustomer(CustomerSummaryDto customer)
        {
            _selectedCustomer = customer;
            _currentCustomerId = customer.CustomerId;
            _currentCustomerName = customer.CustomerName;
            _currentLocation = customer.ServiceLocation;

            if (_lblFixedCustomer != null)
            {
                _lblFixedCustomer.Text = $"{customer.CustomerName} (CUST-{customer.CustomerId:D4})" +
                    (!string.IsNullOrWhiteSpace(customer.ServiceLocation) ? $" — {customer.ServiceLocation}" : "");
            }

            if (_txtCustomerDisplay != null)
            {
                _txtCustomerDisplay.Text = customer.CustomerName;
                _txtCustomerDisplay.BackColor = Color.FromArgb(240, 253, 244);
            }
            if (_btnClearCustomer != null) _btnClearCustomer.Visible = true;

            string contact = string.IsNullOrWhiteSpace(customer.ContactDetails) ? "No contact info" : customer.ContactDetails;
            string location = string.IsNullOrWhiteSpace(customer.ServiceLocation) ? "No address specified" : customer.ServiceLocation;
            string type = string.IsNullOrWhiteSpace(customer.CustomerType) ? "Client" : customer.CustomerType;

            if (_lblCustomerDetails != null)
            {
                _lblCustomerDetails.Text = $"📞 {contact}   |   📍 {location}   |   🏷️ {type} ({customer.TotalBookings} prior booking{(customer.TotalBookings == 1 ? "" : "s")})";
                _lblCustomerDetails.ForeColor = Color.FromArgb(15, 118, 110);
                _lblCustomerDetails.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            }

            if (_txtSpecialRequests != null && string.IsNullOrWhiteSpace(_txtSpecialRequests.Text))
            {
                if (!string.IsNullOrWhiteSpace(customer.ServiceLocation))
                {
                    _txtSpecialRequests.Text = $"Location: {customer.ServiceLocation}";
                }
            }

            if (_txtNotes != null && string.IsNullOrWhiteSpace(_txtNotes.Text))
            {
                _txtNotes.Text = $"Customer Tier: {type} | Total Prior Bookings: {customer.TotalBookings}";
            }

            if (_txtCustomerDisplay != null && _lblErrCustomer != null)
            {
                ClearErr(_txtCustomerDisplay, _lblErrCustomer);
            }
        }

        private void ClearSelectedCustomer()
        {
            _selectedCustomer = null;
            _currentCustomerId = null;
            _txtCustomerDisplay.Text = string.Empty;
            _txtCustomerDisplay.BackColor = Color.White;
            _btnClearCustomer.Visible = false;

            _lblCustomerDetails.Text = "No customer selected. Click 'Search...' to select an existing account.";
            _lblCustomerDetails.ForeColor = Color.FromArgb(100, 116, 139);
            _lblCustomerDetails.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        }

        private bool ValidateForm(out decimal? parsedPrice)
        {
            parsedPrice = null;
            bool ok = true;

            // Customer
            if (!_currentCustomerId.HasValue && _selectedCustomer == null)
            {
                _lblErrCustomer.Text = "⚠ Please search and select an existing customer.";
                _lblErrCustomer.Visible = true;
                if (_txtCustomerDisplay != null) _txtCustomerDisplay.BackColor = Color.FromArgb(254, 226, 226);
                ok = false;
            }

            // Service Type
            if (_cmbServiceType.SelectedIndex < 0)
            {
                _lblErrServiceType.Text = "⚠ Please select a service type.";
                _lblErrServiceType.Visible = true;
                _cmbServiceType.BackColor = Color.FromArgb(254, 226, 226);
                ok = false;
            }

            // Preferred Date (cannot be in the past)
            if (!ValidationHelper.IsValidDateNotPast(_dtpPreferredDate.Value.Date, "Preferred date", out var dateErr))
            {
                _lblStatus.Text = dateErr;
                ok = false;
            }

            // Quoted Price (optional, but must be numeric if provided)
            if (!string.IsNullOrWhiteSpace(_txtQuotedPrice.Text))
            {
                if (ValidationHelper.IsValidPrice(_txtQuotedPrice.Text, false, out var price, out var priceErr))
                {
                    parsedPrice = price;
                }
                else
                {
                    _lblErrPrice.Text = priceErr;
                    _lblErrPrice.Visible = true;
                    _txtQuotedPrice.BackColor = Color.FromArgb(254, 226, 226);
                    ok = false;
                }
            }

            // Length boundaries matching database schema
            if (!string.IsNullOrWhiteSpace(_txtSpecialRequests.Text) &&
                !ValidationHelper.IsValidTextLength(_txtSpecialRequests.Text, "Special requests", 500, false, out var specErr))
            {
                _lblStatus.Text = specErr;
                ok = false;
            }

            if (!string.IsNullOrWhiteSpace(_txtNotes.Text) &&
                !ValidationHelper.IsValidTextLength(_txtNotes.Text, "Internal notes", 1000, false, out var notesErr))
            {
                _lblStatus.Text = notesErr;
                ok = false;
            }

            return ok;
        }

        private async Task OnSubmitAsync()
        {
            if (!ValidateForm(out var quotedPrice)) return;

            _btnSubmit.Enabled = false;
            _btnSubmit.Text = "⏳ Submitting Request...";
            _lblStatus.Text = string.Empty;

            try
            {
                int targetCustomerId;
                if (_currentCustomerId.HasValue)
                {
                    targetCustomerId = _currentCustomerId.Value;
                }
                else if (_selectedCustomer != null)
                {
                    targetCustomerId = _selectedCustomer.CustomerId;
                }
                else
                {
                    _lblStatus.Text = "Please select a valid customer.";
                    return;
                }

                var dto = new WorkOrderCreateDto
                {
                    CustomerId      = targetCustomerId,
                    ServiceType     = _cmbServiceType.SelectedItem?.ToString() ?? "Standard Residential Cleaning",
                    PreferredDate   = _dtpPreferredDate.Value.Date,
                    AssignedStaff   = SessionManager.CurrentUser?.Username ?? "SalesStaff",
                    Status          = "Requested", // Strictly 'Requested' for Sales staff
                    QuotedPrice     = quotedPrice,
                    SpecialRequests = string.IsNullOrWhiteSpace(_txtSpecialRequests.Text) ? null : _txtSpecialRequests.Text.Trim(),
                    Notes           = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
                };

                var (success, message, createdWorkOrder) = await _api.CreateWorkOrderAsync(dto);

                if (success)
                {
                    BookingRequestSaved?.Invoke(createdWorkOrder);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblStatus.Text = message;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _btnSubmit.Enabled = true;
                _btnSubmit.Text = "✓  Submit Request";
            }
        }
    }
}
