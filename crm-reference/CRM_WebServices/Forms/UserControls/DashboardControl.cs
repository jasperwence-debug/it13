using CRM.winforms.Controls;
using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class DashboardControl : UserControl
    {
        private Label _dot = null!;
        private Label _greeting = null!;
        private Label _subtitle = null!;
        private TableLayoutPanel _kpiRow = null!;
        private Panel _recentPanel = null!;
        private Label _recentTitle = null!;
        private DataGridView _recentGrid = null!;
        private Label _recentEmptyLabel = null!;

        // Team section (Manager+)
        private Panel? _teamPanel;
        private DataGridView? _teamGrid;

        private KpiCard _kpiCustomers = null!;
        private KpiCard _kpiLeads = null!;
        private KpiCard _kpiInquiries = null!;
        private KpiCard _kpiFollowUps = null!;
        private KpiCard _kpiOverdue = null!;

        /// <summary>
        /// Raised when the user clicks a KPI card.
        /// Arg is the destination page name (e.g., "Customers").
        /// MainForm listens and navigates.
        /// </summary>
        public event Action<string>? NavigationRequested;

        public DashboardControl()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Paper;
            Build();
            _ = LoadAsync();
        }

        private void Build()
        {
            // Signature dot + greeting
            _dot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 13F),
                ForeColor = AppTheme.Signal,
                Location = new Point(0, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_dot);

            var firstName = SessionManager.Current.Name.Split(' ').FirstOrDefault() ?? "there";
            _greeting = new Label
            {
                Text = $"Welcome back, {firstName}",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_greeting);

            _subtitle = new Label
            {
                Text = "Here's what's happening with your accounts today.",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(2, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_subtitle);

            // ─── KPI Row (5 columns) ────────────────────────────
            _kpiRow = new TableLayoutPanel
            {
                Location = new Point(0, 96),
                Height = 120,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            for (int i = 0; i < 5; i++)
                _kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            _kpiRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _kpiCustomers = new KpiCard("MY CUSTOMERS", "customers", AppTheme.Signal, "👥")
            { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
            _kpiLeads = new KpiCard("ACTIVE LEADS", "leads", Color.FromArgb(0xC1, 0x7B, 0x12), "🎯")
            { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
            _kpiInquiries = new KpiCard("OPEN INQUIRIES", "inquiries", AppTheme.Signal, "📩")
            { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
            _kpiFollowUps = new KpiCard("PENDING FOLLOW-UPS", "followups", Color.FromArgb(0x1B, 0x7A, 0x47), "⏰")
            { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 6, 0) };
            _kpiOverdue = new KpiCard("OVERDUE", "overdue", Color.FromArgb(0xA3, 0x24, 0x24), "⚠")
            { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };

            // ⭐ Wire clicks to navigation events
            _kpiCustomers.KpiClicked += (_, _) => RequestNavigation("Customers");
            _kpiLeads.KpiClicked += (_, _) => RequestNavigation("Leads");
            _kpiInquiries.KpiClicked += (_, _) => RequestNavigation("Inquiries");
            _kpiFollowUps.KpiClicked += (_, _) => RequestNavigation("Follow-ups");
            _kpiOverdue.KpiClicked += (_, _) => RequestNavigation("Follow-ups?Overdue"); // special flag

            _kpiRow.Controls.Add(_kpiCustomers, 0, 0);
            _kpiRow.Controls.Add(_kpiLeads, 1, 0);
            _kpiRow.Controls.Add(_kpiInquiries, 2, 0);
            _kpiRow.Controls.Add(_kpiFollowUps, 3, 0);
            _kpiRow.Controls.Add(_kpiOverdue, 4, 0);
            Controls.Add(_kpiRow);

            // ─── Recent Activities card ─────────────────────────
            _recentPanel = new Panel
            {
                Location = new Point(0, 240),
                BackColor = AppTheme.Surface,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Padding = new Padding(20)
            };
            UiRadiusHelper.StyleCard(_recentPanel, 12);

            _recentTitle = new Label
            {
                Text = "Recent Activities",
                Font = AppTheme.FontSubheading,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _recentPanel.Controls.Add(_recentTitle);

            _recentEmptyLabel = new Label
            {
                Text = "No activities yet. Log one from the Activities tab.",
                Font = new Font("Segoe UI Variable Text", 10.5F, FontStyle.Italic),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                BackColor = Color.Transparent
            };
            _recentPanel.Controls.Add(_recentEmptyLabel);

            _recentGrid = new DataGridView
            {
                Location = new Point(20, 56),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoGenerateColumns = false
            };
            AppTheme.ApplyGridStyle(_recentGrid);
            _recentGrid.RowTemplate.Height = 42;

            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "ActivityType", HeaderText = "TYPE", FillWeight = 80 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Description", HeaderText = "DESCRIPTION", FillWeight = 260 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "CustomerName", HeaderText = "CUSTOMER", FillWeight = 140 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "LeadName", HeaderText = "LEAD", FillWeight = 120 });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ActivityDate",
                HeaderText = "WHEN",
                FillWeight = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, HH:mm" }
            });

            _recentPanel.Controls.Add(_recentGrid);
            Controls.Add(_recentPanel);

            // ─── Team Snapshot (Manager+) ───────────────────────
            if (RoleHelper.CanAssign)
            {
                var teamPanel = new Panel
                {
                    Location = new Point(0, _recentPanel.Bottom + 12),
                    BackColor = AppTheme.Surface,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                    Padding = new Padding(20)
                };
                UiRadiusHelper.StyleCard(teamPanel, 12);

                var teamTitle = new Label
                {
                    Text = "Team Snapshot",
                    Font = AppTheme.FontSubheading,
                    ForeColor = AppTheme.Ink,
                    Location = new Point(20, 18),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                teamPanel.Controls.Add(teamTitle);

                var teamGrid = new DataGridView
                {
                    Location = new Point(20, 56),
                    Size = new Size(teamPanel.Width - 40, teamPanel.Height - 76),
                    AutoGenerateColumns = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
                };
                AppTheme.ApplyGridStyle(teamGrid);
                teamGrid.RowTemplate.Height = 42;

                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                { DataPropertyName = "Name", HeaderText = "REP", FillWeight = 160 });
                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                { DataPropertyName = "Role", HeaderText = "ROLE", FillWeight = 90 });
                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                { DataPropertyName = "ConvertedLeads", HeaderText = "CONVERTED", FillWeight = 90 });
                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                { DataPropertyName = "ActivitiesLogged", HeaderText = "ACTIVITIES", FillWeight = 90 });
                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "PipelineValue",
                    HeaderText = "PIPELINE ₱",
                    FillWeight = 120,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "₱#,##0" }
                });
                teamGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = "ConversionRate",
                    HeaderText = "RATE",
                    FillWeight = 80,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "P0" }
                });

                teamPanel.Controls.Add(teamGrid);
                Controls.Add(teamPanel);

                _teamPanel = teamPanel;
                _teamGrid = teamGrid;
            }

            Resize += (_, _) => LayoutChildren();
            LayoutChildren();
        }

        private void RequestNavigation(string destination)
        {
            NavigationRequested?.Invoke(destination);
        }

        private void LayoutChildren()
        {
            int w = ClientSize.Width - 4;

            _kpiRow.Width = w;

            int available = ClientSize.Height - _recentPanel.Top - 12;

            if (_teamPanel != null)
            {
                int recentH = (int)(available * 0.55);
                _recentPanel.Width = w;
                _recentPanel.Height = Math.Max(150, recentH);

                _teamPanel.Location = new Point(0, _recentPanel.Bottom + 12);
                _teamPanel.Width = w;
                _teamPanel.Height = Math.Max(150, available - _recentPanel.Height - 12);

                if (_teamGrid != null)
                {
                    _teamGrid.Width = _teamPanel.Width - 40;
                    _teamGrid.Height = _teamPanel.Height - 76;
                }
            }
            else
            {
                _recentPanel.Width = w;
                _recentPanel.Height = Math.Max(200, available);
            }

            _recentGrid.Width = _recentPanel.Width - 40;
            _recentGrid.Height = _recentPanel.Height - 76;
        }

        private async Task LoadAsync()
        {
            try
            {
                var customers = new CustomerApiClient();
                var leads = new LeadApiClient();
                var followUps = new FollowUpApiClient();
                var activities = new ActivityApiClient();
                var inquiries = new CustomerInquiryApiClient();

                var myCustomers = await customers.ListAsync();
                var myLeads = await leads.ListAsync(pageSize: 100);
                var myOpenInquiries = await inquiries.ListAsync(status: "Open", pageSize: 100);
                var myPending = await followUps.ListAsync(status: "Pending", pageSize: 100);
                var myOverdue = await followUps.ListAsync(overdue: true, pageSize: 100);
                var myActivities = await activities.ListAsync(pageSize: 10);

                TeamPerformanceDto? teamData = null;
                if (RoleHelper.CanAssign && _teamGrid != null)
                {
                    try
                    {
                        var reportsClient = new ReportsApiClient();
                        var from = DateTime.UtcNow.Date.AddDays(-30);
                        var to = DateTime.UtcNow.Date;
                        teamData = await reportsClient.GetTeamPerformanceAsync(from, to);
                    }
                    catch { }
                }

                if (InvokeRequired) Invoke(Fill); else Fill();

                void Fill()
                {
                    _kpiCustomers.SetValue(myCustomers?.Total ?? 0);
                    _kpiLeads.SetValue(myLeads?.Total ?? 0);
                    _kpiInquiries.SetValue(myOpenInquiries?.Total ?? 0);
                    _kpiFollowUps.SetValue(myPending?.Total ?? 0);
                    _kpiOverdue.SetValue(myOverdue?.Total ?? 0);

                    var rows = myActivities?.Items ?? new List<ActivityDto>();
                    _recentGrid.DataSource = rows;
                    _recentEmptyLabel.Visible = rows.Count == 0;
                    _recentGrid.Visible = rows.Count > 0;

                    if (_teamGrid != null && teamData != null)
                    {
                        _teamGrid.DataSource = teamData.Members
                            .OrderByDescending(m => m.ConvertedLeads)
                            .ThenByDescending(m => m.ActivitiesLogged)
                            .Take(10)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired) Invoke(() => ShowError(ex)); else ShowError(ex);
            }
        }

        private void ShowError(Exception ex)
        {
            _recentGrid.Visible = false;
            _recentEmptyLabel.Visible = true;
            _recentEmptyLabel.Text = $"⚠ Unable to load: {ex.Message}";
            _recentEmptyLabel.ForeColor = AppTheme.DangerText;
        }
    }
}