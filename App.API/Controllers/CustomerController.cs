using System;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET /api/customer/check/{contactInfo}
        // GET /api/customers/check/{contactInfo}
        // Returns existing Customer details if found, or 404 if not.
        // ============================================================
        [HttpGet("check/{contactInfo}")]
        public async Task<ActionResult<CustomerDto>> CheckCustomerByRoute(string contactInfo)
        {
            return await FindCustomerAsync(contactInfo);
        }

        // Query param fallback: GET /api/customers/check?contact={contactInfo}
        [HttpGet("check")]
        public async Task<ActionResult<CustomerDto>> CheckCustomerByQuery([FromQuery] string? contact, [FromQuery] string? contactInfo)
        {
            var search = !string.IsNullOrWhiteSpace(contact) ? contact : contactInfo;
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest("Contact information query parameter is required.");

            return await FindCustomerAsync(search);
        }

        private async Task<ActionResult<CustomerDto>> FindCustomerAsync(string contactInfo)
        {
            var search = contactInfo?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest("Contact information cannot be empty.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.IsActive && c.ContactInfo == search);

            if (customer == null)
                return NotFound();

            return Ok(new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                CustomerType = customer.CustomerType,
                ContactDetails = customer.ContactInfo,
                ServiceLocation = customer.ServiceLocation
            });
        }

        // ============================================================
        // POST /api/customer
        // POST /api/data-collection
        // Upsert Pattern with IDbContextTransaction
        // ============================================================
        [HttpPost]
        [HttpPost("/api/data-collection")]
        public async Task<IActionResult> SaveCustomerBooking([FromBody] DataCollectionDto dto)
        {
            if (dto == null)
                return BadRequest("Request body cannot be null.");

            var contact = (!string.IsNullOrWhiteSpace(dto.ContactInfo) ? dto.ContactInfo : dto.ContactDetails)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contact))
                return BadRequest("Contact information is required.");

            using IDbContextTransaction tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Look up customer by ContactInfo (deduplication check)
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.IsActive && c.ContactInfo == contact);

                Customer targetCustomer;

                if (existingCustomer != null)
                {
                    // If EXISTS: Do NOT insert a new Customer. Update their existing fields if changed
                    if (!string.IsNullOrWhiteSpace(dto.ServiceLocation) && existingCustomer.ServiceLocation != dto.ServiceLocation.Trim())
                    {
                        existingCustomer.ServiceLocation = dto.ServiceLocation.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(dto.CustomerType) && existingCustomer.CustomerType != dto.CustomerType.Trim())
                    {
                        existingCustomer.CustomerType = dto.CustomerType.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(dto.CustomerName) && existingCustomer.CustomerName != dto.CustomerName.Trim())
                    {
                        existingCustomer.CustomerName = dto.CustomerName.Trim();
                    }

                    targetCustomer = existingCustomer;
                }
                else
                {
                    // If NOT EXISTS: Insert the new Customer model
                    targetCustomer = new Customer
                    {
                        CustomerType = dto.CustomerType?.Trim() ?? "Individual",
                        CustomerName = dto.CustomerName?.Trim() ?? "Valued Customer",
                        ContactInfo = contact,
                        ServiceLocation = dto.ServiceLocation?.Trim() ?? string.Empty,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(targetCustomer);
                }

                // Insert new Lead entry for tracking
                var lead = new Lead
                {
                    LeadName = !string.IsNullOrWhiteSpace(dto.LeadName) ? dto.LeadName.Trim() : targetCustomer.CustomerName,
                    ContactInfo = contact,
                    LeadSource = !string.IsNullOrWhiteSpace(dto.LeadSource) ? dto.LeadSource.Trim() : "Direct",
                    ServiceOfInterest = dto.ServiceOfInterest?.Trim() ?? dto.RequestedService?.Trim() ?? "General",
                    InquiryDetails = dto.InquiryDetails?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Leads.Add(lead);

                // DateTime Sanitization (prevents SQL Server DateTime overflow on default(DateTime))
                var safePreferredDate = dto.PreferredDate.Year < 1753 ? DateTime.UtcNow.AddDays(1) : dto.PreferredDate;
                var safeFollowUpDate = (dto.FollowUpDate.HasValue && dto.FollowUpDate.Value.Year > 1753) ? dto.FollowUpDate : null;

                // Insert new ServiceRequest linked to targetCustomer (existing or new)
                var serviceRequest = new ServiceRequest
                {
                    Lead = lead,
                    Customer = targetCustomer,
                    RequestedService = dto.RequestedService?.Trim() ?? "General Cleaning",
                    PreferredDate = safePreferredDate,
                    SpecialRequests = dto.SpecialRequests?.Trim(),
                    FollowUpDate = safeFollowUpDate,
                    Notes = dto.Notes?.Trim(),
                    AssignedSalesStaff = !string.IsNullOrWhiteSpace(dto.AssignedSalesStaff) ? dto.AssignedSalesStaff.Trim() : "Staff",
                    IsActive = true,
                    BookingDate = DateTime.UtcNow
                };
                _context.ServiceRequests.Add(serviceRequest);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Created(
                    $"/api/data-collection/{serviceRequest.ServiceRequestId}",
                    new
                    {
                        serviceRequestId = serviceRequest.ServiceRequestId,
                        leadId = lead.LeadId,
                        customerId = targetCustomer.CustomerId
                    });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"[DB TRANSACTION ERROR]: {innerMsg}");
                return StatusCode(500, $"Transaction failed: {innerMsg}");
            }
        }
    }
}
