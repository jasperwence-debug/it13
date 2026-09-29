namespace CRM.infrastructure.Services;

public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseInfo> GetDatabaseInfoAsync(Guid tenantId);
}