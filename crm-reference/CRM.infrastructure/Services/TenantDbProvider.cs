using CRM.infrastructure.Data;

namespace CRM.infrastructure.Services
{
    public class TenantDbProvider : ITenantDbProvider
    {
        private readonly ITenantContext _context;
        private readonly ITenantDbContextFactory _factory;

        public TenantDbProvider(
            ITenantContext context,
            ITenantDbContextFactory factory)
        {
            _context = context;
            _factory = factory;
        }

        public async Task<TenantCrmDbContext> GetAsync(CancellationToken ct = default)
        {
            if (_context.TenantId == null)
                throw new UnauthorizedAccessException(
                    "No tenant associated with the current user.");

            return await _factory.CreateAsync(_context.TenantId.Value);
        }
    }
}