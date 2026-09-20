using System;
using System.Drawing;
using System.Windows.Forms;

namespace App.WinForms.Views
{
    public class DataCollectionView : UserControl
    {
        // ============================================================
        // State
        // ============================================================
        private int _currentStep = 1;
        private readonly ApiClient _api = new ApiClient();

        // ============================================================
        // Header / Progress
        // ============================================================
        private Label _lblStepTitle = null!;
        private Label _lblProgress = null!;
        private Panel _pnlFormHost = null!;

        // ============================================================
        // Step 1 — Lead
        // ============================================================
        private Panel _pnlStep1 = null!;
        private TextBox _txtLeadName = null!;
        private TextBox _txtContactInfo = null!;
        private ComboBox _cmbLeadSource = null!;
        private ComboBox _cmbServiceOfInterest = null!;
        private TextBox _txtInquiryDetails = null!;

        // ============================================================
        // Step 2 — Customer
        // ============================================================
        private Panel _pnlStep2 = null!;
        private ComboBox _cmbCustomerType = null!;
        private TextBox _txtCustomerName = null!;
        private TextBox _txtContactDetails = null!;
        private TextBox _txtServiceLocation = null!;

        // ============================================================
        // Step 3 — Service
        // ============================================================
        private Panel _pnlStep3 = null!;
        private ComboBox _cmbRequestedService = null!;
        private DateTimePicker _dtpPreferredDate = null!;
        private TextBox _txtSpecialRequests = null!;
        private DateTimePicker _dtpFollowUpDate = null!;
        private TextBox _txtNotes = null!;
        private ComboBox _cmbAssignedStaff = null!;

        // ============================================================
        // Buttons
        // ============================================================
        private Button _btnBack = null!;
        private Button _btnNext = null!;
        private Button _btnSave = null!;

        public DataCollectionView()
        {
            BuildUI();
            ShowStep(1);
        }

        // ============================================================
        // Build UI
        // ============================================================
        private void BuildUI()
        {
            BackColor = Color.White;
            Padding = new Padding(40, 20, 40, 20);

            // ---- Step title (top-left) ----
            _lblStepTitle = new Label
            {
                Text = "Step 1 of 3: Lead Information",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 35
            };
            Controls.Add(_lblStepTitle);

            // ---- Progress indicator (below title) ----
            _lblProgress = new Label
            {
                Text = "●━━━━━○━━━━━○",
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.FromArgb(59, 130, 246),
                Dock = DockStyle.Top,
                Height = 30
            };
            Controls.Add(_lblProgress);
            _lblProgress.BringToFront();

            // ---- Bottom button bar (first, so it stays at bottom) ----
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                Padding = new Padding(0, 15, 0, 0)
            };
            Controls.Add(pnlButtons);

            _btnBack = new Button
            {
                Text = "← Back",
                Size = new Size(120, 40),
                Location = new Point(0, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            _btnBack.FlatAppearance.BorderSize = 0;
            _btnBack.Click += (s, e) => ShowStep(_currentStep - 1);
            pnlButtons.Controls.Add(_btnBack);

            _btnNext = new Button
            {
                Text = "Next →",
                Size = new Size(140, 40),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            _btnNext.FlatAppearance.BorderSize = 0;
            _btnNext.Location = new Point(pnlButtons.Width - 140, 15);
            _btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlButtons.Resize += (s, e) =>
            {
                _btnNext.Left = pnlButtons.Width - _btnNext.Width;
            };
            _btnNext.Click += (s, e) => ShowStep(_currentStep + 1);
            pnlButtons.Controls.Add(_btnNext);

            _btnSave = new Button
            {
                Text = "✓ Save to CRM",
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            pnlButtons.Controls.Add(_btnSave);
            pnlButtons.Resize += (s, e) =>
            {
                _btnSave.Left = pnlButtons.Width - _btnSave.Width;
            };
            _btnSave.Click += BtnSave_Click;

            // ---- Form host (holds step panels) ----
            _pnlFormHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            Controls.Add(_pnlFormHost);
            _pnlFormHost.BringToFront();

            // ============================================================
            // STEP 1 — Lead
            // ============================================================
            _pnlStep1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 20, 10, 10) };
            _pnlFormHost.Controls.Add(_pnlStep1);

            int y = 20;
            AddLabel(_pnlStep1, "Lead Name *", y); y += 25;
            _txtLeadName = AddTextBox(_pnlStep1, y); y += 45;

            AddLabel(_pnlStep1, "Contact Information *", y); y += 25;
            _txtContactInfo = AddTextBox(_pnlStep1, y); y += 45;

            AddLabel(_pnlStep1, "Lead Source *", y); y += 25;
            _cmbLeadSource = AddComboBox(_pnlStep1, y);
            _cmbLeadSource.Items.AddRange(new object[]
            {
                "Facebook", "Website", "Referral", "Walk-in", "Google Ads", "Other"
            });
            y += 45;

            AddLabel(_pnlStep1, "Service of Interest *", y); y += 25;
            _cmbServiceOfInterest = AddComboBox(_pnlStep1, y);
            _cmbServiceOfInterest.Items.AddRange(new object[]
            {
                "General Cleaning", "Deep Cleaning", "Office Cleaning",
                "Move-in Cleaning", "Move-out Cleaning"
            });
            y += 45;

            AddLabel(_pnlStep1, "Inquiry Details", y); y += 25;
            _txtInquiryDetails = AddMultiline(_pnlStep1, y, 90);

            // ============================================================
            // STEP 2 — Customer
            // ============================================================
            _pnlStep2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 20, 10, 10), Visible = false };
            _pnlFormHost.Controls.Add(_pnlStep2);

            y = 20;
            AddLabel(_pnlStep2, "Customer Type *", y); y += 25;
            _cmbCustomerType = AddComboBox(_pnlStep2, y);
            _cmbCustomerType.Items.AddRange(new object[] { "Individual", "Company" });
            y += 45;




            AddLabel(_pnlStep2, "Service Location (Address) *", y); y += 25;
            _txtServiceLocation = AddMultiline(_pnlStep2, y, 100);
            // ============================================================
            // STEP 3 — Service & CRM
            // ============================================================
            _pnlStep3 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 20, 10, 10), Visible = false };
            _pnlFormHost.Controls.Add(_pnlStep3);

            y = 20;
            AddLabel(_pnlStep3, "Requested Service *", y); y += 25;
            _cmbRequestedService = AddComboBox(_pnlStep3, y);
            _cmbRequestedService.Items.AddRange(new object[]
            {
                "General Cleaning", "Deep Cleaning", "Office Cleaning",
                "Move-in Cleaning", "Move-out Cleaning"
            });
            y += 45;

            AddLabel(_pnlStep3, "Preferred Date *", y); y += 25;
            _dtpPreferredDate = AddDatePicker(_pnlStep3, y);
            _dtpPreferredDate.Value = DateTime.Today.AddDays(1);
            y += 45;

            AddLabel(_pnlStep3, "Special Requests", y); y += 25;
            _txtSpecialRequests = AddMultiline(_pnlStep3, y, 60);
            y += 75;

            AddLabel(_pnlStep3, "Follow-Up Date (optional)", y); y += 25;
            _dtpFollowUpDate = AddDatePicker(_pnlStep3, y);
            _dtpFollowUpDate.Format = DateTimePickerFormat.Custom;
            _dtpFollowUpDate.CustomFormat = " ";
            _dtpFollowUpDate.ValueChanged += (s, e) =>
            {
                _dtpFollowUpDate.Format = DateTimePickerFormat.Short;
            };
            y += 45;

            AddLabel(_pnlStep3, "Notes", y); y += 25;
            _txtNotes = AddMultiline(_pnlStep3, y, 60);
            y += 75;

            AddLabel(_pnlStep3, "Assigned Sales Staff *", y); y += 25;
            _cmbAssignedStaff = AddComboBox(_pnlStep3, y);
            _cmbAssignedStaff.Items.AddRange(new object[]
            {
                "Juan Dela Cruz", "Maria Santos", "Pedro Reyes", "Ana Lopez"
            });
        }

        // ============================================================
        // Helper factories
        // ============================================================
        private static void AddLabel(Control parent, string text, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(500, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            });
        }

        private static TextBox AddTextBox(Control parent, int y)
        {
            var tb = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(700, 28),
                Font = new Font("Segoe UI", 10F)
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private static TextBox AddMultiline(Control parent, int y, int h)
        {
            var tb = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(700, h),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10F)
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private static ComboBox AddComboBox(Control parent, int y)
        {
            var cb = new ComboBox
            {
                Location = new Point(10, y),
                Size = new Size(700, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            parent.Controls.Add(cb);
            return cb;
        }

        private static DateTimePicker AddDatePicker(Control parent, int y)
        {
            var dp = new DateTimePicker
            {
                Location = new Point(10, y),
                Size = new Size(700, 28),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };
            parent.Controls.Add(dp);
            return dp;
        }

        // ============================================================
        // Step navigation
        // ============================================================
        private void ShowStep(int step)
        {
            if (step < 1 || step > 3) return;

            // Validate current step before advancing
            if (step > _currentStep && !ValidateStep(_currentStep))
                return;

            _currentStep = step;

            _pnlStep1.Visible = step == 1;
            _pnlStep2.Visible = step == 2;
            _pnlStep3.Visible = step == 3;

            _lblStepTitle.Text = step switch
            {
                1 => "Step 1 of 3: Lead Information",
                2 => "Step 2 of 3: Customer Information",
                3 => "Step 3 of 3: Service & CRM Information",
                _ => ""
            };

            _lblProgress.Text = step switch
            {
                1 => "●━━━━━○━━━━━○",
                2 => "○━━━━━●━━━━━○",
                3 => "○━━━━━○━━━━━●",
                _ => ""
            };

            _btnBack.Visible = step > 1;
            _btnNext.Visible = step < 3;
            _btnSave.Visible = step == 3;
        }

        // ============================================================
        // Validation
        // ============================================================
        private bool ValidateStep(int step)
        {
            switch (step)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(_txtLeadName?.Text)) { Warn("Lead Name is required."); return false; }
                    if (string.IsNullOrWhiteSpace(_txtContactInfo?.Text)) { Warn("Contact Information is required."); return false; }
                    if (_cmbLeadSource == null || _cmbLeadSource.SelectedIndex < 0) { Warn("Please select a Lead Source."); return false; }
                    if (_cmbServiceOfInterest == null || _cmbServiceOfInterest.SelectedIndex < 0) { Warn("Please select Service of Interest."); return false; }
                    return true;

                case 2:
                    if (_cmbCustomerType == null || _cmbCustomerType.SelectedIndex < 0) { Warn("Please select Customer Type."); return false; }
                    if (string.IsNullOrWhiteSpace(_txtCustomerName?.Text)) { Warn("Name / Company Name is required."); return false; }
                    if (string.IsNullOrWhiteSpace(_txtContactDetails?.Text)) { Warn("Contact Details is required."); return false; }
                    if (string.IsNullOrWhiteSpace(_txtServiceLocation?.Text)) { Warn("Service Location is required."); return false; }
                    return true;

                case 3:
                    if (_cmbRequestedService == null || _cmbRequestedService.SelectedIndex < 0) { Warn("Please select Requested Service."); return false; }
                    if (_dtpPreferredDate == null || _dtpPreferredDate.Value.Date < DateTime.Today) { Warn("Preferred Date cannot be in the past."); return false; }
                    if (_cmbAssignedStaff == null || _cmbAssignedStaff.SelectedIndex < 0) { Warn("Please select Assigned Sales Staff."); return false; }
                    return true;
            }
            return true;
        }

        private void Warn(string msg)
        {
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ============================================================
        // Save handler
        // ============================================================
        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!ValidateStep(3)) return;

            var dto = new DataCollectionDto
            {
                // Lead
                LeadName = _txtLeadName.Text.Trim(),
                ContactInfo = _txtContactInfo.Text.Trim(),
                LeadSource = _cmbLeadSource.SelectedItem?.ToString() ?? "",
                ServiceOfInterest = _cmbServiceOfInterest.SelectedItem?.ToString() ?? "",
                InquiryDetails = string.IsNullOrWhiteSpace(_txtInquiryDetails.Text) ? null : _txtInquiryDetails.Text.Trim(),

                // Customer
                CustomerType = _cmbCustomerType.SelectedItem?.ToString() ?? "",
                CustomerName = _txtCustomerName.Text.Trim(),
                ContactDetails = _txtContactDetails.Text.Trim(),
                ServiceLocation = _txtServiceLocation.Text.Trim(),

                // Service
                RequestedService = _cmbRequestedService.SelectedItem?.ToString() ?? "",
                PreferredDate = _dtpPreferredDate.Value,
                SpecialRequests = string.IsNullOrWhiteSpace(_txtSpecialRequests.Text) ? null : _txtSpecialRequests.Text.Trim(),
                FollowUpDate = _dtpFollowUpDate.Format == DateTimePickerFormat.Short ? _dtpFollowUpDate.Value : (DateTime?)null,
                Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim(),
                AssignedSalesStaff = _cmbAssignedStaff.SelectedItem?.ToString() ?? ""
            };

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            var (success, message) = await _api.SaveAsync(dto);

            _btnSave.Enabled = true;
            _btnSave.Text = "✓ Save to CRM";

            if (success)
            {
                MessageBox.Show(message, "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                ShowStep(1);
            }
            else
            {
                MessageBox.Show(message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Clear form
        // ============================================================
        private void ClearForm()
        {
            _txtLeadName.Clear();
            _txtContactInfo.Clear();
            _cmbLeadSource.SelectedIndex = -1;
            _cmbServiceOfInterest.SelectedIndex = -1;
            _txtInquiryDetails.Clear();

            _cmbCustomerType.SelectedIndex = -1;
            _txtCustomerName.Clear();
            _txtContactDetails.Clear();
            _txtServiceLocation.Clear();

            _cmbRequestedService.SelectedIndex = -1;
            _dtpPreferredDate.Value = DateTime.Today.AddDays(1);
            _txtSpecialRequests.Clear();
            _dtpFollowUpDate.Format = DateTimePickerFormat.Custom;
            _dtpFollowUpDate.CustomFormat = " ";
            _txtNotes.Clear();
            _cmbAssignedStaff.SelectedIndex = -1;
        }
    }
}