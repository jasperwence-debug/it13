using System;
using System.Drawing;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    public class NewBookingModalForm : Form
    {
        private readonly DataCollectionView _wizard;

        public NewBookingModalForm()
        {
            Text = "New Booking — Operations & Client Management";
            Size = new Size(840, 780);
            MinimumSize = new Size(700, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            _wizard = new DataCollectionView
            {
                Dock = DockStyle.Fill
            };

            if (SessionManager.CurrentUser != null)
            {
                _wizard.ApplyViewPermissions(SessionManager.CurrentUser.Role);
            }

            _wizard.RecordSaved += () =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.Add(_wizard);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}
