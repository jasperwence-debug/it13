using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Dialog for creating Work Orders (Service Requests).
    ///
    /// STRICT RULE: Requires selecting an existing Customer from the picker.
    ///              This form NEVER auto-creates Customers.
    ///
    /// Fields: Customer (picker), Service Type, Preferred Date,
    ///         Assigned Staff, Status, Special Requests (optional), Notes (optional)
    /// </summary>
    public class NewWorkOrderDialog : Form
    {
        public event Action? WorkOrderSaved;

        private readonly ApiClient _api = new();
        private List<CustomerSummaryDto> _customers = new();

        // Fields
        private CustomerSummaryDto? _selectedCustomer;
        private TextBox _txtCustomerDisplay = null!;
        private Button _btnSearchCustomer = null!;
        private Button _btnClearCustomer = null!;
        private Label _lblCustomerDetails = null!;
        private ComboBox _cmbServiceType = null!;
        private DateTimePicker _dtpPreferredDate = null!;
        private ComboBox _cmbAssignedStaff = null!;
        private TextBox _txtSpecialRequests = null!;
        private TextBox _txtNotes = null!;

        // Validation labels
        private Label _lblErrCustomer = null!;
        private Label _lblErrServiceType = null!;
        private Label _lblErrPreferredDate = null!;
        private Label _lblErrAssignedStaff = null!;

        // Buttons
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Label _lblStatus = null!;

        public NewWorkOrderDialog()
        {
            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = LoadCustomersAsync();
        }

        private void BuildUI()
        {
            Text = "New Work Order";
            Size = new Size(680, 620);
            MinimumSize = new Size(580, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(24, 0, 24, 0)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "Create Work Order",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White,
                Dock = DockStyle.Left,
                Width = 340,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Assign a service booking to an existing customer.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.White,
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlHeader.Controls.Add(lblSub);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            Controls.Add(pnlDivider);

            // Footer buttons
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
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
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };
            pnlFooter.Controls.Add(_lblStatus);

            _btnSave = new Button
            {
                Text = "✓ Create Work Order",
                Dock = DockStyle.Right,
                Width = 190
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await OnSaveAsync();
            pnlFooter.Controls.Add(_btnSave);

            var pnlFooterDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            Controls.Add(pnlFooterDivider);

            // Scrollable form body
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24, 16, 24, 16)
            };
            Controls.Add(scroll);

            // Ensure z-order
            Controls.SetChildIndex(scroll, 0);
            Controls.SetChildIndex(pnlFooterDivider, 1);
            Controls.SetChildIndex(pnlFooter, 2);
            Controls.SetChildIndex(pnlDivider, 3);
            Controls.SetChildIndex(pnlHeader, 4);

            // Form fields
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            scroll.Controls.Add(tlp);

            int row = 0;

            // --- Customer Search Field (full width) ---
            var pnlCustField = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                Height = 84,
                BackColor = Color.FromArgb(248, 250, 252)
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
            row++;

            // --- Service Type (left) + Status (right) ---
            _cmbServiceType = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbServiceType.Items.AddRange(new object[]
            {
                "General House Cleaning", "Deep Cleaning", "Move-in Sanitization",
                "Move-out Detailed Clean", "Office Cleaning", "Restaurant Cleaning",
                "Commercial Sanitization", "Window Cleaning",
                "Post-Construction Cleaning", "Carpet & Upholstery Cleaning"
            });
            _cmbServiceType.SelectedIndex = -1;
            _cmbServiceType.SelectedIndexChanged += (s, e) => ClearErr(_cmbServiceType, _lblErrServiceType);

            var pnlSvc = MakeField("Service Type *", _cmbServiceType, out _lblErrServiceType, 30);
            pnlSvc.Margin = new Padding(0, 4, 8, 8);
            tlp.Controls.Add(pnlSvc, 0, row);

            // --- Status (Read-Only Badge as per Industry Standard) ---
            var pnlStatus = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 4, 0, 8),
                Height = 72,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var lblStatusTitle = new Label
            {
                Text = "Status",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, 0),
                Size = new Size(200, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlStatus.Controls.Add(lblStatusTitle);

            var pnlBadge = new Panel
            {
                Location = new Point(0, 22),
                Size = new Size(240, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(254, 243, 199) // Light Amber badge
            };
            pnlStatus.Controls.Add(pnlBadge);

            var lblBadge = new Label
            {
                Text = "●  Pending",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9), // Amber-800
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            pnlBadge.Controls.Add(lblBadge);

            var lblStatusHint = new Label
            {
                Text = "Set automatically when work order is created",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 52),
                Size = new Size(260, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlStatus.Controls.Add(lblStatusHint);

            tlp.Controls.Add(pnlStatus, 1, row);
            row++;

            // --- Preferred Date (left) + Assigned Staff (right) ---
            _dtpPreferredDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 9.5F),
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(1)
            };
            _dtpPreferredDate.ValueChanged += (s, e) => ClearErr(_dtpPreferredDate, _lblErrPreferredDate);

            var pnlDate = MakeField("Preferred Date *", _dtpPreferredDate, out _lblErrPreferredDate, 30);
            pnlDate.Margin = new Padding(0, 4, 8, 8);
            tlp.Controls.Add(pnlDate, 0, row);

            _cmbAssignedStaff = new ComboBox
            {
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbAssignedStaff.Items.AddRange(new object[]
            {
                "Juan Dela Cruz", "Maria Santos", "Pedro Reyes", "Ana Lopez",
                "Pedro Reyes", "Sarah Jane", "Michael John", "Jessica Mae"
            });
            _cmbAssignedStaff.SelectedIndex = -1;
            _cmbAssignedStaff.SelectedIndexChanged += (s, e) => ClearErr(_cmbAssignedStaff, _lblErrAssignedStaff);

            var pnlStaff = MakeField("Assigned Staff *", _cmbAssignedStaff, out _lblErrAssignedStaff, 30);
            pnlStaff.Margin = new Padding(8, 4, 0, 8);
            tlp.Controls.Add(pnlStaff, 1, row);
            row++;

            // --- Special Requests (full width) ---
            _txtSpecialRequests = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500,
                PlaceholderText = "Specific instructions, access notes, or crew requirements (optional)"
            };
            var pnlSpecial = MakeField("Special Requests", _txtSpecialRequests, out _, 80);
            tlp.Controls.Add(pnlSpecial, 0, row);
            tlp.SetColumnSpan(pnlSpecial, 2);
            row++;

            // --- Internal Notes (full width) ---
            _txtNotes = new TextBox
            {
                Font = new Font("Segoe UI", 9.5F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 1000,
                PlaceholderText = "Internal operational context, pricing notes, or dispatch instructions (optional)"
            };
            var pnlNotes = MakeField("Internal Notes", _txtNotes, out _, 80);
            tlp.Controls.Add(pnlNotes, 0, row);
            tlp.SetColumnSpan(pnlNotes, 2);
        }

        private static Panel MakeField(string labelText, Control ctrl, out Label errLabel, int inputH, Color? bg = null)
        {
            var backColor = bg ?? Color.FromArgb(248, 250, 252);
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                Height = 22 + inputH + 20,
                BackColor = backColor
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = backColor,
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
                BackColor = backColor,
                Location = new Point(0, 22 + inputH + 2),
                Size = new Size(pnl.Width, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };
            pnl.Controls.Add(errLabel);

            return pnl;
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                _customers = await _api.GetCustomersAsync();
            }
            catch
            {
                // Background preload failed; CustomerSearchDialog will retry upon opening
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
            _txtCustomerDisplay.Text = customer.CustomerName;
            _txtCustomerDisplay.BackColor = Color.FromArgb(240, 253, 244);
            _btnClearCustomer.Visible = true;

            string contact = string.IsNullOrWhiteSpace(customer.ContactDetails) ? "No contact info" : customer.ContactDetails;
            string location = string.IsNullOrWhiteSpace(customer.ServiceLocation) ? "No address specified" : customer.ServiceLocation;
            string type = string.IsNullOrWhiteSpace(customer.CustomerType) ? "Client" : customer.CustomerType;

            _lblCustomerDetails.Text = $"📞 {contact}   |   📍 {location}   |   🏷️ {type} ({customer.TotalBookings} prior booking{(customer.TotalBookings == 1 ? "" : "s")})";
            _lblCustomerDetails.ForeColor = Color.FromArgb(15, 118, 110);
            _lblCustomerDetails.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

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

            ClearErr(_txtCustomerDisplay, _lblErrCustomer);
        }

        private void ClearSelectedCustomer()
        {
            _selectedCustomer = null;
            _txtCustomerDisplay.Text = string.Empty;
            _txtCustomerDisplay.BackColor = Color.White;
            _btnClearCustomer.Visible = false;

            _lblCustomerDetails.Text = "No customer selected. Click 'Search...' to select an existing account.";
            _lblCustomerDetails.ForeColor = Color.FromArgb(100, 116, 139);
            _lblCustomerDetails.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        }

        private new bool Validate()
        {
            bool ok = true;

            // Customer
            if (_selectedCustomer == null)
            {
                _lblErrCustomer.Text = "⚠ Please search and select a customer.";
                _lblErrCustomer.Visible = true;
                _txtCustomerDisplay.BackColor = Color.FromArgb(254, 226, 226);
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

            // Preferred Date
            if (_dtpPreferredDate.Value.Date < DateTime.Today)
            {
                _lblErrPreferredDate.Text = "⚠ Preferred date cannot be in the past.";
                _lblErrPreferredDate.Visible = true;
                ok = false;
            }

            // Assigned Staff
            if (_cmbAssignedStaff.SelectedIndex < 0)
            {
                _lblErrAssignedStaff.Text = "⚠ Please assign a staff member.";
                _lblErrAssignedStaff.Visible = true;
                _cmbAssignedStaff.BackColor = Color.FromArgb(254, 226, 226);
                ok = false;
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

        private static void ClearErr(Control ctrl, Label lbl)
        {
            ctrl.BackColor = Color.FromArgb(248, 250, 252);
            lbl.Text = string.Empty;
            lbl.Visible = false;
        }

        private async Task OnSaveAsync()
        {
            if (!Validate()) return;

            _btnSave.Enabled = false;
            _btnSave.Text = "⏳ Creating...";
            _lblStatus.Text = string.Empty;

            try
            {
                if (_selectedCustomer == null)
                {
                    _lblStatus.Text = "Please select a valid customer.";
                    return;
                }

                var customerId = _selectedCustomer.CustomerId;

                var dto = new WorkOrderCreateDto
                {
                    CustomerId      = customerId,
                    ServiceType     = _cmbServiceType.SelectedItem?.ToString() ?? string.Empty,
                    PreferredDate   = _dtpPreferredDate.Value.Date,
                    AssignedStaff   = _cmbAssignedStaff.SelectedItem?.ToString() ?? string.Empty,
                    Status          = "Requested", // Status is set automatically by system workflow
                    SpecialRequests = string.IsNullOrWhiteSpace(_txtSpecialRequests.Text) ? null : _txtSpecialRequests.Text.Trim(),
                    Notes           = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
                };

                var (success, message, _) = await _api.CreateWorkOrderAsync(dto);

                if (success)
                {
                    WorkOrderSaved?.Invoke();
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
                _btnSave.Enabled = true;
                _btnSave.Text = "✓ Create Work Order";
            }
        }
    }
}
