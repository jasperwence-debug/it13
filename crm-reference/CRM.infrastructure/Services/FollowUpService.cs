using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ITenantDbProvider _tenantDb;

        public FollowUpService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        public async Task<IReadOnlyList<FollowUp>> ListAsync(
            Guid? customerId, Guid? leadId, FollowUpStatus? status,
            bool? overdueOnly, Guid? callerUserId, string? callerRole,
            int page, int pageSize)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, leadId, status, overdueOnly, callerUserId, callerRole);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await query
                .OrderBy(f => f.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(
            Guid? customerId, Guid? leadId, FollowUpStatus? status,
            bool? overdueOnly, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, leadId, status, overdueOnly, callerUserId, callerRole);
            return await query.CountAsync();
        }

        public async Task<FollowUp?> GetByIdAsync(Guid id, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var f = await db.FollowUps
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return null;

            if (IsSalesStaff(callerRole) && f.UserId != callerUserId)
                return null;

            return f;
        }

        public async Task<FollowUp> CreateAsync(FollowUp followUp)
        {
            var db = await _tenantDb.GetAsync();

            followUp.Id = Guid.NewGuid();
            followUp.CreatedAt = DateTime.UtcNow;
            followUp.UpdatedAt = DateTime.UtcNow;
            followUp.Status = FollowUpStatus.Pending;

            db.FollowUps.Add(followUp);
            await db.SaveChangesAsync();

            return await db.FollowUps
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.User)
                .AsNoTracking()
                .FirstAsync(x => x.Id == followUp.Id);
        }

        public async Task<FollowUp?> UpdateAsync(
            Guid id, FollowUp updated, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var f = await db.FollowUps.FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return null;

            if (IsSalesStaff(callerRole) && f.UserId != callerUserId)
                return null;

            // Business rule: completed/cancelled follow-ups cannot be edited
            if (f.Status == FollowUpStatus.Completed || f.Status == FollowUpStatus.Cancelled)
                throw new InvalidOperationException(
                    "Completed or cancelled follow-ups cannot be edited. " +
                    "Reopen them first by setting status to Pending.");

            f.Title = updated.Title;
            f.Description = updated.Description;
            f.DueDate = updated.DueDate;
            f.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return await db.FollowUps
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.User)
                .AsNoTracking()
                .FirstAsync(x => x.Id == id);
        }

        public async Task<FollowUp?> UpdateStatusAsync(
            Guid id, FollowUpStatus status, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var f = await db.FollowUps.FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return null;

            if (IsSalesStaff(callerRole) && f.UserId != callerUserId)
                return null;

            f.Status = status;
            f.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return await db.FollowUps
                .Include(x => x.Customer)
                .Include(x => x.Lead)
                .Include(x => x.User)
                .AsNoTracking()
                .FirstAsync(x => x.Id == id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();
            var f = await db.FollowUps.FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return false;

            db.FollowUps.Remove(f);
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

        public async Task<User?> FindUserAsync(Guid userId)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        }

        private static IQueryable<FollowUp> BuildQuery(
            Data.TenantCrmDbContext db,
            Guid? customerId, Guid? leadId, FollowUpStatus? status,
            bool? overdueOnly, Guid? callerUserId, string? callerRole)
        {
            var q = db.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.User)
                .AsNoTracking()
                .AsQueryable();

            if (IsSalesStaff(callerRole) && callerUserId.HasValue)
                q = q.Where(f => f.UserId == callerUserId.Value);

            if (customerId.HasValue) q = q.Where(f => f.CustomerId == customerId.Value);
            if (leadId.HasValue) q = q.Where(f => f.LeadId == leadId.Value);
            if (status.HasValue) q = q.Where(f => f.Status == status.Value);

            if (overdueOnly == true)
            {
                var now = DateTime.UtcNow;
                q = q.Where(f => f.DueDate < now
                              && f.Status != FollowUpStatus.Completed
                              && f.Status != FollowUpStatus.Cancelled);
            }

            return q;
        }

        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}