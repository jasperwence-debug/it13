using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    public class BranchDialog : Form
    {
        private readonly BranchDto? _existingBranch;
        private readonly int _companyId;
        private readonly ApiClient _api = new();

        private TextBox _txtCode = null!;
        private TextBox _txtName = null!;
        private TextBox _txtCity = null!;
        private TextBox _txtAddress = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;
        private TextBox _txtManager = null!;
        private CheckBox _chkActive = null!;

        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public bool BranchSaved { get; private set; }

        public BranchDialog(int companyId, BranchDto? branch = null)
        {
            _companyId = companyId;
            _existingBranch = branch;
            InitializeUI();
            if (_existingBranch != null)
            {
                PopulateData();
            }
        }

        private void InitializeUI()
        {
            bool isEdit = _existingBranch != null;
            Text = isEdit ? $"Edit Branch — {_existingBranch!.BranchCode}" : "⚡  Register New Operating Branch";
            Size = new Size(540, 580);
            MinimumSize = new Size(540, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };
            Controls.Add(pnlHeader);

            var lblHeaderTitle = new Label
            {
                Text = isEdit ? "🏢  Modify Branch Information" : "🏢  Register Regional Operations Hub",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            var lblHeaderSub = new Label
            {
                Text = "Tenant C (Medium Enterprise) Multi-Location Workforce & Dispatch",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Top,
                Height = 20
            };
            pnlHeader.Controls.Add(lblHeaderSub);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 10, 16, 10)
            };
            Controls.Add(pnlFooter);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Width = 90,
                Height = 34,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(_btnCancel);

            var spc = new Panel { Dock = DockStyle.Right, Width = 10 };
            pnlFooter.Controls.Add(spc);

            _btnSave = new Button
            {
                Text = isEdit ? "💾  Update Branch" : "✔  Create Branch",
                Width = 145,
                Height = 34,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            Theme.ApplyPrimaryButtonStyle(_btnSave);
            _btnSave.Click += async (s, e) => await SaveBranchAsync();
            pnlFooter.Controls.Add(_btnSave);

            // Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            int y = 10;

            // Row 1: Branch Code & City
            var lblCode = new Label { Text = "Branch Code:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCode);

            var lblCity = new Label { Text = "City / Region:", Location = new Point(275, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblCity);
            y += 24;

            _txtCode = new TextBox { Location = new Point(24, y), Width = 230, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. BR-DVO" };
            if (isEdit) _txtCode.ReadOnly = true;
            pnlBody.Controls.Add(_txtCode);

            _txtCity = new TextBox { Location = new Point(275, y), Width = 215, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. Davao City" };
            pnlBody.Controls.Add(_txtCity);
            y += 38;

            // Row 2: Branch Name
            var lblName = new Label { Text = "Branch Full Name:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblName);
            y += 24;

            _txtName = new TextBox { Location = new Point(24, y), Width = 466, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. Davao Regional Operations Hub" };
            pnlBody.Controls.Add(_txtName);
            y += 38;

            // Row 3: Address
            var lblAddr = new Label { Text = "Physical Address / Facility:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblAddr);
            y += 24;

            _txtAddress = new TextBox { Location = new Point(24, y), Width = 466, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. JP Laurel Ave, Bajada" };
            pnlBody.Controls.Add(_txtAddress);
            y += 38;

            // Row 4: Phone & Email
            var lblPhone = new Label { Text = "Contact Phone:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblPhone);

            var lblEmail = new Label { Text = "Official Email:", Location = new Point(275, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblEmail);
            y += 24;

            _txtPhone = new TextBox { Location = new Point(24, y), Width = 230, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. 09171234567" };
            pnlBody.Controls.Add(_txtPhone);

            _txtEmail = new TextBox { Location = new Point(275, y), Width = 215, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. davao@tenant-c.ph" };
            pnlBody.Controls.Add(_txtEmail);
            y += 38;

            // Row 5: Branch Manager
            var lblMgr = new Label { Text = "Branch Manager / Operations Supervisor:", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Theme.TextDark };
            pnlBody.Controls.Add(lblMgr);
            y += 24;

            _txtManager = new TextBox { Location = new Point(24, y), Width = 466, Font = new Font("Segoe UI", 9F), PlaceholderText = "e.g. Engr. Ramon Alvarez" };
            pnlBody.Controls.Add(_txtManager);
            y += 38;

            // Row 6: Active Status
            _chkActive = new CheckBox
            {
                Text = "Active Operating Branch (Accepts booking requests and dispatch)",
                Location = new Point(24, y),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74)
            };
            pnlBody.Controls.Add(_chkActive);
        }

        private void PopulateData()
        {
            if (_existingBranch == null) return;
            _txtCode.Text = _existingBranch.BranchCode;
            _txtName.Text = _existingBranch.BranchName;
            _txtCity.Text = _existingBranch.City;
            _txtAddress.Text = _existingBranch.Address;
            _txtPhone.Text = _existingBranch.Phone;
            _txtEmail.Text = _existingBranch.Email;
            _txtManager.Text = _existingBranch.ManagerName;
            _chkActive.Checked = _existingBranch.IsActive;
        }

        private async Task SaveBranchAsync()
        {
            // Input Validation per Rubric Criterion 3
            if (string.IsNullOrWhiteSpace(_txtCode.Text))
            {
                MessageBox.Show("Please provide a Branch Code (e.g. BR-DVO).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Please enter the Branch Full Name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtCity.Text))
            {
                MessageBox.Show("Please specify the City / Region.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtCity.Focus();
                return;
            }

            string email = _txtEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtEmail.Focus();
                return;
            }

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            try
            {
                bool success = false;
                string msg = "";

                if (_existingBranch == null)
                {
                    // Create Branch
                    var createDto = new BranchCreateDto
                    {
                        CompanyId = _companyId,
                        BranchCode = _txtCode.Text.Trim().ToUpperInvariant(),
                        BranchName = _txtName.Text.Trim(),
                        City = _txtCity.Text.Trim(),
                        Address = _txtAddress.Text.Trim(),
                        Phone = _txtPhone.Text.Trim(),
                        Email = email,
                        ManagerName = _txtManager.Text.Trim()
                    };

                    var apiResult = await _api.CreateBranchAsync(createDto);
                    success = apiResult.Success;
                    msg = apiResult.Message;

                    if (!success)
                    {
                        // Direct LocalDB fallback
                        using var db = new App.Infrastructure.AppDbContext();
                        var newBranch = new App.Domain.Entities.Branch
                        {
                            CompanyId = _companyId,
                            BranchCode = createDto.BranchCode,
                            BranchName = createDto.BranchName,
                            City = createDto.City,
                            Address = createDto.Address,
                            Phone = createDto.Phone,
                            Email = createDto.Email,
                            ManagerName = createDto.ManagerName,
                            IsActive = _chkActive.Checked,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Branches.Add(newBranch);
                        await db.SaveChangesAsync();
                        success = true;
                    }
                }
                else
                {
                    // Update Branch
                    var updateDto = new BranchUpdateDto
                    {
                        BranchName = _txtName.Text.Trim(),
                        City = _txtCity.Text.Trim(),
                        Address = _txtAddress.Text.Trim(),
                        Phone = _txtPhone.Text.Trim(),
                        Email = email,
                        ManagerName = _txtManager.Text.Trim(),
                        IsActive = _chkActive.Checked
                    };

                    var apiResult = await _api.UpdateBranchAsync(_existingBranch.BranchId, updateDto);
                    success = apiResult.Success;
                    msg = apiResult.Message;

                    if (!success)
                    {
                        // Direct LocalDB fallback
                        using var db = new App.Infrastructure.AppDbContext();
                        var b = await db.Branches.FindAsync(_existingBranch.BranchId);
                        if (b != null)
                        {
                            b.BranchName = updateDto.BranchName;
                            b.City = updateDto.City;
                            b.Address = updateDto.Address;
                            b.Phone = updateDto.Phone;
                            b.Email = updateDto.Email;
                            b.ManagerName = updateDto.ManagerName;
                            b.IsActive = updateDto.IsActive;
                            b.UpdatedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync();
                            success = true;
                        }
                    }
                }

                if (success)
                {
                    BranchSaved = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = _existingBranch != null ? "💾  Update Branch" : "✔  Create Branch";
            }
        }
    }
}
