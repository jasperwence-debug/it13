using System;
using System.Drawing;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Modal container for the Lead Capture wizard (DataCollectionView).
    ///
    /// This dialog is ONLY for creating Leads (inquiries).
    /// Scheduling is handled in WorkOrdersView / NewWorkOrderDialog.
    /// </summary>
    public class NewLeadDialog : Form
    {
        private readonly DataCollectionView _wizard;

        public NewLeadDialog()
        {
            Text = "New Lead — Inquiry Capture";
            Size = new Size(780, 660);
            MinimumSize = new Size(680, 560);
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
    }

    // ----------------------------------------------------------------
    // Backward-compatibility alias: keeps any remaining references
    // to the old name compiling without errors.
    // ----------------------------------------------------------------
    [Obsolete("Use NewLeadDialog instead. This alias will be removed in a future release.")]
    public class NewBookingModalForm : NewLeadDialog { }
}
