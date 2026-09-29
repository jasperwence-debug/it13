using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms
{
    public class LeadEditorForm : Form
    {
        private readonly LeadDto? _existing;
        private TextBox _name = null!, _email = null!, _phone = null!,
                         _source = null!, _value = null!, _notes = null!;
        private ComboBox _status = null!, _priority = null!;
        private Label _error = null!;

        public LeadEditorForm(LeadDto? existing)
        {
            _existing = existing;
            InitializeUI();
        }

        private void InitializeUI()
        {
            bool isEdit = _existing != null;
            Text = isEdit ? "Edit Lead" : "New Lead";
            ClientSize = new Size(560, 720);
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
                Text = isEdit ? "Edit Lead" : "New Lead",
                Font = new Font("Bahnschrift SemiBold", 15F),
                ForeColor = AppTheme.Ink,
                Location = new Point(46, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var sub = new Label
            {
                Text = isEdit ? "Update the lead details below." : "Enter the lead details below.",
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
            _name = AddField("Name *", ref y, _existing?.Name);
            _email = AddField("Email", ref y, _existing?.Email);
            _phone = AddField("Phone", ref y, _existing?.Phone);
            _source = AddField("Source", ref y, _existing?.Source);
            _value = AddField("Expected Value", ref y, _existing?.ExpectedValue.ToString("0.##"));

            _status = AddCombo("Status", ref y, new object[]
            {
                "New", "Contacted", "Qualified", "ProposalSent", "Negotiation", "Won", "Lost"
            }, _existing?.Status ?? "New");

            _priority = AddCombo("Priority", ref y, new object[] { "Low", "Medium", "High" },
                _existing?.Priority ?? "Medium");

            // Notes (multiline)
            var notesLbl = new Label
            {
                Text = "Notes",
                Location = new Point(28, y),
                Size = new Size(504, 20),
                ForeColor = AppTheme.Trace,
                Font = AppTheme.FontLabel,
                BackColor = Color.Transparent
            };
            _notes = new TextBox
            {
                Location = new Point(28, y + 22),
                Size = new Size(504, 56),
                Font = AppTheme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _existing?.Notes ?? ""
            };
            Controls.Add(notesLbl);
            Controls.Add(_notes);
            y += 96;

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

            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                _error.Text = "Name is required.";
                return;
            }

            if (!decimal.TryParse(_value.Text, out var val))
                val = 0m;

            try
            {
                var client = new LeadApiClient();

                if (_existing == null)
                {
                    await client.CreateAsync(new CreateLeadRequest
                    {
                        Name = _name.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Phone = _phone.Text.Trim(),
                        Source = _source.Text.Trim(),
                        Status = _status.SelectedItem?.ToString() ?? "New",
                        Priority = _priority.SelectedItem?.ToString() ?? "Medium",
                        ExpectedValue = val,
                        Notes = _notes.Text.Trim()
                    });
                }
                else
                {
                    await client.UpdateAsync(_existing.Id, new UpdateLeadRequest
                    {
                        Name = _name.Text.Trim(),
                        Email = _email.Text.Trim(),
                        Phone = _phone.Text.Trim(),
                        Source = _source.Text.Trim(),
                        Status = _status.SelectedItem?.ToString() ?? "New",
                        Priority = _priority.SelectedItem?.ToString() ?? "Medium",
                        ExpectedValue = val,
                        Notes = _notes.Text.Trim()
                    });
                }

                DialogResult = DialogResult.OK;
            }
            catch (ApiException ex) { _error.Text = ex.Message; }
            catch (Exception ex) { _error.Text = $"Error: {ex.Message}"; }
        }
    }
}