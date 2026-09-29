using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class InquiryStatusForm : Form
    {
        private readonly CustomerInquiryDto _inquiry;
        private ComboBox _status = null!;
        private Label _resolutionLabel = null!;
        private TextBox _resolution = null!;
        private Label _error = null!;

        public InquiryStatusForm(CustomerInquiryDto inquiry)
        {
            _inquiry = inquiry;
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Update Inquiry Status";
            ClientSize = new Size(520, 440);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBody;

            var dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppTheme.Signal,
                Location = new Point(24, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var title = new Label
            {
                Text = "Update Status",
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(42, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = $"Inquiry: {_inquiry.Subject}",
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(44, 48),
                Size = new Size(456, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            var div = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(472, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            int y = 100;

            var statusLbl = new Label
            {
                Text = "New Status *",
                Location = new Point(24, y),
                Size = new Size(472, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            _status = new ComboBox
            {
                Location = new Point(24, y + 22),
                Size = new Size(472, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            _status.Items.AddRange(new object[] { "Open", "InProgress", "Resolved", "Closed" });
            _status.SelectedItem = _inquiry.Status;
            _status.SelectedIndexChanged += (_, _) => UpdateResolutionVisibility();
            Controls.Add(statusLbl);
            Controls.Add(_status);
            y += 72;

            _resolutionLabel = new Label
            {
                Text = "Resolution *",
                Location = new Point(24, y),
                Size = new Size(472, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                Visible = false,
                BackColor = Color.Transparent
            };
            _resolution = new TextBox
            {
                Location = new Point(24, y + 22),
                Size = new Size(472, 80),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Visible = false,
                Text = _inquiry.Resolution ?? ""
            };
            Controls.Add(_resolutionLabel);
            Controls.Add(_resolution);

            _error = new Label
            {
                Location = new Point(24, 300),
                Size = new Size(472, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);

            var save = new Button
            {
                Text = "Update",
                Location = new Point(24, 336),
                Size = new Size(230, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(save, 8);
            UiRadiusHelper.AttachHoverFeedback(save, AppTheme.Signal, AppTheme.SignalHover);
            save.Click += async (_, _) => await UpdateAsync();

            var cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(266, 336),
                Size = new Size(230, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            cancel.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(cancel, 8);
            UiRadiusHelper.AttachHoverFeedback(cancel, AppTheme.Surface, Color.FromArgb(0xF0, 0xF3, 0xF9));
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            Controls.Add(save);
            Controls.Add(cancel);

            AcceptButton = save;
            CancelButton = cancel;

            UpdateResolutionVisibility();
        }

        private void UpdateResolutionVisibility()
        {
            bool isResolved = (_status.SelectedItem?.ToString() ?? "") == "Resolved";
            _resolutionLabel.Visible = isResolved;
            _resolution.Visible = isResolved;
        }

        private async Task UpdateAsync()
        {
            _error.Text = "";
            var newStatus = _status.SelectedItem?.ToString() ?? "Open";

            if (newStatus == "Resolved" && string.IsNullOrWhiteSpace(_resolution.Text))
            {
                _error.Text = "Resolution text is required when marking as Resolved.";
                return;
            }

            try
            {
                var client = new CustomerInquiryApiClient();
                await client.UpdateStatusAsync(
                    _inquiry.Id,
                    newStatus,
                    newStatus == "Resolved" ? _resolution.Text.Trim() : null);

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}