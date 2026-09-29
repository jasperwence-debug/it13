using CRM.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Services;

public class TenantDatabaseResolver : ITenantDatabaseResolver
{
    private readonly MasterCrmsDbContext _masterDb;

    public TenantDatabaseResolver(MasterCrmsDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(Guid tenantId)
    {
        var tenantDatabase = await _masterDb.TenantDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive);

        if (tenantDatabase == null)
        {
            throw new InvalidOperationException(
                $"No active tenant database found for TenantId {tenantId}.");
        }

        return new TenantDatabaseInfo
        {
            TenantId = tenantDatabase.TenantId,
            DatabaseName = tenantDatabase.DatabaseName,
            ConnectionString = tenantDatabase.ConnectionString
        };
    }
}