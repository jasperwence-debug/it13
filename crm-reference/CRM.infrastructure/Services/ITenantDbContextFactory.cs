using CRM.infrastructure.Data;

namespace CRM.infrastructure.Services;

public interface ITenantDbContextFactory
{
    Task<TenantCrmDbContext> CreateAsync(Guid tenantId);
}