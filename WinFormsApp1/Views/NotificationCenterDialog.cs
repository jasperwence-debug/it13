using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    /// <summary>
    /// Interactive Notification & Operations Approval Flyout.
    /// Displays pending manager approvals, financial settlement alerts, and operational confirmations.
    /// </summary>
    public class NotificationCenterDialog : Form
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<NotificationItem>? OnActionSelected { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public NotificationItem? SelectedActionItem { get; private set; }

        private readonly List<NotificationItem> _items;
        private Panel _pnlContent = null!;

        public NotificationCenterDialog(List<NotificationItem> items)
        {
            _items = items ?? new List<NotificationItem>();
            InitializeUI();
        }

        public void ShowUnderButton(Control anchorButton, Form? owner = null)
        {
            if (anchorButton != null)
            {
                var screen = Screen.FromControl(anchorButton).WorkingArea;
                var screenPoint = anchorButton.PointToScreen(new Point(anchorButton.Width - Width, anchorButton.Height + 6));
                int x = Math.Max(screen.Left + 10, Math.Min(screenPoint.X, screen.Right - Width - 10));
                int y = Math.Max(screen.Top + 10, Math.Min(screenPoint.Y, screen.Bottom - Height - 10));
                Location = new Point(x, y);
            }
            ShowDialog(owner ?? anchorButton?.FindForm());
        }

        private void InitializeUI()
        {
            Text = "Notifications & Approvals";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(490, 480);
            BackColor = Color.White;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            pnlContainer.Paint += (s, e) =>
            {
                using var penBorder = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                e.Graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
                using var brushTop = new SolidBrush(Color.FromArgb(37, 99, 235));
                e.Graphics.FillRectangle(brushTop, 0, 0, Width, 3);
            };
            Controls.Add(pnlContainer);

            // Header (52px)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 0, 16, 0)
            };
            pnlContainer.Controls.Add(pnlHeader);

            var lblTitle = new Label
            {
                Text = "Notifications & Approvals",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Left,
                Width = 220,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblBadge = new Label
            {
                Text = $"{_items.Count} Pending",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = _items.Count > 0 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52),
                BackColor = _items.Count > 0 ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244),
                Dock = DockStyle.Left,
                Width = 84,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 15, 0, 15)
            };
            pnlHeader.Controls.Add(lblBadge);

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Right,
                Width = 32,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            pnlHeader.Controls.Add(btnClose);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlContainer.Controls.Add(pnlDivider);

            // Scrollable Content
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12)
            };
            pnlContainer.Controls.Add(_pnlContent);

            if (_items.Count == 0)
            {
                RenderEmptyState();
            }
            else
            {
                RenderNotificationCards();
            }

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private void RenderEmptyState()
        {
            var pnlEmpty = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _pnlContent.Controls.Add(pnlEmpty);

            var lblEmptyIcon = new Label
            {
                Text = "🎉",
                Font = new Font("Segoe UI", 32F),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.BottomCenter
            };
            pnlEmpty.Controls.Add(lblEmptyIcon);

            var lblEmptyTitle = new Label
            {
                Text = "All caught up!",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmpty.Controls.Add(lblEmptyTitle);

            var lblEmptySub = new Label
            {
                Text = "There are no pending approvals or operational tasks requiring your action right now.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(24, 0, 24, 0)
            };
            pnlEmpty.Controls.Add(lblEmptySub);
        }

        private void RenderNotificationCards()
        {
            _pnlContent.SuspendLayout();

            int cardWidth = _pnlContent.Width - 44;
            if (cardWidth < 430) cardWidth = 430;

            foreach (var item in _items)
            {
                var card = CreateNotificationCard(item, cardWidth);
                _pnlContent.Controls.Add(card);
            }

            _pnlContent.ResumeLayout(true);
        }

        private Panel CreateNotificationCard(NotificationItem item, int width)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 132,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 10, 14, 10)
            };

            // Subtle border and left accent color bar
            var accentColor = item.Type switch
            {
                NotificationType.ApprovalRequired => Color.FromArgb(217, 119, 6),  // Amber
                NotificationType.PaymentPending   => Color.FromArgb(37, 99, 235),  // Blue
                NotificationType.ServiceDispatched=> Color.FromArgb(22, 163, 74),  // Green
                _                                 => Color.FromArgb(100, 116, 139) // Slate
            };

            card.Paint += (s, e) =>
            {
                using var penBorder = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(penBorder, 0, 0, card.Width - 1, card.Height - 1);
                using var brushAccent = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brushAccent, 0, 0, 4, card.Height);
            };

            // Top row: Type Badge + Timestamp
            var pnlTopRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent
            };
            card.Controls.Add(pnlTopRow);

            var (badgeText, badgeBg, badgeFg) = item.Type switch
            {
                NotificationType.ApprovalRequired => ("⚡ APPROVAL REQUIRED", Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64, 14)),
                NotificationType.PaymentPending   => ("💰 SETTLEMENT PENDING", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175)),
                NotificationType.ServiceDispatched=> ("✓ DISPATCHED", Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52)),
                _                                 => ("ℹ ALERT", Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105))
            };

            var lblBadge = new Label
            {
                Text = badgeText,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTopRow.Controls.Add(lblBadge);

            var lblTime = new Label
            {
                Text = FormatRelativeTime(item.Timestamp),
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlTopRow.Controls.Add(lblTime);

            // Middle row: Title & Message
            var lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblTitle);

            var lblMessage = new Label
            {
                Text = item.Message,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(lblMessage);

            // Bottom row: Action button
            var pnlBottomRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.Transparent
            };
            card.Controls.Add(pnlBottomRow);

            var btnAction = new Button
            {
                Text = item.ActionLabel,
                Dock = DockStyle.Right,
                Width = 145
            };
            Theme.ApplyPrimaryButtonStyle(btnAction);
            btnAction.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnAction.Click += (s, e) =>
            {
                SelectedActionItem = item;
                DialogResult = DialogResult.OK;
                Close();
                OnActionSelected?.Invoke(item);
            };
            pnlBottomRow.Controls.Add(btnAction);

            // Spacer between cards
            var pnlSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.Transparent
            };

            var wrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 126,
                BackColor = Color.Transparent
            };
            wrapper.Controls.Add(card);
            wrapper.Controls.Add(pnlSpacer);

            return wrapper;
        }

        private static string FormatRelativeTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 2) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return dt.ToString("MMM dd");
        }
    }
}
