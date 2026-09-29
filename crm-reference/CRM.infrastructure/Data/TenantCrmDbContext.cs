using CRM.domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.Data
{
    public class TenantCrmDbContext : DbContext
    {
        public TenantCrmDbContext(DbContextOptions<TenantCrmDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<FollowUp> FollowUps { get; set; }
        public DbSet<CustomerInquiry> CustomerInquiries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- User Configuration -----
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Name).IsRequired().HasMaxLength(150);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);

                // Unique email per tenant
                entity.HasIndex(u => u.Email).IsUnique();

                // One User -> Many Customers
                entity.HasMany(u => u.Customers)
                      .WithOne(c => c.AssignedUser)
                      .HasForeignKey(c => c.AssignedUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // One User -> Many Leads
                entity.HasMany(u => u.Leads)
                      .WithOne(l => l.AssignedUser)
                      .HasForeignKey(l => l.AssignedUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ----- Customer Configuration -----
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Email).IsRequired().HasMaxLength(256);
                entity.Property(c => c.Phone).HasMaxLength(50);
                entity.Property(c => c.Company).HasMaxLength(200);
                entity.Property(c => c.Address).HasMaxLength(500);
                entity.Property(c => c.Status).HasMaxLength(50);

                entity.HasIndex(c => c.Email);

                // One Customer -> Many Activities
                entity.HasMany(c => c.Activities)
                      .WithOne(a => a.Customer)
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // One Customer -> Many FollowUps
                entity.HasMany(c => c.FollowUps)
                      .WithOne(f => f.Customer)
                      .HasForeignKey(f => f.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- Lead Configuration -----
            modelBuilder.Entity<Lead>(entity =>
            {
                entity.ToTable("Leads");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.Name).IsRequired().HasMaxLength(200);
                entity.Property(l => l.Email).HasMaxLength(256);
                entity.Property(l => l.Phone).HasMaxLength(50);
                entity.Property(l => l.Source).HasMaxLength(100);
                entity.Property(l => l.Status).HasConversion<int>();
                entity.Property(l => l.Priority).HasConversion<int>();
                entity.Property(l => l.Notes).HasMaxLength(2000);

                // decimal(18,2) for currency
                entity.Property(l => l.ExpectedValue)
                      .HasColumnType("decimal(18,2)");

                // Optional link to Customer
                entity.HasOne(l => l.Customer)
                      .WithMany()
                      .HasForeignKey(l => l.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                // One Lead -> Many Activities
                entity.HasMany(l => l.Activities)
                      .WithOne(a => a.Lead)
                      .HasForeignKey(a => a.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);

                // One Lead -> Many FollowUps
                entity.HasMany(l => l.FollowUps)
                      .WithOne(f => f.Lead)
                      .HasForeignKey(f => f.LeadId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- Activity Configuration -----
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.ToTable("Activities");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.ActivityType).IsRequired().HasMaxLength(50);
                entity.Property(a => a.Description).IsRequired().HasMaxLength(2000);
                entity.Property(a => a.ActivityType).HasConversion<int>();

                // One User -> Many Activities (no cascade delete so we don't lose history)
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ----- FollowUp Configuration -----
            modelBuilder.Entity<FollowUp>(entity =>
            {
                entity.ToTable("FollowUps");
                entity.HasKey(f => f.Id);

                entity.Property(f => f.Title).IsRequired().HasMaxLength(200);
                entity.Property(f => f.Description).HasMaxLength(2000);
                entity.Property(f => f.Status).IsRequired().HasMaxLength(50);
                entity.Property(f => f.Status).HasConversion<int>();

                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── CustomerInquiry Configuration ───
            modelBuilder.Entity<CustomerInquiry>(entity =>
            {
                entity.ToTable("CustomerInquiries");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Subject).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Description).IsRequired().HasMaxLength(4000);
                entity.Property(x => x.Resolution).HasMaxLength(2000);
                entity.Property(x => x.Type).HasConversion<int>();
                entity.Property(x => x.Status).HasConversion<int>();
                entity.Property(x => x.Priority).HasConversion<int>();

                // Customer relationship
                entity.HasOne(x => x.Customer)
                      .WithMany()
                      .HasForeignKey(x => x.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Assigned user
                entity.HasOne(x => x.AssignedUser)
                      .WithMany()
                      .HasForeignKey(x => x.AssignedUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Resolved by user
                entity.HasOne(x => x.ResolvedByUser)
                      .WithMany()
                      .HasForeignKey(x => x.ResolvedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Created by user
                entity.HasOne(x => x.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(x => x.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.CustomerId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.AssignedUserId);
            });
        }
    }
}