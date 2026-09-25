using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using App.WinForms.Core;

namespace App.WinForms.Views
{
    public enum PaletteItemType
    {
        Navigate,
        Action,
        Customer,
        WorkOrder,
        Invoice
    }

    public class PaletteItem
    {
        public PaletteItemType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public object? DataPayload { get; set; }
    }

    /// <summary>
    /// Global Command Palette (Ctrl + K) for rapid spotlight search across
    /// customers, work orders, invoices, navigation screens, and quick workflows.
    /// </summary>
    public class CommandPaletteDialog : Form
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<PaletteItem>? OnItemSelected { get; set; }

        private readonly ApiClient _api = new();
        private TextBox _txtSearch = null!;
        private ListBox _lstResults = null!;
        private Label _lblEmpty = null!;
        private Label _lblHint = null!;

        private readonly List<PaletteItem> _staticItems = new();
        private List<PaletteItem> _dynamicItems = new();
        private List<PaletteItem> _filteredItems = new();

        public CommandPaletteDialog()
        {
            InitializePaletteUI();
            BuildStaticItems();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CenterOnOwner();
            _ = LoadDynamicDataAsync();
            FilterItems();
            _txtSearch.Focus();
        }

        private void CenterOnOwner()
        {
            if (Owner != null)
            {
                int x = Owner.Location.X + (Owner.Width - Width) / 2;
                int y = Owner.Location.Y + 90;
                Location = new Point(Math.Max(x, 10), Math.Max(y, 10));
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void InitializePaletteUI()
        {
            Text = "Quick Navigation & Search";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(680, 440);
            BackColor = Color.White;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;

            // Border container
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

            // Search Header Box
            var pnlSearchHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlContainer.Controls.Add(pnlSearchHeader);

            var lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12F),
                Dock = DockStyle.Left,
                Width = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlSearchHeader.Controls.Add(lblSearchIcon);

            var lblEscHint = new Label
            {
                Text = "ESC to close",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Right,
                Width = 96,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSearchHeader.Controls.Add(lblEscHint);

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = "Search customers, work orders, invoices, or type a screen..."
            };
            _txtSearch.TextChanged += (s, e) => FilterItems();
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down)
                {
                    if (_lstResults.Items.Count > 0)
                    {
                        int next = Math.Min(_lstResults.SelectedIndex + 1, _lstResults.Items.Count - 1);
                        _lstResults.SelectedIndex = next;
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Up)
                {
                    if (_lstResults.Items.Count > 0)
                    {
                        int prev = Math.Max(_lstResults.SelectedIndex - 1, 0);
                        _lstResults.SelectedIndex = prev;
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    ExecuteSelectedItem();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };
            pnlSearchHeader.Controls.Add(_txtSearch);

            var pnlDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlContainer.Controls.Add(pnlDivider);

            // Footer hints
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 0, 16, 0)
            };
            pnlContainer.Controls.Add(pnlFooter);

            _lblHint = new Label
            {
                Text = "↑ ↓ to navigate   •   ↵ to select   •   ESC to dismiss",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlFooter.Controls.Add(_lblHint);

            // Results List
            _lstResults = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawVariable,
                ItemHeight = 52,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F)
            };
            _lstResults.MeasureItem += (s, e) => e.ItemHeight = 52;
            _lstResults.DrawItem += ListResults_DrawItem;
            _lstResults.DoubleClick += (s, e) => ExecuteSelectedItem();
            pnlContainer.Controls.Add(_lstResults);

            // Empty state overlay
            _lblEmpty = new Label
            {
                Text = "No matching records or commands found.\nTry searching with a customer name, phone #, or WO-XXXX.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.White,
                Visible = false
            };
            pnlContainer.Controls.Add(_lblEmpty);
            _lblEmpty.BringToFront();

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        private void BuildStaticItems()
        {
            var role = SessionManager.CurrentUser?.Role ?? Roles.SalesStaff;

            // Navigation Screens
            _staticItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Navigate,
                Category = "NAVIGATION",
                Title = "BI Dashboard & Analytics",
                Subtitle = "Overview of revenue, active orders, and business KPIs",
                Icon = "📊",
                Tag = "dashboard"
            });
            _staticItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Navigate,
                Category = "NAVIGATION",
                Title = "Client & Contract Management",
                Subtitle = "Customer directory, service histories, and lifetime spend",
                Icon = "📋",
                Tag = "clients"
            });
            _staticItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Navigate,
                Category = "NAVIGATION",
                Title = "Scheduling & Dispatch Center",
                Subtitle = "Operational schedule board and crew assignments",
                Icon = "📅",
                Tag = "scheduling"
            });

            if (role != Roles.SalesStaff)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Navigate,
                    Category = "NAVIGATION",
                    Title = "Work Orders Ledger",
                    Subtitle = "Operations master register and service tickets",
                    Icon = "🔧",
                    Tag = "workorders"
                });
            }

            _staticItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Navigate,
                Category = "NAVIGATION",
                Title = "Sales & Customer Retention",
                Subtitle = "At-risk accounts, churn monitoring, and win-back pipeline",
                Icon = "💼",
                Tag = "sales"
            });

            if (role == Roles.Admin || role == Roles.SuperAdmin)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Navigate,
                    Category = "NAVIGATION",
                    Title = "Financial Management",
                    Subtitle = "Invoicing ledger, billing settlement, and cash collections",
                    Icon = "💰",
                    Tag = "financial"
                });
            }

            if (role != Roles.SalesStaff)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Navigate,
                    Category = "NAVIGATION",
                    Title = "Reports & Compliance Audit",
                    Subtitle = "Executive BI reports and system activity audit trail",
                    Icon = "📈",
                    Tag = "reports"
                });
            }

            if (role == Roles.SuperAdmin)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Navigate,
                    Category = "NAVIGATION",
                    Title = "User Management",
                    Subtitle = "Staff accounts, role permissions, and access control",
                    Icon = "👥",
                    Tag = "users"
                });
            }

            // Quick Workflow Actions
            _staticItems.Add(new PaletteItem
            {
                Type = PaletteItemType.Action,
                Category = "QUICK ACTIONS",
                Title = "New Booking Request",
                Subtitle = "Submit a service request for an existing customer account",
                Icon = "⚡",
                Tag = "action_new_booking"
            });

            if (role != Roles.SalesStaff)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Action,
                    Category = "QUICK ACTIONS",
                    Title = "Create & Dispatch Work Order",
                    Subtitle = "Directly schedule a work order and assign crew technicians",
                    Icon = "🔧",
                    Tag = "action_new_workorder"
                });
            }

            if (role == Roles.SuperAdmin)
            {
                _staticItems.Add(new PaletteItem
                {
                    Type = PaletteItemType.Action,
                    Category = "QUICK ACTIONS",
                    Title = "Provision New User Account",
                    Subtitle = "Create credentials and assign roles for new employees",
                    Icon = "👤",
                    Tag = "action_new_user"
                });
            }
        }

        private async Task LoadDynamicDataAsync()
        {
            try
            {
                var customersTask = _api.GetCustomersAsync();
                var ordersTask = _api.GetWorkOrdersAsync();

                await Task.WhenAll(customersTask, ordersTask);

                var customers = await customersTask;
                var orders = await ordersTask;

                var items = new List<PaletteItem>();

                // Customers
                if (customers != null)
                {
                    foreach (var c in customers.Take(50))
                    {
                        items.Add(new PaletteItem
                        {
                            Type = PaletteItemType.Customer,
                            Category = "CUSTOMERS",
                            Title = c.CustomerName,
                            Subtitle = $"{c.ContactDetails} • {c.ServiceLocation} ({c.CustomerType})",
                            Icon = "👤",
                            Tag = $"cust_{c.CustomerId}",
                            DataPayload = c
                        });
                    }
                }

                // Work Orders & Invoices
                if (orders != null)
                {
                    foreach (var wo in orders.Take(50))
                    {
                        items.Add(new PaletteItem
                        {
                            Type = PaletteItemType.WorkOrder,
                            Category = "WORK ORDERS",
                            Title = $"WO-{wo.ServiceRequestId:D4} — {wo.CustomerName}",
                            Subtitle = $"{wo.ServiceType} • Status: {wo.Status} • Crew: {wo.AssignedStaff}",
                            Icon = "🔧",
                            Tag = $"wo_{wo.ServiceRequestId}",
                            DataPayload = wo
                        });

                        if (wo.ActualPrice.HasValue || wo.QuotedPrice.HasValue)
                        {
                            decimal price = wo.ActualPrice ?? wo.QuotedPrice ?? 0m;
                            items.Add(new PaletteItem
                            {
                                Type = PaletteItemType.Invoice,
                                Category = "INVOICES",
                                Title = $"INV-{wo.ServiceRequestId:D4} — ₱{price:N2}",
                                Subtitle = $"{wo.CustomerName} • {wo.ServiceType} ({wo.Status})",
                                Icon = "💰",
                                Tag = $"inv_{wo.ServiceRequestId}",
                                DataPayload = wo
                            });
                        }
                    }
                }

                _dynamicItems = items;
                FilterItems();
            }
            catch
            {
                // Non-critical background telemetry
            }
        }

        private void FilterItems()
        {
            string query = _txtSearch.Text.Trim();

            var pool = new List<PaletteItem>();
            pool.AddRange(_staticItems);
            pool.AddRange(_dynamicItems);

            if (string.IsNullOrWhiteSpace(query))
            {
                // Show default mix: Actions + Navigation + first 5 customers
                _filteredItems = pool.Where(i => i.Type == PaletteItemType.Action || i.Type == PaletteItemType.Navigate)
                                     .Take(12)
                                     .ToList();
            }
            else
            {
                _filteredItems = pool.Where(i =>
                    i.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).Take(20).ToList();
            }

            _lstResults.BeginUpdate();
            _lstResults.Items.Clear();
            foreach (var item in _filteredItems)
            {
                _lstResults.Items.Add(item);
            }
            if (_lstResults.Items.Count > 0)
            {
                _lstResults.SelectedIndex = 0;
                _lblEmpty.Visible = false;
            }
            else
            {
                _lblEmpty.Visible = true;
            }
            _lstResults.EndUpdate();
        }

        private void ListResults_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filteredItems.Count) return;

            var item = _filteredItems[e.Index];
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            Color bg = isSelected ? Color.FromArgb(239, 246, 255) : Color.White;
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Selection indicator bar
            if (isSelected)
            {
                using var penActive = new SolidBrush(Color.FromArgb(37, 99, 235));
                e.Graphics.FillRectangle(penActive, e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);
            }

            // Icon
            using (var fontIcon = new Font("Segoe UI", 12F))
            {
                e.Graphics.DrawString(item.Icon, fontIcon, Brushes.Black, e.Bounds.X + 14, e.Bounds.Y + 12);
            }

            // Category tag (pill) on right
            using (var fontCat = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            {
                var szCat = e.Graphics.MeasureString(item.Category, fontCat);
                int catX = e.Bounds.Right - (int)szCat.Width - 18;
                int catY = e.Bounds.Y + 16;
                using (var brushCat = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    e.Graphics.FillRectangle(brushCat, catX - 6, catY - 2, szCat.Width + 12, szCat.Height + 4);
                }
                using (var brushTextCat = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    e.Graphics.DrawString(item.Category, fontCat, brushTextCat, catX, catY);
                }
            }

            // Title
            using (var fontTitle = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var brushTitle = new SolidBrush(isSelected ? Color.FromArgb(29, 78, 216) : Color.FromArgb(15, 23, 42)))
            {
                e.Graphics.DrawString(item.Title, fontTitle, brushTitle, e.Bounds.X + 46, e.Bounds.Y + 8);
            }

            // Subtitle
            using (var fontSub = new Font("Segoe UI", 8F))
            using (var brushSub = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                e.Graphics.DrawString(item.Subtitle, fontSub, brushSub, e.Bounds.X + 46, e.Bounds.Y + 28);
            }

            // Bottom border separator
            using (var penSep = new Pen(Color.FromArgb(241, 245, 249), 1))
            {
                e.Graphics.DrawLine(penSep, e.Bounds.X + 46, e.Bounds.Bottom - 1, e.Bounds.Right - 16, e.Bounds.Bottom - 1);
            }
        }

        private void ExecuteSelectedItem()
        {
            if (_lstResults.SelectedItem is PaletteItem item)
            {
                Close();
                OnItemSelected?.Invoke(item);
            }
        }
    }
}
