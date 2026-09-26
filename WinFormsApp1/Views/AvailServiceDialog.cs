using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Modal dialog to book a service for a Lead.
    /// In one step, this:
    ///   1. Confirms the Service Type and Service Address (both required).
    ///   2. Automatically converts the Lead to an official Customer record.
    ///   3. Creates the Work Order / Service Booking on the cleaning schedule.
    /// </summary>
    public class AvailServiceDialog : Form
    {
        private readonly ApiClient _api = new();
        private readonly LeadDto _lead;

        // UI Controls
        private TextBox _txtLeadName = null!;
        private TextBox _txtContactInfo = null!;
        private TextBox _txtServiceAddress = null!;
        private ComboBox _cmbServiceType = null!;
        private DateTimePicker _dtpPreferredDate = null!;
        private TextBox _txtQuotedPrice = null!;
        private TextBox _txtSpecialRequests = null!;
        private Label _lblErrAddress = null!;
        private Label _lblErrService = null!;
        private Label _lblStatus = null!;
        private Button _btnConfirm = null!;
        private Button _btnCancel = null!;

        public AvailServiceDialog(LeadDto lead)
        {
            _lead = lead ?? throw new ArgumentNullException(nameof(lead));
            BuildUI();
            PrefillData();
        }

        private void BuildUI()
        {
            Text = "Avail Service & Book Cleaning";
            Size = new Size(620, 680);
            MinimumSize = new Size(580, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // ── Top Header Banner ───────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "⚡  Avail Service & Schedule Booking",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = $"Booking service for Lead LD-{_lead.LeadId:D4} ({_lead.LeadName}). This will officially convert them into an active Customer and add the job to the cleaning schedule.",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill
            };
            pnlHeader.Controls.Add(lblSub);
            lblSub.BringToFront();

            // ── Footer Action Bar ───────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };
            Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            var pnlSpF = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(pnlSpF);

            _btnConfirm = new Button
            {
                Text = "⚡ Confirm Booking & Convert",
                Dock = DockStyle.Right,
                Width = 220,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnConfirm);
            _btnConfirm.Click += async (s, e) => await OnConfirmClickAsync();
            pnlFooter.Controls.Add(_btnConfirm);

            _lblStatus = new Label
            {
                Dock = DockStyle.Left,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 240
            };
            pnlFooter.Controls.Add(_lblStatus);

            // ── Main Content Scroll Container ───────────────────────
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Theme.Background
            };
            Controls.Add(pnlContent);
            pnlContent.BringToFront();

            int y = 8;

            // Section 1: Customer Contact
            var lblSec1 = CreateSectionHeader("1. Customer & Contact Details", ref y, pnlContent);

            CreateFieldLabel("Customer / Lead Name:", ref y, pnlContent);
            _txtLeadName = new TextBox
            {
                Location = new Point(0, y),
                Width = 540,
                Font = Theme.BodyFont,
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlContent.Controls.Add(_txtLeadName);
            y += 34;

            CreateFieldLabel("Contact Information (Phone / Email):", ref y, pnlContent);
            _txtContactInfo = new TextBox
            {
                Location = new Point(0, y),
                Width = 540,
                Font = Theme.BodyFont,
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            pnlContent.Controls.Add(_txtContactInfo);
            y += 34;

            // Address (REQUIRED)
            var lblAddressHeader = new Label
            {
                Text = "Service Address * (Required):",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark
            };
            pnlContent.Controls.Add(lblAddressHeader);
            y += 20;

            _txtServiceAddress = new TextBox
            {
                Location = new Point(0, y),
                Width = 540,
                Font = Theme.BodyFont,
                PlaceholderText = "Enter full service address (Street, Unit/Bldg, Barangay, City)..."
            };
            _txtServiceAddress.TextChanged += (s, e) => _lblErrAddress.Visible = false;
            pnlContent.Controls.Add(_txtServiceAddress);
            y += 28;

            _lblErrAddress = new Label
            {
                Text = "⚠ Service address is required to dispatch cleaning crew.",
                Location = new Point(0, y),
                AutoSize = true,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(220, 38, 38),
                Visible = false
            };
            pnlContent.Controls.Add(_lblErrAddress);
            y += 20;

            // Section 2: Service Booking
            var lblSec2 = CreateSectionHeader("2. Service & Scheduling Details", ref y, pnlContent);

            // Service Type (REQUIRED)
            var lblServiceHeader = new Label
            {
                Text = "Type of Service * (Required):",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark
            };
            pnlContent.Controls.Add(lblServiceHeader);
            y += 20;

            _cmbServiceType = new ComboBox
            {
                Location = new Point(0, y),
                Width = 540,
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbServiceType.Items.AddRange(new object[]
            {
                "General Home Cleaning",
                "Deep Cleaning",
                "Move-in / Move-out Cleaning",
                "Post-Construction Cleaning",
                "Office / Commercial Cleaning",
                "Upholstery & Carpet Cleaning",
                "Sanitization & Disinfection"
            });
            _cmbServiceType.SelectedIndexChanged += (s, e) => _lblErrService.Visible = false;
            pnlContent.Controls.Add(_cmbServiceType);
            y += 28;

            _lblErrService = new Label
            {
                Text = "⚠ Please select a service type.",
                Location = new Point(0, y),
                AutoSize = true,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(220, 38, 38),
                Visible = false
            };
            pnlContent.Controls.Add(_lblErrService);
            y += 20;

            // Preferred Date & Time
            CreateFieldLabel("Preferred Service Date & Time *:", ref y, pnlContent);
            _dtpPreferredDate = new DateTimePicker
            {
                Location = new Point(0, y),
                Width = 260,
                Font = Theme.BodyFont,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd  hh:mm tt",
                MinDate = DateTime.Today
            };
            pnlContent.Controls.Add(_dtpPreferredDate);

            // Quoted Price
            var lblPrice = new Label
            {
                Text = "Agreed / Quoted Price (₱):",
                Location = new Point(280, y - 20),
                AutoSize = true,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextDark
            };
            pnlContent.Controls.Add(lblPrice);

            _txtQuotedPrice = new TextBox
            {
                Location = new Point(280, y),
                Width = 260,
                Font = Theme.BodyFont,
                PlaceholderText = "e.g. 2500.00"
            };
            pnlContent.Controls.Add(_txtQuotedPrice);
            y += 36;

            // Special Requests & Notes
            CreateFieldLabel("Special Requests / Instructions (Optional):", ref y, pnlContent);
            _txtSpecialRequests = new TextBox
            {
                Location = new Point(0, y),
                Width = 540,
                Height = 54,
                Multiline = true,
                Font = Theme.BodyFont,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Notes regarding gate access, key drop, pet precautions, etc."
            };
            pnlContent.Controls.Add(_txtSpecialRequests);
            y += 64;
        }

        private void PrefillData()
        {
            _txtLeadName.Text = _lead.LeadName ?? "";
            _txtContactInfo.Text = _lead.ContactInfo ?? "";
            _txtServiceAddress.Text = _lead.ServiceAddress ?? "";

            // Auto-select matching service
            if (!string.IsNullOrEmpty(_lead.ServiceOfInterest))
            {
                for (int i = 0; i < _cmbServiceType.Items.Count; i++)
                {
                    string item = _cmbServiceType.Items[i]?.ToString() ?? "";
                    if (item.Contains(_lead.ServiceOfInterest, StringComparison.OrdinalIgnoreCase) ||
                        _lead.ServiceOfInterest.Contains(item, StringComparison.OrdinalIgnoreCase))
                    {
                        _cmbServiceType.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (_cmbServiceType.SelectedIndex < 0 && _cmbServiceType.Items.Count > 0)
            {
                _cmbServiceType.SelectedIndex = 0;
            }

            // Default date: Tomorrow at 9:00 AM
            _dtpPreferredDate.Value = DateTime.Today.AddDays(1).AddHours(9);

            // Prefill price
            if (_lead.QuotedPrice.HasValue && _lead.QuotedPrice.Value > 0)
            {
                _txtQuotedPrice.Text = _lead.QuotedPrice.Value.ToString("F2");
            }

            if (!string.IsNullOrWhiteSpace(_lead.InquiryDetails))
            {
                _txtSpecialRequests.Text = _lead.InquiryDetails;
            }
        }

        private async Task OnConfirmClickAsync()
        {
            // ── Validation ──────────────────────────────────────────
            bool isValid = true;

            string address = _txtServiceAddress.Text.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                _lblErrAddress.Visible = true;
                _txtServiceAddress.Focus();
                isValid = false;
            }

            string serviceType = _cmbServiceType.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(serviceType))
            {
                _lblErrService.Visible = true;
                isValid = false;
            }

            if (!isValid) return;

            decimal? price = null;
            if (!string.IsNullOrWhiteSpace(_txtQuotedPrice.Text))
            {
                string cleaned = _txtQuotedPrice.Text.Replace("₱", "").Replace("$", "").Trim();
                if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out decimal p) ||
                    decimal.TryParse(cleaned, out p))
                {
                    price = p;
                }
            }

            _btnConfirm.Enabled = false;
            _btnCancel.Enabled = false;
            _lblStatus.Text = "Converting lead & scheduling...";

            try
            {
                // Step 1: Convert Lead to Customer (if not already converted)
                int customerId;
                if (_lead.ConvertedCustomerId.HasValue && _lead.ConvertedCustomerId.Value > 0)
                {
                    customerId = _lead.ConvertedCustomerId.Value;
                }
                else
                {
                    var convertResponse = await _api.ConvertLeadAsync(_lead.LeadId, serviceAddress: address);

                    if (!convertResponse.Success)
                    {
                        if (convertResponse.DuplicateConflict && convertResponse.DuplicateInfo != null)
                        {
                            using var dupDlg = new LeadDuplicateResolutionDialog(_lead, convertResponse.DuplicateInfo);
                            if (dupDlg.ShowDialog(this) == DialogResult.OK)
                            {
                                if (dupDlg.UserChoice == DuplicateResolutionChoice.UseExisting)
                                {
                                    var linkRes = await _api.ConvertLeadAsync(_lead.LeadId, useExistingCustomerId: convertResponse.DuplicateInfo.ExistingCustomerId, serviceAddress: address);
                                    if (linkRes.Success && linkRes.Result != null)
                                    {
                                        customerId = linkRes.Result.CustomerId;
                                    }
                                    else
                                    {
                                        ShowError(linkRes.Message);
                                        return;
                                    }
                                }
                                else
                                {
                                    var forceRes = await _api.ConvertLeadAsync(_lead.LeadId, forceCreate: true, serviceAddress: address);
                                    if (forceRes.Success && forceRes.Result != null)
                                    {
                                        customerId = forceRes.Result.CustomerId;
                                    }
                                    else
                                    {
                                        ShowError(forceRes.Message);
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                _btnConfirm.Enabled = true;
                                _btnCancel.Enabled = true;
                                _lblStatus.Text = "";
                                return;
                            }
                        }
                        else
                        {
                            ShowError(convertResponse.Message);
                            return;
                        }
                    }
                    else
                    {
                        customerId = convertResponse.Result?.CustomerId ?? 0;
                    }
                }

                if (customerId <= 0)
                {
                    ShowError("Failed to obtain customer account for this lead.");
                    return;
                }

                // Step 2: Create the Service Request (Work Order / Cleaning Schedule)
                var workOrderDto = new WorkOrderCreateDto
                {
                    CustomerId = customerId,
                    ServiceType = serviceType,
                    PreferredDate = _dtpPreferredDate.Value,
                    QuotedPrice = price,
                    SpecialRequests = _txtSpecialRequests.Text.Trim(),
                    Notes = $"Booked directly from Lead LD-{_lead.LeadId:D4} (Service: {serviceType})"
                };

                var (woSuccess, woMessage, createdWo) = await _api.CreateWorkOrderAsync(workOrderDto);
                if (!woSuccess)
                {
                    ShowError($"Customer created, but scheduling failed: {woMessage}");
                    return;
                }

                MessageBox.Show(
                    $"Booking Confirmed!\n\n" +
                    $"• Lead LD-{_lead.LeadId:D4} is now an active Customer.\n" +
                    $"• Service: {serviceType}\n" +
                    $"• Scheduled Date: {_dtpPreferredDate.Value:MMM dd, yyyy  hh:mm tt}\n" +
                    $"• Address: {address}\n\n" +
                    $"The job has been placed on the cleaning schedule.",
                    "Service Booked Successfully",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Unexpected error: {ex.Message}");
            }
            finally
            {
                _btnConfirm.Enabled = true;
                _btnCancel.Enabled = true;
                _lblStatus.Text = "";
            }
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, "Booking Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblStatus.Text = "Error during booking.";
            _btnConfirm.Enabled = true;
            _btnCancel.Enabled = true;
        }

        private static Label CreateSectionHeader(string text, ref int y, Panel container)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(0, y),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                AutoSize = true
            };
            container.Controls.Add(lbl);
            y += 24;
            return lbl;
        }

        private static void CreateFieldLabel(string text, ref int y, Panel container)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(0, y),
                AutoSize = true,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextDark
            };
            container.Controls.Add(lbl);
            y += 18;
        }
    }
}
