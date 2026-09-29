using System;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Common;
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
        // GET /api/customers
        // Returns distinct master customer records — 1 customer = 1 row.
        // Includes computed aggregates: TotalBookings, LatestService, LatestDate.
        // Read-only: AsNoTracking() throughout.
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] int? assignedUserId = null, [FromQuery] string? assignedStaff = null)
        {
            var now = DateTime.UtcNow;

            var query = _context.Customers
                .AsNoTracking()
                .Where(c => c.IsActive);

            var staffTrimmed = assignedStaff?.Trim() ?? string.Empty;
            var uid = assignedUserId ?? 0;

            if (uid > 0 && !string.IsNullOrEmpty(staffTrimmed))
            {
                query = query.Where(c => c.AssignedUserId == uid || c.AssignedSalesStaff == staffTrimmed);
            }
            else if (uid > 0)
            {
                query = query.Where(c => c.AssignedUserId == uid);
            }
            else if (!string.IsNullOrEmpty(staffTrimmed))
            {
                query = query.Where(c => c.AssignedSalesStaff == staffTrimmed);
            }

            var rawCustomers = await query
                .Select(c => new
                {
                    c.CustomerId,
                    c.LeadId,
                    c.FirstName,
                    c.MiddleName,
                    c.LastName,
                    c.Suffix,
                    c.CustomerName,
                    c.CustomerType,
                    c.Email,
                    ContactDetails = c.ContactInfo,
                    c.ServiceLocation,
                    c.CreatedAt,
                    c.AssignedUserId,
                    c.AssignedSalesStaff,
                    TotalBookings = _context.ServiceRequests.Count(sr => sr.CustomerId == c.CustomerId && sr.IsActive),
                    CompletedBookings = _context.ServiceRequests.Count(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Status == "Completed"),
                    TotalSpent = _context.ServiceRequests
                        .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Status == "Completed")
                        .Sum(sr => (decimal?)sr.ActualPrice) ?? 0m,
                    LatestService = _context.ServiceRequests
                        .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive)
                        .OrderByDescending(sr => sr.PreferredDate)
                        .Select(sr => sr.RequestedService)
                        .FirstOrDefault(),
                    LatestDate = _context.ServiceRequests
                        .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive)
                        .OrderByDescending(sr => sr.PreferredDate)
                        .Select(sr => (DateTime?)sr.PreferredDate)
                        .FirstOrDefault(),
                    LatestCompletedDate = _context.ServiceRequests
                        .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Status == "Completed")
                        .OrderByDescending(sr => sr.PreferredDate)
                        .Select(sr => (DateTime?)sr.PreferredDate)
                        .FirstOrDefault(),
                    HasNegativeFeedback = _context.ServiceRequests
                        .Any(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Rating != null && sr.Rating <= 2),
                    LatestRating = _context.ServiceRequests
                        .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Rating != null)
                        .OrderByDescending(sr => sr.PreferredDate)
                        .Select(sr => sr.Rating)
                        .FirstOrDefault()
                })
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            var result = rawCustomers.Select(c =>
            {
                int? daysSince = c.LatestCompletedDate.HasValue
                    ? (int?)Math.Max(0, (now - c.LatestCompletedDate.Value).TotalDays)
                    : (c.LatestDate.HasValue ? (int?)Math.Max(0, (now - c.LatestDate.Value).TotalDays) : null);

                bool isAtRisk = c.CompletedBookings > 0 && daysSince.HasValue && daysSince.Value >= 60;
                string retentionStatus = isAtRisk
                    ? "At-Risk"
                    : (c.CompletedBookings > 1 ? "Repeat" : (c.CompletedBookings == 1 ? "Active" : "New"));

                var resolvedEmail = ExtractEmail(c.Email, c.ContactDetails);

                return new CustomerSummaryDto
                {
                    CustomerId           = c.CustomerId,
                    LeadId               = c.LeadId,
                    FirstName            = c.FirstName,
                    MiddleName           = c.MiddleName,
                    LastName             = c.LastName,
                    Suffix               = c.Suffix,
                    CustomerName         = c.CustomerName,
                    CustomerType         = c.CustomerType,
                    ContactDetails       = c.ContactDetails,
                    Email                = resolvedEmail,
                    ServiceLocation      = c.ServiceLocation,
                    TotalBookings        = c.TotalBookings,
                    CompletedBookings    = c.CompletedBookings,
                    TotalSpent           = c.TotalSpent,
                    DaysSinceLastService = daysSince,
                    IsAtRisk             = isAtRisk,
                    RetentionStatus      = retentionStatus,
                    LatestService        = c.LatestService,
                    LatestDate           = c.LatestDate,
                    CreatedAt            = c.CreatedAt,
                    AssignedUserId       = c.AssignedUserId,
                    AssignedSalesStaff   = c.AssignedSalesStaff,
                    HasNegativeFeedback  = c.HasNegativeFeedback,
                    LatestRating         = c.LatestRating
                };
            }).ToList();

            return Ok(result);
        }

        public static string ExtractEmail(string? email, string? contactDetails)
        {
            if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
                return email.Trim();
            if (!string.IsNullOrWhiteSpace(contactDetails))
            {
                var parts = contactDetails.Split('|', StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    if (p.Contains('@') && p.Contains('.'))
                        return p.Trim();
                }
            }
            return string.Empty;
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

            var searchLower = search.ToLower();
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.IsActive && (
                    (c.Email != null && c.Email.ToLower() == searchLower) ||
                    c.ContactInfo == search ||
                    c.ContactInfo.StartsWith(search + " |") ||
                    c.ContactInfo.EndsWith("| " + search) ||
                    c.ContactInfo.Contains(search)));

            if (customer == null)
                return NotFound();

            return Ok(new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                FirstName = customer.FirstName,
                MiddleName = customer.MiddleName,
                LastName = customer.LastName,
                Suffix = customer.Suffix,
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

            // Detect email and phone components
            string? email = null;
            string? phone = null;

            if (contact.Contains("@"))
            {
                if (contact.Contains("|"))
                {
                    var parts = contact.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        if (p.Contains("@")) email = p.ToLower();
                        else phone = p;
                    }
                }
                else
                {
                    email = contact.ToLower();
                }
            }
            else
            {
                phone = contact;
            }

            // Resolve Normalized Names
            string firstName = !string.IsNullOrWhiteSpace(dto.FirstName) ? NameNormalizer.Normalize(dto.FirstName) : "";
            string? middleName = !string.IsNullOrWhiteSpace(dto.MiddleName) ? NameNormalizer.Normalize(dto.MiddleName) : null;
            string lastName = !string.IsNullOrWhiteSpace(dto.LastName) ? NameNormalizer.Normalize(dto.LastName) : "";
            string? suffix = !string.IsNullOrWhiteSpace(dto.Suffix) ? NameNormalizer.NormalizeSuffix(dto.Suffix) : null;

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                var rawName = !string.IsNullOrWhiteSpace(dto.CustomerName)
                    ? dto.CustomerName
                    : (!string.IsNullOrWhiteSpace(dto.LeadName) ? dto.LeadName : "Valued Customer");
                var (f, m, l) = NameNormalizer.SplitSingleString(rawName);
                firstName = f;
                middleName = m;
                lastName = l;
            }

            using IDbContextTransaction tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Look up customer by email (case-insensitive) or phone (deduplication check)
                Customer? existingCustomer = null;
                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(phone))
                {
                    existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.IsActive && (
                            (c.Email != null && c.Email.ToLower() == email) ||
                            c.ContactInfo == phone ||
                            c.ContactInfo.StartsWith(phone + " |") ||
                            c.ContactInfo.EndsWith("| " + phone)));
                }
                else if (!string.IsNullOrEmpty(email))
                {
                    existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.IsActive && (
                            (c.Email != null && c.Email.ToLower() == email) ||
                            c.ContactInfo.Contains(email)));
                }
                else
                {
                    existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.IsActive && (
                            c.ContactInfo == contact ||
                            c.ContactInfo.StartsWith(contact + " |") ||
                            c.ContactInfo.EndsWith("| " + contact)));
                }

                Customer targetCustomer;

                if (existingCustomer != null)
                {
                    // If EXISTS: Do NOT insert a new Customer. Update existing fields if provided
                    if (!string.IsNullOrWhiteSpace(dto.ServiceLocation) && existingCustomer.ServiceLocation != dto.ServiceLocation.Trim())
                    {
                        existingCustomer.ServiceLocation = dto.ServiceLocation.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(dto.CustomerType) && existingCustomer.CustomerType != dto.CustomerType.Trim())
                    {
                        existingCustomer.CustomerType = dto.CustomerType.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                    {
                        existingCustomer.FirstName = firstName;
                        existingCustomer.MiddleName = middleName;
                        existingCustomer.LastName = lastName;
                        existingCustomer.Suffix = suffix;
                        existingCustomer.CustomerName = existingCustomer.FullName;
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.CustomerName) && existingCustomer.CustomerName != dto.CustomerName.Trim())
                    {
                        existingCustomer.CustomerName = dto.CustomerName.Trim();
                    }

                    if (existingCustomer.Email == null && email != null)
                    {
                        existingCustomer.Email = email;
                    }

                    targetCustomer = existingCustomer;
                }
                else
                {
                    // If NOT EXISTS: Insert the new Customer model
                    int? assignedUid = null;
                    var assignedStaffTrimmed = dto.AssignedSalesStaff?.Trim();
                    if (!string.IsNullOrWhiteSpace(assignedStaffTrimmed))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == assignedStaffTrimmed.ToLower());
                        if (user != null) assignedUid = user.Id;
                    }

                    targetCustomer = new Customer
                    {
                        CustomerType = dto.CustomerType?.Trim() ?? "Individual",
                        FirstName = firstName,
                        MiddleName = middleName,
                        LastName = lastName,
                        Suffix = suffix,
                        CustomerName = NameNormalizer.FormatFullName(firstName, middleName, lastName, suffix),
                        ContactInfo = contact,
                        Email = email,
                        ServiceLocation = dto.ServiceLocation?.Trim() ?? string.Empty,
                        AssignedSalesStaff = !string.IsNullOrWhiteSpace(assignedStaffTrimmed) ? assignedStaffTrimmed : null,
                        AssignedUserId = assignedUid,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(targetCustomer);
                }

                // Insert new Lead entry for tracking
                int? leadAssignedUid = targetCustomer.AssignedUserId;
                string? leadAssignedStaff = targetCustomer.AssignedSalesStaff;
                if (string.IsNullOrWhiteSpace(leadAssignedStaff) && !string.IsNullOrWhiteSpace(dto.AssignedSalesStaff))
                {
                    leadAssignedStaff = dto.AssignedSalesStaff.Trim();
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == leadAssignedStaff.ToLower());
                    if (user != null) leadAssignedUid = user.Id;
                }

                var lead = new Lead
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Suffix = suffix,
                    LeadName = NameNormalizer.FormatFullName(firstName, middleName, lastName, suffix),
                    ContactInfo = contact,
                    LeadSource = !string.IsNullOrWhiteSpace(dto.LeadSource) ? dto.LeadSource.Trim() : "Direct",
                    ServiceOfInterest = dto.ServiceOfInterest?.Trim() ?? dto.RequestedService?.Trim() ?? "General",
                    InquiryDetails = dto.InquiryDetails?.Trim(),
                    AssignedSalesStaff = leadAssignedStaff,
                    AssignedUserId = leadAssignedUid,
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

        // ============================================================
        // PATCH /api/customers/{id}/assign
        // Reassigns customer ownership to a sales staff member.
        // ============================================================
        [HttpPatch("{id:int}/assign")]
        public async Task<IActionResult> AssignOwner(int id, [FromBody] AssignOwnerDto dto)
        {
            if (dto == null)
                return BadRequest("Payload is required.");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id && c.IsActive);
            if (customer == null)
                return NotFound($"No active customer found with ID {id}.");

            customer.AssignedUserId = dto.AssignedUserId;
            customer.AssignedSalesStaff = dto.AssignedSalesStaff;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                customer.CustomerId,
                customer.AssignedUserId,
                customer.AssignedSalesStaff,
                Message = $"Customer #{customer.CustomerId} reassigned to {customer.AssignedSalesStaff ?? "unassigned"}."
            });
        }
    }
}

