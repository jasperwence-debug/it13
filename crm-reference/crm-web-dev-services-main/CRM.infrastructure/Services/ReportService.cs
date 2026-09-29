using CRM.domain.Constants;
using CRM.domain.Dtos.Reports;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly ITenantDbProvider _tenantDb;

        public ReportService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // ─────────────────────────────────────────────────────
        // OVERVIEW
        // ─────────────────────────────────────────────────────
        public async Task<OverviewReportDto> GetOverviewAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var customers = ScopedCustomers(db, callerUserId, callerRole);
            var leads = ScopedLeads(db, callerUserId, callerRole);
            var activities = ScopedActivities(db, callerUserId, callerRole);
            var followUps = ScopedFollowUps(db, callerUserId, callerRole);
            var inquiries = ScopedInquiries(db, callerUserId, callerRole);

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            int totalCustomers = await customers.CountAsync();
            int activeCustomers = await customers.CountAsync(c => c.Status == "Active");
            int newThisMonth = await customers.CountAsync(c => c.CreatedAt >= monthStart);

            int totalLeads = await leads.CountAsync();
            int activeLeads = await leads.CountAsync(l =>
                l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost);
            decimal pipelineValue = await leads
                .Where(l => l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost)
                .SumAsync(l => (decimal?)l.ExpectedValue) ?? 0m;

            int pending = await followUps.CountAsync(f => f.Status == FollowUpStatus.Pending);
            int overdue = await followUps.CountAsync(f =>
                f.DueDate < now &&
                f.Status != FollowUpStatus.Completed &&
                f.Status != FollowUpStatus.Cancelled);
            int completedThisMonth = await followUps.CountAsync(f =>
                f.Status == FollowUpStatus.Completed &&
                f.UpdatedAt >= monthStart);

            int openInquiries = await inquiries.CountAsync(i =>
            i.Status == CustomerInquiryStatus.Open ||
            i.Status == CustomerInquiryStatus.InProgress);

            // Overdue inquiries:
            // - Complaints: open more than 7 days
            // - Others:     open more than 14 days
            // Split into two queries because EF Core cannot translate the ternary operator.
            var complaintCutoff = now.AddDays(-7);
            var otherCutoff = now.AddDays(-14);

            int overdueComplaints = await inquiries.CountAsync(i =>
                (i.Status == CustomerInquiryStatus.Open ||
                 i.Status == CustomerInquiryStatus.InProgress) &&
                i.Type == CustomerInquiryType.Complaint &&
                i.CreatedAt < complaintCutoff);

            int overdueOther = await inquiries.CountAsync(i =>
                (i.Status == CustomerInquiryStatus.Open ||
                 i.Status == CustomerInquiryStatus.InProgress) &&
                i.Type != CustomerInquiryType.Complaint &&
                i.CreatedAt < otherCutoff);

            int overdueInquiries = overdueComplaints + overdueOther;

            int wonLeadsThisMonth = await leads.CountAsync(l =>
                l.Status == LeadStatus.Won && l.UpdatedAt >= monthStart);
            int leadsThisMonth = await leads.CountAsync(l => l.CreatedAt >= monthStart);
            double conversionRate = leadsThisMonth == 0
                ? 0.0
                : (double)wonLeadsThisMonth / leadsThisMonth;

            // Retention: customers with activity in last 60 days / total
            var activeWindow = now.AddDays(-60);
            var activityCustomerIds = await activities
                .Where(a => a.CustomerId.HasValue && a.ActivityDate >= activeWindow)
                .Select(a => a.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            var leadActivityCustomerIds = await activities
                .Where(a => a.LeadId.HasValue && a.ActivityDate >= activeWindow)
                .Select(a => a.LeadId!.Value)
                .Distinct()
                .ToListAsync();

            var customerIdsWithActivity = activityCustomerIds.ToHashSet();

            // Simpler retention: (customers created before 60 days ago AND have activity in last 60d) / (customers created before 60 days ago)
            var olderThan60 = now.AddDays(-60);
            int olderCustomers = await customers.CountAsync(c => c.CreatedAt < olderThan60);
            int retainedOlder = await customers
                .Where(c => c.CreatedAt < olderThan60)
                .CountAsync(c => customerIdsWithActivity.Contains(c.Id));

            double retentionRate = olderCustomers == 0
                ? 0.0
                : (double)retainedOlder / olderCustomers;

            int atRisk = await customers.CountAsync(c =>
                !customerIdsWithActivity.Contains(c.Id));

            // Scope label
            string scope = callerRole switch
            {
                var r when r == Roles.SuperAdmin => "Tenant",
                var r when r == Roles.Admin => "Tenant",
                var r when r == Roles.Manager => "Team",
                _ => "Personal"
            };

            return new OverviewReportDto(
                totalCustomers, activeCustomers, newThisMonth,
                totalLeads, activeLeads, pipelineValue,
                pending, overdue, completedThisMonth,
                openInquiries, overdueInquiries,
                wonLeadsThisMonth, conversionRate,
                retentionRate, atRisk,
                from, to, scope);
        }

        // ─────────────────────────────────────────────────────
        // PIPELINE
        // ─────────────────────────────────────────────────────
        public async Task<PipelineReportDto> GetPipelineAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var leads = ScopedLeads(db, callerUserId, callerRole)
                .Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

            var grouped = await leads
                .GroupBy(l => l.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(l => l.ExpectedValue)
                })
                .ToListAsync();

            // Ensure all enum values are present (even with 0)
            var stageNames = Enum.GetValues<LeadStatus>();
            var stages = new List<PipelineStageDto>();
            decimal grandTotal = 0m;
            int grandCount = 0;

            foreach (var s in stageNames)
            {
                var g = grouped.FirstOrDefault(x => x.Status == s);
                int count = g?.Count ?? 0;
                decimal value = g?.TotalValue ?? 0m;
                stages.Add(new PipelineStageDto(s.ToString(), count, value, 0.0));
                grandTotal += value;
                grandCount += count;
            }

            // Recompute percentages
            stages = stages
                .Select(s => s with
                {
                    PercentageOfTotal = grandTotal == 0
                        ? 0.0
                        : (double)(s.TotalValue / grandTotal)
                })
                .ToList();

            return new PipelineReportDto(stages, grandCount, grandTotal, DateTime.UtcNow);
        }

        // ─────────────────────────────────────────────────────
        // ACTIVITY
        // ─────────────────────────────────────────────────────
        public async Task<ActivityReportDto> GetActivityAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var activities = ScopedActivities(db, callerUserId, callerRole)
                .Where(a => a.ActivityDate >= from && a.ActivityDate <= to);

            int total = await activities.CountAsync();

            var byType = await activities
                .GroupBy(a => a.ActivityType)
                .Select(g => new ActivityTypeBreakdownDto(g.Key.ToString(), g.Count()))
                .ToListAsync();

            // Fill missing types
            var allTypes = Enum.GetValues<ActivityType>();
            foreach (var t in allTypes)
                if (byType.All(b => !string.Equals(b.ActivityType, t.ToString(), StringComparison.OrdinalIgnoreCase)))
                    byType.Add(new ActivityTypeBreakdownDto(t.ToString(), 0));

            // Daily counts
            var byDayRaw = await activities
                .GroupBy(a => a.ActivityDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // Fill missing days between from and to
            var byDay = new List<DailyActivityDto>();
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                var match = byDayRaw.FirstOrDefault(x => x.Date == d);
                byDay.Add(new DailyActivityDto(d, match?.Count ?? 0));
            }

            // Per-user breakdown
            var userGroups = await activities
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    Calls = g.Count(a => a.ActivityType == ActivityType.Call),
                    Emails = g.Count(a => a.ActivityType == ActivityType.Email),
                    Meetings = g.Count(a => a.ActivityType == ActivityType.Meeting),
                    Notes = g.Count(a => a.ActivityType == ActivityType.Note),
                    Tasks = g.Count(a => a.ActivityType == ActivityType.Task)
                })
                .ToListAsync();

            var userIds = userGroups.Select(u => u.UserId).ToList();
            var users = await db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            var byUser = userGroups
                .Select(g =>
                {
                    var name = users.FirstOrDefault(u => u.Id == g.UserId)?.Name ?? "(unknown)";
                    return new UserActivityDto(g.UserId, name, g.Total, g.Calls, g.Emails, g.Meetings, g.Notes, g.Tasks);
                })
                .OrderByDescending(u => u.TotalActivities)
                .ToList();

            return new ActivityReportDto(total, byType, byDay, byUser, from, to);
        }

        // ─────────────────────────────────────────────────────
        // RETENTION
        // ─────────────────────────────────────────────────────
        public async Task<RetentionReportDto> GetRetentionAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole,
            int atRiskDays = 60)
        {
            var db = await _tenantDb.GetAsync();
            var customers = ScopedCustomers(db, callerUserId, callerRole);
            var activities = ScopedActivities(db, callerUserId, callerRole);

            var now = DateTime.UtcNow;
            int total = await customers.CountAsync();
            int active = await customers.CountAsync(c => c.Status == "Active");

            // Retention logic: customers older than atRiskDays who have activity in last atRiskDays
            var cutoff = now.AddDays(-atRiskDays);
            var customersOlderThanCutoff = await customers
                .Where(c => c.CreatedAt < cutoff)
                .Select(c => c.Id)
                .ToListAsync();

            var recentlyActive = await activities
                .Where(a => a.CustomerId.HasValue && a.ActivityDate >= cutoff)
                .Select(a => a.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            var recentlyActiveSet = recentlyActive.ToHashSet();
            int retained = customersOlderThanCutoff.Count(id => recentlyActiveSet.Contains(id));
            double retentionRate = customersOlderThanCutoff.Count == 0
                ? 1.0
                : (double)retained / customersOlderThanCutoff.Count;
            double churnRate = 1.0 - retentionRate;

            // At-risk customers
            var allCustomerIds = await customers.Select(c => c.Id).ToListAsync();
            var atRiskIds = allCustomerIds.Where(id => !recentlyActiveSet.Contains(id)).ToList();

            var atRiskCustomers = await customers
                .Where(c => atRiskIds.Contains(c.Id))
                .Include(c => c.AssignedUser)
                .Take(50)
                .ToListAsync();

            // Compute days since last activity for each
            var atRiskList = new List<AtRiskCustomerDto>();
            foreach (var c in atRiskCustomers)
            {
                var lastActivity = await activities
                    .Where(a => a.CustomerId == c.Id)
                    .OrderByDescending(a => a.ActivityDate)
                    .Select(a => (DateTime?)a.ActivityDate)
                    .FirstOrDefaultAsync();

                var lastDate = lastActivity ?? c.CreatedAt;
                int daysSince = (int)(now - lastDate).TotalDays;

                atRiskList.Add(new AtRiskCustomerDto(
                    c.Id,
                    $"{c.FirstName} {c.LastName}".Trim(),
                    c.Email,
                    c.Company,
                    daysSince,
                    lastDate,
                    c.AssignedUser?.Name));
            }

            atRiskList = atRiskList.OrderByDescending(a => a.DaysSinceLastActivity).ToList();

            // Monthly trend: last 6 months of new vs returning
            var monthly = new List<MonthlyRetentionDto>();
            for (int i = 5; i >= 0; i--)
            {
                var m = now.AddMonths(-i);
                var mStart = new DateTime(m.Year, m.Month, 1);
                var mEnd = mStart.AddMonths(1);

                int newCount = await customers
                    .CountAsync(c => c.CreatedAt >= mStart && c.CreatedAt < mEnd);

                int activeInMonth = await activities
                    .Where(a => a.CustomerId.HasValue &&
                                a.ActivityDate >= mStart &&
                                a.ActivityDate < mEnd)
                    .Select(a => a.CustomerId!.Value)
                    .Distinct()
                    .CountAsync();

                int returning = Math.Max(0, activeInMonth - newCount);

                monthly.Add(new MonthlyRetentionDto(
                    mStart.ToString("yyyy-MM"),
                    newCount,
                    returning));
            }

            return new RetentionReportDto(
                retentionRate, churnRate,
                total, active, atRiskList.Count,
                atRiskList, monthly,
                now);
        }

        // ─────────────────────────────────────────────────────
        // CONVERSION
        // ─────────────────────────────────────────────────────
        public async Task<ConversionReportDto> GetConversionAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var leads = ScopedLeads(db, callerUserId, callerRole)
                .Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

            int totalLeads = await leads.CountAsync();
            int converted = await leads.CountAsync(l => l.Status == LeadStatus.Won);
            double overallRate = totalLeads == 0 ? 0.0 : (double)converted / totalLeads;

            // Average days to convert (from creation to Won update)
            var convertedLeads = await leads
                .Where(l => l.Status == LeadStatus.Won)
                .Select(l => new { l.CreatedAt, l.UpdatedAt })
                .ToListAsync();

            double avgDays = convertedLeads.Count == 0
                ? 0.0
                : convertedLeads.Average(l => (l.UpdatedAt - l.CreatedAt).TotalDays);

            // By source
            var bySource = await leads
                .GroupBy(l => string.IsNullOrWhiteSpace(l.Source) ? "Unknown" : l.Source)
                .Select(g => new
                {
                    Source = g.Key,
                    Total = g.Count(),
                    Converted = g.Count(l => l.Status == LeadStatus.Won)
                })
                .ToListAsync();

            var sourceList = bySource
                .Select(g => new ConversionBySourceDto(
                    g.Source,
                    g.Total,
                    g.Converted,
                    g.Total == 0 ? 0.0 : (double)g.Converted / g.Total))
                .OrderByDescending(s => s.TotalLeads)
                .ToList();

            return new ConversionReportDto(
                totalLeads, converted, overallRate, avgDays, sourceList, from, to);
        }

        // ─────────────────────────────────────────────────────
        // TEAM PERFORMANCE
        // ─────────────────────────────────────────────────────
        public async Task<TeamPerformanceDto> GetTeamPerformanceAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            // Identify team members: for Manager, all SalesStaff in tenant.
            // For Admin/SuperAdmin, everyone.
            var usersQuery = db.Users.AsNoTracking();

            if (string.Equals(callerRole, Roles.Manager, StringComparison.OrdinalIgnoreCase))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Role == Roles.SalesStaff ||
                    u.Role == Roles.Manager);
            }

            var users = await usersQuery
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.Name, u.Role })
                .ToListAsync();

            var members = new List<TeamMemberPerformanceDto>();

            foreach (var u in users)
            {
                int assignedCustomers = await db.Customers
                    .CountAsync(c => c.AssignedUserId == u.Id);

                int assignedLeads = await db.Leads
                    .CountAsync(l => l.AssignedUserId == u.Id &&
                                     l.CreatedAt >= from && l.CreatedAt <= to);

                int convertedLeads = await db.Leads
                    .CountAsync(l => l.AssignedUserId == u.Id &&
                                     l.Status == LeadStatus.Won &&
                                     l.UpdatedAt >= from && l.UpdatedAt <= to);

                int activitiesLogged = await db.Activities
                    .CountAsync(a => a.UserId == u.Id &&
                                     a.ActivityDate >= from && a.ActivityDate <= to);

                decimal pipelineValue = await db.Leads
                    .Where(l => l.AssignedUserId == u.Id &&
                                l.Status != LeadStatus.Won &&
                                l.Status != LeadStatus.Lost)
                    .SumAsync(l => (decimal?)l.ExpectedValue) ?? 0m;

                double conv = assignedLeads == 0 ? 0.0 : (double)convertedLeads / assignedLeads;

                members.Add(new TeamMemberPerformanceDto(
                    u.Id, u.Name, u.Role,
                    assignedCustomers, assignedLeads, convertedLeads,
                    activitiesLogged, pipelineValue, conv));
            }

            members = members
                .OrderByDescending(m => m.ConvertedLeads)
                .ThenByDescending(m => m.ActivitiesLogged)
                .ToList();

            return new TeamPerformanceDto(members, members.Count, from, to);
        }

        // ─────────────────────────────────────────────────────
        // SCOPING HELPERS (same pattern as other services)
        // ─────────────────────────────────────────────────────
        private static IQueryable<domain.Entities.Customer> ScopedCustomers(
            Data.TenantCrmDbContext db, Guid? userId, string? role)
        {
            var q = db.Customers.AsNoTracking();
            if (IsSalesStaff(role) && userId.HasValue)
                q = q.Where(c => c.AssignedUserId == userId.Value);
            return q;
        }

        private static IQueryable<domain.Entities.Lead> ScopedLeads(
            Data.TenantCrmDbContext db, Guid? userId, string? role)
        {
            var q = db.Leads.AsNoTracking();
            if (IsSalesStaff(role) && userId.HasValue)
                q = q.Where(l => l.AssignedUserId == userId.Value);
            return q;
        }

        private static IQueryable<domain.Entities.Activity> ScopedActivities(
            Data.TenantCrmDbContext db, Guid? userId, string? role)
        {
            var q = db.Activities.AsNoTracking();
            if (IsSalesStaff(role) && userId.HasValue)
                q = q.Where(a => a.UserId == userId.Value);
            return q;
        }

        private static IQueryable<domain.Entities.FollowUp> ScopedFollowUps(
            Data.TenantCrmDbContext db, Guid? userId, string? role)
        {
            var q = db.FollowUps.AsNoTracking();
            if (IsSalesStaff(role) && userId.HasValue)
                q = q.Where(f => f.UserId == userId.Value);
            return q;
        }

        private static IQueryable<domain.Entities.CustomerInquiry> ScopedInquiries(
            Data.TenantCrmDbContext db, Guid? userId, string? role)
        {
            var q = db.CustomerInquiries.AsNoTracking();
            if (IsSalesStaff(role) && userId.HasValue)
                q = q.Where(i =>
                    i.AssignedUserId == userId.Value ||
                    i.CreatedByUserId == userId.Value);
            return q;
        }

        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}