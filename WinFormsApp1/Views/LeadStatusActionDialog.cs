using System;
using System.Drawing;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Modal dialog to enter and submit a Quoted Price for a Lead,
    /// transitioning its lifecycle status to 'Quoted' so it can be converted.
    /// </summary>
    public class LeadQuoteDialog : Form
    {
        private readonly LeadDto _lead;
        private TextBox _txtPrice = null!;
        private Label _lblError = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public decimal QuotedPrice { get; private set; }

        public LeadQuoteDialog(LeadDto lead)
        {
            _lead = lead ?? throw new ArgumentNullException(nameof(lead));
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"Quote Price — {_lead.LeadName}";
            Size = new Size(460, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            // Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "💲 Set Quoted Price & Qualify Lead",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"Lead: {_lead.LeadName} (LD-{_lead.LeadId:D4})",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 16)
            };
            Controls.Add(pnlBody);

            var lblPrompt = new Label
            {
                Text = "Enter agreed quotation amount for this prospective customer:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 24,
                UseMnemonic = false
            };
            pnlBody.Controls.Add(lblPrompt);

            var pnlInputRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(0, 2, 0, 2)
            };
            pnlBody.Controls.Add(pnlInputRow);

            var lblCurrency = new Label
            {
                Text = "$",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Left,
                Width = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlInputRow.Controls.Add(lblCurrency);

            _txtPrice = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                Text = _lead.QuotedPrice.HasValue ? _lead.QuotedPrice.Value.ToString("F2") : ""
            };
            _txtPrice.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Submit();
                }
            };
            pnlInputRow.Controls.Add(_txtPrice);

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(220, 38, 38),
                Dock = DockStyle.Top,
                Height = 20,
                Visible = false
            };
            pnlBody.Controls.Add(_lblError);

            var lblNote = new Label
            {
                Text = "ℹ Quoting a price sets status to 'Quoted' and enables 1-click Conversion to formal Customer.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Bottom,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlBody.Controls.Add(lblNote);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };
            Controls.Add(pnlFooter);

            _btnSave = new Button
            {
                Text = "✓  Save & Set Quoted",
                Dock = DockStyle.Right,
                Width = 160,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) => Submit();
            pnlFooter.Controls.Add(_btnSave);

            var pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(pnlSpacer);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 85,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private void Submit()
        {
            var text = _txtPrice.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || !decimal.TryParse(text, out var val) || val < 0)
            {
                _lblError.Text = "Please enter a valid positive quoted price (e.g. 150.00).";
                _lblError.Visible = true;
                _txtPrice.Focus();
                return;
            }

            QuotedPrice = val;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    /// <summary>
    /// Modal dialog to mark a Lead as Lost with an explanation reason.
    /// </summary>
    public class LeadLostDialog : Form
    {
        private readonly LeadDto _lead;
        private ComboBox _cmbReason = null!;
        private TextBox _txtCustomReason = null!;
        private Label _lblError = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public string LostReason { get; private set; } = string.Empty;

        public LeadLostDialog(LeadDto lead)
        {
            _lead = lead ?? throw new ArgumentNullException(nameof(lead));
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = $"Mark Lost — {_lead.LeadName}";
            Size = new Size(460, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;

            // Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "❌ Mark Lead as Lost",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"Lead: {_lead.LeadName} (LD-{_lead.LeadId:D4})",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };
            Controls.Add(pnlBody);

            var lblReason = new Label
            {
                Text = "Primary Reason for Loss:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 22,
                UseMnemonic = false
            };
            pnlBody.Controls.Add(lblReason);

            _cmbReason = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            _cmbReason.Items.AddRange(new object[]
            {
                "Quoted price too high / outside budget",
                "Selected another cleaning provider",
                "Customer unresponsive / no callback",
                "Service location out of service range",
                "Required service schedule unavailable",
                "Other (specify below)"
            });
            _cmbReason.SelectedIndex = 0;
            pnlBody.Controls.Add(_cmbReason);

            var lblCustom = new Label
            {
                Text = "Additional Notes / Comments:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 8, 0, 0),
                UseMnemonic = false
            };
            pnlBody.Controls.Add(lblCustom);

            _txtCustomReason = new TextBox
            {
                Dock = DockStyle.Top,
                Multiline = true,
                Height = 60,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Optional details on why the client declined..."
            };
            pnlBody.Controls.Add(_txtCustomReason);

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(220, 38, 38),
                Dock = DockStyle.Top,
                Height = 20,
                Visible = false
            };
            pnlBody.Controls.Add(_lblError);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };
            Controls.Add(pnlFooter);

            _btnSave = new Button
            {
                Text = "Confirm Lost",
                Dock = DockStyle.Right,
                Width = 140,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) => Submit();
            pnlFooter.Controls.Add(_btnSave);

            var pnlSpacer = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(pnlSpacer);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 85,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(_btnCancel);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private void Submit()
        {
            var selected = _cmbReason.SelectedItem?.ToString() ?? "Other";
            var notes = _txtCustomReason.Text.Trim();
            LostReason = string.IsNullOrWhiteSpace(notes) ? selected : $"{selected}: {notes}";

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
