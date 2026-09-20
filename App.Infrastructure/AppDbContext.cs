using App.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Existing
        public DbSet<Company> Companies { get; set; }

        // NEW — Data Collection
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Service> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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

                entity.Property(x => x.ContactDetails)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.ServiceLocation)
                    .HasMaxLength(300)
                    .IsRequired();
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