using App.Domain.Entities;
using App.Domain.Enums;
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

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'Status')
                    BEGIN
                        ALTER TABLE [Leads] ADD [Status] nvarchar(20) NOT NULL DEFAULT 'New';
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'QuotedPrice')
                    BEGIN
                        ALTER TABLE [Leads] ADD [QuotedPrice] decimal(18,2) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ServiceAddress')
                    BEGIN
                        ALTER TABLE [Leads] ADD [ServiceAddress] nvarchar(300) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'LostReason')
                    BEGIN
                        ALTER TABLE [Leads] ADD [LostReason] nvarchar(500) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'QuotedPrice')
                    BEGIN
                        ALTER TABLE [ServiceRequests] ADD [QuotedPrice] decimal(18,2) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'LeadId')
                    BEGIN
                        ALTER TABLE [Customers] ADD [LeadId] int NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ConvertedCustomerId')
                    BEGIN
                        ALTER TABLE [Leads] ADD [ConvertedCustomerId] int NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'ConvertedAt')
                    BEGIN
                        ALTER TABLE [Leads] ADD [ConvertedAt] datetime2 NULL;
                    END

                    -- Migrate legacy Pending status to Scheduled
                    UPDATE [ServiceRequests] SET [Status] = 'Scheduled' WHERE [Status] = 'Pending';

                    UPDATE [Customers] SET [Email] = LOWER(LTRIM(RTRIM(SUBSTRING(ContactDetails, CHARINDEX('|', ContactDetails) + 1, LEN(ContactDetails))))) WHERE [Email] IS NULL AND ContactDetails LIKE '%|%';

                    -- Ensure Companies table exists
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Companies')
                    BEGIN
                        CREATE TABLE [Companies] (
                            [CompanyId] int NOT NULL IDENTITY(1,1),
                            [CompanyCode] nvarchar(max) NOT NULL,
                            [CompanyName] nvarchar(max) NOT NULL,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_Companies] PRIMARY KEY ([CompanyId])
                        );
                    END

                    -- Seed 3 Standard Tenant Companies for Master Tier multi-tenancy
                    IF NOT EXISTS (SELECT 1 FROM [Companies] WHERE [CompanyCode] = 'T-MICRO')
                        INSERT INTO [Companies] ([CompanyCode], [CompanyName], [IsActive], [CreatedAt])
                        VALUES ('T-MICRO', 'Tenant A Cleaners (Micro)', 1, GETUTCDATE());

                    IF NOT EXISTS (SELECT 1 FROM [Companies] WHERE [CompanyCode] = 'T-SMALL')
                        INSERT INTO [Companies] ([CompanyCode], [CompanyName], [IsActive], [CreatedAt])
                        VALUES ('T-SMALL', 'Tenant B Commercial Cleaning (Small)', 1, GETUTCDATE());

                    IF NOT EXISTS (SELECT 1 FROM [Companies] WHERE [CompanyCode] = 'T-ENTERPRISE')
                        INSERT INTO [Companies] ([CompanyCode], [CompanyName], [IsActive], [CreatedAt])
                        VALUES ('T-ENTERPRISE', 'Tenant C Facility Solutions (Enterprise)', 1, GETUTCDATE());

                    -- Ensure Subscriptions table exists
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subscriptions')
                    BEGIN
                        CREATE TABLE [Subscriptions] (
                            [SubscriptionId] int NOT NULL IDENTITY(1,1),
                            [CompanyId] int NOT NULL,
                            [Tier] int NOT NULL DEFAULT 1,
                            [Status] nvarchar(30) NOT NULL DEFAULT 'Active',
                            [BillingCycle] nvarchar(30) NOT NULL DEFAULT 'Monthly',
                            [MonthlyPrice] decimal(18,2) NOT NULL DEFAULT 2500.00,
                            [StartDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [EndDate] datetime2 NOT NULL DEFAULT DATEADD(month, 1, GETUTCDATE()),
                            [MaxUsers] int NOT NULL DEFAULT 3,
                            [MaxBranches] int NOT NULL DEFAULT 1,
                            [HasDataCollection] bit NOT NULL DEFAULT 1,
                            [HasTransactions] bit NOT NULL DEFAULT 1,
                            [HasBusinessIntelligence] bit NOT NULL DEFAULT 0,
                            [HasActions] bit NOT NULL DEFAULT 0,
                            [HasBranching] bit NOT NULL DEFAULT 0,
                            [Notes] nvarchar(500) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] datetime2 NULL,
                            CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([SubscriptionId]),
                            CONSTRAINT [FK_Subscriptions_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([CompanyId]) ON DELETE CASCADE
                        );
                    END

                    -- Seed initial tenant subscriptions if none exist
                    IF NOT EXISTS (SELECT 1 FROM [Subscriptions])
                    BEGIN
                        DECLARE @MicroId int = (SELECT TOP 1 [CompanyId] FROM [Companies] WHERE [CompanyCode] = 'T-MICRO');
                        DECLARE @SmallId int = (SELECT TOP 1 [CompanyId] FROM [Companies] WHERE [CompanyCode] = 'T-SMALL');
                        DECLARE @MedId int = (SELECT TOP 1 [CompanyId] FROM [Companies] WHERE [CompanyCode] = 'T-ENTERPRISE');

                        IF @MicroId IS NOT NULL
                            INSERT INTO [Subscriptions] ([CompanyId], [Tier], [Status], [BillingCycle], [MonthlyPrice], [StartDate], [EndDate], [MaxUsers], [MaxBranches], [HasDataCollection], [HasTransactions], [HasBusinessIntelligence], [HasActions], [HasBranching], [Notes])
                            VALUES (@MicroId, 1, 'Active', 'Monthly', 2500.00, GETUTCDATE(), DATEADD(month, 1, GETUTCDATE()), 3, 1, 1, 1, 0, 0, 0, 'Tenant A standard micro tier plan (Main Transaction & Data Collection)');

                        IF @SmallId IS NOT NULL
                            INSERT INTO [Subscriptions] ([CompanyId], [Tier], [Status], [BillingCycle], [MonthlyPrice], [StartDate], [EndDate], [MaxUsers], [MaxBranches], [HasDataCollection], [HasTransactions], [HasBusinessIntelligence], [HasActions], [HasBranching], [Notes])
                            VALUES (@SmallId, 2, 'Active', 'Monthly', 5500.00, GETUTCDATE(), DATEADD(month, 1, GETUTCDATE()), 10, 1, 1, 1, 1, 1, 0, 'Tenant B small company tier (Business Intelligence & Actions)');

                        IF @MedId IS NOT NULL
                            INSERT INTO [Subscriptions] ([CompanyId], [Tier], [Status], [BillingCycle], [MonthlyPrice], [StartDate], [EndDate], [MaxUsers], [MaxBranches], [HasDataCollection], [HasTransactions], [HasBusinessIntelligence], [HasActions], [HasBranching], [Notes])
                            VALUES (@MedId, 3, 'Active', 'Annual', 12000.00, GETUTCDATE(), DATEADD(year, 1, GETUTCDATE()), 50, 10, 1, 1, 1, 1, 1, 'Tenant C medium enterprise tier (Branching, BI & Actions)');
                    END

                    -- Ensure Branches table exists
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branches')
                    BEGIN
                        CREATE TABLE [Branches] (
                            [BranchId] int NOT NULL IDENTITY(1,1),
                            [CompanyId] int NOT NULL,
                            [BranchCode] nvarchar(50) NOT NULL,
                            [BranchName] nvarchar(150) NOT NULL,
                            [Address] nvarchar(300) NOT NULL DEFAULT '',
                            [City] nvarchar(100) NOT NULL DEFAULT '',
                            [Phone] nvarchar(50) NOT NULL DEFAULT '',
                            [Email] nvarchar(150) NOT NULL DEFAULT '',
                            [ManagerName] nvarchar(100) NOT NULL DEFAULT '',
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] datetime2 NULL,
                            CONSTRAINT [PK_Branches] PRIMARY KEY ([BranchId]),
                            CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([CompanyId]) ON DELETE CASCADE
                        );
                    END

                    -- Add BranchId to ServiceRequests if missing
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'BranchId')
                    BEGIN
                        ALTER TABLE [ServiceRequests] ADD [BranchId] int NULL;
                    END

                    -- Seed 3 Branches for Tenant C (T-ENTERPRISE)
                    DECLARE @EnterpriseCompanyId int = (SELECT TOP 1 [CompanyId] FROM [Companies] WHERE [CompanyCode] = 'T-ENTERPRISE');
                    IF @EnterpriseCompanyId IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE [BranchCode] = 'BR-DVO')
                            INSERT INTO [Branches] ([CompanyId], [BranchCode], [BranchName], [Address], [City], [Phone], [Email], [ManagerName], [IsActive], [CreatedAt])
                            VALUES (@EnterpriseCompanyId, 'BR-DVO', 'Davao Regional Operations Hub', 'JP Laurel Ave, Bajada', 'Davao City', '09171234567', 'davao.hub@tenant-c.ph', 'Engr. Ramon Alvarez', 1, GETUTCDATE());

                        IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE [BranchCode] = 'BR-MNL')
                            INSERT INTO [Branches] ([CompanyId], [BranchCode], [BranchName], [Address], [City], [Phone], [Email], [ManagerName], [IsActive], [CreatedAt])
                            VALUES (@EnterpriseCompanyId, 'BR-MNL', 'Metro Manila Corporate Hub', 'BGC High Street, Taguig', 'Metro Manila', '09189876543', 'manila.hub@tenant-c.ph', 'Maria Christina Santos', 1, GETUTCDATE());

                        IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE [BranchCode] = 'BR-CEB')
                            INSERT INTO [Branches] ([CompanyId], [BranchCode], [BranchName], [Address], [City], [Phone], [Email], [ManagerName], [IsActive], [CreatedAt])
                            VALUES (@EnterpriseCompanyId, 'BR-CEB', 'Cebu IT Park Operations Hub', 'Salinas Drive, Lahug', 'Cebu City', '09225556677', 'cebu.hub@tenant-c.ph', 'Capt. Vicente Navarro', 1, GETUTCDATE());
                    END
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
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;

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

                // Additive: lifecycle status column (New, Contacted, Quoted, Won, Lost, Converted)
                entity.Property(x => x.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("New")
                    .IsRequired();

                entity.Property(x => x.QuotedPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ServiceAddress)
                    .HasMaxLength(300);

                entity.Property(x => x.LostReason)
                    .HasMaxLength(500);

                entity.Property(x => x.ConvertedCustomerId);
                entity.Property(x => x.ConvertedAt);
            });

            // ==============================
            // Customer
            // ==============================
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.LeadId);

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

                entity.Property(x => x.QuotedPrice)
                    .HasPrecision(18, 2);

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

                // Relationship: Branch (nullable)
                entity.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==============================
            // Subscription
            // ==============================
            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(x => x.SubscriptionId);

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.BillingCycle)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.MonthlyPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==============================
            // Branch (Tenant C Multi-Branching)
            // ==============================
            builder.Entity<Branch>(entity =>
            {
                entity.HasKey(x => x.BranchId);

                entity.Property(x => x.BranchCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.BranchName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Address)
                    .HasMaxLength(300);

                entity.Property(x => x.City)
                    .HasMaxLength(100);

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Email)
                    .HasMaxLength(150);

                entity.Property(x => x.ManagerName)
                    .HasMaxLength(100);

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}