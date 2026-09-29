using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class LeadService : ILeadService
    {
        private readonly ITenantDbProvider _tenantDb;

        public LeadService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // ─── LIST (paginated) ─────────────────────────────────
        public async Task<IReadOnlyList<Lead>> ListAsync(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            Guid? callerUserId,
            string? callerRole,
            int page,
            int pageSize)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, search, status, priority, callerUserId, callerRole);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await query
                .OrderByDescending(l => l.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();
            var query = BuildQuery(db, search, status, priority, callerUserId, callerRole);
            return await query.CountAsync();
        }

        // ─── GET BY ID ────────────────────────────────────────
        public async Task<Lead?> GetByIdAsync(
            Guid id, Guid? callerUserId, string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lead == null) return null;

            if (IsSalesStaff(callerRole) && lead.AssignedUserId != callerUserId)
                return null;

            return lead;
        }

        // ─── CREATE ───────────────────────────────────────────
        public async Task<Lead> CreateAsync(Lead lead)
        {
            var db = await _tenantDb.GetAsync();

            lead.Id = Guid.NewGuid();
            lead.CreatedAt = DateTime.UtcNow;
            lead.UpdatedAt = DateTime.UtcNow;

            db.Leads.Add(lead);
            await db.SaveChangesAsync();
            return lead;
        }

        // ─── UPDATE ───────────────────────────────────────────
        public async Task<Lead?> UpdateAsync(
            Guid id,
            Lead updated,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null) return null;

            if (IsSalesStaff(callerRole) && lead.AssignedUserId != callerUserId)
                return null;

            lead.Name = updated.Name;
            lead.Email = updated.Email;
            lead.Phone = updated.Phone;
            lead.Source = updated.Source;
            lead.Status = updated.Status;
            lead.Priority = updated.Priority;
            lead.ExpectedValue = updated.ExpectedValue;
            lead.Notes = updated.Notes;
            lead.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return lead;
        }

        // ─── DELETE ───────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null) return false;

            db.Leads.Remove(lead);
            await db.SaveChangesAsync();
            return true;
        }

        // ─── UPDATE STATUS ────────────────────────────────────
        public async Task<Lead?> UpdateStatusAsync(
            Guid id,
            LeadStatus status,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null) return null;

            if (IsSalesStaff(callerRole) && lead.AssignedUserId != callerUserId)
                return null;

            lead.Status = status;
            lead.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return lead;
        }

        // ─── ASSIGN ───────────────────────────────────────────
        public async Task<Lead?> AssignAsync(Guid id, Guid assignedUserId)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null) return null;

            lead.AssignedUserId = assignedUserId;
            lead.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return lead;
        }

        // ─── CONVERT TO CUSTOMER ──────────────────────────────
        public async Task<(Lead lead, Customer customer)?> ConvertAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null) return null;

            if (IsSalesStaff(callerRole) && lead.AssignedUserId != callerUserId)
                return null;

            // Business rule: only "Won" leads can be converted
            if (lead.Status != LeadStatus.Won)
                throw new InvalidOperationException(
                    "Only leads with status 'Won' can be converted to customers.");

            // Business rule: cannot convert twice
            if (lead.CustomerId.HasValue)
                throw new InvalidOperationException(
                    "This lead has already been converted to a customer.");

            // Split the lead's Name into first/last for Customer creation.
            // If it's a single word, use it as FirstName and "Lead" as LastName.
            var parts = lead.Name.Trim().Split(' ', 2,
                StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 0 ? parts[0] : "Unknown";
            var lastName = parts.Length > 1 ? parts[1] : "(Lead)";

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = string.IsNullOrWhiteSpace(lead.Email)
                    ? $"lead-{lead.Id:N}@placeholder.local"
                    : lead.Email,
                Phone = lead.Phone,
                Company = string.Empty,
                Address = string.Empty,
                Status = "Active",
                AssignedUserId = lead.AssignedUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Customers.Add(customer);

            // Link lead → customer
            lead.CustomerId = customer.Id;
            lead.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return (lead, customer);
        }

        // ─── FIND USER ────────────────────────────────────────
        public async Task<User?> FindUserAsync(Guid userId)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ─── Helpers ──────────────────────────────────────────
        private static IQueryable<Lead> BuildQuery(
            Data.TenantCrmDbContext db,
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            Guid? callerUserId,
            string? callerRole)
        {
            var query = db.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            if (IsSalesStaff(callerRole) && callerUserId.HasValue)
                query = query.Where(l => l.AssignedUserId == callerUserId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(l =>
                    l.Name.ToLower().Contains(s) ||
                    l.Email.ToLower().Contains(s) ||
                    l.Source.ToLower().Contains(s));
            }

            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(l => l.Priority == priority.Value);

            return query;
        }

        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);
    }
}