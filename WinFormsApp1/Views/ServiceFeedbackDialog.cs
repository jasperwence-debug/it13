using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// PHASE 6 — Quality Assurance & Customer Feedback Dialog.
    /// Captures 1-5 star customer ratings, supervisor inspection verdicts, and feedback comments.
    /// </summary>
    public class ServiceFeedbackDialog : Form
    {
        public event Action<WorkOrderDto>? FeedbackSaved;

        private readonly ApiClient _api = new();
        private readonly WorkOrderDto _order;

        private ComboBox _cmbRating = null!;
        private ComboBox _cmbInspection = null!;
        private TextBox _txtInspectedBy = null!;
        private TextBox _txtComments = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private Label _lblStatus = null!;

        public ServiceFeedbackDialog(WorkOrderDto order)
        {
            _order = order ?? throw new ArgumentNullException(nameof(order));
            BuildUI();
        }

        private void BuildUI()
        {
            Text = $"Quality Assurance & Feedback — WO #{_order.ServiceRequestId:d4}";
            Size = new Size(540, 620);
            MinimumSize = new Size(500, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Background;
            Font = new Font("Segoe UI", 9F);

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Theme.Background
            };
            Controls.Add(pnlMain);

            // ── Header ──────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 14, 20, 14)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "⭐  Quality Assurance & Customer Feedback",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"WO #{_order.ServiceRequestId:d4}  •  {_order.CustomerName}  •  {_order.ServiceType}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 18
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // ── Info Summary Card ───────────────────────────────
            var cardInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Theme.Surface,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlMain.Controls.Add(cardInfo);

            var lblInfoDate = new Label
            {
                Text = $"📅 Completed Date: {_order.PreferredDate:MMM dd, yyyy}",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 12),
                AutoSize = true
            };
            cardInfo.Controls.Add(lblInfoDate);

            var lblInfoStaff = new Label
            {
                Text = $"👷 Assigned Staff: {_order.AssignedStaff}",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(260, 12),
                AutoSize = true
            };
            cardInfo.Controls.Add(lblInfoStaff);

            var priceText = _order.ActualPrice.HasValue
                ? $"💰 Billed: {_order.ActualPrice.Value:C2}"
                : (_order.QuotedPrice.HasValue ? $"💰 Quoted: {_order.QuotedPrice.Value:C2}" : "💰 Price: N/A");

            var lblInfoPrice = new Label
            {
                Text = priceText,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 40),
                AutoSize = true
            };
            cardInfo.Controls.Add(lblInfoPrice);

            var lblInfoStatus = new Label
            {
                Text = $"⚡ Status: {_order.Status}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(260, 40),
                AutoSize = true
            };
            cardInfo.Controls.Add(lblInfoStatus);

            var spacer1 = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Theme.Background };
            pnlMain.Controls.Add(spacer1);

            // ── Feedback Inputs Card ────────────────────────────
            var cardInputs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 290,
                BackColor = Theme.Surface,
                Padding = new Padding(20, 16, 20, 16)
            };
            pnlMain.Controls.Add(cardInputs);

            // Rating field
            var lblRating = new Label
            {
                Text = "Customer Satisfaction Rating (1 to 5 Stars): *",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(20, 14),
                AutoSize = true
            };
            cardInputs.Controls.Add(lblRating);

            _cmbRating = new ComboBox
            {
                Location = new Point(20, 36),
                Width = 440,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.BodyFont
            };
            _cmbRating.Items.AddRange(new object[]
            {
                "⭐⭐⭐⭐⭐  5 Stars — Excellent",
                "⭐⭐⭐⭐☆  4 Stars — Very Good",
                "⭐⭐⭐☆☆  3 Stars — Good / Acceptable",
                "⭐⭐☆☆☆  2 Stars — Fair (Follow-Up Recommended)",
                "⭐☆☆☆☆  1 Star — Poor (Quality Alert)"
            });
            _cmbRating.SelectedIndex = _order.Rating.HasValue
                ? Math.Clamp(5 - _order.Rating.Value, 0, 4)
                : 0; // Default: 5 Stars
            cardInputs.Controls.Add(_cmbRating);

            // Inspection verdict
            var lblInspection = new Label
            {
                Text = "Quality Assurance (QA) Inspection Verdict: *",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(20, 74),
                AutoSize = true
            };
            cardInputs.Controls.Add(lblInspection);

            _cmbInspection = new ComboBox
            {
                Location = new Point(20, 96),
                Width = 210,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.BodyFont
            };
            _cmbInspection.Items.AddRange(new object[] { "Passed", "NeedsRework", "Pending" });
            _cmbInspection.SelectedItem = !string.IsNullOrWhiteSpace(_order.InspectionStatus)
                ? _order.InspectionStatus
                : "Passed";
            cardInputs.Controls.Add(_cmbInspection);

            // Inspected By
            var lblInspectedBy = new Label
            {
                Text = "Inspector / Recorded By: *",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(250, 74),
                AutoSize = true
            };
            cardInputs.Controls.Add(lblInspectedBy);

            _txtInspectedBy = new TextBox
            {
                Location = new Point(250, 96),
                Width = 210,
                Font = Theme.BodyFont,
                Text = !string.IsNullOrWhiteSpace(_order.InspectedBy)
                    ? _order.InspectedBy
                    : (SessionManager.CurrentUser?.Username ?? "Supervisor")
            };
            cardInputs.Controls.Add(_txtInspectedBy);

            // Comments
            var lblComments = new Label
            {
                Text = "Customer Feedback & Inspection Notes:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(20, 136),
                AutoSize = true
            };
            cardInputs.Controls.Add(lblComments);

            _txtComments = new TextBox
            {
                Location = new Point(20, 158),
                Width = 440,
                Height = 110,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = Theme.BodyFont,
                PlaceholderText = "Enter customer comments, specific praises, or areas requiring rework...",
                Text = _order.FeedbackNotes ?? string.Empty
            };
            cardInputs.Controls.Add(_txtComments);

            // ── Status Message & Footer Buttons ─────────────────
            _lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(220, 38, 38)
            };
            pnlMain.Controls.Add(_lblStatus);

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.Background
            };
            pnlMain.Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 38
            };
            Theme.ApplySecondaryButtonStyle(_btnCancel);
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(_btnCancel);

            var spacerBtn = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Theme.Background };
            pnlFooter.Controls.Add(spacerBtn);

            _btnSave = new Button
            {
                Text = "✔  Save Feedback & QA",
                Dock = DockStyle.Right,
                Width = 205,
                Height = 38
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await OnSaveFeedbackClickAsync();
            pnlFooter.Controls.Add(_btnSave);

            // Order panels properly inside pnlMain
            pnlMain.Controls.SetChildIndex(pnlFooter, 0);
            pnlMain.Controls.SetChildIndex(_lblStatus, 1);
            pnlMain.Controls.SetChildIndex(cardInputs, 2);
            pnlMain.Controls.SetChildIndex(spacer1, 3);
            pnlMain.Controls.SetChildIndex(cardInfo, 4);
        }

        private async Task OnSaveFeedbackClickAsync()
        {
            int rating = 5 - _cmbRating.SelectedIndex; // index 0 = 5 stars, index 4 = 1 star
            if (rating < 1 || rating > 5)
            {
                _lblStatus.Text = "Please select a valid rating (1-5 stars).";
                return;
            }

            var inspection = _cmbInspection.SelectedItem?.ToString() ?? "Passed";
            var inspectedBy = _txtInspectedBy.Text.Trim();
            if (string.IsNullOrWhiteSpace(inspectedBy))
            {
                _lblStatus.Text = "Please specify who inspected or recorded this feedback.";
                _txtInspectedBy.Focus();
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";
            _lblStatus.ForeColor = Theme.TextMuted;
            _lblStatus.Text = "Submitting feedback to server...";

            var dto = new ServiceFeedbackDto
            {
                Rating = rating,
                InspectionStatus = inspection,
                InspectedBy = inspectedBy,
                FeedbackNotes = _txtComments.Text.Trim()
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
                _btnSave.Text = "✔  Save Feedback & QA";
            }
        }
    }
}
