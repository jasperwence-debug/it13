using System;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.API.Controllers
{
    public class BookingRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? ServiceType { get; set; }
        public DateTime? PreferredDate { get; set; }

        // Domain property aliases for client compatibility
        public string? CustomerName { get => FullName; set => FullName = value; }
        public string? ContactInfo { get => Phone; set => Phone = value; }
        public string? ContactDetails { get => Phone; set => Phone = value; }
        public string? ServiceLocation { get => Location; set => Location = value; }
        public string? RequestedService { get => ServiceType; set => ServiceType = value; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Route("api/service-requests")]
    [Route("api/service-request")]
    [Route("api/servicerequests")]
    [Route("api/bookings")]
    [Route("api/booking")]
    public class ServiceRequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceRequestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [HttpPost("book")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body cannot be null." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Check for existing customer by email (case-insensitive) or phone number
                var normalizedEmail = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim().ToLower() : null;
                var normalizedPhone = !string.IsNullOrWhiteSpace(request.Phone) ? request.Phone.Trim() : null;

                Customer? customer = null;

                if (!string.IsNullOrEmpty(normalizedEmail) && !string.IsNullOrEmpty(normalizedPhone))
                {
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            (c.Email != null && c.Email.ToLower() == normalizedEmail) ||
                            c.ContactInfo == normalizedPhone ||
                            c.ContactInfo.StartsWith(normalizedPhone + " |") ||
                            c.ContactInfo.EndsWith("| " + normalizedPhone));
                }
                else if (!string.IsNullOrEmpty(normalizedEmail))
                {
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            (c.Email != null && c.Email.ToLower() == normalizedEmail) ||
                            c.ContactInfo.Contains(normalizedEmail));
                }
                else if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.ContactInfo == normalizedPhone ||
                            c.ContactInfo.StartsWith(normalizedPhone + " |") ||
                            c.ContactInfo.EndsWith("| " + normalizedPhone));
                }

                // 2. If not found, create and insert the new customer
                if (customer == null)
                {
                    customer = new Customer
                    {
                        CustomerName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : "Valued Customer",
                        Email = normalizedEmail,
                        ContactInfo = normalizedPhone ?? normalizedEmail ?? string.Empty,
                        Location = request.Location ?? string.Empty,
                        Type = "Individual",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Customers.AddAsync(customer);
                    await _context.SaveChangesAsync();
                }

                var safePreferredDate = (request.PreferredDate.HasValue && request.PreferredDate.Value.Year >= 1753)
                    ? request.PreferredDate.Value
                    : DateTime.UtcNow.AddDays(1);

                // 3. Link the new booking/service request safely to customer.Id
                var serviceRequest = new ServiceRequest
                {
                    CustomerId = customer.Id,
                    ServiceType = !string.IsNullOrWhiteSpace(request.ServiceType) ? request.ServiceType.Trim() : "General Cleaning",
                    PreferredDate = safePreferredDate,
                    Status = "Pending",
                    BookingDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    AssignedSalesStaff = "Staff"
                };

                await _context.ServiceRequests.AddAsync(serviceRequest);
                await _context.SaveChangesAsync();

                // 4. Commit transaction safely
                await transaction.CommitAsync();
                return Ok(new { success = true, customerId = customer.Id, requestId = serviceRequest.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Database transaction failed", details = ex.Message });
            }
        }
    }
}
