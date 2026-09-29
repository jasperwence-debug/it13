using CRM.api.Contracts.FollowUps;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/follow-ups")]
    [Authorize]
    public class FollowUpController : ControllerBase
    {
        private readonly IFollowUpService _followUps;
        private readonly ITenantContext _ctx;

        public FollowUpController(IFollowUpService followUps, ITenantContext ctx)
        {
            _followUps = followUps;
            _ctx = ctx;
        }

        // GET /api/follow-ups?customerId=&leadId=&status=&overdue=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? leadId,
            [FromQuery] string? status,
            [FromQuery] bool? overdue,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            FollowUpStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<FollowUpStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest(new
                    {
                        message = $"Invalid status '{status}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<FollowUpStatus>())}"
                    });
                statusEnum = parsed;
            }

            var items = await _followUps.ListAsync(
                customerId, leadId, statusEnum, overdue,
                _ctx.UserId, _ctx.Role, page, pageSize);

            var total = await _followUps.CountAsync(
                customerId, leadId, statusEnum, overdue, _ctx.UserId, _ctx.Role);

            return Ok(new { page, pageSize, total, items = items.Select(ToDto) });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var f = await _followUps.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            if (f == null) return NotFound();
            return Ok(ToDto(f));
        }

        [HttpPost]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Create(CreateFollowUpRequest request)
        {
            if (!request.CustomerId.HasValue && !request.LeadId.HasValue)
                return BadRequest(new
                {
                    message = "Follow-up must be linked to a customer or a lead."
                });

            if (request.CustomerId.HasValue &&
                !await _followUps.CustomerExistsAsync(request.CustomerId.Value))
                return BadRequest(new { message = "CustomerId not found." });

            if (request.LeadId.HasValue &&
                !await _followUps.LeadExistsAsync(request.LeadId.Value))
                return BadRequest(new { message = "LeadId not found." });

            // Business rule: due date cannot be in the past
            if (request.DueDate.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "DueDate cannot be in the past." });

            // Resolve assignee — defaults to caller
            var assignee = request.AssignedUserId ?? _ctx.UserId
                ?? throw new UnauthorizedAccessException("No authenticated user.");

            if (request.AssignedUserId.HasValue)
            {
                var user = await _followUps.FindUserAsync(request.AssignedUserId.Value);
                if (user == null)
                    return BadRequest(new { message = "AssignedUserId not found." });
            }

            var entity = new FollowUp
            {
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                UserId = assignee,
                Title = request.Title.Trim(),
                Description = (request.Description ?? "").Trim(),
                DueDate = request.DueDate,
                Status = FollowUpStatus.Pending
            };

            var created = await _followUps.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById),
                new { id = created.Id }, ToDto(created));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Update(Guid id, UpdateFollowUpRequest request)
        {
            try
            {
                var updated = new FollowUp
                {
                    Title = request.Title.Trim(),
                    Description = (request.Description ?? "").Trim(),
                    DueDate = request.DueDate
                };

                var result = await _followUps.UpdateAsync(id, updated, _ctx.UserId, _ctx.Role);
                if (result == null) return NotFound();
                return Ok(ToDto(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> UpdateStatus(Guid id, CompleteFollowUpRequest request)
        {
            var result = await _followUps.UpdateStatusAsync(
                id, request.Status, _ctx.UserId, _ctx.Role);
            if (result == null) return NotFound();
            return Ok(ToDto(result));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _followUps.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // GET /api/follow-ups/overdue — convenience
        [HttpGet("overdue")]
        public async Task<IActionResult> Overdue()
        {
            var items = await _followUps.ListAsync(
                null, null, null, overdueOnly: true,
                _ctx.UserId, _ctx.Role, 1, 100);
            return Ok(items.Select(ToDto));
        }

        // ─── Mapping ──────────────────────────────────────────
        private static FollowUpDto ToDto(FollowUp f)
        {
            var isOverdue = f.DueDate < DateTime.UtcNow
                          && f.Status != FollowUpStatus.Completed
                          && f.Status != FollowUpStatus.Cancelled;

            return new FollowUpDto(
                f.Id,
                f.CustomerId,
                f.Customer != null
                    ? $"{f.Customer.FirstName} {f.Customer.LastName}".Trim()
                    : null,
                f.LeadId,
                f.Lead?.Name,
                f.UserId,
                f.User?.Name ?? "(unknown)",
                f.Title,
                f.Description,
                f.DueDate,
                f.Status,
                isOverdue,
                f.CreatedAt,
                f.UpdatedAt);
        }
    }
}