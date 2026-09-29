using CRM.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantDatabaseResolver _resolver;

    public TenantDbContextFactory(ITenantDatabaseResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<TenantCrmDbContext> CreateAsync(Guid tenantId)
    {
        var info = await _resolver.GetDatabaseInfoAsync(tenantId);

        if (string.IsNullOrWhiteSpace(info.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Connection string missing for tenant {tenantId}.");
        }

        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(info.ConnectionString)
            .Options;

        return new TenantCrmDbContext(options);
    }
}