using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.infrastructure.Data
{
    /// <summary>
    /// Design-time factory used by EF Core CLI tools (Add-Migration, Update-Database, etc.)
    /// so they can instantiate the TenantCrmDbContext without the running ASP.NET app.
    /// </summary>
    public class TenantCrmDbContextFactory : IDesignTimeDbContextFactory<TenantCrmDbContext>
    {
        public TenantCrmDbContext CreateDbContext(string[] args)
        {
            // Design-time connection string — points to your LocalDB
            // (Update this if you use a different instance)
            const string connectionString =
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=TenantCrm_Default;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<TenantCrmDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new TenantCrmDbContext(optionsBuilder.Options);
        }
    }
}