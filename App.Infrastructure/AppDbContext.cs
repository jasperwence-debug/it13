using App.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AppDb;Trusted_Connection=True;TrustServerCertificate=True;");
            }
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        public static void EnsureSeedData(AppDbContext db)
        {
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE [Users] (
                            [Id] int NOT NULL IDENTITY(1,1),
                            [Username] nvarchar(100) NOT NULL,
                            [PasswordHash] nvarchar(200) NOT NULL,
                            [Role] nvarchar(50) NOT NULL,
                            CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = 'superadmin')
                        INSERT INTO [Users] ([Username], [PasswordHash], [Role]) VALUES ('superadmin', 'super123', 'SuperAdmin');
                    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = 'admin')
                        INSERT INTO [Users] ([Username], [PasswordHash], [Role]) VALUES ('admin', 'admin123', 'Admin');
                    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = 'manager')
                        INSERT INTO [Users] ([Username], [PasswordHash], [Role]) VALUES ('manager', 'manager123', 'Manager');
                    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = 'staff')
                        INSERT INTO [Users] ([Username], [PasswordHash], [Role]) VALUES ('staff', 'staff123', 'SalesStaff');

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'Email')
                    BEGIN
                        ALTER TABLE [Customers] ADD [Email] nvarchar(150) NULL;
                    END

                    EXEC('UPDATE [Customers] SET [Email] = LOWER(LTRIM(RTRIM(SUBSTRING(ContactDetails, CHARINDEX(''''|'''', ContactDetails) + 1, LEN(ContactDetails))))) WHERE [Email] IS NULL AND ContactDetails LIKE ''''%|%''''');
                ");
            }
            catch
            {
                // Ignore if migration already handled or table exists
            }
        }

        // Existing
        public DbSet<Company> Companies { get; set; }

        // RBAC Users
        public new DbSet<User> Users { get; set; } = null!;

        // NEW — Data Collection
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Service> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==============================
            // User & RBAC Seed
            // ==============================
            builder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Username)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Role)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasData(
                    new User { Id = 1, Username = "superadmin", PasswordHash = "super123", Role = "SuperAdmin" },
                    new User { Id = 2, Username = "admin", PasswordHash = "admin123", Role = "Admin" },
                    new User { Id = 3, Username = "manager", PasswordHash = "manager123", Role = "Manager" },
                    new User { Id = 4, Username = "staff", PasswordHash = "staff123", Role = "SalesStaff" }
                );
            });

            // ==============================
            // Company
            // ==============================
            // ==============================
            
            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);
                // Note: no Property config — columns stay nvarchar(max)
            });
            // ==============================
            // Lead
            // ==============================
            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(x => x.LeadId);

                entity.Property(x => x.LeadName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ContactInfo)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.LeadSource)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ServiceOfInterest)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.InquiryDetails)
                    .HasMaxLength(500);
            });

            // ==============================
            // Customer
            // ==============================
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.CustomerType)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.CustomerName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ContactInfo)
                    .HasColumnName("ContactDetails")
                    .HasMaxLength(150)
                    .IsRequired();

                entity.HasIndex(c => c.ContactInfo)
                    .IsUnique();

                entity.Property(x => x.ServiceLocation)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(150);

                entity.Ignore(x => x.Id);
                entity.Ignore(x => x.FullName);
                entity.Ignore(x => x.Type);
                entity.Ignore(x => x.Location);
                entity.Ignore(x => x.ContactDetails);
                entity.Ignore(x => x.Phone);
            });

            // ==============================
            // Service
            // ==============================
            builder.Entity<Service>(entity =>
            {
                entity.HasKey(x => x.ServiceId);

                entity.Property(x => x.ServiceName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Category)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.BasePrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Description)
                    .HasMaxLength(300);
            });

            // ==============================
            // ServiceRequest
            // ==============================
            builder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(x => x.ServiceRequestId);

                entity.Property(x => x.RequestedService)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Ignore(x => x.Id);
                entity.Ignore(x => x.ServiceType);

                entity.Property(x => x.Status)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.ActualPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.SpecialRequests)
                    .HasMaxLength(500);

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.AssignedSalesStaff)
                    .HasMaxLength(100)
                    .IsRequired();

                // Relationship: Lead (nullable)
                entity.HasOne(x => x.Lead)
                    .WithMany()
                    .HasForeignKey(x => x.LeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Customer (required)
                entity.HasOne(x => x.Customer)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Service (nullable)
                entity.HasOne(x => x.Service)
                    .WithMany()
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}