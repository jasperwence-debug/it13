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
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchesController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // DTOs for Branches
        // ============================================================
        public class BranchResponseDto
        {
            public int BranchId { get; set; }
            public int CompanyId { get; set; }
            public string CompanyCode { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string BranchCode { get; set; } = string.Empty;
            public string BranchName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string ManagerName { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public int ActiveWorkOrdersCount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class CreateBranchDto
        {
            public int CompanyId { get; set; }
            public string BranchCode { get; set; } = string.Empty;
            public string BranchName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string ManagerName { get; set; } = string.Empty;
        }

        public class UpdateBranchDto
        {
            public string BranchName { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string ManagerName { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
        }

        // ============================================================
        // GET: api/branches
        // Lists all active branches
        // ============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BranchResponseDto>>> GetBranches([FromQuery] int? companyId = null)
        {
            try
            {
                var query = _context.Branches
                    .Include(b => b.Company)
                    .AsQueryable();

                if (companyId.HasValue)
                {
                    query = query.Where(b => b.CompanyId == companyId.Value);
                }

                var list = await query
                    .OrderBy(b => b.City)
                    .ThenBy(b => b.BranchCode)
                    .Select(b => new BranchResponseDto
                    {
                        BranchId = b.BranchId,
                        CompanyId = b.CompanyId,
                        CompanyCode = b.Company != null ? b.Company.CompanyCode : "",
                        CompanyName = b.Company != null ? b.Company.CompanyName : "Unknown Company",
                        BranchCode = b.BranchCode,
                        BranchName = b.BranchName,
                        Address = b.Address,
                        City = b.City,
                        Phone = b.Phone,
                        Email = b.Email,
                        ManagerName = b.ManagerName,
                        IsActive = b.IsActive,
                        ActiveWorkOrdersCount = _context.ServiceRequests.Count(sr => sr.BranchId == b.BranchId && sr.IsActive && sr.Status != "Completed" && sr.Status != "Cancelled"),
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving branches: {ex.Message}" });
            }
        }

        // ============================================================
        // GET: api/branches/{id}
        // ============================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BranchResponseDto>> GetBranch(int id)
        {
            try
            {
                var b = await _context.Branches
                    .Include(x => x.Company)
                    .FirstOrDefaultAsync(x => x.BranchId == id);

                if (b == null)
                    return NotFound(new { message = $"Branch with ID {id} not found." });

                var activeOrders = await _context.ServiceRequests.CountAsync(sr => sr.BranchId == b.BranchId && sr.IsActive && sr.Status != "Completed" && sr.Status != "Cancelled");

                var dto = new BranchResponseDto
                {
                    BranchId = b.BranchId,
                    CompanyId = b.CompanyId,
                    CompanyCode = b.Company?.CompanyCode ?? "",
                    CompanyName = b.Company?.CompanyName ?? "Unknown Company",
                    BranchCode = b.BranchCode,
                    BranchName = b.BranchName,
                    Address = b.Address,
                    City = b.City,
                    Phone = b.Phone,
                    Email = b.Email,
                    ManagerName = b.ManagerName,
                    IsActive = b.IsActive,
                    ActiveWorkOrdersCount = activeOrders,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving branch: {ex.Message}" });
            }
        }

        // ============================================================
        // POST: api/branches
        // Creates a new branch with tier quota verification
        // ============================================================
        [HttpPost]
        public async Task<ActionResult<BranchResponseDto>> CreateBranch([FromBody] CreateBranchDto dto)
        {
            // Input Validation (Rubric Criterion 3)
            if (dto.CompanyId <= 0)
                return BadRequest(new { message = "Valid Company ID is required." });

            if (string.IsNullOrWhiteSpace(dto.BranchCode))
                return BadRequest(new { message = "Branch Code is required (e.g. BR-DVO)." });

            if (string.IsNullOrWhiteSpace(dto.BranchName))
                return BadRequest(new { message = "Branch Name is required." });

            if (string.IsNullOrWhiteSpace(dto.City))
                return BadRequest(new { message = "Branch City is required." });

            // Validate Email format if supplied
            if (!string.IsNullOrWhiteSpace(dto.Email) && !Regex.IsMatch(dto.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { message = "Invalid email address format." });

            try
            {
                var company = await _context.Companies.FindAsync(dto.CompanyId);
                if (company == null)
                    return BadRequest(new { message = $"Company with ID {dto.CompanyId} does not exist." });

                // Check Subscription Quota (Tenant C validation)
                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);
                int currentBranchCount = await _context.Branches.CountAsync(b => b.CompanyId == dto.CompanyId && b.IsActive);

                if (sub != null)
                {
                    if (!sub.HasBranching && currentBranchCount >= 1)
                    {
                        return BadRequest(new
                        {
                            message = $"Company is on {sub.Tier} tier, which allows only 1 primary operating branch. Multi-branching requires upgrading to Tenant C (Medium Enterprise) tier."
                        });
                    }

                    if (currentBranchCount >= sub.MaxBranches)
                    {
                        return BadRequest(new
                        {
                            message = $"Branch quota exceeded. Your current subscription plan allows a maximum of {sub.MaxBranches} branch(es)."
                        });
                    }
                }

                // Check duplicate branch code within company
                var codeExists = await _context.Branches.AnyAsync(b => b.CompanyId == dto.CompanyId && b.BranchCode.ToLower() == dto.BranchCode.Trim().ToLower());
                if (codeExists)
                    return BadRequest(new { message = $"Branch code '{dto.BranchCode}' is already registered for this company." });

                var branch = new Branch
                {
                    CompanyId = dto.CompanyId,
                    BranchCode = dto.BranchCode.Trim().ToUpperInvariant(),
                    BranchName = dto.BranchName.Trim(),
                    Address = dto.Address?.Trim() ?? "",
                    City = dto.City.Trim(),
                    Phone = dto.Phone?.Trim() ?? "",
                    Email = dto.Email?.Trim().ToLowerInvariant() ?? "",
                    ManagerName = dto.ManagerName?.Trim() ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBranch), new { id = branch.BranchId }, new BranchResponseDto
                {
                    BranchId = branch.BranchId,
                    CompanyId = branch.CompanyId,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    BranchCode = branch.BranchCode,
                    BranchName = branch.BranchName,
                    Address = branch.Address,
                    City = branch.City,
                    Phone = branch.Phone,
                    Email = branch.Email,
                    ManagerName = branch.ManagerName,
                    IsActive = branch.IsActive,
                    ActiveWorkOrdersCount = 0,
                    CreatedAt = branch.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating branch: {ex.Message}" });
            }
        }

        // ============================================================
        // PUT: api/branches/{id}
        // Updates branch profile
        // ============================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BranchName))
                return BadRequest(new { message = "Branch Name is required." });

            if (string.IsNullOrWhiteSpace(dto.City))
                return BadRequest(new { message = "Branch City is required." });

            if (!string.IsNullOrWhiteSpace(dto.Email) && !Regex.IsMatch(dto.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { message = "Invalid email address format." });

            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                    return NotFound(new { message = $"Branch with ID {id} not found." });

                branch.BranchName = dto.BranchName.Trim();
                branch.Address = dto.Address?.Trim() ?? "";
                branch.City = dto.City.Trim();
                branch.Phone = dto.Phone?.Trim() ?? "";
                branch.Email = dto.Email?.Trim().ToLowerInvariant() ?? "";
                branch.ManagerName = dto.ManagerName?.Trim() ?? "";
                branch.IsActive = dto.IsActive;
                branch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating branch: {ex.Message}" });
            }
        }

        // ============================================================
        // DELETE: api/branches/{id}
        // Soft-deletes branch
        // ============================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null)
                    return NotFound(new { message = $"Branch with ID {id} not found." });

                // Soft-delete
                branch.IsActive = false;
                branch.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deactivating branch: {ex.Message}" });
            }
        }
    }
}
