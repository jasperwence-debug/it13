using System.Text;
using CRM.winforms.Controls;
using CRM.winforms.Controls.Charts;
using CRM.winforms.Helpers;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Forms.UserControls
{
    public class ReportsControl : UserControl
    {
        // ─── Header ────────────────────────────────────────────
        private Label _dot = null!;
        private Label _title = null!;
        private Label _subtitle = null!;
        private ComboBox _rangeFilter = null!;
        private DateTimePicker _fromPicker = null!;
        private DateTimePicker _toPicker = null!;
        private Button _exportBtn = null!;

        // ─── Tabs ──────────────────────────────────────────────
        private Panel _tabBar = null!;
        private readonly List<(Button btn, string key)> _tabs = new();
        private string _activeTab = "Overview";

        // ─── Content host ──────────────────────────────────────
        private Panel _contentHost = null!;
        private Panel _loading = null!;

        // ─── Cached data for CSV export ────────────────────────
        private OverviewReportDto? _overviewData;
        private PipelineReportDto? _pipelineData;
        private ActivityReportDto? _activityData;
        private RetentionReportDto? _retentionData;
        private ConversionReportDto? _conversionData;
        private TeamPerformanceDto? _teamData;

        private DateTime _from = DateTime.UtcNow.Date.AddDays(-30);
        private DateTime _to = DateTime.UtcNow.Date;

        public ReportsControl()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Paper;
            Build();
            _ = LoadActiveTabAsync();
        }

        // ═══════════════════════════════════════════════════════
        // BUILD
        // ═══════════════════════════════════════════════════════
        private void Build()
        {
            // ─── Header ────────────────────────────────────────
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

            _title = new Label
            {
                Text = "Reports & Business Intelligence",
                Font = AppTheme.FontTitle,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_title);

            _subtitle = new Label
            {
                Text = "Analyze pipeline, activity, retention, and conversion.",
                Font = AppTheme.FontBody,
                ForeColor = AppTheme.Trace,
                Location = new Point(22, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_subtitle);

            // ─── Range filter row ──────────────────────────────
            var rangeLbl = new Label
            {
                Text = "Range",
                Location = new Point(0, 96),
                Size = new Size(50, 24),
                Font = AppTheme.FontLabel,
                ForeColor = AppTheme.Trace,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(rangeLbl);

            _rangeFilter = new ComboBox
            {
                Location = new Point(50, 92),
                Size = new Size(160, 32),
                Font = AppTheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Ink
            };
            _rangeFilter.Items.AddRange(new object[]
            {
                "Last 7 days",
                "Last 30 days",
                "This month",
                "Last 90 days",
                "This quarter",
                "This year",
                "Custom range"
            });
            _rangeFilter.SelectedIndex = 1;
            _rangeFilter.SelectedIndexChanged += async (_, _) => await OnRangeChanged();
            Controls.Add(_rangeFilter);

            // From / To pickers (only shown for Custom)
            _fromPicker = new DateTimePicker
            {
                Location = new Point(220, 92),
                Size = new Size(140, 32),
                Font = AppTheme.FontBody,
                Format = DateTimePickerFormat.Short,
                Value = _from,
                Visible = false
            };
            _fromPicker.ValueChanged += async (_, _) =>
            {
                _from = _fromPicker.Value.Date;
                await LoadActiveTabAsync();
            };
            Controls.Add(_fromPicker);

            _toPicker = new DateTimePicker
            {
                Location = new Point(366, 92),
                Size = new Size(140, 32),
                Font = AppTheme.FontBody,
                Format = DateTimePickerFormat.Short,
                Value = _to,
                Visible = false
            };
            _toPicker.ValueChanged += async (_, _) =>
            {
                _to = _toPicker.Value.Date;
                await LoadActiveTabAsync();
            };
            Controls.Add(_toPicker);

            // Export button (right-aligned)
            _exportBtn = new Button
            {
                Text = "Export CSV",
                Size = new Size(140, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.Trace,
                Font = new Font("Segoe UI Variable Text Semibold", 9.5F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _exportBtn.FlatAppearance.BorderColor = AppTheme.Line;
            UiRadiusHelper.StyleButton(_exportBtn, 8);
            UiRadiusHelper.AttachHoverFeedback(_exportBtn, AppTheme.Surface, Color.FromArgb(0xF0, 0xF3, 0xF9));
            _exportBtn.Click += (_, _) => ExportCurrentTab();
            Controls.Add(_exportBtn);

            // ─── Tab bar ───────────────────────────────────────
            _tabBar = new Panel
            {
                Location = new Point(0, 140),
                Height = 42,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_tabBar);

            BuildTabs();

            // ─── Content host ──────────────────────────────────
            _contentHost = new Panel
            {
                Location = new Point(0, 194),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true
            };
            Controls.Add(_contentHost);

            // ─── Loading overlay ───────────────────────────────
            _loading = new Panel
            {
                BackColor = Color.FromArgb(180, 255, 255, 255),
                Visible = false
            };
            var loadingLbl = new Label
            {
                Text = "Loading…",
                Font = new Font("Bahnschrift SemiBold", 14F),
                ForeColor = AppTheme.Signal,
                AutoSize = true
            };
            _loading.Controls.Add(loadingLbl);
            _loading.Resize += (_, _) =>
            {
                loadingLbl.Location = new Point(
                    (_loading.Width - loadingLbl.Width) / 2,
                    (_loading.Height - loadingLbl.Height) / 2);
            };
            Controls.Add(_loading);
            _loading.BringToFront();

            Resize += (_, _) => LayoutChildren();
            LayoutChildren();
        }

        private void BuildTabs()
        {
            _tabBar.Controls.Clear();
            _tabs.Clear();

            // Role-based tab visibility
            var visible = new List<string> { "Overview" };

            if (SessionManager.Current.CanManageTeam)
            {
                visible.Add("Pipeline");
                visible.Add("Activity");
                visible.Add("Conversion");
            }
            else
            {
                // SalesStaff: still allow Pipeline & Activity (their own data)
                visible.Add("Pipeline");
                visible.Add("Activity");
                visible.Add("Conversion");
            }

            if (SessionManager.Current.CanManageTeam)
            {
                visible.Add("Retention");
                visible.Add("Team");
            }

            int x = 0;
            foreach (var key in visible)
            {
                var btn = new Button
                {
                    Text = key,
                    Location = new Point(x, 4),
                    Size = new Size(120, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = AppTheme.Surface,
                    ForeColor = AppTheme.Trace,
                    Font = new Font("Segoe UI Variable Text Semibold", 9.5F),
                    Cursor = Cursors.Hand,
                    Tag = key
                };
                btn.FlatAppearance.BorderColor = AppTheme.Line;
                UiRadiusHelper.StyleButton(btn, 8);
                btn.Click += async (_, _) =>
                {
                    _activeTab = (string)btn.Tag;
                    UpdateTabStyles();
                    await LoadActiveTabAsync();
                };

                _tabs.Add((btn, key));
                _tabBar.Controls.Add(btn);
                x += 126;
            }

            UpdateTabStyles();
        }

        private void UpdateTabStyles()
        {
            foreach (var (btn, key) in _tabs)
            {
                bool sel = key == _activeTab;
                btn.BackColor = sel ? AppTheme.Signal : AppTheme.Surface;
                btn.ForeColor = sel ? Color.White : AppTheme.Trace;
                btn.Invalidate();
            }
        }

        private void LayoutChildren()
        {
            int w = ClientSize.Width;
            _exportBtn.Location = new Point(w - _exportBtn.Width - 4, 88);
            _tabBar.Width = w;
            _contentHost.Width = w;
            _contentHost.Height = Math.Max(200, ClientSize.Height - _contentHost.Top - 8);
            _loading.Location = _contentHost.Location;
            _loading.Size = _contentHost.Size;
        }

        // ═══════════════════════════════════════════════════════
        // RANGE HANDLING
        // ═══════════════════════════════════════════════════════
        private async Task OnRangeChanged()
        {
            var choice = _rangeFilter.SelectedItem?.ToString() ?? "Last 30 days";
            var today = DateTime.UtcNow.Date;

            switch (choice)
            {
                case "Last 7 days":
                    _from = today.AddDays(-7);
                    _to = today;
                    break;
                case "Last 30 days":
                    _from = today.AddDays(-30);
                    _to = today;
                    break;
                case "This month":
                    _from = new DateTime(today.Year, today.Month, 1);
                    _to = today;
                    break;
                case "Last 90 days":
                    _from = today.AddDays(-90);
                    _to = today;
                    break;
                case "This quarter":
                    int q = (today.Month - 1) / 3;
                    _from = new DateTime(today.Year, q * 3 + 1, 1);
                    _to = today;
                    break;
                case "This year":
                    _from = new DateTime(today.Year, 1, 1);
                    _to = today;
                    break;
                case "Custom range":
                    _fromPicker.Value = _from;
                    _toPicker.Value = _to;
                    break;
            }

            bool showCustom = choice == "Custom range";
            _fromPicker.Visible = showCustom;
            _toPicker.Visible = showCustom;

            await LoadActiveTabAsync();
        }

        // ═══════════════════════════════════════════════════════
        // LOAD
        // ═══════════════════════════════════════════════════════
        private async Task LoadActiveTabAsync()
        {
            ShowLoading(true);
            try
            {
                switch (_activeTab)
                {
                    case "Overview": await LoadOverviewAsync(); break;
                    case "Pipeline": await LoadPipelineAsync(); break;
                    case "Activity": await LoadActivityAsync(); break;
                    case "Retention": await LoadRetentionAsync(); break;
                    case "Conversion": await LoadConversionAsync(); break;
                    case "Team": await LoadTeamAsync(); break;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadOverviewAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetOverviewAsync(_from, _to);
            if (data == null) return;
            _overviewData = data;

            _contentHost.Controls.Clear();

            // KPI grid — 3 columns, 4 rows
            int cardW = (ClientSize.Width - 32) / 3;
            int cardH = 90;
            int gap = 12;
            int y = 0;

            var kpis = new (string label, string value, Color accent)[]
            {
                ("Total Customers",        data.TotalCustomers.ToString(),                  AppTheme.Signal),
                ("Active Customers",        data.ActiveCustomers.ToString(),                 AppTheme.SuccessText),
                ("New This Month",          data.NewCustomersThisMonth.ToString(),           AppTheme.InfoText),
                ("Total Leads",            data.TotalLeads.ToString(),                      AppTheme.Signal),
                ("Active Leads",           data.ActiveLeads.ToString(),                     Color.FromArgb(0xC1, 0x7B, 0x12)),
                ("Pipeline Value",         $"₱{data.PipelineValue:N0}",                     AppTheme.Signal),
                ("Pending Follow-ups",     data.PendingFollowUps.ToString(),                AppTheme.WarningText),
                ("Overdue Follow-ups",     data.OverdueFollowUps.ToString(),                AppTheme.DangerText),
                ("Completed This Month",    data.CompletedFollowUpsThisMonth.ToString(),     AppTheme.SuccessText),
                ("Open Inquiries",         data.OpenInquiries.ToString(),                   AppTheme.InfoText),
                ("Overdue Inquiries",      data.OverdueInquiries.ToString(),                AppTheme.DangerText),
                ("Conversion Rate",        $"{data.ConversionRate * 100:0.#}%",              AppTheme.Signal),
            };

            for (int i = 0; i < kpis.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                var card = MakeKpiCard(
                    kpis[i].label,
                    kpis[i].value,
                    kpis[i].accent,
                    new Point(col * (cardW + gap), row * (cardH + gap)),
                    new Size(cardW, cardH));
                _contentHost.Controls.Add(card);
                y = Math.Max(y, card.Bottom);
            }

            // Role + range footer
            var footer = new Label
            {
                Text = $"Scope: {data.Scope}  ·  {data.RangeFrom:MMM d} – {data.RangeTo:MMM d, yyyy}",
                Font = AppTheme.FontBodySmall,
                ForeColor = AppTheme.Trace,
                Location = new Point(0, y + 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _contentHost.Controls.Add(footer);
        }

        private async Task LoadPipelineAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetPipelineAsync(_from, _to);
            if (data == null) return;
            _pipelineData = data;

            _contentHost.Controls.Clear();

            // Chart 1 — Pipeline by stage (count)
            var countChart = new HorizontalBarChartControl
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width - 4, Math.Max(280, data.Stages.Count * 40 + 60))
            };
            countChart.SetTitle("Leads by Stage");
            countChart.SetData(data.Stages.Select(s => new HorizontalBarChartControl.Row
            {
                Label = Humanize(s.Stage),
                Value = s.Count,
                Color = StageColor(s.Stage)
            }));
            _contentHost.Controls.Add(countChart);

            // Chart 2 — Pipeline value by stage
            var valueChart = new HorizontalBarChartControl
            {
                Location = new Point(0, countChart.Bottom + 12),
                Size = new Size(ClientSize.Width - 4, Math.Max(280, data.Stages.Count * 40 + 60))
            };
            valueChart.SetTitle("Pipeline Value by Stage (₱)");
            valueChart.SetData(data.Stages.Select(s => new HorizontalBarChartControl.Row
            {
                Label = Humanize(s.Stage),
                Value = (double)s.TotalValue,
                Color = StageColor(s.Stage)
            }));
            _contentHost.Controls.Add(valueChart);
        }

        private async Task LoadActivityAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetActivityAsync(_from, _to);
            if (data == null) return;
            _activityData = data;

            _contentHost.Controls.Clear();

            // Chart 1 — activities per day (line chart)
            var dailyChart = new LineChartControl
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width - 4, 260)
            };
            dailyChart.SetTitle($"Activities per Day — Total: {data.TotalActivities}");
            dailyChart.SetData(data.ByDay.Select(d => new LineChartControl.Point2
            {
                Label = d.Date.ToString("MMM d"),
                Value = d.Count
            }));
            _contentHost.Controls.Add(dailyChart);

            // Chart 2 — activities per user (vertical bar)
            var userChart = new BarChartControl
            {
                Location = new Point(0, dailyChart.Bottom + 12),
                Size = new Size(ClientSize.Width - 4, 280)
            };
            userChart.SetTitle("Activities by User");
            userChart.SetData(data.ByUser.Take(10).Select(u => new BarChartControl.Bar
            {
                Label = u.UserName.Length > 12 ? u.UserName.Substring(0, 12) : u.UserName,
                Value = u.TotalActivities,
                Color = AppTheme.Signal
            }));
            _contentHost.Controls.Add(userChart);

            // Chart 3 — breakdown by type
            var typeChart = new DonutChartControl
            {
                Location = new Point(0, userChart.Bottom + 12),
                Size = new Size(ClientSize.Width - 4, 320)
            };
            typeChart.SetTitle("Activities by Type");
            typeChart.SetCenter("TOTAL", data.TotalActivities.ToString());
            typeChart.SetData(data.ByType.Select(t => new DonutChartControl.Slice
            {
                Label = t.ActivityType,
                Value = t.Count,
                Color = ActivityTypeColor(t.ActivityType)
            }));
            _contentHost.Controls.Add(typeChart);
        }

        private async Task LoadRetentionAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetRetentionAsync(_from, _to, 60);
            if (data == null) return;
            _retentionData = data;

            _contentHost.Controls.Clear();

            // Row 1 — 3 KPI cards
            int cardW = (ClientSize.Width - 32) / 3;
            int cardH = 100;
            int gap = 12;

            _contentHost.Controls.Add(MakeKpiCard(
                "Retention Rate", $"{data.RetentionRate * 100:0.#}%", AppTheme.SuccessText,
                new Point(0, 0), new Size(cardW, cardH)));

            _contentHost.Controls.Add(MakeKpiCard(
                "Churn Rate", $"{data.ChurnRate * 100:0.#}%", AppTheme.DangerText,
                new Point(cardW + gap, 0), new Size(cardW, cardH)));

            _contentHost.Controls.Add(MakeKpiCard(
                "At-Risk Customers", data.AtRiskCustomers.ToString(), AppTheme.WarningText,
                new Point(2 * (cardW + gap), 0), new Size(cardW, cardH)));

            // Row 2 — Line chart: new vs returning
            var trendChart = new LineChartControl
            {
                Location = new Point(0, cardH + gap),
                Size = new Size(ClientSize.Width - 4, 260)
            };
            trendChart.SetTitle("New Customers per Month");
            trendChart.SetData(data.MonthlyTrend.Select(m => new LineChartControl.Point2
            {
                Label = MonthLabel(m.Month),
                Value = m.NewCustomers
            }));
            _contentHost.Controls.Add(trendChart);

            // Row 3 — At-risk customer list
            var atRiskPanel = new Panel
            {
                Location = new Point(0, trendChart.Bottom + gap),
                Size = new Size(ClientSize.Width - 4, 380),
                BackColor = AppTheme.Surface,
                Padding = new Padding(20)
            };
            UiRadiusHelper.StyleCard(atRiskPanel, 12);

            var atRiskTitle = new Label
            {
                Text = $"At-Risk Customers  ({data.AtRiskList.Count})",
                Font = AppTheme.FontSubheading,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 16),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            atRiskPanel.Controls.Add(atRiskTitle);

            var grid = new DataGridView
            {
                Location = new Point(20, 52),
                Size = new Size(atRiskPanel.Width - 40, atRiskPanel.Height - 72),
                AutoGenerateColumns = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            AppTheme.ApplyGridStyle(grid);
            grid.RowTemplate.Height = 42;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Name", HeaderText = "CUSTOMER", FillWeight = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Company", HeaderText = "COMPANY", FillWeight = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "AssignedUserName", HeaderText = "ASSIGNED", FillWeight = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DaysSinceLastActivity",
                HeaderText = "DAYS INACTIVE",
                FillWeight = 100
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LastActivityDate",
                HeaderText = "LAST ACTIVITY",
                FillWeight = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, yyyy" }
            });

            grid.DataSource = data.AtRiskList;
            atRiskPanel.Controls.Add(grid);

            _contentHost.Controls.Add(atRiskPanel);
        }

        private async Task LoadConversionAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetConversionAsync(_from, _to);
            if (data == null) return;
            _conversionData = data;

            _contentHost.Controls.Clear();

            // Row 1 — 3 KPI cards
            int cardW = (ClientSize.Width - 32) / 3;
            int cardH = 100;
            int gap = 12;

            _contentHost.Controls.Add(MakeKpiCard(
                "Total Leads", data.TotalLeads.ToString(), AppTheme.Signal,
                new Point(0, 0), new Size(cardW, cardH)));

            _contentHost.Controls.Add(MakeKpiCard(
                "Converted", data.ConvertedLeads.ToString(), AppTheme.SuccessText,
                new Point(cardW + gap, 0), new Size(cardW, cardH)));

            _contentHost.Controls.Add(MakeKpiCard(
                "Conversion Rate", $"{data.OverallConversionRate * 100:0.#}%", AppTheme.Signal,
                new Point(2 * (cardW + gap), 0), new Size(cardW, cardH)));

            // Row 2 — Line chart: conversion by source
            var sourceChart = new HorizontalBarChartControl
            {
                Location = new Point(0, cardH + gap),
                Size = new Size(ClientSize.Width - 4, Math.Max(260, data.BySource.Count * 40 + 60))
            };
            sourceChart.SetTitle($"Conversion by Source  ·  Avg Days: {data.AverageDaysToConvert:0.#}");
            sourceChart.SetData(data.BySource.Select(s => new HorizontalBarChartControl.Row
            {
                Label = s.Source,
                Value = s.Converted,
                Color = AppTheme.SuccessText
            }));
            _contentHost.Controls.Add(sourceChart);
        }

        private async Task LoadTeamAsync()
        {
            var client = new ReportsApiClient();
            var data = await client.GetTeamPerformanceAsync(_from, _to);
            if (data == null) return;
            _teamData = data;

            _contentHost.Controls.Clear();

            // Progress bar list of conversion rates
            var progressList = new ProgressBarListControl
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width - 4, Math.Max(300, data.Members.Count * 52 + 60))
            };
            progressList.SetTitle($"Team Conversion Rates  ·  {data.TotalMembers} members");
            progressList.SetData(data.Members.Select(m => new ProgressBarListControl.Item
            {
                Label = m.Name,
                Subtitle = $"{m.Role}  ·  {m.ConvertedLeads} converted / {m.AssignedLeads} assigned  ·  {m.ActivitiesLogged} activities",
                Value = m.ConversionRate,
                Color = m.ConversionRate >= 0.5 ? AppTheme.SuccessText
                       : m.ConversionRate >= 0.25 ? AppTheme.WarningText
                       : AppTheme.DangerText
            }));
            _contentHost.Controls.Add(progressList);

            // DataGridView with detailed table
            var gridPanel = new Panel
            {
                Location = new Point(0, progressList.Bottom + 12),
                Size = new Size(ClientSize.Width - 4, 380),
                BackColor = AppTheme.Surface,
                Padding = new Padding(20)
            };
            UiRadiusHelper.StyleCard(gridPanel, 12);

            var gridTitle = new Label
            {
                Text = "Detailed Performance",
                Font = AppTheme.FontSubheading,
                ForeColor = AppTheme.Ink,
                Location = new Point(20, 16),
                AutoSize = true
            };
            gridPanel.Controls.Add(gridTitle);

            var grid = new DataGridView
            {
                Location = new Point(20, 52),
                Size = new Size(gridPanel.Width - 40, gridPanel.Height - 72),
                AutoGenerateColumns = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            AppTheme.ApplyGridStyle(grid);
            grid.RowTemplate.Height = 42;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Name", HeaderText = "NAME", FillWeight = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "Role", HeaderText = "ROLE", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "AssignedCustomers", HeaderText = "CUSTOMERS", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "AssignedLeads", HeaderText = "LEADS", FillWeight = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "ConvertedLeads", HeaderText = "CONVERTED", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { DataPropertyName = "ActivitiesLogged", HeaderText = "ACTIVITIES", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PipelineValue",
                HeaderText = "PIPELINE ₱",
                FillWeight = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "₱#,##0" }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ConversionRate",
                HeaderText = "RATE",
                FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "P0" }
            });

            grid.DataSource = data.Members;
            gridPanel.Controls.Add(grid);

            _contentHost.Controls.Add(gridPanel);
        }

        // ═══════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════
        private Panel MakeKpiCard(string label, string value, Color accent, Point loc, Size size)
        {
            var card = new Panel
            {
                Location = loc,
                Size = size,
                BackColor = AppTheme.Surface
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, 12);
                using var pen = new Pen(AppTheme.Line, 1);
                g.DrawPath(pen, path);
                using var brush = new SolidBrush(accent);
                g.FillRectangle(brush, 0, 14, 4, card.Height - 28);
            };

            var lblTitle = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = AppTheme.FontKpiLabel,
                ForeColor = AppTheme.Trace,
                Location = new Point(20, 16),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Bahnschrift SemiBold", 22F),
                ForeColor = AppTheme.Ink,
                Location = new Point(18, 42),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblValue);

            return card;
        }

        private void ShowLoading(bool show)
        {
            _loading.Visible = show;
            if (show) _loading.BringToFront();
        }

        private void ShowError(string message)
        {
            _contentHost.Controls.Clear();
            var lbl = new Label
            {
                Text = $"⚠ {message}",
                Font = new Font("Segoe UI Variable Text", 11F, FontStyle.Italic),
                ForeColor = AppTheme.DangerText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _contentHost.Controls.Add(lbl);
        }

        private static string Humanize(string enumName)
        {
            // "ProposalSent" → "Proposal Sent"
            var sb = new StringBuilder();
            foreach (var ch in enumName)
            {
                if (char.IsUpper(ch) && sb.Length > 0) sb.Append(' ');
                sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string MonthLabel(string yyyyMM)
        {
            if (DateTime.TryParse(yyyyMM + "-01", out var d))
                return d.ToString("MMM");
            return yyyyMM;
        }

        private static Color StageColor(string stage)
        {
            var s = stage.ToLowerInvariant();
            if (s.Contains("won")) return AppTheme.SuccessText;
            if (s.Contains("lost")) return AppTheme.DangerText;
            if (s.Contains("new")) return AppTheme.InfoText;
            if (s.Contains("qualified") || s.Contains("proposal") || s.Contains("negotiation"))
                return Color.FromArgb(0xC1, 0x7B, 0x12);
            return AppTheme.Trace;
        }

        private static Color ActivityTypeColor(string type)
        {
            var t = type.ToLowerInvariant();
            return t switch
            {
                "call" => AppTheme.InfoText,
                "email" => Color.FromArgb(0xC1, 0x7B, 0x12),
                "meeting" => AppTheme.SuccessText,
                "task" => Color.FromArgb(0x6B, 0x21, 0xA8),
                _ => AppTheme.Trace
            };
        }

        // ═══════════════════════════════════════════════════════
        // CSV EXPORT
        // ═══════════════════════════════════════════════════════
        private void ExportCurrentTab()
        {
            string? csv = _activeTab switch
            {
                "Overview" => ExportOverview(),
                "Pipeline" => ExportPipeline(),
                "Activity" => ExportActivity(),
                "Retention" => ExportRetention(),
                "Conversion" => ExportConversion(),
                "Team" => ExportTeam(),
                _ => null
            };

            if (string.IsNullOrWhiteSpace(csv))
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"KonSpot_Report_{_activeTab}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                File.WriteAllText(sfd.FileName, csv, Encoding.UTF8);
                ToastHost.Show(this, $"{_activeTab} report exported", ToastKind.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string? ExportOverview()
        {
            if (_overviewData == null) return null;
            var d = _overviewData;
            var sb = new StringBuilder();
            sb.AppendLine($"KonSpot — Overview Report ({d.Scope})");
            sb.AppendLine($"Range,{d.RangeFrom:yyyy-MM-dd},{d.RangeTo:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Total Customers,{d.TotalCustomers}");
            sb.AppendLine($"Active Customers,{d.ActiveCustomers}");
            sb.AppendLine($"New This Month,{d.NewCustomersThisMonth}");
            sb.AppendLine($"Total Leads,{d.TotalLeads}");
            sb.AppendLine($"Active Leads,{d.ActiveLeads}");
            sb.AppendLine($"Pipeline Value,{d.PipelineValue}");
            sb.AppendLine($"Pending Follow-ups,{d.PendingFollowUps}");
            sb.AppendLine($"Overdue Follow-ups,{d.OverdueFollowUps}");
            sb.AppendLine($"Completed This Month,{d.CompletedFollowUpsThisMonth}");
            sb.AppendLine($"Open Inquiries,{d.OpenInquiries}");
            sb.AppendLine($"Overdue Inquiries,{d.OverdueInquiries}");
            sb.AppendLine($"Conversion Rate,{d.ConversionRate:P1}");
            sb.AppendLine($"Retention Rate,{d.RetentionRate:P1}");
            sb.AppendLine($"At-Risk Customers,{d.AtRiskCustomers}");
            return sb.ToString();
        }

        private string? ExportPipeline()
        {
            if (_pipelineData == null) return null;
            var sb = new StringBuilder();
            sb.AppendLine("Stage,Count,Total Value,Percentage");
            foreach (var s in _pipelineData.Stages)
                sb.AppendLine($"{s.Stage},{s.Count},{s.TotalValue},{s.PercentageOfTotal:P1}");
            sb.AppendLine();
            sb.AppendLine($"TOTAL,{_pipelineData.TotalLeads},{_pipelineData.TotalPipelineValue}");
            return sb.ToString();
        }

        private string? ExportActivity()
        {
            if (_activityData == null) return null;
            var sb = new StringBuilder();
            sb.AppendLine("=== Activities by Type ===");
            sb.AppendLine("Type,Count");
            foreach (var t in _activityData.ByType)
                sb.AppendLine($"{t.ActivityType},{t.Count}");
            sb.AppendLine();
            sb.AppendLine("=== Activities by User ===");
            sb.AppendLine("User,Total,Calls,Emails,Meetings,Notes,Tasks");
            foreach (var u in _activityData.ByUser)
                sb.AppendLine($"{u.UserName},{u.TotalActivities},{u.Calls},{u.Emails},{u.Meetings},{u.Notes},{u.Tasks}");
            sb.AppendLine();
            sb.AppendLine("=== Daily Trend ===");
            sb.AppendLine("Date,Count");
            foreach (var d in _activityData.ByDay)
                sb.AppendLine($"{d.Date:yyyy-MM-dd},{d.Count}");
            return sb.ToString();
        }

        private string? ExportRetention()
        {
            if (_retentionData == null) return null;
            var d = _retentionData;
            var sb = new StringBuilder();
            sb.AppendLine("=== Summary ===");
            sb.AppendLine($"Retention Rate,{d.RetentionRate:P1}");
            sb.AppendLine($"Churn Rate,{d.ChurnRate:P1}");
            sb.AppendLine($"Total Customers,{d.TotalCustomers}");
            sb.AppendLine($"Active,{d.ActiveCustomers}");
            sb.AppendLine($"At-Risk,{d.AtRiskCustomers}");
            sb.AppendLine();
            sb.AppendLine("=== At-Risk Customers ===");
            sb.AppendLine("Name,Email,Company,Assigned,Days Inactive,Last Activity");
            foreach (var c in d.AtRiskList)
                sb.AppendLine($"{c.Name},{c.Email},{c.Company},{c.AssignedUserName},{c.DaysSinceLastActivity},{c.LastActivityDate:yyyy-MM-dd}");
            return sb.ToString();
        }

        private string? ExportConversion()
        {
            if (_conversionData == null) return null;
            var d = _conversionData;
            var sb = new StringBuilder();
            sb.AppendLine($"Total Leads,{d.TotalLeads}");
            sb.AppendLine($"Converted,{d.ConvertedLeads}");
            sb.AppendLine($"Conversion Rate,{d.OverallConversionRate:P1}");
            sb.AppendLine($"Avg Days to Convert,{d.AverageDaysToConvert:0.#}");
            sb.AppendLine();
            sb.AppendLine("Source,Total,Converted,Rate");
            foreach (var s in d.BySource)
                sb.AppendLine($"{s.Source},{s.TotalLeads},{s.Converted},{s.ConversionRate:P1}");
            return sb.ToString();
        }

        private string? ExportTeam()
        {
            if (_teamData == null) return null;
            var sb = new StringBuilder();
            sb.AppendLine("Name,Role,Customers,Leads,Converted,Activities,Pipeline,Rate");
            foreach (var m in _teamData.Members)
                sb.AppendLine($"{m.Name},{m.Role},{m.AssignedCustomers},{m.AssignedLeads},{m.ConvertedLeads},{m.ActivitiesLogged},{m.PipelineValue},{m.ConversionRate:P1}");
            return sb.ToString();
        }
    }
}