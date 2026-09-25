using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;
using App.WinForms.Reporting;

namespace App.WinForms.Views
{
    /// <summary>
    /// LAYER 3 — Work Order Dispatch & Operations Dialog.
    ///
    /// Responsibilities:
    ///   - Manager / Admin Dispatch: assign field technicians/crew, set scheduled date, transition 'Requested' -> 'Scheduled'.
    ///   - Operations Execution Lifecycle: advance work orders through 'Scheduled' -> 'InProgress' -> 'Completed' / 'Rescheduled' / 'Cancelled'.
    ///   - Actual Price capture upon service completion.
    ///   - Strictly enforces Manager/Admin permissions (SalesStaff is view-only).
    /// </summary>
    public class DispatchWorkOrderDialog : Form
    {
        public event Action<WorkOrderDto?>? WorkOrderUpdated;

        private readonly ApiClient _api = new();
        private WorkOrderDto _order;

        // UI Controls
        private Label _lblStatusBadge = null!;
        private Label _lblCustomerName = null!;
        private Label _lblServiceType = null!;
        private Label _lblQuotedPrice = null!;
        private Label _lblActualPrice = null!;
        private Label _lblSpecialRequests = null!;

        // Dispatch Controls
        private ComboBox _cmbAssignedStaff = null!;
        private DateTimePicker _dtpScheduledDate = null!;
        private Button _btnDispatch = null!;
        private Label _lblConflictWarning = null!;
        private List<WorkOrderDto> _allActiveOrders = new();

        // Lifecycle Action Controls
        private Button _btnStartService = null!;
        private Button _btnCompleteService = null!;
        private Button _btnReschedule = null!;
        private Button _btnCancelOrder = null!;

        // Completion & Notes Controls
        private Panel _pnlCompletionBox = null!;
        private TextBox _txtActualPrice = null!;
        private TextBox _txtNewNote = null!;
        private TextBox _txtNotesHistory = null!;

        // Status & Footer
        private Label _lblStatus = null!;
        private Button _btnClose = null!;

        public DispatchWorkOrderDialog(WorkOrderDto order)
        {
            _order = order ?? throw new ArgumentNullException(nameof(order));
            BuildUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = LoadStaffListAsync();
            _ = LoadExistingOrdersForConflictCheckAsync();
            RefreshOrderDisplay();
            ApplyPermissions();
        }

        private void BuildUI()
        {
            Text = $"Work Order WO-{_order.ServiceRequestId:D4} — Dispatch & Operations";
            Size = new Size(760, 780);
            MinimumSize = new Size(680, 680);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // ── Header Panel ─────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 0, 24, 0)
            };
            Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = $"WO-{_order.ServiceRequestId:D4} — Operations & Dispatch",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 420,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            string roleText = SessionManager.IsSalesStaff
                ? "Sales Staff (Read-Only)"
                : $"Manager: {SessionManager.CurrentUser?.Username ?? "Admin"}";

            var lblRoleBadge = new Label
            {
                Text = roleText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = SessionManager.IsSalesStaff ? Color.FromArgb(217, 119, 6) : Theme.Primary,
                BackColor = SessionManager.IsSalesStaff ? Color.FromArgb(254, 243, 199) : Color.FromArgb(239, 246, 255),
                Dock = DockStyle.Right,
                Width = 200,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 18, 0, 18)
            };
            pnlHeader.Controls.Add(lblRoleBadge);

            var pnlDividerTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDividerTop);

            // ── Footer Panel ─────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Theme.Surface,
                Padding = new Padding(24, 10, 24, 10)
            };
            Controls.Add(pnlFooter);

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlFooter.Controls.Add(_lblStatus);

            var btnPrint = new Button
            {
                Text = "🖨️  Print Job Sheet",
                Dock = DockStyle.Right,
                Width = 150,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.ApplySecondaryButtonStyle(btnPrint);
            btnPrint.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnPrint.Click += (s, e) => ReportDocumentEngine.ShowWorkOrderPrintPreview(_order, this);
            pnlFooter.Controls.Add(btnPrint);

            _btnClose = new Button
            {
                Text = "Close",
                Dock = DockStyle.Right,
                Width = 100
            };
            Theme.ApplySecondaryButtonStyle(_btnClose);
            _btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(_btnClose);

            var pnlDividerBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };
            Controls.Add(pnlDividerBottom);

            // ── Scrollable Body ──────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Theme.Background
            };
            Controls.Add(pnlBody);

            int currentY = 16;
            int fullWidth = 690;

            // Notice Banner for Sales Staff
            if (SessionManager.IsSalesStaff)
            {
                var pnlBanner = new Panel
                {
                    Location = new Point(0, currentY),
                    Size = new Size(fullWidth, 38),
                    BackColor = Color.FromArgb(254, 243, 199),
                    Padding = new Padding(12, 8, 12, 8)
                };
                var lblBanner = new Label
                {
                    Text = "ℹ Sales Staff Mode: Read-Only access. Dispatching and operational status updates require Manager or Admin permissions.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(146, 64, 14),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlBanner.Controls.Add(lblBanner);
                pnlBody.Controls.Add(pnlBanner);
                currentY += 46;
            }

            // ── Section 1: Overview Summary Card ────────────────────
            var pnlSummary = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(fullWidth, 122),
                BackColor = Theme.Surface
            };
            ApplyCardStyle(pnlSummary);
            pnlBody.Controls.Add(pnlSummary);

            // Row 1: Customer & Status
            var lblCustTag = new Label
            {
                Text = "CUSTOMER:",
                Location = new Point(16, 12),
                Size = new Size(80, 20),
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted
            };
            pnlSummary.Controls.Add(lblCustTag);

            _lblCustomerName = new Label
            {
                Text = _order.CustomerName,
                Location = new Point(100, 10),
                Size = new Size(360, 24),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.TextDark
            };
            pnlSummary.Controls.Add(_lblCustomerName);

            _lblStatusBadge = new Label
            {
                Location = new Point(530, 10),
                Size = new Size(130, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSummary.Controls.Add(_lblStatusBadge);

            // Row 2: Service & Prices
            var lblSvcTag = new Label
            {
                Text = "SERVICE:",
                Location = new Point(16, 42),
                Size = new Size(80, 20),
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted
            };
            pnlSummary.Controls.Add(lblSvcTag);

            _lblServiceType = new Label
            {
                Text = _order.ServiceType,
                Location = new Point(100, 42),
                Size = new Size(240, 20),
                Font = Theme.BodyFont,
                ForeColor = Theme.TextDark
            };
            pnlSummary.Controls.Add(_lblServiceType);

            _lblQuotedPrice = new Label
            {
                Location = new Point(350, 42),
                Size = new Size(160, 20),
                Font = Theme.BodyFont,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            pnlSummary.Controls.Add(_lblQuotedPrice);

            _lblActualPrice = new Label
            {
                Location = new Point(520, 42),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74)
            };
            pnlSummary.Controls.Add(_lblActualPrice);

            // Row 3: Special requests
            var lblReqTag = new Label
            {
                Text = "REQUESTS:",
                Location = new Point(16, 72),
                Size = new Size(80, 20),
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted
            };
            pnlSummary.Controls.Add(lblReqTag);

            _lblSpecialRequests = new Label
            {
                Text = string.IsNullOrWhiteSpace(_order.SpecialRequests) ? "None specified" : _order.SpecialRequests,
                Location = new Point(100, 72),
                Size = new Size(560, 28),
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlSummary.Controls.Add(_lblSpecialRequests);

            currentY += 122;

            // ── Section 2: Manager Dispatch Card ────────────────────
            var pnlDispatch = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(fullWidth, 150),
                BackColor = Theme.Surface
            };
            ApplyCardStyle(pnlDispatch);
            pnlBody.Controls.Add(pnlDispatch);

            var lblDispatchSec = new Label
            {
                Text = "1. Dispatch & Technician Assignment",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(16, 12),
                Size = new Size(300, 22)
            };
            pnlDispatch.Controls.Add(lblDispatchSec);

            var lblStaff = new Label
            {
                Text = "Assigned Crew / Technician *",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 42),
                Size = new Size(200, 18)
            };
            pnlDispatch.Controls.Add(lblStaff);

            _cmbAssignedStaff = new ComboBox
            {
                Location = new Point(16, 64),
                Size = new Size(230, 28),
                Font = Theme.BodyFont,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cmbAssignedStaff.SelectedIndexChanged += (s, e) => CheckDoubleBookingConflict();
            _cmbAssignedStaff.TextChanged += (s, e) => CheckDoubleBookingConflict();
            pnlDispatch.Controls.Add(_cmbAssignedStaff);

            var lblDate = new Label
            {
                Text = "Confirmed Scheduled Date *",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(266, 42),
                Size = new Size(180, 18)
            };
            pnlDispatch.Controls.Add(lblDate);

            _dtpScheduledDate = new DateTimePicker
            {
                Location = new Point(266, 64),
                Size = new Size(190, 28),
                Font = Theme.BodyFont,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd"
            };
            if (_order.PreferredDate.Year >= 1753)
            {
                _dtpScheduledDate.Value = _order.PreferredDate;
            }
            _dtpScheduledDate.ValueChanged += (s, e) => CheckDoubleBookingConflict();
            pnlDispatch.Controls.Add(_dtpScheduledDate);

            _btnDispatch = new Button
            {
                Text = "⚡ Dispatch & Confirm",
                Location = new Point(470, 56),
                Size = new Size(190, 38)
            };
            Theme.ApplyPrimaryButtonStyle(_btnDispatch);
            _btnDispatch.Click += OnDispatchClick;
            pnlDispatch.Controls.Add(_btnDispatch);

            // Double-booking conflict warning banner
            _lblConflictWarning = new Label
            {
                Location = new Point(16, 102),
                Size = new Size(644, 36),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                BackColor = Color.FromArgb(254, 242, 242),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Visible = false
            };
            pnlDispatch.Controls.Add(_lblConflictWarning);

            currentY += 162;

            // ── Section 3: Operational Execution Lifecycle ───────────
            var pnlLifecycle = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(fullWidth, 110),
                BackColor = Theme.Surface
            };
            ApplyCardStyle(pnlLifecycle);
            pnlBody.Controls.Add(pnlLifecycle);

            var lblLifecycleSec = new Label
            {
                Text = "2. Operations Lifecycle Actions",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(16, 12),
                Size = new Size(300, 22)
            };
            pnlLifecycle.Controls.Add(lblLifecycleSec);

            // Action Buttons: Focused primary for the next step, clean neutral for secondary, muted outline for cancellation
            _btnStartService = new Button
            {
                Text = "▶  Start Service",
                Location = new Point(16, 46),
                Size = new Size(145, 38)
            };
            Theme.ApplyPrimaryButtonStyle(_btnStartService);
            _btnStartService.Click += async (s, e) => await UpdateStatusAsync("InProgress");
            pnlLifecycle.Controls.Add(_btnStartService);

            _btnCompleteService = new Button
            {
                Text = "✓  Complete Service",
                Location = new Point(172, 46),
                Size = new Size(160, 38)
            };
            Theme.ApplyPrimaryButtonStyle(_btnCompleteService);
            _btnCompleteService.Click += OnCompleteServiceClick;
            pnlLifecycle.Controls.Add(_btnCompleteService);

            _btnReschedule = new Button
            {
                Text = "📅  Reschedule",
                Location = new Point(344, 46),
                Size = new Size(145, 38)
            };
            Theme.ApplySecondaryButtonStyle(_btnReschedule);
            _btnReschedule.Click += OnRescheduleClick;
            pnlLifecycle.Controls.Add(_btnReschedule);

            _btnCancelOrder = new Button
            {
                Text = "✕  Cancel Order",
                Location = new Point(502, 46),
                Size = new Size(145, 38)
            };
            Theme.ApplyDestructiveButtonStyle(_btnCancelOrder);
            _btnCancelOrder.Click += OnCancelOrderClick;
            pnlLifecycle.Controls.Add(_btnCancelOrder);

            currentY += 122;

            // ── Section 4: Completion Price Input Panel (collapsible) ─
            _pnlCompletionBox = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(fullWidth, 80),
                BackColor = Color.FromArgb(240, 253, 244), // Mint/light green
                Visible = false
            };
            ApplyCardStyle(_pnlCompletionBox);
            pnlBody.Controls.Add(_pnlCompletionBox);

            var lblCompTitle = new Label
            {
                Text = "Service Completion Details:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(16, 10),
                Size = new Size(200, 18)
            };
            _pnlCompletionBox.Controls.Add(lblCompTitle);

            var lblActPrice = new Label
            {
                Text = "Final Billed Amount (₱) *",
                Font = Theme.CaptionFont,
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(16, 32),
                Size = new Size(160, 18)
            };
            _pnlCompletionBox.Controls.Add(lblActPrice);

            _txtActualPrice = new TextBox
            {
                Location = new Point(16, 50),
                Size = new Size(160, 24),
                Font = Theme.BodyFont,
                Text = _order.QuotedPrice?.ToString("F2") ?? ""
            };
            _pnlCompletionBox.Controls.Add(_txtActualPrice);

            var btnConfirmComplete = new Button
            {
                Text = "✓ Confirm Completion",
                Location = new Point(190, 48),
                Size = new Size(170, 30)
            };
            Theme.ApplyPrimaryButtonStyle(btnConfirmComplete);
            btnConfirmComplete.Click += async (s, e) => await SubmitCompletionAsync();
            _pnlCompletionBox.Controls.Add(btnConfirmComplete);

            var btnCancelComp = new Button
            {
                Text = "Cancel",
                Location = new Point(370, 48),
                Size = new Size(85, 30)
            };
            Theme.ApplySecondaryButtonStyle(btnCancelComp);
            btnCancelComp.Click += (s, e) => { _pnlCompletionBox.Visible = false; };
            _pnlCompletionBox.Controls.Add(btnCancelComp);

            currentY += 88;

            // ── Section 5: Activity Log & Operational Notes ─────────
            var pnlNotes = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(fullWidth, 230),
                BackColor = Theme.Surface
            };
            ApplyCardStyle(pnlNotes);
            pnlBody.Controls.Add(pnlNotes);

            var lblNotesSec = new Label
            {
                Text = "3. Operational Notes & Activity Audit Trail",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Theme.TextDark,
                Location = new Point(16, 12),
                Size = new Size(350, 22)
            };
            pnlNotes.Controls.Add(lblNotesSec);

            _txtNotesHistory = new TextBox
            {
                Location = new Point(16, 38),
                Size = new Size(640, 100),
                Font = new Font("Segoe UI", 8.5F),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            pnlNotes.Controls.Add(_txtNotesHistory);

            var lblAddNote = new Label
            {
                Text = "Add Operational Note:",
                Font = Theme.CaptionFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 146),
                Size = new Size(200, 18)
            };
            pnlNotes.Controls.Add(lblAddNote);

            _txtNewNote = new TextBox
            {
                Location = new Point(16, 168),
                Size = new Size(510, 48),
                Font = Theme.BodyFont,
                Multiline = true,
                PlaceholderText = "Type dispatch, technician instructions, or status notes here..."
            };
            pnlNotes.Controls.Add(_txtNewNote);

            var btnAppendNote = new Button
            {
                Text = "Post Note",
                Location = new Point(540, 168),
                Size = new Size(116, 48)
            };
            Theme.ApplySecondaryButtonStyle(btnAppendNote);
            btnAppendNote.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAppendNote.Click += async (s, e) => await AppendNoteOnlyAsync();
            pnlNotes.Controls.Add(btnAppendNote);
        }

        private async Task LoadStaffListAsync()
        {
            try
            {
                var staffList = await _api.GetAvailableStaffAsync();
                _cmbAssignedStaff.Items.Clear();
                foreach (var s in staffList)
                {
                    _cmbAssignedStaff.Items.Add(s);
                }

                if (!string.IsNullOrWhiteSpace(_order.AssignedStaff) &&
                    _order.AssignedStaff != "Unassigned" &&
                    _order.AssignedStaff != "Staff" &&
                    _order.AssignedStaff != "SalesStaff")
                {
                    _cmbAssignedStaff.Text = _order.AssignedStaff;
                }
                else if (_cmbAssignedStaff.Items.Count > 0)
                {
                    _cmbAssignedStaff.SelectedIndex = 0;
                }
            }
            catch
            {
                // Fallback default
                _cmbAssignedStaff.Items.AddRange(new object[]
                {
                    "Pedro Reyes", "Maria Santos", "Mark Anthony", "Sarah Jane", "Michael John", "Jessica Mae"
                });
                _cmbAssignedStaff.SelectedIndex = 0;
            }

            CheckDoubleBookingConflict();
        }

        private async Task LoadExistingOrdersForConflictCheckAsync()
        {
            try
            {
                var orders = await _api.GetWorkOrdersAsync();
                _allActiveOrders = orders ?? new List<WorkOrderDto>();
                CheckDoubleBookingConflict();
            }
            catch
            {
                // Non-critical background telemetry
            }
        }

        private void CheckDoubleBookingConflict()
        {
            if (_lblConflictWarning == null || _cmbAssignedStaff == null || _dtpScheduledDate == null) return;

            string staff = _cmbAssignedStaff.Text.Trim();
            if (string.IsNullOrWhiteSpace(staff) || staff == "Unassigned" || staff == "Staff" || staff == "SalesStaff")
            {
                _lblConflictWarning.Visible = false;
                return;
            }

            DateTime selectedDate = _dtpScheduledDate.Value.Date;

            var conflict = _allActiveOrders.Find(o =>
                o.ServiceRequestId != _order.ServiceRequestId &&
                !string.IsNullOrWhiteSpace(o.AssignedStaff) &&
                o.AssignedStaff.Trim().Equals(staff, StringComparison.OrdinalIgnoreCase) &&
                o.PreferredDate.Year >= 1753 && o.PreferredDate.Date == selectedDate &&
                o.Status != "Completed" &&
                o.Status != "Cancelled"
            );

            if (conflict != null)
            {
                _lblConflictWarning.Text = $"⚠ Double-Booking Alert: '{staff}' already has active WO-{conflict.ServiceRequestId:D4} ({conflict.CustomerName} - {conflict.ServiceType}) on {selectedDate:MMM dd, yyyy}!";
                _lblConflictWarning.Visible = true;
            }
            else
            {
                _lblConflictWarning.Visible = false;
            }
        }

        private void RefreshOrderDisplay()
        {
            _lblCustomerName.Text = _order.CustomerName;
            _lblServiceType.Text = _order.ServiceType;
            _lblQuotedPrice.Text = _order.QuotedPrice.HasValue
                ? $"Quoted: ₱{_order.QuotedPrice.Value:N2}"
                : "Quoted: --";

            _lblActualPrice.Text = _order.ActualPrice.HasValue
                ? $"Billed: ₱{_order.ActualPrice.Value:N2}"
                : "";

            _lblSpecialRequests.Text = string.IsNullOrWhiteSpace(_order.SpecialRequests)
                ? "No special requests."
                : _order.SpecialRequests;

            _txtNotesHistory.Text = string.IsNullOrWhiteSpace(_order.Notes)
                ? "(No activity notes yet)"
                : _order.Notes.Replace("\n", Environment.NewLine);

            // Status Badge
            string status = _order.Status;
            _lblStatusBadge.Text = status.ToUpperInvariant();
            var (bg, fg) = status switch
            {
                "Completed"   => (Color.FromArgb(220, 252, 231), Color.FromArgb(22, 163, 74)),
                "Cancelled"   => (Color.FromArgb(254, 226, 226), Color.FromArgb(220, 38, 38)),
                "Requested"   => (Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6)),
                "InProgress"  => (Color.FromArgb(219, 234, 254), Color.FromArgb(37, 99, 235)),
                "In Progress" => (Color.FromArgb(219, 234, 254), Color.FromArgb(37, 99, 235)),
                "Rescheduled" => (Color.FromArgb(255, 237, 213), Color.FromArgb(234, 88, 12)),
                "Scheduled"   => (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105)),
                _             => (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105))
            };
            _lblStatusBadge.BackColor = bg;
            _lblStatusBadge.ForeColor = fg;

            // Configure button states based on lifecycle & role authority (Rule 5: Manager owns operations)
            bool isTerminal = status == "Completed" || status == "Cancelled";
            bool canOperate = SessionManager.IsManager;

            _btnDispatch.Enabled = !isTerminal && canOperate;
            _btnStartService.Enabled = (status == "Scheduled" || status == "Rescheduled") && canOperate;
            _btnCompleteService.Enabled = (status == "InProgress" || status == "Scheduled") && canOperate;
            _btnReschedule.Enabled = !isTerminal && canOperate;
            _btnCancelOrder.Enabled = !isTerminal && canOperate;

            if (isTerminal)
            {
                _lblStatus.Text = $"Order is in terminal state '{status}'.";
            }
            else if (!canOperate)
            {
                _lblStatus.Text = "Read-Only View: Dispatch & operations execution is restricted to Operations Manager.";
            }
        }

        private void ApplyPermissions()
        {
            // Only Manager has FULL access to dispatch, assign staff, and execute operations.
            // Super Admin, Admin, and Sales Staff have VIEW/PARTIAL access (read-only for dispatch actions).
            if (!SessionManager.IsManager)
            {
                _cmbAssignedStaff.Enabled = false;
                _dtpScheduledDate.Enabled = false;
                _btnDispatch.Enabled = false;
                _btnStartService.Enabled = false;
                _btnCompleteService.Enabled = false;
                _btnReschedule.Enabled = false;
                _btnCancelOrder.Enabled = false;
                _txtNewNote.Enabled = false;
            }
        }

        // ============================================================
        // Actions
        // ============================================================
        private async void OnDispatchClick(object? sender, EventArgs e)
        {
            var staff = _cmbAssignedStaff.Text.Trim();
            if (string.IsNullOrWhiteSpace(staff))
            {
                MessageBox.Show("Please select or enter an assigned technician or crew member.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var date = _dtpScheduledDate.Value.Date;
            var note = _txtNewNote.Text.Trim();

            // Guard against accidental double-booking
            var conflict = _allActiveOrders.Find(o =>
                o.ServiceRequestId != _order.ServiceRequestId &&
                !string.IsNullOrWhiteSpace(o.AssignedStaff) &&
                o.AssignedStaff.Trim().Equals(staff, StringComparison.OrdinalIgnoreCase) &&
                o.PreferredDate.Year >= 1753 && o.PreferredDate.Date == date &&
                o.Status != "Completed" &&
                o.Status != "Cancelled"
            );

            if (conflict != null)
            {
                var dr = MessageBox.Show(
                    $"Technician '{staff}' is already scheduled for Work Order WO-{conflict.ServiceRequestId:D4} ({conflict.CustomerName} - {conflict.ServiceType}) on {date:MMM dd, yyyy}.\n\nDo you want to proceed with double-booking anyway?",
                    "Double-Booking Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dr != DialogResult.Yes)
                {
                    return;
                }
            }

            _btnDispatch.Enabled = false;
            _lblStatus.Text = "Dispatching work order...";

            var dto = new WorkOrderDispatchDto
            {
                AssignedStaff = staff,
                ScheduledDate = date,
                Notes = string.IsNullOrWhiteSpace(note) ? null : note
            };

            var (success, msg, updated) = await _api.DispatchWorkOrderAsync(_order.ServiceRequestId, dto);
            _btnDispatch.Enabled = true;

            if (success && updated != null)
            {
                _order = updated;
                _txtNewNote.Clear();
                RefreshOrderDisplay();
                WorkOrderUpdated?.Invoke(_order);
                MessageBox.Show($"Work order WO-{_order.ServiceRequestId:D4} successfully dispatched to {staff} for {date:MMM dd, yyyy}!", "Dispatched", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Dispatch failed:\n{msg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateStatusAsync(string newStatus, decimal? actualPrice = null, DateTime? newDate = null, string? customNote = null)
        {
            string note = customNote ?? _txtNewNote.Text.Trim();

            _lblStatus.Text = $"Updating status to '{newStatus}'...";

            var dto = new WorkOrderStatusUpdateDto
            {
                Status = newStatus,
                ActualPrice = actualPrice,
                ScheduledDate = newDate,
                Notes = string.IsNullOrWhiteSpace(note) ? null : note
            };

            var (success, msg, updated) = await _api.UpdateWorkOrderStatusAsync(_order.ServiceRequestId, dto);

            if (success && updated != null)
            {
                _order = updated;
                _txtNewNote.Clear();
                _pnlCompletionBox.Visible = false;
                RefreshOrderDisplay();
                WorkOrderUpdated?.Invoke(_order);
                _lblStatus.Text = $"Status updated to '{newStatus}'.";
            }
            else
            {
                MessageBox.Show($"Failed to update status:\n{msg}", "Status Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCompleteServiceClick(object? sender, EventArgs e)
        {
            // Show inline completion price box
            _pnlCompletionBox.Visible = true;
            _txtActualPrice.Focus();
        }

        private async Task SubmitCompletionAsync()
        {
            decimal actualPrice = _order.QuotedPrice ?? 0m;
            if (!string.IsNullOrWhiteSpace(_txtActualPrice.Text))
            {
                if (decimal.TryParse(_txtActualPrice.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ||
                    decimal.TryParse(_txtActualPrice.Text.Trim(), out parsed))
                {
                    actualPrice = parsed;
                }
                else
                {
                    MessageBox.Show("Please enter a valid numeric amount for final billed price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string note = _txtNewNote.Text.Trim();
            await UpdateStatusAsync("Completed", actualPrice, null, string.IsNullOrWhiteSpace(note) ? "Service completed." : note);
        }

        private async void OnRescheduleClick(object? sender, EventArgs e)
        {
            var newDate = _dtpScheduledDate.Value.Date;
            if (newDate <= DateTime.Today && MessageBox.Show("The chosen reschedule date is today or in the past. Are you sure?", "Confirm Date", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            string note = _txtNewNote.Text.Trim();
            await UpdateStatusAsync("Rescheduled", null, newDate, string.IsNullOrWhiteSpace(note) ? $"Rescheduled to {newDate:MMM dd, yyyy}." : note);
        }

        private async void OnCancelOrderClick(object? sender, EventArgs e)
        {
            if (MessageBox.Show($"Are you sure you want to cancel Work Order WO-{_order.ServiceRequestId:D4}?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            string note = _txtNewNote.Text.Trim();
            await UpdateStatusAsync("Cancelled", null, null, string.IsNullOrWhiteSpace(note) ? "Cancelled by operations manager." : note);
        }

        private async Task AppendNoteOnlyAsync()
        {
            var note = _txtNewNote.Text.Trim();
            if (string.IsNullOrWhiteSpace(note))
            {
                MessageBox.Show("Please enter a note to post.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update with same status to append note
            await UpdateStatusAsync(_order.Status, _order.ActualPrice, null, note);
        }

        private static void ApplyCardStyle(Panel pnl)
        {
            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };
        }
    }
}
