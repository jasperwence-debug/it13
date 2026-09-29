using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class CustomerEditorForm : Form
    {
        private readonly CustomerDto? _existing;
        private readonly bool _readOnly;

        private TextBox _first = null!, _last = null!, _email = null!,
                         _phone = null!, _company = null!, _address = null!;
        private ComboBox _status = null!;
        private Label _error = null!;

        public CustomerEditorForm(CustomerDto? existing, bool readOnly = false)
        {
            _existing = existing;
            _readOnly = readOnly;
            InitializeUI();
        }

        private void InitializeUI()
        {
            bool isView = _readOnly;
            bool isEdit = !_readOnly && _existing != null;

            Text = isView ? "View Customer" : (isEdit ? "Edit Customer" : "New Customer");
            ClientSize = new Size(560, 740);
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
                Text = isView ? "Customer Details" : (isEdit ? "Edit Customer" : "New Customer"),
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
                    : (isEdit ? "Update the customer details below." : "Enter customer details below."),
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(48, 52),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(sub);

            // Divider under header
            var div = new Panel
            {
                Location = new Point(28, 84),
                Size = new Size(504, 1),
                BackColor = AppTheme.Line
            };
            Controls.Add(div);

            int y = 104;

            _first = AddField("First Name *", ref y, _existing?.FirstName);
            _last = AddField("Last Name *", ref y, _existing?.LastName);
            _email = AddField("Email *", ref y, _existing?.Email);
            _phone = AddField("Phone", ref y, _existing?.Phone);
            _company = AddField("Company", ref y, _existing?.Company);
            _address = AddField("Address", ref y, _existing?.Address);

            _status = AddCombo("Status", ref y, new object[] { "Active", "Inactive", "Prospect" }, _existing?.Status ?? "Active");

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
                Text = isView ? "Close" : "Cancel",
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

            if (isView)
            {
                save.Visible = false;
                cancel.Location = new Point(28, y);
                cancel.Size = new Size(504, 42);
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

            // Auto-size
            int maxY = 0;
            foreach (Control c in Controls)
                maxY = Math.Max(maxY, c.Bottom);
            ClientSize = new Size(ClientSize.Width, maxY + 32);

            if (!isView) AcceptButton = save;
            CancelButton = cancel;
        }

        private TextBox AddField(string label, ref int y, string? value)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(504, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var tb = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 32),
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

        private ComboBox AddCombo(string label, ref int y, object[] items, string selected)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(28, y),
                Size = new Size(504, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            var cb = new ComboBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            cb.Items.AddRange(items);
            cb.SelectedItem = selected;
            Controls.Add(lbl);
            Controls.Add(cb);
            y += 72;
            return cb;
        }

        private async Task SaveAsync()
        {
            _error.Text = "";

            if (string.IsNullOrWhiteSpace(_first.Text) ||
                string.IsNullOrWhiteSpace(_last.Text) ||
                string.IsNullOrWhiteSpace(_email.Text))
            {
                _error.Text = "First name, last name and email are required.";
                return;
            }

            try
            {
                var client = new CustomerApiClient();

                if (_existing == null)
                {
                    await client.CreateAsync(new CreateCustomerRequest
                    {
                        FirstName = _first.Text.Trim(),
                        LastName = _last.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Phone = _phone.Text.Trim(),
                        Company = _company.Text.Trim(),
                        Address = _address.Text.Trim(),
                        Status = _status.SelectedItem?.ToString() ?? "Active"
                    });
                }
                else
                {
                    await client.UpdateAsync(_existing.Id, new UpdateCustomerRequest
                    {
                        FirstName = _first.Text.Trim(),
                        LastName = _last.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Phone = _phone.Text.Trim(),
                        Company = _company.Text.Trim(),
                        Address = _address.Text.Trim(),
                        Status = _status.SelectedItem?.ToString() ?? "Active"
                    });
                }

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}