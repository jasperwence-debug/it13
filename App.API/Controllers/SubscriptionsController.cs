using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubscriptionsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // DTOs for Subscriptions
        // ============================================================
        public class SubscriptionResponseDto
        {
            public int SubscriptionId { get; set; }
            public int CompanyId { get; set; }
            public string CompanyCode { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string Tier { get; set; } = string.Empty; // Micro, Small, Medium
            public int TierValue { get; set; }
            public string Status { get; set; } = string.Empty; // Active, Trial, Suspended, Cancelled
            public string BillingCycle { get; set; } = string.Empty; // Monthly, Quarterly, Annual
            public decimal MonthlyPrice { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int MaxUsers { get; set; }
            public int MaxBranches { get; set; }
            public bool HasDataCollection { get; set; }
            public bool HasTransactions { get; set; }
            public bool HasBusinessIntelligence { get; set; }
            public bool HasActions { get; set; }
            public bool HasBranching { get; set; }
            public string? Notes { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class CreateSubscriptionDto
        {
            public int CompanyId { get; set; }
            public int Tier { get; set; } = 1; // 1=Micro, 2=Small, 3=Medium
            public string Status { get; set; } = "Active";
            public string BillingCycle { get; set; } = "Monthly";
            public decimal? MonthlyPrice { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? Notes { get; set; }
        }

        public class UpdateSubscriptionDto
        {
            public int Tier { get; set; } // 1=Micro, 2=Small, 3=Medium
            public string Status { get; set; } = "Active";
            public string BillingCycle { get; set; } = "Monthly";
            public decimal? MonthlyPrice { get; set; }
            public DateTime? EndDate { get; set; }
            public string? Notes { get; set; }
        }

        // ============================================================
        // GET: api/subscriptions
        // Lists all tenant subscriptions with company metadata
        // ============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionResponseDto>>> GetSubscriptions()
        {
            try
            {
                var list = await _context.Subscriptions
                    .Include(s => s.Company)
                    .OrderBy(s => s.Tier)
                    .ThenBy(s => s.SubscriptionId)
                    .Select(s => new SubscriptionResponseDto
                    {
                        SubscriptionId = s.SubscriptionId,
                        CompanyId = s.CompanyId,
                        CompanyCode = s.Company != null ? s.Company.CompanyCode : "",
                        CompanyName = s.Company != null ? s.Company.CompanyName : "Unknown Company",
                        Tier = s.Tier.ToString(),
                        TierValue = (int)s.Tier,
                        Status = s.Status,
                        BillingCycle = s.BillingCycle,
                        MonthlyPrice = s.MonthlyPrice,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        MaxUsers = s.MaxUsers,
                        MaxBranches = s.MaxBranches,
                        HasDataCollection = s.HasDataCollection,
                        HasTransactions = s.HasTransactions,
                        HasBusinessIntelligence = s.HasBusinessIntelligence,
                        HasActions = s.HasActions,
                        HasBranching = s.HasBranching,
                        Notes = s.Notes,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving subscriptions: {ex.Message}" });
            }
        }

        // ============================================================
        // GET: api/subscriptions/{id}
        // ============================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubscriptionResponseDto>> GetSubscription(int id)
        {
            try
            {
                var s = await _context.Subscriptions
                    .Include(x => x.Company)
                    .FirstOrDefaultAsync(x => x.SubscriptionId == id);

                if (s == null)
                    return NotFound(new { message = $"Subscription with ID {id} not found." });

                var dto = new SubscriptionResponseDto
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyId = s.CompanyId,
                    CompanyCode = s.Company?.CompanyCode ?? "",
                    CompanyName = s.Company?.CompanyName ?? "Unknown Company",
                    Tier = s.Tier.ToString(),
                    TierValue = (int)s.Tier,
                    Status = s.Status,
                    BillingCycle = s.BillingCycle,
                    MonthlyPrice = s.MonthlyPrice,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    MaxUsers = s.MaxUsers,
                    MaxBranches = s.MaxBranches,
                    HasDataCollection = s.HasDataCollection,
                    HasTransactions = s.HasTransactions,
                    HasBusinessIntelligence = s.HasBusinessIntelligence,
                    HasActions = s.HasActions,
                    HasBranching = s.HasBranching,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving subscription: {ex.Message}" });
            }
        }

        // ============================================================
        // GET: api/subscriptions/company/{companyId}
        // ============================================================
        [HttpGet("company/{companyId:int}")]
        public async Task<ActionResult<SubscriptionResponseDto>> GetSubscriptionByCompany(int companyId)
        {
            try
            {
                var s = await _context.Subscriptions
                    .Include(x => x.Company)
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId);

                if (s == null)
                    return NotFound(new { message = $"No active subscription found for company ID {companyId}." });

                var dto = new SubscriptionResponseDto
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyId = s.CompanyId,
                    CompanyCode = s.Company?.CompanyCode ?? "",
                    CompanyName = s.Company?.CompanyName ?? "Unknown Company",
                    Tier = s.Tier.ToString(),
                    TierValue = (int)s.Tier,
                    Status = s.Status,
                    BillingCycle = s.BillingCycle,
                    MonthlyPrice = s.MonthlyPrice,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    MaxUsers = s.MaxUsers,
                    MaxBranches = s.MaxBranches,
                    HasDataCollection = s.HasDataCollection,
                    HasTransactions = s.HasTransactions,
                    HasBusinessIntelligence = s.HasBusinessIntelligence,
                    HasActions = s.HasActions,
                    HasBranching = s.HasBranching,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving company subscription: {ex.Message}" });
            }
        }

        // ============================================================
        // POST: api/subscriptions
        // Creates a new tenant subscription
        // ============================================================
        [HttpPost]
        public async Task<ActionResult<SubscriptionResponseDto>> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            if (dto.CompanyId <= 0)
                return BadRequest(new { message = "Valid Company ID is required." });

            var companyExists = await _context.Companies.AnyAsync(c => c.CompanyId == dto.CompanyId);
            if (!companyExists)
                return BadRequest(new { message = $"Company with ID {dto.CompanyId} does not exist." });

            var existing = await _context.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);
            if (existing != null)
                return BadRequest(new { message = $"Company {dto.CompanyId} already has an active subscription plan." });

            if (!Enum.IsDefined(typeof(SubscriptionTier), dto.Tier))
                return BadRequest(new { message = "Invalid subscription tier specified (1=Micro, 2=Small, 3=Medium)." });

            try
            {
                var tier = (SubscriptionTier)dto.Tier;
                var sub = new Subscription
                {
                    CompanyId = dto.CompanyId,
                    Tier = tier,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
                    BillingCycle = string.IsNullOrWhiteSpace(dto.BillingCycle) ? "Monthly" : dto.BillingCycle.Trim(),
                    StartDate = dto.StartDate ?? DateTime.UtcNow,
                    EndDate = dto.EndDate ?? DateTime.UtcNow.AddMonths(1),
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                sub.ApplyTierDefaults(tier);
                if (dto.MonthlyPrice.HasValue && dto.MonthlyPrice.Value > 0)
                {
                    sub.MonthlyPrice = dto.MonthlyPrice.Value;
                }

                _context.Subscriptions.Add(sub);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSubscription), new { id = sub.SubscriptionId }, new SubscriptionResponseDto
                {
                    SubscriptionId = sub.SubscriptionId,
                    CompanyId = sub.CompanyId,
                    Tier = sub.Tier.ToString(),
                    TierValue = (int)sub.Tier,
                    Status = sub.Status,
                    BillingCycle = sub.BillingCycle,
                    MonthlyPrice = sub.MonthlyPrice,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate,
                    MaxUsers = sub.MaxUsers,
                    MaxBranches = sub.MaxBranches,
                    HasDataCollection = sub.HasDataCollection,
                    HasTransactions = sub.HasTransactions,
                    HasBusinessIntelligence = sub.HasBusinessIntelligence,
                    HasActions = sub.HasActions,
                    HasBranching = sub.HasBranching,
                    Notes = sub.Notes,
                    CreatedAt = sub.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating subscription: {ex.Message}" });
            }
        }

        // ============================================================
        // PUT: api/subscriptions/{id}
        // Upgrades/Downgrades tier, modifies status or billing cycle
        // ============================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionDto dto)
        {
            if (!Enum.IsDefined(typeof(SubscriptionTier), dto.Tier))
                return BadRequest(new { message = "Invalid subscription tier specified (1=Micro, 2=Small, 3=Medium)." });

            try
            {
                var sub = await _context.Subscriptions.FindAsync(id);
                if (sub == null)
                    return NotFound(new { message = $"Subscription with ID {id} not found." });

                var newTier = (SubscriptionTier)dto.Tier;
                bool tierChanged = sub.Tier != newTier;

                sub.ApplyTierDefaults(newTier);

                if (!string.IsNullOrWhiteSpace(dto.Status))
                    sub.Status = dto.Status.Trim();

                if (!string.IsNullOrWhiteSpace(dto.BillingCycle))
                    sub.BillingCycle = dto.BillingCycle.Trim();

                if (dto.MonthlyPrice.HasValue && dto.MonthlyPrice.Value > 0)
                    sub.MonthlyPrice = dto.MonthlyPrice.Value;

                if (dto.EndDate.HasValue)
                    sub.EndDate = dto.EndDate.Value;

                if (dto.Notes != null)
                    sub.Notes = dto.Notes;

                sub.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating subscription: {ex.Message}" });
            }
        }

        // ============================================================
        // DELETE: api/subscriptions/{id}
        // ============================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            try
            {
                var sub = await _context.Subscriptions.FindAsync(id);
                if (sub == null)
                    return NotFound(new { message = $"Subscription with ID {id} not found." });

                _context.Subscriptions.Remove(sub);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting subscription: {ex.Message}" });
            }
        }
    }
}
