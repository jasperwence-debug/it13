using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class InquiryEditorForm : Form
    {
        private readonly CustomerInquiryDto? _existing;
        private readonly bool _readOnly;

        private ComboBox _customer = null!;
        private ComboBox _type = null!;
        private TextBox _subject = null!;
        private TextBox _description = null!;
        private ComboBox _priority = null!;
        private ComboBox _assignedTo = null!;
        private Label _error = null!;

        private List<CustomerDto> _customers = new();
        private List<UserSummary> _users = new();

        public InquiryEditorForm(CustomerInquiryDto? existing, bool readOnly = false)
        {
            _existing = existing;
            _readOnly = readOnly;
            InitializeUI();
            _ = LoadPickersAsync();
        }

        private void InitializeUI()
        {
            bool isView = _readOnly;
            bool isEdit = !_readOnly && _existing != null;

            Text = isView ? "View Inquiry" : (isEdit ? "Edit Inquiry" : "New Inquiry");
            ClientSize = new Size(620, 780);
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
                Location = new Point(28, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(dot);

            var header = new Label
            {
                Text = isView ? "Inquiry Details" : (isEdit ? "Edit Inquiry" : "New Inquiry"),
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(46, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var sub = new Label
            {
                Text = isView
                    ? "Read-only view. Close to return."
                    : "Log a customer inquiry, complaint, or feedback.",
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
                Size = new Size(564, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            int y = 104;
            _customer = AddCombo("Customer *", ref y, 564);
            _customer.DisplayMember = nameof(CustomerDto.FullName);
            _customer.ValueMember = nameof(CustomerDto.Id);

            _type = AddCombo("Type *", ref y, 564);
            _type.Items.AddRange(new object[] { "Inquiry", "Complaint", "Feedback" });
            _type.SelectedItem = _existing?.Type ?? "Inquiry";

            _subject = AddField("Subject *", ref y, 564, _existing?.Subject);

            // Description multiline
            var descLbl = new Label
            {
                Text = "Description *",
                Location = new Point(28, y),
                Size = new Size(564, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            _description = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(564, 110),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _existing?.Description ?? ""
            };
            Controls.Add(descLbl);
            Controls.Add(_description);
            y += 150;

            _priority = AddCombo("Priority *", ref y, 564);
            _priority.Items.AddRange(new object[] { "Low", "Medium", "High", "Urgent" });
            _priority.SelectedItem = _existing?.Priority ?? "Medium";

            _assignedTo = AddCombo("Assigned To", ref y, 564);

            _error = new Label
            {
                Location = new Point(28, y),
                Size = new Size(564, 20),
                ForeColor = AppTheme.DangerText,
                Font = AppTheme.FontBodySmall,
                BackColor = Color.Transparent
            };
            Controls.Add(_error);
            y += 30;

            var save = new Button
            {
                Text = "Save",
                Location = new Point(28, y),
                Size = new Size(272, 42),
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
                Text = isView ? "Close" : "Cancel",
                Location = new Point(320, y),
                Size = new Size(272, 42),
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

            if (isView)
            {
                save.Visible = false;
                cancel.Location = new Point(28, y);
                cancel.Size = new Size(564, 42);
                cancel.BackColor = AppTheme.Signal;
                cancel.ForeColor = Color.White;
                cancel.FlatAppearance.BorderSize = 0;
                UiRadiusHelper.StyleButton(cancel, 8);
                UiRadiusHelper.AttachHoverFeedback(cancel, AppTheme.Signal, AppTheme.SignalHover);
            }

            Controls.Add(save);
            Controls.Add(cancel);

            if (isView)
            {
                foreach (Control c in Controls)
                {
                    if (c is TextBox tb)
                    {
                        tb.ReadOnly = true;
                        tb.BackColor = Color.FromArgb(0xF7, 0xF8, 0xFB);
                    }
                    else if (c is ComboBox cb)
                    {
                        cb.Enabled = false;
                        cb.BackColor = Color.FromArgb(0xF7, 0xF8, 0xFB);
                    }
                }
            }

            int maxY = 0;
            foreach (Control c in Controls)
                maxY = Math.Max(maxY, c.Bottom);
            ClientSize = new Size(ClientSize.Width, maxY + 32);

            if (!isView) AcceptButton = save;
            CancelButton = cancel;
        }

        private TextBox AddField(string label, ref int y, int width, string? value)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(width, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var tb = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(width, 32),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Text = value ?? ""
            };
            Controls.Add(lbl);
            Controls.Add(tb);
            y += 72;
            return tb;
        }

        private ComboBox AddCombo(string label, ref int y, int width)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(width, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var cb = new ComboBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(width, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            Controls.Add(lbl);
            Controls.Add(cb);
            y += 72;
            return cb;
        }

        private async Task LoadPickersAsync()
        {
            try
            {
                var custClient = new CustomerApiClient();
                var resp = await custClient.ListAsync();
                _customers = resp?.Items ?? new List<CustomerDto>();
                _customer.DataSource = _customers;

                if (_existing != null)
                {
                    var match = _customers.FirstOrDefault(c => c.Id == _existing.CustomerId);
                    if (match != null) _customer.SelectedItem = match;
                }

                var userClient = new UserApiClient();
                _users = await userClient.ListAssignableAsync() ?? new List<UserSummary>();

                _assignedTo.Items.Clear();
                _assignedTo.Items.Add("(Auto-assign to me)");
                foreach (var u in _users)
                    _assignedTo.Items.Add(u);
                _assignedTo.DisplayMember = nameof(UserSummary.Name);
                _assignedTo.SelectedIndex = 0;

                if (_existing?.AssignedUserId != null)
                {
                    var match = _users.FirstOrDefault(u => u.Id == _existing.AssignedUserId.Value);
                    if (match != null) _assignedTo.SelectedItem = match;
                }
            }
            catch { }
        }

        private async Task SaveAsync()
        {
            _error.Text = "";

            if (_customer.SelectedItem is not CustomerDto cust)
            {
                _error.Text = "Please pick a customer.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_subject.Text))
            {
                _error.Text = "Subject is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_description.Text))
            {
                _error.Text = "Description is required.";
                return;
            }

            try
            {
                var client = new CustomerInquiryApiClient();

                if (_existing == null)
                {
                    Guid? assignedUserId = null;
                    if (_assignedTo.SelectedItem is UserSummary u)
                        assignedUserId = u.Id;

                    await client.CreateAsync(new CreateCustomerInquiryRequest
                    {
                        CustomerId = cust.Id,
                        Type = _type.SelectedItem?.ToString() ?? "Inquiry",
                        Subject = _subject.Text.Trim(),
                        Description = _description.Text.Trim(),
                        Priority = _priority.SelectedItem?.ToString(),
                        AssignedUserId = assignedUserId
                    });
                }
                else
                {
                    await client.UpdateAsync(_existing.Id, new UpdateCustomerInquiryRequest
                    {
                        Subject = _subject.Text.Trim(),
                        Description = _description.Text.Trim(),
                        Priority = _priority.SelectedItem?.ToString() ?? "Medium"
                    });
                }

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}