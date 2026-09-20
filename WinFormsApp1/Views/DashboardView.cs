using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace App.WinForms.Views
{
    public class DashboardView : UserControl
    {
        private readonly ApiClient _api = new();

        // UI Containers
        private Panel _mainScrollPanel = null!;
        private Panel _pnlError = null!;
        private Label _lblErrorMsg = null!;
        private Button _btnRetry = null!;

        // KPI Value Labels
        private Label _lblCustomersVal = null!;
        private Label _lblBookingsVal = null!;
        private Label _lblRevenueVal = null!;
        private Label _lblRepeatRateVal = null!;

        // Row 2 Controls
        private Chart _chartMonthly = null!;
        private Panel _pnlTopServicesList = null!;

        // Row 3 Controls
        private Panel _pnlCategoryList = null!;
        private Label _lblRetentionRepeatRate = null!;
        private Label _lblRetentionAtRisk = null!;
        private Label _lblRetentionAvgValue = null!;
        private Label _lblRetentionCompletedRate = null!;
        private Label _lblLastUpdated = null!;

        public DashboardView()
        {
            BuildUI();
            _ = LoadDashboardAsync();
        }

        private void BuildUI()
        {
            BackColor = Color.FromArgb(245, 246, 250);
            Dock = DockStyle.Fill;
            DoubleBuffered = true;

            // Main scroll container
            _mainScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 246, 250),
                Padding = new Padding(24, 20, 24, 24)
            };
            Controls.Add(_mainScrollPanel);

            // Error Overlay Panel
            _pnlError = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(254, 242, 242),
                Padding = new Padding(16, 12, 16, 12),
                Visible = false
            };
            _pnlError.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(248, 113, 113), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlError.Width - 1, _pnlError.Height - 1);
            };

            _lblErrorMsg = new Label
            {
                Text = "⚠ Unable to load dashboard data. Ensure App.API is running.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pnlError.Controls.Add(_lblErrorMsg);

            _btnRetry = new Button
            {
                Text = "Retry",
                Dock = DockStyle.Right,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnRetry.FlatAppearance.BorderSize = 0;
            _btnRetry.Click += async (s, e) => await LoadDashboardAsync();
            _pnlError.Controls.Add(_btnRetry);

            _mainScrollPanel.Controls.Add(_pnlError);

            // Layout Container
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _mainScrollPanel.Controls.Add(layout);

            // -------------------------------------------------------------
            // ROW 0: Header
            // -------------------------------------------------------------
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 65,
                Margin = new Padding(0, 0, 0, 16)
            };

            var lblTitle = new Label
            {
                Text = "Business Intelligence Dashboard",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Real-time key performance indicators, service metrics, and customer retention analytics",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 32),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSubtitle);

            _lblLastUpdated = new Label
            {
                Text = "Loading...",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.TopRight,
                Width = 240
            };
            pnlHeader.Controls.Add(_lblLastUpdated);

            layout.Controls.Add(pnlHeader, 0, 0);

            // -------------------------------------------------------------
            // ROW 1: 4 KPI Cards
            // -------------------------------------------------------------
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 115,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var (card1, val1) = CreateKpiCard("👥 TOTAL CUSTOMERS", "--", "Active customer accounts", Color.FromArgb(30, 41, 59));
            _lblCustomersVal = val1;
            pnlKpis.Controls.Add(card1, 0, 0);

            var (card2, val2) = CreateKpiCard("📅 TOTAL BOOKINGS", "--", "All service requests", Color.FromArgb(30, 41, 59));
            _lblBookingsVal = val2;
            pnlKpis.Controls.Add(card2, 1, 0);

            var (card3, val3) = CreateKpiCard("💰 TOTAL REVENUE", "--", "Completed bookings total", Color.FromArgb(34, 197, 94));
            _lblRevenueVal = val3;
            pnlKpis.Controls.Add(card3, 2, 0);

            var (card4, val4) = CreateKpiCard("🔁 REPEAT RATE", "--", "Customers with >1 booking", Color.FromArgb(37, 99, 235));
            _lblRepeatRateVal = val4;
            pnlKpis.Controls.Add(card4, 3, 0);

            layout.Controls.Add(pnlKpis, 0, 1);

            // -------------------------------------------------------------
            // ROW 2: Monthly Trend (Left) + Top 5 Services (Right)
            // -------------------------------------------------------------
            var pnlRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };
            pnlRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            pnlRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Monthly Trend Card
            var cardMonthly = CreateCardContainer();
            cardMonthly.Margin = new Padding(0, 0, 8, 0);

            var pnlMonthlyHeader = CreateSectionHeader("Monthly Bookings Trend", "Monthly booking volume over the last 12 months");
            cardMonthly.Controls.Add(pnlMonthlyHeader);

            _chartMonthly = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            SetupEmptyChart(_chartMonthly);
            cardMonthly.Controls.Add(_chartMonthly);
            _chartMonthly.BringToFront();

            pnlRow2.Controls.Add(cardMonthly, 0, 0);

            // Top Services Card
            var cardTopServices = CreateCardContainer();
            cardTopServices.Margin = new Padding(8, 0, 0, 0);

            var pnlTopServicesHeader = CreateSectionHeader("Top 5 Services", "Most requested services by volume");
            cardTopServices.Controls.Add(pnlTopServicesHeader);

            _pnlTopServicesList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4, 8, 4, 4)
            };
            cardTopServices.Controls.Add(_pnlTopServicesList);
            _pnlTopServicesList.BringToFront();

            pnlRow2.Controls.Add(cardTopServices, 1, 0);
            layout.Controls.Add(pnlRow2, 0, 2);

            // -------------------------------------------------------------
            // ROW 3: Category Breakdown (Left) + Retention Insights (Right)
            // -------------------------------------------------------------
            var pnlRow3 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 320,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };
            pnlRow3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlRow3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Category Breakdown Card
            var cardCategory = CreateCardContainer();
            cardCategory.Margin = new Padding(0, 0, 8, 0);

            var pnlCatHeader = CreateSectionHeader("Category Breakdown", "Booking counts & revenue by service tier");
            cardCategory.Controls.Add(pnlCatHeader);

            _pnlCategoryList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4, 12, 4, 4)
            };
            cardCategory.Controls.Add(_pnlCategoryList);
            _pnlCategoryList.BringToFront();

            pnlRow3.Controls.Add(cardCategory, 0, 0);

            // Retention Insights Card
            var cardRetention = CreateCardContainer();
            cardRetention.Margin = new Padding(8, 0, 0, 0);

            var pnlRetentionHeader = CreateSectionHeader("Retention Insights", "Key metrics for customer loyalty and health");
            cardRetention.Controls.Add(pnlRetentionHeader);

            var pnlRetentionBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(8)
            };
            pnlRetentionBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlRetentionBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlRetentionBody.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlRetentionBody.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var (box1, valRet1) = CreateInsightBox("Repeat Customer Rate", "--", "Active accounts with repeat orders", Color.FromArgb(37, 99, 235));
            _lblRetentionRepeatRate = valRet1;
            pnlRetentionBody.Controls.Add(box1, 0, 0);

            var (box2, valRet2) = CreateInsightBox("At-Risk Customers", "--", "No completed booking in 60+ days", Color.FromArgb(220, 38, 38));
            _lblRetentionAtRisk = valRet2;
            pnlRetentionBody.Controls.Add(box2, 1, 0);

            var (box3, valRet3) = CreateInsightBox("Average Booking Value", "--", "Mean revenue per completed booking", Color.FromArgb(30, 41, 59));
            _lblRetentionAvgValue = valRet3;
            pnlRetentionBody.Controls.Add(box3, 0, 1);

            var (box4, valRet4) = CreateInsightBox("Completion Rate", "--", "Completed vs total bookings", Color.FromArgb(22, 163, 74));
            _lblRetentionCompletedRate = valRet4;
            pnlRetentionBody.Controls.Add(box4, 1, 1);

            cardRetention.Controls.Add(pnlRetentionBody);
            pnlRetentionBody.BringToFront();

            pnlRow3.Controls.Add(cardRetention, 1, 0);
            layout.Controls.Add(pnlRow3, 0, 3);
        }

        // =============================================================
        // Data Loading
        // =============================================================
        public async Task LoadDashboardAsync()
        {
            _pnlError.Visible = false;
            _lblLastUpdated.Text = "Refreshing data...";

            var data = await _api.GetDashboardAsync();

            if (data == null)
            {
                _pnlError.Visible = true;
                _lblLastUpdated.Text = "Failed to load data";
                return;
            }

            _lblLastUpdated.Text = $"Last updated: {DateTime.Now:hh:mm:ss tt}";

            // Populate KPIs
            _lblCustomersVal.Text = data.TotalCustomers.ToString("N0");
            _lblBookingsVal.Text = data.TotalBookings.ToString("N0");
            _lblRevenueVal.Text = $"₱{data.TotalRevenue:N2}";
            _lblRepeatRateVal.Text = $"{data.RepeatCustomerRate:F1}%";

            // Populate Chart
            PopulateMonthlyChart(data.MonthlyTrend);

            // Populate Top Services
            PopulateTopServices(data.TopServices);

            // Populate Category Breakdown
            PopulateCategoryBreakdown(data.CategoryBreakdown, data.TotalBookings);

            // Populate Retention Insights
            _lblRetentionRepeatRate.Text = $"{data.RepeatCustomerRate:F1}%";
            _lblRetentionAtRisk.Text = data.AtRiskCustomerCount.ToString("N0");
            _lblRetentionAtRisk.ForeColor = data.AtRiskCustomerCount > 0
                ? Color.FromArgb(220, 38, 38)
                : Color.FromArgb(22, 163, 74);

            _lblRetentionAvgValue.Text = $"₱{data.AverageBookingValue:N2}";

            double completionRate = data.TotalBookings > 0
                ? Math.Round((double)data.CompletedBookings / data.TotalBookings * 100.0, 1)
                : 0.0;
            _lblRetentionCompletedRate.Text = $"{completionRate:F1}%";
        }

        // =============================================================
        // Chart Configuration
        // =============================================================
        private void SetupEmptyChart(Chart chart)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Legends.Clear();

            var area = new ChartArea("MainArea")
            {
                BackColor = Color.White
            };
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(241, 245, 249);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(241, 245, 249);
            area.AxisX.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8F);
            area.AxisX.LabelStyle.ForeColor = Color.FromArgb(100, 116, 139);
            area.AxisY.LabelStyle.ForeColor = Color.FromArgb(100, 116, 139);
            area.AxisX.Interval = 1;

            chart.ChartAreas.Add(area);
        }

        private void PopulateMonthlyChart(List<MonthlyTrendDto>? trends)
        {
            SetupEmptyChart(_chartMonthly);

            if (trends == null || trends.Count == 0)
                return;

            var series = new Series("Bookings")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(59, 130, 246),
                BorderWidth = 0,
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                LabelForeColor = Color.FromArgb(30, 41, 59)
            };

            foreach (var item in trends)
            {
                string label = item.Month;
                if (DateTime.TryParseExact(item.Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    label = dt.ToString("MMM yy");
                }
                var point = new DataPoint();
                point.SetValueXY(label, item.Bookings);
                point.ToolTip = $"{label}: {item.Bookings} bookings (₱{item.Revenue:N2})";
                series.Points.Add(point);
            }

            _chartMonthly.Series.Add(series);
        }

        // =============================================================
        // Top 5 Services List
        // =============================================================
        private void PopulateTopServices(List<TopServiceDto>? services)
        {
            _pnlTopServicesList.Controls.Clear();

            if (services == null || services.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No service booking data available.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Dock = DockStyle.Top,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlTopServicesList.Controls.Add(lblEmpty);
                return;
            }

            for (int i = services.Count - 1; i >= 0; i--)
            {
                var svc = services[i];
                int rank = i + 1;

                var rowPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 48,
                    Padding = new Padding(8, 4, 8, 4),
                    BackColor = Color.White
                };

                // Bottom divider
                rowPanel.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(241, 245, 249), 1);
                    e.Graphics.DrawLine(pen, 0, rowPanel.Height - 1, rowPanel.Width, rowPanel.Height - 1);
                };

                // Rank badge
                var lblRank = new Label
                {
                    Text = $"#{rank}",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = rank <= 3 ? Color.FromArgb(37, 99, 235) : Color.FromArgb(100, 116, 139),
                    Width = 32,
                    Dock = DockStyle.Left,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                rowPanel.Controls.Add(lblRank);

                // Service Name
                var lblName = new Label
                {
                    Text = svc.ServiceName,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                rowPanel.Controls.Add(lblName);

                // Right metrics: Count + Revenue
                var lblStats = new Label
                {
                    Text = $"{svc.Count} bookings   (₱{svc.Revenue:N2})",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Dock = DockStyle.Right,
                    Width = 200,
                    TextAlign = ContentAlignment.MiddleRight
                };
                rowPanel.Controls.Add(lblStats);

                _pnlTopServicesList.Controls.Add(rowPanel);
            }
        }

        // =============================================================
        // Category Breakdown List
        // =============================================================
        private void PopulateCategoryBreakdown(List<CategoryBreakdownDto>? categories, int totalBookings)
        {
            _pnlCategoryList.Controls.Clear();

            if (categories == null || categories.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "No category data available.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Dock = DockStyle.Top,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlCategoryList.Controls.Add(lblEmpty);
                return;
            }

            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                { "Residential", Color.FromArgb(59, 130, 246) },
                { "Commercial", Color.FromArgb(16, 185, 129) },
                { "Specialty", Color.FromArgb(245, 158, 11) }
            };

            for (int i = categories.Count - 1; i >= 0; i--)
            {
                var cat = categories[i];
                double pct = totalBookings > 0 ? (double)cat.Count / totalBookings * 100.0 : 0.0;
                var barColor = colors.TryGetValue(cat.Category, out var c) ? c : Color.FromArgb(100, 116, 139);

                var catCard = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 68,
                    Padding = new Padding(12, 6, 12, 8),
                    BackColor = Color.White
                };

                // Top Info line
                var pnlInfo = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 26
                };

                var lblCatName = new Label
                {
                    Text = cat.Category,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Dock = DockStyle.Left,
                    AutoSize = true
                };
                pnlInfo.Controls.Add(lblCatName);

                var lblCatMetrics = new Label
                {
                    Text = $"{cat.Count} bookings ({pct:F1}%)  •  ₱{cat.Revenue:N2}",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Dock = DockStyle.Right,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleRight
                };
                pnlInfo.Controls.Add(lblCatMetrics);
                catCard.Controls.Add(pnlInfo);

                // Progress Bar Container
                var pnlBarBg = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 12,
                    BackColor = Color.FromArgb(241, 245, 249)
                };

                var pnlBarFill = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = Math.Max(4, (int)(pnlBarBg.Width * (pct / 100.0))),
                    BackColor = barColor
                };
                pnlBarBg.SizeChanged += (s, e) =>
                {
                    pnlBarFill.Width = Math.Max(4, (int)(pnlBarBg.Width * (pct / 100.0)));
                };
                pnlBarBg.Controls.Add(pnlBarFill);
                catCard.Controls.Add(pnlBarBg);

                _pnlCategoryList.Controls.Add(catCard);
            }
        }

        // =============================================================
        // UI Helper Methods
        // =============================================================
        private (Panel Card, Label ValueLabel) CreateKpiCard(string header, string initialVal, string subtitle, Color valueColor)
        {
            var card = CreateCardContainer();
            card.Margin = new Padding(4);

            var lblHeader = new Label
            {
                Text = header,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 20
            };
            card.Controls.Add(lblHeader);

            var lblValue = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = valueColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblValue);
            lblValue.BringToFront();

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 20
            };
            card.Controls.Add(lblSub);

            return (card, lblValue);
        }

        private (Panel Box, Label ValueLabel) CreateInsightBox(string title, string initialVal, string description, Color valueColor)
        {
            var box = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12),
                Margin = new Padding(6)
            };
            box.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 22
            };
            box.Controls.Add(lblTitle);

            var lblVal = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = valueColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            box.Controls.Add(lblVal);
            lblVal.BringToFront();

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 20
            };
            box.Controls.Add(lblDesc);

            return (box, lblVal);
        }

        private Panel CreateCardContainer()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            return panel;
        }

        private Panel CreateSectionHeader(string title, string subtitle)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.White
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnl.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 22),
                AutoSize = true
            };
            pnl.Controls.Add(lblSub);

            return pnl;
        }
    }
}