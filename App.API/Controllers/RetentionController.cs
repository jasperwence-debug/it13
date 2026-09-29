using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Domain.Models.Email;
using App.Infrastructure;
using App.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/retention")]
    public class RetentionController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<RetentionController> _logger;

        public RetentionController(
            IEmailService emailService,
            AppDbContext context,
            ILogger<RetentionController> logger)
        {
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // GET /api/retention/smtp-status
        // Safe diagnostics on SMTP transport configuration
        // ============================================================
        [HttpGet("smtp-status")]
        public IActionResult GetSmtpStatus()
        {
            var settings = _emailService.GetSettings();
            return Ok(new
            {
                host = settings.Host,
                port = settings.Port,
                enableSsl = settings.EnableSsl,
                userName = settings.UserName,
                fromEmail = settings.FromEmail,
                fromName = settings.FromName,
                timeoutSeconds = settings.TimeoutSeconds,
                isValid = settings.IsValid()
            });
        }

        // ============================================================
        // POST /api/retention/send-winback
        // Sends an individual win-back email via SMTP
        // ============================================================
        [HttpPost("send-winback")]
        public async Task<IActionResult> SendWinBackEmail([FromBody] WinBackEmailRequest request)
        {
            if (request == null)
                return BadRequest(EmailResult.Failed(string.Empty, "Request body cannot be null."));

            // If CustomerId provided, enrich details from database if missing
            if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId.Value && c.IsActive);

                if (customer != null)
                {
                    if (string.IsNullOrWhiteSpace(request.RecipientName))
                    {
                        request.RecipientName = customer.CustomerName;
                    }

                    if (string.IsNullOrWhiteSpace(request.RecipientEmail))
                    {
                        request.RecipientEmail = CustomerController.ExtractEmail(customer.Email, customer.ContactInfo);
                    }

                    if (string.IsNullOrWhiteSpace(request.ServiceLocation))
                    {
                        request.ServiceLocation = customer.ServiceLocation;
                    }

                    // Look up latest completed service if days inactive is not provided
                    if (!request.DaysInactive.HasValue)
                    {
                        var latestSr = await _context.ServiceRequests
                            .AsNoTracking()
                            .Where(sr => sr.CustomerId == customer.CustomerId && sr.IsActive && sr.Status == "Completed")
                            .OrderByDescending(sr => sr.PreferredDate)
                            .FirstOrDefaultAsync();

                        if (latestSr != null)
                        {
                            var serviceDate = latestSr.PreferredDate != default ? latestSr.PreferredDate : latestSr.BookingDate;
                            request.DaysInactive = Math.Max(0, (int)(DateTime.UtcNow - serviceDate).TotalDays);
                            request.LastServiceType ??= latestSr.RequestedService;
                        }
                        else
                        {
                            request.DaysInactive = 30;
                            request.LastServiceType ??= "General Cleaning";
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(request.RecipientEmail))
            {
                return BadRequest(EmailResult.Failed(string.Empty, "Recipient email address is required.",
                    "No valid email address was provided or found in the customer's profile."));
            }

            _logger.LogInformation("Processing Win-Back email request for Customer: {Name} ({Email}), Promo: {Promo}",
                request.RecipientName, request.RecipientEmail, request.PromoCode);

            var result = await _emailService.SendWinBackEmailAsync(request);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Win-Back email sent successfully to {Email}", request.RecipientEmail);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Win-Back email failed for {Email}: {Error}", request.RecipientEmail, result.Message);
                return StatusCode(502, result); // Bad Gateway: upstream SMTP server failure
            }
        }

        // ============================================================
        // POST /api/retention/batch-winback
        // Dispatches win-back emails to multiple qualifying customer accounts
        // ============================================================
        [HttpPost("batch-winback")]
        public async Task<IActionResult> BatchWinBack([FromBody] WinBackBatchRequest batchRequest)
        {
            if (batchRequest == null || batchRequest.CustomerIds == null || batchRequest.CustomerIds.Count == 0)
            {
                return BadRequest(new WinBackBatchResult
                {
                    TotalRequested = 0,
                    Results = new List<EmailResult> { EmailResult.Failed(string.Empty, "No customer IDs provided in batch.") }
                });
            }

            var uniqueIds = batchRequest.CustomerIds.Distinct().ToList();
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => uniqueIds.Contains(c.CustomerId) && c.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var requests = new List<WinBackEmailRequest>();

            foreach (var c in customers)
            {
                var email = CustomerController.ExtractEmail(c.Email, c.ContactInfo);
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue; // Skip customers without any reachable email
                }

                var latestSr = await _context.ServiceRequests
                    .AsNoTracking()
                    .Where(sr => sr.CustomerId == c.CustomerId && sr.IsActive && sr.Status == "Completed")
                    .OrderByDescending(sr => sr.PreferredDate)
                    .FirstOrDefaultAsync();

                int daysInactive = 30;
                string lastService = "Professional Cleaning";

                if (latestSr != null)
                {
                    var date = latestSr.PreferredDate != default ? latestSr.PreferredDate : latestSr.BookingDate;
                    daysInactive = Math.Max(0, (int)(now - date).TotalDays);
                    lastService = latestSr.RequestedService;
                }

                requests.Add(new WinBackEmailRequest
                {
                    CustomerId = c.CustomerId,
                    RecipientName = c.CustomerName,
                    RecipientEmail = email,
                    Subject = batchRequest.Subject ?? $"We Miss You at CleanPro! Here's {batchRequest.DiscountPercentage}% Off Your Next Clean 🎁",
                    PromoCode = batchRequest.PromoCode,
                    DiscountPercentage = batchRequest.DiscountPercentage,
                    CampaignType = batchRequest.CampaignType,
                    CustomMessage = batchRequest.CustomMessage,
                    DaysInactive = daysInactive,
                    LastServiceType = lastService,
                    ServiceLocation = c.ServiceLocation
                });
            }

            if (requests.Count == 0)
            {
                return Ok(new WinBackBatchResult
                {
                    TotalRequested = uniqueIds.Count,
                    TotalSent = 0,
                    TotalFailed = uniqueIds.Count,
                    Results = new List<EmailResult>
                    {
                        EmailResult.Failed(string.Empty, "None of the selected customers have a valid email address on file.")
                    }
                });
            }

            var batchResult = await _emailService.SendBatchWinBackEmailsAsync(requests);
            return Ok(batchResult);
        }

        // ============================================================
        // POST /api/retention/test-email
        // Quick verification endpoint for IT/Admin to test SMTP credentials
        // ============================================================
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RecipientEmail))
                return BadRequest("Recipient email is required.");

            var result = await _emailService.TestConnectionAsync(dto.RecipientEmail.Trim());
            return result.IsSuccess ? Ok(result) : StatusCode(502, result);
        }

        public class TestEmailDto
        {
            public string RecipientEmail { get; set; } = string.Empty;
        }
    }
}
