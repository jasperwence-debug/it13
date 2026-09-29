using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class LeadStatusForm : Form
    {
        private readonly LeadDto _lead;
        private ComboBox _status = null!;
        private Label _error = null!;

        public LeadStatusForm(LeadDto lead)
        {
            _lead = lead;
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Update Lead Status";
            ClientSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.Surface;
            Font = AppTheme.FontBody;

            // Header
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
                Text = $"Lead: {_lead.Name}",
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(44, 48),
                Size = new Size(420, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            var div = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(432, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            // Status label
            var lbl = new Label
            {
                Text = "New Status *",
                Location = new Point(24, 104),
                Size = new Size(432, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);

            _status = new ComboBox
            {
                Location = new Point(24, 126),
                Size = new Size(432, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            _status.Items.AddRange(new object[]
            {
                "New", "Contacted", "Qualified", "ProposalSent",
                "Negotiation", "Won", "Lost"
            });
            _status.SelectedItem = _lead.Status;
            Controls.Add(_status);

            // Error
            _error = new Label
            {
                Location = new Point(24, 172),
                Size = new Size(432, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);

            // Buttons
            var save = new Button
            {
                Text = "Update",
                Location = new Point(24, 210),
                Size = new Size(210, 42),
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
                Location = new Point(246, 210),
                Size = new Size(210, 42),
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
        }

        private async Task UpdateAsync()
        {
            try
            {
                var client = new LeadApiClient();
                await client.UpdateStatusAsync(
                    _lead.Id, _status.SelectedItem?.ToString() ?? "New");
                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}