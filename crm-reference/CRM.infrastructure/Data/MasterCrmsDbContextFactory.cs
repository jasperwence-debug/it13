using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM.infrastructure.Data
{
    public class MasterCrmsDbContextFactory : IDesignTimeDbContextFactory<MasterCrmsDbContext>
    {
        public MasterCrmsDbContext CreateDbContext(string[] args)
        {
            const string connectionString =
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=MasterCrms;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<MasterCrmsDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new MasterCrmsDbContext(optionsBuilder.Options);
        }
    }
}