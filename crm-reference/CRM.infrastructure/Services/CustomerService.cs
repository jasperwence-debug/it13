using CRM.domain.Constants;
using CRM.domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ITenantDbProvider _tenantDb;

        public CustomerService(ITenantDbProvider tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // ─── LIST ─────────────────────────────────────────────
        public async Task<IReadOnlyList<Customer>> ListAsync(
            string? search,
            string? status,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var query = db.Customers
                .Include(c => c.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            // 🔒 Role scoping
            if (IsSalesStaff(callerRole) && callerUserId.HasValue)
            {
                query = query.Where(c => c.AssignedUserId == callerUserId.Value);
            }

            // 🔍 Search across name/email/company
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(s) ||
                    c.LastName.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s) ||
                    c.Company.ToLower().Contains(s));
            }

            // 📋 Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                query = query.Where(c => c.Status == st);
            }

            var customers = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            // ─── Attach last-activity metadata (for churn badges) ───
            if (customers.Count > 0)
            {
                var ids = customers.Select(c => c.Id).ToList();

                var lastActivities = await db.Activities
                    .Where(a => a.CustomerId.HasValue && ids.Contains(a.CustomerId.Value))
                    .GroupBy(a => a.CustomerId!.Value)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        LastDate = g.Max(a => a.ActivityDate)
                    })
                    .ToListAsync();

                var lastActivityMap = lastActivities
                    .ToDictionary(x => x.CustomerId, x => x.LastDate);

                var now = DateTime.UtcNow;
                foreach (var c in customers)
                {
                    if (lastActivityMap.TryGetValue(c.Id, out var lastDate))
                    {
                        c.LastActivityAt = lastDate;
                        c.DaysSinceLastActivity = (int)(now - lastDate).TotalDays;
                    }
                    else
                    {
                        c.LastActivityAt = null;
                        c.DaysSinceLastActivity = (int)(now - c.CreatedAt).TotalDays;
                    }

                    c.ChurnRisk = ComputeChurnRisk(c.DaysSinceLastActivity);
                }
            }

            return customers;
        }

        // ─── GET BY ID ────────────────────────────────────────
        public async Task<Customer?> GetByIdAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var customer = await db.Customers
                .Include(c => c.AssignedUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            // 🔒 SalesStaff can only see customers assigned to them
            if (IsSalesStaff(callerRole) &&
                customer.AssignedUserId != callerUserId)
            {
                return null;
            }

            // Attach churn metadata
            var lastActivity = await db.Activities
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .Select(a => (DateTime?)a.ActivityDate)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            if (lastActivity.HasValue)
            {
                customer.LastActivityAt = lastActivity;
                customer.DaysSinceLastActivity = (int)(now - lastActivity.Value).TotalDays;
            }
            else
            {
                customer.LastActivityAt = null;
                customer.DaysSinceLastActivity = (int)(now - customer.CreatedAt).TotalDays;
            }

            customer.ChurnRisk = ComputeChurnRisk(customer.DaysSinceLastActivity);

            return customer;
        }

        // ─── CREATE ───────────────────────────────────────────
        public async Task<Customer> CreateAsync(Customer customer)
        {
            var db = await _tenantDb.GetAsync();

            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return customer;
        }

        // ─── UPDATE ───────────────────────────────────────────
        public async Task<Customer?> UpdateAsync(
            Guid id,
            Customer updated,
            Guid? callerUserId,
            string? callerRole)
        {
            var db = await _tenantDb.GetAsync();

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            if (IsSalesStaff(callerRole) &&
                customer.AssignedUserId != callerUserId)
            {
                return null;
            }

            customer.FirstName = updated.FirstName;
            customer.LastName = updated.LastName;
            customer.Email = updated.Email;
            customer.Phone = updated.Phone;
            customer.Company = updated.Company;
            customer.Address = updated.Address;
            customer.Status = updated.Status;
            customer.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return customer;
        }

        // ─── DELETE ───────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid id)
        {
            var db = await _tenantDb.GetAsync();

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return false;

            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
            return true;
        }

        // ─── ASSIGN ───────────────────────────────────────────
        public async Task<Customer?> AssignAsync(
            Guid id,
            Guid assignedUserId)
        {
            var db = await _tenantDb.GetAsync();

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            customer.AssignedUserId = assignedUserId;
            customer.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return customer;
        }

        // ─── FIND USER (for validation) ───────────────────────
        public async Task<User?> FindUserAsync(Guid userId)
        {
            var db = await _tenantDb.GetAsync();
            return await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // ─── Helpers ──────────────────────────────────────────
        private static bool IsSalesStaff(string? role)
            => string.Equals(role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase);

        private static string ComputeChurnRisk(int daysSinceLastActivity)
        {
            if (daysSinceLastActivity >= 60) return "High";
            if (daysSinceLastActivity >= 30) return "Medium";
            return "Low";
        }
    }
}