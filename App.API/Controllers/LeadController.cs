using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.API.Controllers
{
    /// <summary>
    /// LAYER 1 — Leads Controller.
    ///
    /// Responsibilities:
    ///   POST /api/leads           — Capture a new inquiry (Name, Contact, Source). NEVER touches ServiceRequests.
    ///   POST /api/leads/{id}/convert — Convert Lead to Customer. NEVER creates ServiceRequests.
    ///   GET  /api/leads           — List active/all leads for the Leads grid.
    /// </summary>
    [ApiController]
    [Route("api/leads")]
    public class LeadController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeadController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET /api/leads
        // Returns active leads by default (excluding Converted).
        // Optional filters: status, includeConverted.
        // Read-only: uses AsNoTracking().
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] bool includeConverted = false, [FromQuery] string? status = null)
        {
            var query = _context.Leads.AsNoTracking().Where(l => l.IsActive);

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Status == status.Trim());
            }
            else if (!includeConverted)
            {
                query = query.Where(l => l.Status != "Converted");
            }

            var leads = await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new LeadDto
                {
                    LeadId              = l.LeadId,
                    LeadName            = l.LeadName,
                    ContactInfo         = l.ContactInfo,
                    LeadSource          = l.LeadSource,
                    ServiceOfInterest   = l.ServiceOfInterest,
                    InquiryDetails      = l.InquiryDetails,
                    Status              = l.Status,
                    QuotedPrice         = l.QuotedPrice,
                    ServiceAddress      = l.ServiceAddress,
                    LostReason          = l.LostReason,
                    ConvertedCustomerId = l.ConvertedCustomerId,
                    ConvertedAt         = l.ConvertedAt,
                    CreatedAt           = l.CreatedAt
                })
                .ToListAsync();

            return Ok(leads);
        }

        // ============================================================
        // POST /api/leads
        // Creates a new Lead inquiry record ONLY.
        // Accepts: LeadName, Phone, Email, LeadSource, InquiryDetails, QuotedPrice, ServiceAddress.
        // ContactInfo is stored as "{Phone} | {Email}" or just one of them.
        // NEVER instantiates or writes to ServiceRequests or Customers.
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] LeadCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Request body cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.LeadName))
                return BadRequest("LeadName is required.");

            if (string.IsNullOrWhiteSpace(dto.Phone) && string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("At least one of Phone or Email is required.");

            if (string.IsNullOrWhiteSpace(dto.LeadSource))
                return BadRequest("LeadSource is required.");

            // Build ContactInfo as "Phone | Email", "Phone", or "Email"
            string contactInfo;
            if (!string.IsNullOrWhiteSpace(dto.Phone) && !string.IsNullOrWhiteSpace(dto.Email))
                contactInfo = $"{dto.Phone.Trim()} | {dto.Email.Trim().ToLower()}";
            else if (!string.IsNullOrWhiteSpace(dto.Phone))
                contactInfo = dto.Phone.Trim();
            else
                contactInfo = dto.Email.Trim().ToLower();

            var lead = new Lead
            {
                LeadName          = dto.LeadName.Trim(),
                ContactInfo       = contactInfo,
                LeadSource        = dto.LeadSource.Trim(),
                ServiceOfInterest = string.Empty,   // Inquiry only — no service commitment
                InquiryDetails    = dto.InquiryDetails?.Trim(),
                Status            = "New",
                QuotedPrice       = dto.QuotedPrice,
                ServiceAddress    = dto.ServiceAddress?.Trim(),
                IsActive          = true,
                CreatedAt         = DateTime.UtcNow
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/leads/{lead.LeadId}",
                new LeadDto
                {
                    LeadId            = lead.LeadId,
                    LeadName          = lead.LeadName,
                    ContactInfo       = lead.ContactInfo,
                    LeadSource        = lead.LeadSource,
                    ServiceOfInterest = lead.ServiceOfInterest,
                    InquiryDetails    = lead.InquiryDetails,
                    Status            = lead.Status,
                    QuotedPrice       = lead.QuotedPrice,
                    ServiceAddress    = lead.ServiceAddress,
                    LostReason        = lead.LostReason,
                    CreatedAt         = lead.CreatedAt
                });
        }

        // ============================================================
        // PATCH /api/leads/{id}/status
        // Updates a Lead's lifecycle status.
        // Enforces:
        //   - Status must be a valid LeadStatus: New, Contacted, Quoted, Won, Lost, Converted
        //   - Quoted status sets QuotedPrice
        //   - Lost status requires LostReason
        // ============================================================
        [HttpPatch("{id:int}/status")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] LeadStatusUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest("Status is required.");

            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id && l.IsActive);
            if (lead == null)
                return NotFound($"No active lead found with ID {id}.");

            var targetStatus = dto.Status.Trim();
            if (!Enum.TryParse<App.Domain.Enums.LeadStatus>(targetStatus, true, out var parsedStatus))
                return BadRequest($"Invalid status '{targetStatus}'. Valid statuses: New, Contacted, Quoted, Won, Lost, Converted.");

            if (parsedStatus == App.Domain.Enums.LeadStatus.Lost && string.IsNullOrWhiteSpace(dto.LostReason))
                return BadRequest("LostReason is required when status is set to Lost.");

            lead.Status = parsedStatus.ToString();

            if (dto.QuotedPrice.HasValue)
                lead.QuotedPrice = dto.QuotedPrice.Value;

            if (!string.IsNullOrWhiteSpace(dto.LostReason))
                lead.LostReason = dto.LostReason.Trim();

            await _context.SaveChangesAsync();

            return Ok(new LeadDto
            {
                LeadId            = lead.LeadId,
                LeadName          = lead.LeadName,
                ContactInfo       = lead.ContactInfo,
                LeadSource        = lead.LeadSource,
                ServiceOfInterest = lead.ServiceOfInterest,
                InquiryDetails    = lead.InquiryDetails,
                Status            = lead.Status,
                QuotedPrice       = lead.QuotedPrice,
                ServiceAddress    = lead.ServiceAddress,
                LostReason        = lead.LostReason,
                CreatedAt         = lead.CreatedAt
            });
        }

        // ============================================================
        // POST /api/leads/{id}/convert
        // Promotes a Lead to a Customer.
        // Enforces:
        //   - Permitted for SalesStaff, Admin, SuperAdmin (Managers forbidden)
        //   - Lead must be in Quoted or Won status
        //   - Deduplication check against Customers table
        //   - Returns 409 Conflict if duplicate found unless ForceCreate or UseExistingCustomerId
        //   - Sets LeadId on Customer and ConvertedCustomerId, ConvertedAt on Lead
        // NEVER creates a ServiceRequest.
        // ============================================================
        [HttpPost("{id:int}/convert")]
        public async Task<IActionResult> ConvertLead(
            int id,
            [FromBody] LeadConvertRequestDto? request = null,
            [FromHeader(Name = "X-User-Role")] string? userRole = null)
        {
            if (!string.IsNullOrWhiteSpace(userRole) && string.Equals(userRole, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, "Managers have view-only access to Leads and are not permitted to convert leads.");
            }

            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id && l.IsActive);
            if (lead == null)
                return NotFound($"No active lead with id {id}.");

            if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                return BadRequest($"Lead {id} is already converted.");

            if (!string.IsNullOrWhiteSpace(request?.ServiceAddress))
            {
                lead.ServiceAddress = request.ServiceAddress.Trim();
            }

            // Parse phone and email from "Phone | Email" format
            string leadPhone = ExtractPhone(lead.ContactInfo);
            string leadEmail = ExtractEmail(lead.ContactInfo);
            string cleanPhoneDigits = !string.IsNullOrEmpty(leadPhone) ? Regex.Replace(leadPhone, @"\D", "") : "";

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int customerId;
                string customerName;
                bool wasExisting;

                if (request?.UseExistingCustomerId.HasValue == true)
                {
                    // User explicitly chose to link to an existing customer
                    var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == request.UseExistingCustomerId.Value && c.IsActive);
                    if (existing == null)
                        return NotFound($"Existing customer with ID {request.UseExistingCustomerId.Value} not found.");

                    customerId = existing.CustomerId;
                    customerName = existing.CustomerName;
                    wasExisting = true;

                    if (!existing.LeadId.HasValue)
                    {
                        existing.LeadId = lead.LeadId;
                    }
                    if (string.IsNullOrWhiteSpace(existing.ServiceLocation) && !string.IsNullOrWhiteSpace(lead.ServiceAddress))
                    {
                        existing.ServiceLocation = lead.ServiceAddress;
                    }
                }
                else
                {
                    // Deduplication check: inspect active customers
                    var activeCustomers = await _context.Customers
                        .Where(c => c.IsActive)
                        .ToListAsync();

                    Customer? matchedCustomer = null;
                    foreach (var c in activeCustomers)
                    {
                        var custEmail = !string.IsNullOrWhiteSpace(c.Email) ? c.Email.Trim().ToLower() : ExtractEmail(c.ContactInfo);
                        if (!string.IsNullOrEmpty(leadEmail) && !string.IsNullOrEmpty(custEmail) &&
                            string.Equals(custEmail, leadEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedCustomer = c;
                            break;
                        }

                        var custPhone = ExtractPhone(c.ContactInfo);
                        var custDigits = !string.IsNullOrEmpty(custPhone) ? Regex.Replace(custPhone, @"\D", "") : "";

                        if (!string.IsNullOrEmpty(cleanPhoneDigits) && cleanPhoneDigits.Length >= 7 &&
                            !string.IsNullOrEmpty(custDigits) && custDigits.Length >= 7)
                        {
                            if (custDigits.EndsWith(cleanPhoneDigits) || cleanPhoneDigits.EndsWith(custDigits))
                            {
                                matchedCustomer = c;
                                break;
                            }
                        }
                    }

                    if (matchedCustomer != null && request?.ForceCreate != true)
                    {
                        // Duplicate found and force flag not set -> 409 Conflict
                        return Conflict(new LeadDuplicateMatchDto
                        {
                            Message = $"A potential duplicate customer was found: {matchedCustomer.CustomerName} ({matchedCustomer.ContactInfo}).",
                            ExistingCustomerId = matchedCustomer.CustomerId,
                            ExistingCustomerName = matchedCustomer.CustomerName,
                            ExistingContactInfo = matchedCustomer.ContactInfo,
                            ExistingEmail = matchedCustomer.Email,
                            ExistingLocation = matchedCustomer.ServiceLocation
                        });
                    }

                    // Create new customer
                    string contactToStore = lead.ContactInfo;
                    if (await _context.Customers.AnyAsync(c => c.ContactInfo == contactToStore))
                    {
                        contactToStore = $"{lead.ContactInfo} #{lead.LeadId}";
                    }

                    var newCustomer = new Customer
                    {
                        CustomerName    = lead.LeadName,
                        CustomerType    = "Individual",
                        ContactInfo     = contactToStore,
                        Email           = !string.IsNullOrEmpty(leadEmail) ? leadEmail : null,
                        ServiceLocation = lead.ServiceAddress ?? string.Empty,
                        LeadId          = lead.LeadId,
                        IsActive        = true,
                        CreatedAt       = DateTime.UtcNow
                    };
                    _context.Customers.Add(newCustomer);
                    await _context.SaveChangesAsync();

                    customerId = newCustomer.CustomerId;
                    customerName = newCustomer.CustomerName;
                    wasExisting = false;
                }

                // Update Lead status and audit trail
                lead.Status = "Converted";
                lead.ConvertedCustomerId = customerId;
                lead.ConvertedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new LeadConvertResultDto
                {
                    CustomerId = customerId,
                    LeadId = lead.LeadId,
                    WasExistingCustomer = wasExisting,
                    CustomerName = customerName,
                    Message = wasExisting
                        ? $"Lead #{lead.LeadId} successfully linked to customer {customerName}."
                        : $"Lead #{lead.LeadId} converted to new customer {customerName}."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Conversion failed: {innerMsg}");
            }
        }

        private static string ExtractPhone(string? contact)
        {
            if (string.IsNullOrWhiteSpace(contact)) return string.Empty;
            if (contact.Contains('|'))
            {
                var parts = contact.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (!p.Contains('@')) return p;
                }
            }
            return !contact.Contains('@') ? contact.Trim() : string.Empty;
        }

        private static string ExtractEmail(string? contact)
        {
            if (string.IsNullOrWhiteSpace(contact)) return string.Empty;
            if (contact.Contains('|'))
            {
                var parts = contact.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (p.Contains('@')) return p.Trim().ToLower();
                }
            }
            return contact.Contains('@') ? contact.Trim().ToLower() : string.Empty;
        }
    }
}
