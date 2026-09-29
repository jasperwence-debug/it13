using CRM.infrastructure.Data;

namespace CRM.infrastructure.Services
{
    public interface ITenantDbProvider
    {
        Task<TenantCrmDbContext> GetAsync(CancellationToken ct = default);
    }
}