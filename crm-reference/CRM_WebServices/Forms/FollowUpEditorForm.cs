using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class FollowUpEditorForm : Form
    {
        private ComboBox _linkType = null!, _linkTarget = null!;
        private DateTimePicker _dueDate = null!;
        private TextBox _title = null!, _description = null!;
        private Label _error = null!;

        private List<CustomerDto> _customers = new();
        private List<LeadDto> _leads = new();

        public FollowUpEditorForm()
        {
            InitializeUI();
            _ = LoadTargetsAsync();
        }

        private void InitializeUI()
        {
            Text = "New Follow-up";
            ClientSize = new Size(560, 660);
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
                Location = new Point(28, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var header = new Label
            {
                Text = "New Follow-up",
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(46, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var sub = new Label
            {
                Text = "Schedule a reminder to reach out.",
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(48, 52),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            var div = new Panel
            {
                Location = new Point(28, 84),
                Size = new Size(504, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            int y = 104;

            // Linked To
            AddLabel("Linked To *", y);
            _linkType = new ComboBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(180, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            _linkType.Items.AddRange(new object[] { "Customer", "Lead" });
            _linkType.SelectedIndex = 0;
            _linkType.SelectedIndexChanged += (_, _) => PopulateTargets();
            Controls.Add(_linkType);

            _linkTarget = new ComboBox
            {
                Location = new Point(220, y + 22),
                Size = new Size(312, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            Controls.Add(_linkTarget);
            y += 72;

            // Title
            AddLabel("Title *", y);
            _title = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            Controls.Add(_title);
            y += 72;

            // Description
            AddLabel("Description", y);
            _description = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 70),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(_description);
            y += 100;

            // Due Date
            AddLabel("Due Date *", y);
            _dueDate = new DateTimePicker
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 32),
                Font = AppTheme.FontBody,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                Value = DateTime.Now.AddDays(1)
            };
            Controls.Add(_dueDate);
            y += 72;

            // Error
            _error = new Label
            {
                Location = new Point(28, y),
                Size = new Size(504, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);
            y += 30;

            // Buttons
            var save = new Button
            {
                Text = "Save",
                Location = new Point(28, y),
                Size = new Size(242, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Signal,
                ForeColor = Color.White,
                Font = new Font("Bahnschrift SemiBold", 10F),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            UiRadiusHelper.StyleButton(save, 8);
            UiRadiusHelper.AttachHoverFeedback(save, AppTheme.Signal, AppTheme.SignalHover);
            save.Click += async (_, _) => await SaveAsync();

            var cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(290, y),
                Size = new Size(242, 42),
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

            int maxY = 0;
            foreach (Control c in Controls)
                maxY = Math.Max(maxY, c.Bottom);
            ClientSize = new Size(ClientSize.Width, maxY + 32);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private void AddLabel(string text, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(28, y),
                Size = new Size(504, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            });
        }

        private async Task LoadTargetsAsync()
        {
            try
            {
                var custClient = new CustomerApiClient();
                var leadClient = new LeadApiClient();
                _customers = (await custClient.ListAsync())?.Items ?? new();
                _leads = (await leadClient.ListAsync(pageSize: 100))?.Items ?? new();
                PopulateTargets();
            }
            catch { }
        }

        private void PopulateTargets()
        {
            _linkTarget.Items.Clear();
            if (_linkType.SelectedItem?.ToString() == "Customer")
                foreach (var c in _customers)
                    _linkTarget.Items.Add($"{c.FullName} ({c.Email})");
            else
                foreach (var l in _leads)
                    _linkTarget.Items.Add($"{l.Name} ({l.Email})");
            if (_linkTarget.Items.Count > 0)
                _linkTarget.SelectedIndex = 0;
        }

        private async Task SaveAsync()
        {
            _error.Text = "";

            if (_linkTarget.SelectedIndex < 0)
            {
                _error.Text = "Pick a customer or lead.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_title.Text))
            {
                _error.Text = "Title is required.";
                return;
            }

            try
            {
                Guid? customerId = null, leadId = null;
                if (_linkType.SelectedItem?.ToString() == "Customer")
                    customerId = _customers[_linkTarget.SelectedIndex].Id;
                else
                    leadId = _leads[_linkTarget.SelectedIndex].Id;

                var client = new FollowUpApiClient();
                await client.CreateAsync(new CreateFollowUpRequest
                {
                    CustomerId = customerId,
                    LeadId = leadId,
                    Title = _title.Text.Trim(),
                    Description = _description.Text.Trim(),
                    DueDate = _dueDate.Value
                });

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}