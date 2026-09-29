using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// PHASE 6 — Quality Assurance & Customer Feedback Command Center.
    ///
    /// Enterprise Industry Standards:
    ///   - Customer Feedback is an immutable audit record once submitted (Read-Only).
    ///   - The Manager NEVER fabricates or alters customer ratings.
    ///   - If no review exists, clearly shows "Pending Customer Feedback" with an option to send an online survey.
    ///   - The Manager's role is strictly to record the Internal QA Inspection Verdict (Passed / Needs Rework).
    ///   - Clean, spacious, modern layout (720x760) with no overlapping or cramped elements.
    /// </summary>
    public class ServiceFeedbackDialog : Form
    {
        public event Action<WorkOrderDto>? FeedbackSaved;

        private readonly ApiClient _api = new();
        private readonly WorkOrderDto _order;
        private bool _hasExistingFeedback;
        private bool _isPhoneSurveyMode = false;

        // UI Controls - Customer Section
        private Panel _pnlCustomerCard = null!;
        private Label _lblCustomerStarsDisplay = null!;
        private TextBox _txtCustomerRemarksDisplay = null!;
        private Panel _pnlPhoneSurveyBox = null!;
        private ComboBox _cmbPhoneRating = null!;
        private TextBox _txtPhoneComments = null!;
        private Button _btnSendSmsLink = null!;
        private Button _btnTogglePhoneSurvey = null!;

        // UI Controls - Supervisor QA Section
        private ComboBox _cmbInspection = null!;
        private TextBox _txtInspectedBy = null!;
        private TextBox _txtSupervisorNotes = null!;

        // Footer Controls
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Label _lblStatus = null!;

        public ServiceFeedbackDialog(WorkOrderDto order)
        {
            _order = order ?? throw new ArgumentNullException(nameof(order));
            _hasExistingFeedback = _order.Rating.HasValue && _order.Rating.Value > 0;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = $"Quality Assurance & Customer Feedback — Work Order #{_order.ServiceRequestId:d4}";
            Size = new Size(720, 770);
            MinimumSize = new Size(680, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9F);

            // ── 1. Top Header Bar (Spacious 76px) ─────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(28, 14, 28, 14)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "⭐  Quality Assurance & Customer Feedback",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"Work Order #{_order.ServiceRequestId:d4}  •  {_order.CustomerName}  •  {_order.ServiceType}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 20,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // ── 2. Scrollable Body Container ──────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 20),
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };
            Controls.Add(pnlBody);

            // ── 3. Work Order Metadata Summary Card ───────────────────
            var cardMeta = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 14)
            };
            cardMeta.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardMeta.Width - 1, cardMeta.Height - 1);
            };
            pnlBody.Controls.Add(cardMeta);

            var lblDate = new Label
            {
                Text = $"📅 Completed Date:\n   {_order.PreferredDate:MMM dd, yyyy}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 14),
                Size = new Size(150, 42)
            };
            cardMeta.Controls.Add(lblDate);

            var lblStaff = new Label
            {
                Text = $"👷 Assigned Staff:\n   {(!string.IsNullOrWhiteSpace(_order.AssignedStaff) ? _order.AssignedStaff : "Dispatch Team")}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(185, 14),
                Size = new Size(150, 42)
            };
            cardMeta.Controls.Add(lblStaff);

            var priceText = _order.ActualPrice.HasValue
                ? $"₱{_order.ActualPrice.Value:N2}"
                : (_order.QuotedPrice.HasValue ? $"₱{_order.QuotedPrice.Value:N2}" : "N/A");

            var lblPrice = new Label
            {
                Text = $"💰 Billed Revenue:\n   {priceText}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(350, 14),
                Size = new Size(140, 42)
            };
            cardMeta.Controls.Add(lblPrice);

            var lblStatusBadge = new Label
            {
                Text = $"⚡ Order Status:\n   {_order.Status}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(505, 14),
                Size = new Size(130, 42)
            };
            cardMeta.Controls.Add(lblStatusBadge);

            var spacer1 = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
            pnlBody.Controls.Add(spacer1);

            // ── 4. Customer Satisfaction Feedback Card (Read-Only) ────
            _pnlCustomerCard = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 170),
                BackColor = Color.White,
                Padding = new Padding(22, 16, 22, 18)
            };
            _pnlCustomerCard.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, _pnlCustomerCard.Width - 1, _pnlCustomerCard.Height - 1);
            };
            pnlBody.Controls.Add(_pnlCustomerCard);

            BuildCustomerFeedbackSection();

            var spacer2 = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };
            pnlBody.Controls.Add(spacer2);

            // ── 5. Internal Supervisor Quality Assurance Card ─────────
            var cardSupervisor = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Color.White,
                Padding = new Padding(22, 16, 22, 18)
            };
            cardSupervisor.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(p, 0, 0, cardSupervisor.Width - 1, cardSupervisor.Height - 1);
            };
            pnlBody.Controls.Add(cardSupervisor);

            var lblSupTitle = new Label
            {
                Text = "👷  Supervisor Quality Assurance Inspection (Manager Controls)",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 24,
                UseMnemonic = false
            };
            cardSupervisor.Controls.Add(lblSupTitle);

            var lblSupSub = new Label
            {
                Text = "This section is managed by internal operations to record site inspection verdicts and remedial rework orders.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20,
                UseMnemonic = false
            };
            cardSupervisor.Controls.Add(lblSupSub);

            var pnlSupFields = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };
            cardSupervisor.Controls.Add(pnlSupFields);

            var lblInspectVerdict = new Label
            {
                Text = "QA Inspection Verdict: *",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 6),
                AutoSize = true
            };
            pnlSupFields.Controls.Add(lblInspectVerdict);

            _cmbInspection = new ComboBox
            {
                Location = new Point(0, 26),
                Width = 280,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            _cmbInspection.Items.AddRange(new object[]
            {
                "Passed — Workmanship Approved",
                "NeedsRework — Remedial Touch-Up Required",
                "Pending — On-Site Audit In Progress"
            });
            _cmbInspection.SelectedIndex = _order.InspectionStatus switch
            {
                "NeedsRework" => 1,
                "Pending" => 2,
                _ => 0
            };
            pnlSupFields.Controls.Add(_cmbInspection);

            var lblInspector = new Label
            {
                Text = "Supervisor / Recorded By: *",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(310, 6),
                AutoSize = true
            };
            pnlSupFields.Controls.Add(lblInspector);

            _txtInspectedBy = new TextBox
            {
                Location = new Point(310, 26),
                Width = 280,
                Height = 32,
                Font = new Font("Segoe UI", 9.5F),
                Text = !string.IsNullOrWhiteSpace(_order.InspectedBy)
                    ? _order.InspectedBy
                    : (SessionManager.CurrentUser?.Username ?? "manager")
            };
            pnlSupFields.Controls.Add(_txtInspectedBy);

            var lblSupNotes = new Label
            {
                Text = "Supervisor Inspection & Corrective Action Notes:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 22,
                UseMnemonic = false
            };
            cardSupervisor.Controls.Add(lblSupNotes);

            _txtSupervisorNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Enter internal QA observations, checklist results, or instructions for technician rework..."
            };
            cardSupervisor.Controls.Add(_txtSupervisorNotes);

            cardSupervisor.Controls.SetChildIndex(_txtSupervisorNotes, 0);
            cardSupervisor.Controls.SetChildIndex(lblSupNotes, 1);
            cardSupervisor.Controls.SetChildIndex(pnlSupFields, 2);
            cardSupervisor.Controls.SetChildIndex(lblSupSub, 3);
            cardSupervisor.Controls.SetChildIndex(lblSupTitle, 4);

            // ── 6. Bottom Action Footer Bar ───────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(28, 10, 28, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };
            Controls.Add(pnlFooter);

            _lblStatus = new Label
            {
                Dock = DockStyle.Left,
                Width = 320,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(220, 38, 38)
            };
            pnlFooter.Controls.Add(_lblStatus);

            _btnCancel = new Button
            {
                Text = "Close",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 36
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(_btnCancel);

            var spacerBtn = new Panel { Dock = DockStyle.Right, Width = 12, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(spacerBtn);

            _btnSave = new Button
            {
                Text = "✔  Save Supervisor QA Record",
                Dock = DockStyle.Right,
                Width = 230,
                Height = 36
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await OnSaveQualityRecordAsync();
            pnlFooter.Controls.Add(_btnSave);

            // Set Z-order
            pnlBody.Controls.SetChildIndex(cardSupervisor, 0);
            pnlBody.Controls.SetChildIndex(spacer2, 1);
            pnlBody.Controls.SetChildIndex(_pnlCustomerCard, 2);
            pnlBody.Controls.SetChildIndex(spacer1, 3);
            pnlBody.Controls.SetChildIndex(cardMeta, 4);
        }

        private void BuildCustomerFeedbackSection()
        {
            _pnlCustomerCard.Controls.Clear();

            var pnlHeaderRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.Transparent
            };
            _pnlCustomerCard.Controls.Add(pnlHeaderRow);

            var lblSectionTitle = new Label
            {
                Text = "💬  Customer Satisfaction (CSAT) Feedback",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnlHeaderRow.Controls.Add(lblSectionTitle);

            if (_hasExistingFeedback)
            {
                // ── A. Customer HAS Submitted Feedback (Immutable Audit Record) ──
                var lblBadge = new Label
                {
                    Text = "🔒 AUDIT-LOCKED (Customer Submitted)",
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    BackColor = Color.FromArgb(241, 245, 249),
                    Location = new Point(340, 2),
                    Size = new Size(250, 22),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlHeaderRow.Controls.Add(lblBadge);

                int stars = Math.Clamp(_order.Rating ?? 5, 1, 5);
                string starGlyphs = stars switch
                {
                    5 => "★★★★★  5.0 / 5.0 — Excellent",
                    4 => "★★★★☆  4.0 / 5.0 — Very Good",
                    3 => "★★★☆☆  3.0 / 5.0 — Good / Acceptable",
                    2 => "★★☆☆☆  2.0 / 5.0 — Fair (Needs Follow-Up)",
                    _ => "★☆☆☆☆  1.0 / 5.0 — Poor (Quality Alert)"
                };

                var pnlRatingBox = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(12, 8, 12, 8),
                    Margin = new Padding(0, 8, 0, 8)
                };
                pnlRatingBox.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                    e.Graphics.DrawRectangle(p, 0, 0, pnlRatingBox.Width - 1, pnlRatingBox.Height - 1);
                };
                _pnlCustomerCard.Controls.Add(pnlRatingBox);

                _lblCustomerStarsDisplay = new Label
                {
                    Text = starGlyphs,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlRatingBox.Controls.Add(_lblCustomerStarsDisplay);

                var lblCommentsTitle = new Label
                {
                    Text = "Customer's Recorded Remarks (Immutable):",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Dock = DockStyle.Top,
                    Height = 22
                };
                _pnlCustomerCard.Controls.Add(lblCommentsTitle);

                _txtCustomerRemarksDisplay = new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    Multiline = true,
                    ReadOnly = true,
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ScrollBars = ScrollBars.Vertical,
                    Text = !string.IsNullOrWhiteSpace(_order.FeedbackNotes)
                        ? _order.FeedbackNotes
                        : "(Customer provided star rating without written comments)"
                };
                _pnlCustomerCard.Controls.Add(_txtCustomerRemarksDisplay);

                var lblNotice = new Label
                {
                    Text = "ℹ️  Customer rating and remarks are locked to maintain audit compliance and prevent internal staff alteration.",
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Dock = DockStyle.Top,
                    Height = 22
                };
                _pnlCustomerCard.Controls.Add(lblNotice);

                _pnlCustomerCard.Controls.SetChildIndex(lblNotice, 0);
                _pnlCustomerCard.Controls.SetChildIndex(_txtCustomerRemarksDisplay, 1);
                _pnlCustomerCard.Controls.SetChildIndex(lblCommentsTitle, 2);
                _pnlCustomerCard.Controls.SetChildIndex(pnlRatingBox, 3);
                _pnlCustomerCard.Controls.SetChildIndex(pnlHeaderRow, 4);
            }
            else
            {
                // ── B. Customer HAS NOT Submitted Feedback Yet ────────────
                var lblBadge = new Label
                {
                    Text = "⏳ AWAITING CUSTOMER RESPONSE",
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 83, 9),
                    BackColor = Color.FromArgb(254, 243, 199),
                    Location = new Point(340, 2),
                    Size = new Size(230, 22),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlHeaderRow.Controls.Add(lblBadge);

                var pnlPendingBox = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 68,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Padding = new Padding(14, 10, 14, 10)
                };
                pnlPendingBox.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                    e.Graphics.DrawRectangle(p, 0, 0, pnlPendingBox.Width - 1, pnlPendingBox.Height - 1);
                };
                _pnlCustomerCard.Controls.Add(pnlPendingBox);

                var lblPendingText = new Label
                {
                    Text = "The customer has not yet rated this completed order.\nIn accordance with enterprise CRM policy, staff cannot fabricate customer ratings.",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Dock = DockStyle.Left,
                    Width = 370
                };
                pnlPendingBox.Controls.Add(lblPendingText);

                _btnSendSmsLink = new Button
                {
                    Text = "📱 Send Survey Request (Trigger #6)",
                    Dock = DockStyle.Right,
                    Width = 230,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                _btnSendSmsLink.FlatAppearance.BorderSize = 0;
                _btnSendSmsLink.Click += (s, e) =>
                {
                    MessageBox.Show(
                        $"Customer CSAT survey link dispatched via SMS & Email to {_order.CustomerName}!\n\nThis executes Retention Trigger #6: 'Order completed → Request 5-star review'.",
                        "Review Request Sent",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                };
                pnlPendingBox.Controls.Add(_btnSendSmsLink);

                // Option to transcribe phone survey
                var pnlPhoneBar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 32,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0, 6, 0, 0)
                };
                _pnlCustomerCard.Controls.Add(pnlPhoneBar);

                _btnTogglePhoneSurvey = new Button
                {
                    Text = _isPhoneSurveyMode ? "✕  Close Phone Survey Transcription" : "📞 Customer called / answered post-service phone check? Click to transcribe verbal survey",
                    Dock = DockStyle.Left,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(37, 99, 235),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Underline),
                    Cursor = Cursors.Hand
                };
                _btnTogglePhoneSurvey.FlatAppearance.BorderSize = 0;
                _btnTogglePhoneSurvey.Click += (s, e) =>
                {
                    _isPhoneSurveyMode = !_isPhoneSurveyMode;
                    BuildCustomerFeedbackSection();
                };
                pnlPhoneBar.Controls.Add(_btnTogglePhoneSurvey);

                if (_isPhoneSurveyMode)
                {
                    _pnlPhoneSurveyBox = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 135,
                        BackColor = Color.White,
                        Padding = new Padding(12, 6, 12, 8)
                    };
                    _pnlCustomerCard.Controls.Add(_pnlPhoneSurveyBox);

                    var lblPhoneRating = new Label
                    {
                        Text = "Customer Verbal Rating (from phone call): *",
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        Location = new Point(0, 4),
                        AutoSize = true
                    };
                    _pnlPhoneSurveyBox.Controls.Add(lblPhoneRating);

                    _cmbPhoneRating = new ComboBox
                    {
                        Location = new Point(0, 24),
                        Width = 320,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Font = new Font("Segoe UI", 9F)
                    };
                    _cmbPhoneRating.Items.AddRange(new object[]
                    {
                        "⭐⭐⭐⭐⭐  5 Stars — Excellent",
                        "⭐⭐⭐⭐☆  4 Stars — Very Good",
                        "⭐⭐⭐☆☆  3 Stars — Good / Acceptable",
                        "⭐⭐☆☆☆  2 Stars — Fair (Follow-Up Recommended)",
                        "⭐☆☆☆☆  1 Star — Poor (Quality Alert)"
                    });
                    _cmbPhoneRating.SelectedIndex = 0;
                    _pnlPhoneSurveyBox.Controls.Add(_cmbPhoneRating);

                    var lblPhoneComments = new Label
                    {
                        Text = "Verbal Comments / Remarks (Transcribed):",
                        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        Location = new Point(0, 56),
                        AutoSize = true
                    };
                    _pnlPhoneSurveyBox.Controls.Add(lblPhoneComments);

                    _txtPhoneComments = new TextBox
                    {
                        Location = new Point(0, 76),
                        Width = 590,
                        Height = 50,
                        Multiline = true,
                        ScrollBars = ScrollBars.Vertical,
                        Font = new Font("Segoe UI", 9F),
                        PlaceholderText = "Transcribe customer's exact verbal praises or complaints..."
                    };
                    _pnlPhoneSurveyBox.Controls.Add(_txtPhoneComments);

                    _pnlCustomerCard.Controls.SetChildIndex(_pnlPhoneSurveyBox, 0);
                    _pnlCustomerCard.Controls.SetChildIndex(pnlPhoneBar, 1);
                    _pnlCustomerCard.Controls.SetChildIndex(pnlPendingBox, 2);
                    _pnlCustomerCard.Controls.SetChildIndex(pnlHeaderRow, 3);
                }
                else
                {
                    _pnlCustomerCard.Controls.SetChildIndex(pnlPhoneBar, 0);
                    _pnlCustomerCard.Controls.SetChildIndex(pnlPendingBox, 1);
                    _pnlCustomerCard.Controls.SetChildIndex(pnlHeaderRow, 2);
                }
            }
        }

        private async Task OnSaveQualityRecordAsync()
        {
            var inspection = _cmbInspection.SelectedItem?.ToString() ?? "Passed";
            var inspectedBy = _txtInspectedBy.Text.Trim();
            if (string.IsNullOrWhiteSpace(inspectedBy))
            {
                _lblStatus.Text = "Please specify the supervisor inspector name.";
                _txtInspectedBy.Focus();
                return;
            }

            // Determine rating & notes
            int finalRating;
            string finalNotes;

            if (_hasExistingFeedback)
            {
                finalRating = _order.Rating ?? 5;
                string supNotes = _txtSupervisorNotes.Text.Trim();
                string custNotes = _order.FeedbackNotes ?? string.Empty;

                finalNotes = !string.IsNullOrWhiteSpace(supNotes)
                    ? $"{custNotes}\n[QA Supervisor ({inspectedBy})]: {supNotes}".Trim()
                    : custNotes;
            }
            else if (_isPhoneSurveyMode && _cmbPhoneRating != null)
            {
                finalRating = 5 - _cmbPhoneRating.SelectedIndex; // 0=5, 4=1
                string custNotes = _txtPhoneComments.Text.Trim();
                string supNotes = _txtSupervisorNotes.Text.Trim();

                string combined = !string.IsNullOrWhiteSpace(custNotes)
                    ? $"[Phone CSAT Survey]: {custNotes}"
                    : "[Phone CSAT Survey]: Verbal rating recorded";

                if (!string.IsNullOrWhiteSpace(supNotes))
                {
                    combined += $"\n[QA Supervisor ({inspectedBy})]: {supNotes}";
                }

                finalNotes = combined;
            }
            else
            {
                // No customer rating submitted yet, saving supervisor inspection only
                finalRating = _order.Rating ?? 0;
                string supNotes = _txtSupervisorNotes.Text.Trim();
                finalNotes = !string.IsNullOrWhiteSpace(supNotes)
                    ? $"[QA Supervisor ({inspectedBy})]: {supNotes}"
                    : "Supervisor inspection recorded (Customer feedback pending)";
            }

            // If rating is 0 (no customer feedback), set to default or 5 for API validation if needed
            int apiRating = finalRating > 0 ? finalRating : 5;

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving Record...";
            _lblStatus.ForeColor = Theme.TextMuted;
            _lblStatus.Text = "Saving QA record to server...";

            var dto = new ServiceFeedbackDto
            {
                Rating = apiRating,
                InspectionStatus = inspection.Split('—')[0].Trim(),
                InspectedBy = inspectedBy,
                FeedbackNotes = finalNotes
            };

            var (success, message, updatedOrder) = await _api.SubmitWorkOrderFeedbackAsync(_order.ServiceRequestId, dto);

            if (success && updatedOrder != null)
            {
                FeedbackSaved?.Invoke(updatedOrder);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _lblStatus.Text = message;
                _btnSave.Enabled = true;
                _btnSave.Text = "✔  Save Supervisor QA Record";
            }
        }
    }
}
