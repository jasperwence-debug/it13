using App.API;
using App.API.Data;
using App.Domain.Entities;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============================================================
// GET /api/data-collection
// Lists only ACTIVE ServiceRequests (IsActive = true).
// ============================================================
app.MapGet("/api/data-collection", async (AppDbContext db) =>
{
    var records = await db.ServiceRequests
        .Include(sr => sr.Lead)
        .Include(sr => sr.Customer)
        .Where(sr => sr.IsActive)
        .OrderByDescending(sr => sr.ServiceRequestId)
        .Select(sr => new
        {
            serviceRequestId = sr.ServiceRequestId,
            leadId = sr.LeadId,
            customerId = sr.CustomerId,

            // Lead
            leadName = sr.Lead != null ? sr.Lead.LeadName : "",
            contactInfo = sr.Lead != null ? sr.Lead.ContactInfo : "",
            leadSource = sr.Lead != null ? sr.Lead.LeadSource : "",
            serviceOfInterest = sr.Lead != null ? sr.Lead.ServiceOfInterest : "",
            inquiryDetails = sr.Lead != null ? sr.Lead.InquiryDetails : null,

            // Customer
            customerType = sr.Customer != null ? sr.Customer.CustomerType : "",
            customerName = sr.Customer != null ? sr.Customer.CustomerName : "",
            contactDetails = sr.Customer != null ? sr.Customer.ContactDetails : "",
            serviceLocation = sr.Customer != null ? sr.Customer.ServiceLocation : "",

            // Service
            requestedService = sr.RequestedService,
            preferredDate = sr.PreferredDate,
            specialRequests = sr.SpecialRequests,
            followUpDate = sr.FollowUpDate,
            notes = sr.Notes,
            assignedSalesStaff = sr.AssignedSalesStaff
        })
        .ToListAsync();

    return Results.Ok(records);
});

// ============================================================
// GET /api/data-collection/{id}
// Gets a single record by ServiceRequestId (for Edit dialog).
// ============================================================
app.MapGet("/api/data-collection/{id:int}", async (int id, AppDbContext db) =>
{
    var sr = await db.ServiceRequests
        .Include(x => x.Lead)
        .Include(x => x.Customer)
        .FirstOrDefaultAsync(x => x.ServiceRequestId == id && x.IsActive);

    if (sr == null)
        return Results.NotFound($"No active record with id {id}.");

    var dto = new
    {
        serviceRequestId = sr.ServiceRequestId,
        leadId = sr.LeadId,
        customerId = sr.CustomerId,

        leadName = sr.Lead?.LeadName ?? "",
        contactInfo = sr.Lead?.ContactInfo ?? "",
        leadSource = sr.Lead?.LeadSource ?? "",
        serviceOfInterest = sr.Lead?.ServiceOfInterest ?? "",
        inquiryDetails = sr.Lead?.InquiryDetails,

        customerType = sr.Customer?.CustomerType ?? "",
        customerName = sr.Customer?.CustomerName ?? "",
        contactDetails = sr.Customer?.ContactDetails ?? "",
        serviceLocation = sr.Customer?.ServiceLocation ?? "",

        requestedService = sr.RequestedService,
        preferredDate = sr.PreferredDate,
        specialRequests = sr.SpecialRequests,
        followUpDate = sr.FollowUpDate,
        notes = sr.Notes,
        assignedSalesStaff = sr.AssignedSalesStaff
    };

    return Results.Ok(dto);
});

// ============================================================
// Customer & Data-Collection Upsert endpoints are routed to CustomerController:
// GET  /api/customers/check/{contactInfo}
// GET  /api/customers/check?contact={contactInfo}
// POST /api/customer
// POST /api/data-collection (Upsert)
// ============================================================

// ============================================================
// PUT /api/data-collection/{id}
// Updates Lead + Customer + ServiceRequest in one transaction.
// ============================================================
app.MapPut("/api/data-collection/{id:int}", async (int id, DataCollectionDto dto, AppDbContext db) =>
{
    using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        var sr = await db.ServiceRequests
            .Include(x => x.Lead)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.ServiceRequestId == id && x.IsActive);

        if (sr == null)
            return Results.NotFound($"No active record with id {id}.");

        // Update Lead
        if (sr.Lead != null)
        {
            sr.Lead.LeadName = dto.LeadName;
            sr.Lead.ContactInfo = dto.ContactInfo;
            sr.Lead.LeadSource = dto.LeadSource;
            sr.Lead.ServiceOfInterest = dto.ServiceOfInterest;
            sr.Lead.InquiryDetails = dto.InquiryDetails;
        }

        // Update Customer
        if (sr.Customer != null)
        {
            sr.Customer.CustomerType = dto.CustomerType;
            sr.Customer.CustomerName = dto.CustomerName;
            sr.Customer.ContactDetails = dto.ContactDetails;
            sr.Customer.ServiceLocation = dto.ServiceLocation;
        }

        // Update ServiceRequest
        sr.RequestedService = dto.RequestedService;
        sr.PreferredDate = dto.PreferredDate;
        sr.SpecialRequests = dto.SpecialRequests;
        sr.FollowUpDate = dto.FollowUpDate;
        sr.Notes = dto.Notes;
        sr.AssignedSalesStaff = dto.AssignedSalesStaff;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.NoContent();   // 204
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.Problem($"Failed to update: {ex.Message}");
    }
});

// ============================================================
// DELETE /api/data-collection/{id}
// SOFT DELETE: sets IsActive = false on Lead, Customer, ServiceRequest.
// ============================================================
app.MapDelete("/api/data-collection/{id:int}", async (int id, AppDbContext db) =>
{
    using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        var sr = await db.ServiceRequests
            .Include(x => x.Lead)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.ServiceRequestId == id && x.IsActive);

        if (sr == null)
            return Results.NotFound($"No active record with id {id}.");

        // Soft delete — set IsActive = false
        sr.IsActive = false;
        if (sr.Lead != null) sr.Lead.IsActive = false;
        if (sr.Customer != null) sr.Customer.IsActive = false;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.NoContent();   // 204
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.Problem($"Failed to delete: {ex.Message}");
    }
});

// ============================================================
// GET /api/analytics/dashboard
// Business Intelligence & Retention Analytics
// ============================================================
app.MapGet("/api/analytics/dashboard", async (AppDbContext db) =>
{
    var activeRequestsQuery = db.ServiceRequests.Where(sr => sr.IsActive);

    var totalCustomers = await db.Customers.CountAsync(c => c.IsActive);
    var totalBookings = await activeRequestsQuery.CountAsync();
    var completedBookings = await activeRequestsQuery.CountAsync(sr => sr.Status == "Completed");
    var cancelledBookings = await activeRequestsQuery.CountAsync(sr => sr.Status == "Cancelled");
    var scheduledBookings = await activeRequestsQuery.CountAsync(sr => sr.Status == "Scheduled");
    var requestedBookings = await activeRequestsQuery.CountAsync(sr => sr.Status == "Requested");

    var totalLeads = await db.Leads.CountAsync(l => l.IsActive);
    var leadConversionRate = totalLeads > 0
        ? Math.Round((double)totalCustomers / totalLeads * 100.0, 1)
        : (totalBookings > 0 ? Math.Round((double)(completedBookings + scheduledBookings) / totalBookings * 100.0, 1) : 0.0);

    var totalRevenue = await activeRequestsQuery
        .Where(sr => sr.Status == "Completed")
        .SumAsync(sr => (decimal?)sr.ActualPrice) ?? 0m;

    var averageBookingValue = completedBookings > 0
        ? Math.Round(totalRevenue / completedBookings, 2)
        : 0m;

    // Repeat customer rate: % of customers with >1 completed bookings among customers who have completed bookings
    var customerCompletedGroups = await activeRequestsQuery
        .Where(sr => sr.Status == "Completed")
        .GroupBy(sr => sr.CustomerId)
        .Select(g => new { CustomerId = g.Key, Count = g.Count() })
        .ToListAsync();

    var customersWithCompleted = customerCompletedGroups.Count;
    var repeatCustomers = customerCompletedGroups.Count(c => c.Count > 1);
    var repeatCustomerRate = customersWithCompleted > 0
        ? Math.Round((double)repeatCustomers / customersWithCompleted * 100.0, 1)
        : 0.0;

    // At-risk customers: customers whose latest completed booking was 60+ days ago
    var cutoff60Days = DateTime.UtcNow.AddDays(-60);
    var latestCompletedDates = await activeRequestsQuery
        .Where(sr => sr.Status == "Completed")
        .GroupBy(sr => sr.CustomerId)
        .Select(g => g.Max(sr => sr.BookingDate))
        .ToListAsync();

    var atRiskCustomerCount = latestCompletedDates.Count(date => date <= cutoff60Days);

    // Monthly trend (last 12 months in UTC)
    var startMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

    var rawMonthly = await activeRequestsQuery
        .Where(sr => sr.BookingDate >= startMonth)
        .GroupBy(sr => new { Year = sr.BookingDate.Year, Month = sr.BookingDate.Month })
        .Select(g => new
        {
            Year = g.Key.Year,
            Month = g.Key.Month,
            Bookings = g.Count(),
            Revenue = g.Where(sr => sr.Status == "Completed").Sum(sr => (decimal?)sr.ActualPrice) ?? 0m
        })
        .ToListAsync();

    var monthlyTrend = new List<object>();
    for (int i = 0; i < 12; i++)
    {
        var targetDate = startMonth.AddMonths(i);
        var item = rawMonthly.FirstOrDefault(m => m.Year == targetDate.Year && m.Month == targetDate.Month);
        monthlyTrend.Add(new
        {
            month = targetDate.ToString("yyyy-MM"),
            bookings = item?.Bookings ?? 0,
            revenue = item?.Revenue ?? 0m
        });
    }

    // Top 5 services by count
    var topServices = await activeRequestsQuery
        .Where(sr => !string.IsNullOrEmpty(sr.RequestedService))
        .GroupBy(sr => sr.RequestedService)
        .Select(g => new
        {
            serviceName = g.Key,
            count = g.Count(),
            revenue = g.Where(sr => sr.Status == "Completed").Sum(sr => (decimal?)sr.ActualPrice) ?? 0m
        })
        .OrderByDescending(x => x.count)
        .Take(5)
        .ToListAsync();

    // Category breakdown (Residential, Commercial, Specialty)
    var categories = new[] { "Residential", "Commercial", "Specialty" };
    var rawCategories = await activeRequestsQuery
        .Where(sr => sr.Service != null)
        .GroupBy(sr => sr.Service!.Category)
        .Select(g => new
        {
            category = g.Key,
            count = g.Count(),
            revenue = g.Where(sr => sr.Status == "Completed").Sum(sr => (decimal?)sr.ActualPrice) ?? 0m
        })
        .ToListAsync();

    var categoryBreakdown = categories.Select(cat =>
    {
        var found = rawCategories.FirstOrDefault(c => string.Equals(c.category, cat, StringComparison.OrdinalIgnoreCase));
        return new
        {
            category = cat,
            count = found?.count ?? 0,
            revenue = found?.revenue ?? 0m
        };
    }).ToList();

    return Results.Ok(new
    {
        totalCustomers,
        totalBookings,
        completedBookings,
        cancelledBookings,
        scheduledBookings,
        requestedBookings,
        leadConversionRate,
        totalRevenue,
        averageBookingValue,
        repeatCustomerRate,
        atRiskCustomerCount,
        monthlyTrend,
        topServices,
        categoryBreakdown
    });
});

app.Run();