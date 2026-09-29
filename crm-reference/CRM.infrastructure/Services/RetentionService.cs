using CRM.domain.Constants;
using CRM.domain.Dtos.Retention;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class RetentionService : IRetentionService
    {
        private const int RECOVERY_WINDOW_DAYS = 30;

        private readonly ITenantDbProvider _tenantDb;

        public RetentionService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // ─────────────────────────────────────────────────────
        // SUMMARY
        // ─────────────────────────────────────────────────────
        public async Task<RetentionSummaryDto> GetSummaryAsync(
            int thresholdDays,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-thresholdDays);

            // Customer IDs that had activity within the threshold window
            var recentlyActiveIds = await db.Activities
                .Where(a => a.CustomerId.HasValue && a.ActivityDate >= cutoff)
                .Select(a => a.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            var activeSet = recentlyActiveIds.ToHashSet();

            int totalCustomers = await db.Customers.CountAsync();
            int activeCustomers = await db.Customers.CountAsync(c => c.Status == "Active");

            // At-risk = customers with no recent activity
            int atRiskCount = await db.Customers
                .CountAsync(c => !activeSet.Contains(c.Id));

            // Contacted = at-risk customers who have an open win-back follow-up
            var openWinBackCustomerIds = await db.FollowUps
                .Where(f =>
                    f.CustomerId.HasValue &&
                    (f.Status == FollowUpStatus.Pending || f.Status == FollowUpStatus.InProgress) &&
                    f.Title.StartsWith("Win-back"))
                .Select(f => f.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            var winBackSet = openWinBackCustomerIds.ToHashSet();
            int contactedCount = atRiskCount == 0 ? 0 : winBackSet.Count(id => !activeSet.Contains(id));

            // Recovered = at-risk at some point, had a win-back follow-up, and activity within 30 days of that follow-up
            var recoveredCount = await CountRecoveredAsync(db, now);

            // Retention rate (customers older than threshold who had activity)
            var olderCutoff = now.AddDays(-thresholdDays);
            int olderCount = await db.Customers.CountAsync(c => c.CreatedAt < olderCutoff);
            int retainedOlder = await db.Customers
                .Where(c => c.CreatedAt < olderCutoff)
                .CountAsync(c => activeSet.Contains(c.Id));
            double retentionRate = olderCount == 0 ? 0.0 : (double)retainedOlder / olderCount;

            return new RetentionSummaryDto(
                atRiskCount, contactedCount, recoveredCount,
                totalCustomers, activeCustomers, retentionRate,
                now, thresholdDays);
        }

        // ─────────────────────────────────────────────────────
        // AT-RISK LIST
        // ─────────────────────────────────────────────────────
        public async Task<IReadOnlyList<AtRiskCustomerDto>> GetAtRiskAsync(
            int thresholdDays,
            bool onlyUnassigned,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-thresholdDays);

            // Recently active customer IDs
            var recentlyActiveIds = await db.Activities
                .Where(a => a.CustomerId.HasValue && a.ActivityDate >= cutoff)
                .Select(a => a.CustomerId!.Value)
                .Distinct()
                .ToListAsync();
            var activeSet = recentlyActiveIds.ToHashSet();

            // Query customers
            var q = db.Customers
                .Include(c => c.AssignedUser)
                .AsNoTracking()
                .Where(c => !activeSet.Contains(c.Id));

            // SalesStaff scoping: only their own
            if (string.Equals(callerRole, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase) &&
                callerUserId.HasValue)
            {
                q = q.Where(c => c.AssignedUserId == callerUserId.Value);
            }

            // Only unassigned (for bulk assignment UI)
            if (onlyUnassigned)
            {
                q = q.Where(c => c.AssignedUserId == null);
            }

            var customers = await q.ToListAsync();

            // Get latest activity for each customer
            var customerIds = customers.Select(c => c.Id).ToList();

            var lastActivities = await db.Activities
                .Where(a => a.CustomerId.HasValue && customerIds.Contains(a.CustomerId.Value))
                .GroupBy(a => a.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, LastDate = g.Max(a => a.ActivityDate) })
                .ToListAsync();

            var lastActivityMap = lastActivities.ToDictionary(x => x.CustomerId, x => x.LastDate);

            // Open win-back follow-ups for these customers
            var openWinBacks = await db.FollowUps
                .Where(f =>
                    f.CustomerId.HasValue &&
                    customerIds.Contains(f.CustomerId.Value) &&
                    (f.Status == FollowUpStatus.Pending || f.Status == FollowUpStatus.InProgress) &&
                    f.Title.StartsWith("Win-back"))
                .Select(f => new { f.Id, CustomerId = f.CustomerId!.Value })
                .ToListAsync();

            var winBackMap = openWinBacks
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var result = new List<AtRiskCustomerDto>();

            foreach (var c in customers)
            {
                DateTime lastDate;
                if (lastActivityMap.TryGetValue(c.Id, out var la))
                    lastDate = la;
                else
                    lastDate = c.CreatedAt;

                int daysSince = (int)(now - lastDate).TotalDays;

                Guid? winBackId = winBackMap.TryGetValue(c.Id, out var wb) ? wb : (Guid?)null;

                result.Add(new AtRiskCustomerDto(
                    c.Id,
                    $"{c.FirstName} {c.LastName}".Trim(),
                    c.Email,
                    c.Company,
                    daysSince,
                    lastDate,
                    c.AssignedUserId,
                    c.AssignedUser?.Name,
                    winBackId.HasValue,
                    winBackId));
            }

            return result
                .OrderByDescending(x => x.DaysSinceLastActivity)
                .ToList();
        }

        // ─────────────────────────────────────────────────────
        // RECOVERED LIST
        // ─────────────────────────────────────────────────────
        public async Task<IReadOnlyList<RecoveredCustomerDto>> GetRecoveredAsync(
            int lookbackDays,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var now = DateTime.UtcNow;
            var lookback = now.AddDays(-lookbackDays);

            // Find win-back follow-ups that are completed OR still pending, created in the lookback window
            var winBacks = await db.FollowUps
                .Where(f =>
                    f.CustomerId.HasValue &&
                    f.Title.StartsWith("Win-back") &&
                    f.CreatedAt >= lookback)
                .Select(f => new
                {
                    FollowUpId = f.Id,
                    CustomerId = f.CustomerId!.Value,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            if (winBacks.Count == 0) return new List<RecoveredCustomerDto>();

            var customerIds = winBacks.Select(w => w.CustomerId).Distinct().ToList();

            // For each, find the first activity after the win-back was created
            var allActivities = await db.Activities
                .Where(a => a.CustomerId.HasValue && customerIds.Contains(a.CustomerId.Value))
                .Select(a => new { CustomerId = a.CustomerId!.Value, a.ActivityDate })
                .ToListAsync();

            // Load customer names
            var customers = await db.Customers
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Company
                })
                .ToListAsync();

            var result = new List<RecoveredCustomerDto>();

            foreach (var wb in winBacks)
            {
                // First activity strictly AFTER the win-back was created
                var firstAfter = allActivities
                    .Where(a => a.CustomerId == wb.CustomerId && a.ActivityDate > wb.CreatedAt)
                    .OrderBy(a => a.ActivityDate)
                    .FirstOrDefault();

                if (firstAfter == null) continue;

                int daysToRecover = (int)(firstAfter.ActivityDate - wb.CreatedAt).TotalDays;

                // Only count as recovered if within window
                if (daysToRecover > RECOVERY_WINDOW_DAYS) continue;

                var cust = customers.FirstOrDefault(c => c.Id == wb.CustomerId);
                if (cust == null) continue;

                result.Add(new RecoveredCustomerDto(
                    wb.CustomerId,
                    $"{cust.FirstName} {cust.LastName}".Trim(),
                    cust.Email,
                    cust.Company,
                    wb.CreatedAt,
                    firstAfter.ActivityDate,
                    daysToRecover));
            }

            return result
                .OrderByDescending(x => x.FirstActivityAfterWinBack)
                .ToList();
        }

        // ─────────────────────────────────────────────────────
        // BULK ASSIGN
        // ─────────────────────────────────────────────────────
        public async Task<int> BulkAssignAsync(
            IReadOnlyList<Guid> customerIds,
            Guid assignedUserId)
        {
            var db = await _tenantDb.GetAsync();

            // Validate user exists
            bool userExists = await db.Users.AnyAsync(u => u.Id == assignedUserId && u.IsActive);
            if (!userExists)
                throw new InvalidOperationException("Assigned user not found or inactive.");

            var customers = await db.Customers
                .Where(c => customerIds.Contains(c.Id))
                .ToListAsync();

            int updated = 0;
            foreach (var c in customers)
            {
                c.AssignedUserId = assignedUserId;
                c.UpdatedAt = DateTime.UtcNow;
                updated++;
            }

            await db.SaveChangesAsync();
            return updated;
        }

        // ─────────────────────────────────────────────────────
        // CREATE WIN-BACKS
        // ─────────────────────────────────────────────────────
        public async Task<WinBackResultDto> CreateWinBacksAsync(
            IReadOnlyList<Guid> customerIds,
            Guid? assignedUserId,
            DateTime? dueDate,
            string? notes,
            Guid? callerUserId)
        {
            var db = await _tenantDb.GetAsync();

            var assignee = assignedUserId ?? callerUserId
                ?? throw new InvalidOperationException("No assignee and no caller.");

            // Validate user
            bool userExists = await db.Users.AnyAsync(u => u.Id == assignee && u.IsActive);
            if (!userExists)
                throw new InvalidOperationException("Assigned user not found or inactive.");

            // Find which of these customers already have an open win-back
            var existingWinBackCustomerIds = await db.FollowUps
                .Where(f =>
                    f.CustomerId.HasValue &&
                    customerIds.Contains(f.CustomerId.Value) &&
                    (f.Status == FollowUpStatus.Pending || f.Status == FollowUpStatus.InProgress) &&
                    f.Title.StartsWith("Win-back"))
                .Select(f => f.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            var existingSet = existingWinBackCustomerIds.ToHashSet();

            // Load customers we're creating follow-ups for
            var customersToProcess = await db.Customers
                .Where(c => customerIds.Contains(c.Id) && !existingSet.Contains(c.Id))
                .ToListAsync();

            var now = DateTime.UtcNow;
            var due = dueDate ?? now.AddDays(7);
            if (due <= now) due = now.AddDays(7);

            var createdIds = new List<Guid>();

            foreach (var c in customersToProcess)
            {
                var title = $"Win-back: {c.FirstName} {c.LastName}".Trim();
                if (title.Length > 200) title = title.Substring(0, 200);

                var description = string.IsNullOrWhiteSpace(notes)
                    ? $"Re-engage customer. Last activity: see activity log. " +
                      $"Auto-generated on {now:yyyy-MM-dd}."
                    : notes.Trim();

                var followUp = new FollowUp
                {
                    Id = Guid.NewGuid(),
                    CustomerId = c.Id,
                    UserId = assignee,
                    Title = title,
                    Description = description,
                    DueDate = due,
                    Status = FollowUpStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                db.FollowUps.Add(followUp);
                createdIds.Add(followUp.Id);
            }

            await db.SaveChangesAsync();

            return new WinBackResultDto(
                createdIds.Count,
                existingSet.Count,
                createdIds);
        }

        // ─────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────
        private async Task<int> CountRecoveredAsync(Data.TenantCrmDbContext db, DateTime now)
        {
            var oldestLookback = now.AddDays(-90); // limit how far back we analyze

            var winBacks = await db.FollowUps
                .Where(f =>
                    f.CustomerId.HasValue &&
                    f.Title.StartsWith("Win-back") &&
                    f.CreatedAt >= oldestLookback)
                .Select(f => new { f.CustomerId, f.CreatedAt })
                .ToListAsync();

            if (winBacks.Count == 0) return 0;

            var customerIds = winBacks
                .Select(w => w.CustomerId!.Value)
                .Distinct()
                .ToList();

            var activities = await db.Activities
                .Where(a => a.CustomerId.HasValue && customerIds.Contains(a.CustomerId.Value))
                .Select(a => new { a.CustomerId, a.ActivityDate })
                .ToListAsync();

            int count = 0;
            foreach (var wb in winBacks)
            {
                var firstAfter = activities
                    .Where(a => a.CustomerId == wb.CustomerId && a.ActivityDate > wb.CreatedAt)
                    .OrderBy(a => a.ActivityDate)
                    .FirstOrDefault();

                if (firstAfter == null) continue;

                int days = (int)(firstAfter.ActivityDate - wb.CreatedAt).TotalDays;
                if (days <= RECOVERY_WINDOW_DAYS) count++;
            }

            return count;
        }
    }
}