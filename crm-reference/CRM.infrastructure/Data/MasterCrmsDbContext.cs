using CRM.domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Data
{
    public class MasterCrmsDbContext : DbContext
    {
        public MasterCrmsDbContext(DbContextOptions<MasterCrmsDbContext> options)
            : base(options)
        {
        }

        // Platform-level tables
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantDatabase> TenantDatabases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- Tenant Configuration -----
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenants");
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(t => t.ContactEmail)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(t => t.SubscriptionTier)
                      .HasMaxLength(50);

                entity.Property(t => t.IsActive)
                      .HasDefaultValue(true);

                // One Tenant has many TenantDatabases
                entity.HasMany(t => t.TenantDatabases)
                      .WithOne(td => td.Tenant)
                      .HasForeignKey(td => td.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- TenantDatabase Configuration -----
            modelBuilder.Entity<TenantDatabase>(entity =>
            {
                entity.ToTable("TenantDatabases");
                entity.HasKey(td => td.Id);
                entity.Property(td => td.DatabaseName).IsRequired().HasMaxLength(200);
                entity.Property(td => td.ConnectionString).IsRequired().HasMaxLength(1000);
                entity.Property(td => td.IsActive).HasDefaultValue(true);
            });
        }
    }
}