using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ITenantDbProvider _tenantDb;

        public ActivityService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        public async Task<IReadOnlyList<Activity>> ListAsync(
            Guid? customerId, Guid? leadId, ActivityType? type,
            Guid? callerUserId, string? callerRole,
            int page, int pageSize)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, leadId, type, callerUserId, callerRole);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(
            Guid? customerId, Guid? leadId, ActivityType? type,
            Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, leadId, type, callerUserId, callerRole);
            return await query.CountAsync();
        }

        public async Task<Activity?> GetByIdAsync(Guid id, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var activity = await db.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null) return null;

            // SalesStaff can only see their own activities
            if (IsSalesStaff(callerRole) && activity.UserId != callerUserId)
                return null;

            return activity;
        }

        public async Task<Activity> CreateAsync(Activity activity)
        {
            var db = await _tenantDb.GetAsync();

            activity.Id = Guid.NewGuid();
            activity.CreatedAt = DateTime.UtcNow;
            activity.UpdatedAt = DateTime.UtcNow;

            db.Activities.Add(activity);
            await db.SaveChangesAsync();

            // reload to include navigation
            return await db.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.User)
                .AsNoTracking()
                .FirstAsync(a => a.Id == activity.Id);
        }

        public async Task<Activity?> UpdateAsync(
            Guid id, Activity updated, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return null;

            if (IsSalesStaff(callerRole) && activity.UserId != callerUserId)
                return null;

            activity.ActivityType = updated.ActivityType;
            activity.Description = updated.Description;
            if (updated.ActivityDate != default)
                activity.ActivityDate = updated.ActivityDate;
            activity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return await db.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.User)
                .AsNoTracking()
                .FirstAsync(a => a.Id == id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();
            var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return false;

            db.Activities.Remove(activity);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CustomerExistsAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Customers.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> LeadExistsAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Leads.AnyAsync(l => l.Id == id);
        }

        // ─── Helpers ──────────────────────────────────────────
        private static IQueryable<Activity> BuildQuery(
            Data.TenantCrmDbContext db,
            Guid? customerId, Guid? leadId, ActivityType? type,
            Guid? callerUserId, string? callerRole)
        {
            var q = db.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.User)
                .AsNoTracking()
                .AsQueryable();

            if (IsSalesStaff(callerRole) && callerUserId.HasValue)
                q = q.Where(a => a.UserId == callerUserId.Value);

            if (customerId.HasValue)
                q = q.Where(a => a.CustomerId == customerId.Value);

            if (leadId.HasValue)
                q = q.Where(a => a.LeadId == leadId.Value);

            if (type.HasValue)
                q = q.Where(a => a.ActivityType == type.Value);

            return q;
        }

        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}