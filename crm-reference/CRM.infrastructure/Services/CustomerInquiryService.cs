using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class CustomerInquiryService : ICustomerInquiryService
    {
        private readonly ITenantDbProvider _tenantDb;

        public CustomerInquiryService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // ─────────────────────────────────────────────────────
        // LIST
        // ─────────────────────────────────────────────────────
        public async Task<IReadOnlyList<CustomerInquiry>> ListAsync(
            Guid? customerId,
            CustomerInquiryType? type,
            CustomerInquiryStatus? status,
            Guid? callerUserId,
            string? callerRole,
            int page,
            int pageSize)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, type, status, callerUserId, callerRole);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await query
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────
        // COUNT
        // ─────────────────────────────────────────────────────
        public async Task<int> CountAsync(
            Guid? customerId,
            CustomerInquiryType? type,
            CustomerInquiryStatus? status,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, customerId, type, status, callerUserId, callerRole);
            return await query.CountAsync();
        }

        // ─────────────────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────────────────
        public async Task<CustomerInquiry?> GetByIdAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var inquiry = await db.CustomerInquiries
                .Include(x => x.Customer)
                .Include(x => x.AssignedUser)
                .Include(x => x.ResolvedByUser)
                .Include(x => x.CreatedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (inquiry == null) return null;

            // SalesStaff scoping
            if (IsSalesStaff(callerRole) &&
                inquiry.AssignedUserId != callerUserId &&
                inquiry.CreatedByUserId != callerUserId)
            {
                return null;
            }

            return inquiry;
        }

        // ─────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────
        public async Task<CustomerInquiry> CreateAsync(CustomerInquiry inquiry)
        {
            var db = await _tenantDb.GetAsync();

            inquiry.Id = Guid.NewGuid();
            inquiry.CreatedAt = DateTime.UtcNow;
            inquiry.UpdatedAt = DateTime.UtcNow;

            if (inquiry.Status == default)
                inquiry.Status = CustomerInquiryStatus.Open;

            if (inquiry.Priority == default)
            {
                // Complaints default to High priority
                inquiry.Priority = inquiry.Type == CustomerInquiryType.Complaint
                    ? CustomerInquiryPriority.High
                    : CustomerInquiryPriority.Medium;
            }

            db.CustomerInquiries.Add(inquiry);
            await db.SaveChangesAsync();

            // Reload with navigation props
            return await db.CustomerInquiries
                .Include(x => x.Customer)
                .Include(x => x.AssignedUser)
                .Include(x => x.ResolvedByUser)
                .Include(x => x.CreatedByUser)
                .AsNoTracking()
                .FirstAsync(x => x.Id == inquiry.Id);
        }

        // ─────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────
        public async Task<CustomerInquiry?> UpdateAsync(
            Guid id,
            CustomerInquiry updated,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var inquiry = await db.CustomerInquiries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (inquiry == null) return null;

            if (IsSalesStaff(callerRole) &&
                inquiry.AssignedUserId != callerUserId &&
                inquiry.CreatedByUserId != callerUserId)
            {
                return null;
            }

            // Business rule: resolved or closed inquiries cannot be edited
            if (inquiry.Status == CustomerInquiryStatus.Resolved ||
                inquiry.Status == CustomerInquiryStatus.Closed)
            {
                throw new InvalidOperationException(
                    "Resolved or closed inquiries cannot be edited. " +
                    "Reopen them first by changing the status.");
            }

            inquiry.Subject = updated.Subject;
            inquiry.Description = updated.Description;
            inquiry.Priority = updated.Priority;
            inquiry.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return await db.CustomerInquiries
                .Include(x => x.Customer)
                .Include(x => x.AssignedUser)
                .Include(x => x.ResolvedByUser)
                .Include(x => x.CreatedByUser)
                .AsNoTracking()
                .FirstAsync(x => x.Id == id);
        }

        // ─────────────────────────────────────────────────────
        // UPDATE STATUS
        // ─────────────────────────────────────────────────────
        public async Task<CustomerInquiry?> UpdateStatusAsync(
            Guid id,
            CustomerInquiryStatus status,
            string? resolution,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var inquiry = await db.CustomerInquiries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (inquiry == null) return null;

            if (IsSalesStaff(callerRole) &&
                inquiry.AssignedUserId != callerUserId &&
                inquiry.CreatedByUserId != callerUserId)
            {
                return null;
            }

            inquiry.Status = status;
            inquiry.UpdatedAt = DateTime.UtcNow;

            if (status == CustomerInquiryStatus.Resolved)
            {
                // Require resolution text when resolving
                if (string.IsNullOrWhiteSpace(resolution))
                {
                    throw new InvalidOperationException(
                        "Resolution text is required when marking an inquiry as Resolved.");
                }

                inquiry.Resolution = resolution.Trim();
                inquiry.ResolvedAt = DateTime.UtcNow;
                inquiry.ResolvedByUserId = callerUserId;
            }
            else
            {
                // Clear resolution metadata if status is not Resolved
                inquiry.Resolution = null;
                inquiry.ResolvedAt = null;
                inquiry.ResolvedByUserId = null;
            }

            await db.SaveChangesAsync();

            return await db.CustomerInquiries
                .Include(x => x.Customer)
                .Include(x => x.AssignedUser)
                .Include(x => x.ResolvedByUser)
                .Include(x => x.CreatedByUser)
                .AsNoTracking()
                .FirstAsync(x => x.Id == id);
        }

        // ─────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────
        public async Task<bool> CustomerExistsAsync(Guid customerId)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Customers.AnyAsync(c => c.Id == customerId);
        }

        public async Task<User?> FindUserAsync(Guid userId)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ─────────────────────────────────────────────────────
        // QUERY BUILDER (with role scoping)
        // ─────────────────────────────────────────────────────
        private static IQueryable<CustomerInquiry> BuildQuery(
            Data.TenantCrmDbContext db,
            Guid? customerId,
            CustomerInquiryType? type,
            CustomerInquiryStatus? status,
            Guid? callerUserId,
            string? callerRole)
        {
            var q = db.CustomerInquiries
                .Include(x => x.Customer)
                .Include(x => x.AssignedUser)
                .Include(x => x.ResolvedByUser)
                .Include(x => x.CreatedByUser)
                .AsNoTracking()
                .AsQueryable();

            // 🔒 SalesStaff sees only inquiries assigned to or created by them
            if (IsSalesStaff(callerRole) && callerUserId.HasValue)
            {
                q = q.Where(x =>
                    x.AssignedUserId == callerUserId.Value ||
                    x.CreatedByUserId == callerUserId.Value);
            }

            if (customerId.HasValue)
                q = q.Where(x => x.CustomerId == customerId.Value);

            if (type.HasValue)
                q = q.Where(x => x.Type == type.Value);

            if (status.HasValue)
                q = q.Where(x => x.Status == status.Value);

            return q;
        }

        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}