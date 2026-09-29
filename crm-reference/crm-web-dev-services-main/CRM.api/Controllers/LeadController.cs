using CRM.api.Contracts.Leads;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    public class LeadController : ControllerBase
    {
        private readonly ILeadService _leads;
        private readonly ITenantContext _ctx;

        public LeadController(ILeadService leads, ITenantContext ctx)
        {
            _leads = leads;
            _ctx = ctx;
        }

        // ─────────────────────────────────────────────────────
        // GET /api/leads?search=&status=&priority=&page=&pageSize=
        // ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // ─── Parse enum strings with clear error messages ───
            LeadStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<LeadStatus>(status, ignoreCase: true, out var parsedStatus))
                    return BadRequest(new
                    {
                        message = $"Invalid status '{status}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<LeadStatus>())}"
                    });
                statusEnum = parsedStatus;
            }

            LeadPriority? priorityEnum = null;
            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (!Enum.TryParse<LeadPriority>(priority, ignoreCase: true, out var parsedPriority))
                    return BadRequest(new
                    {
                        message = $"Invalid priority '{priority}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<LeadPriority>())}"
                    });
                priorityEnum = parsedPriority;
            }

            var items = await _leads.ListAsync(
                search, statusEnum, priorityEnum,
                _ctx.UserId, _ctx.Role, page, pageSize);

            var total = await _leads.CountAsync(
                search, statusEnum, priorityEnum, _ctx.UserId, _ctx.Role);

            return Ok(new
            {
                page,
                pageSize,
                total,
                items = items.Select(ToDto)
            });
        }

        // ─────────────────────────────────────────────────────
        // GET /api/leads/{id}
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var lead = await _leads.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            if (lead == null) return NotFound();
            return Ok(ToDto(lead));
        }

        // ─────────────────────────────────────────────────────
        // POST /api/leads
        // ─────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Create(CreateLeadRequest request)
        {
            if (request.AssignedUserId.HasValue)
            {
                var user = await _leads.FindUserAsync(request.AssignedUserId.Value);
                if (user == null)
                    return BadRequest(new { message = "AssignedUserId not found." });
            }

            var entity = new Lead
            {
                Name = request.Name.Trim(),
                Email = (request.Email ?? "").Trim().ToLowerInvariant(),
                Phone = (request.Phone ?? "").Trim(),
                Source = (request.Source ?? "").Trim(),
                Status = request.Status,
                Priority = request.Priority,
                ExpectedValue = request.ExpectedValue,
                Notes = (request.Notes ?? "").Trim(),

                // Auto-assign to caller if SalesStaff and no explicit assignee
                AssignedUserId = request.AssignedUserId
                    ?? (string.Equals(_ctx.Role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase)
                        ? _ctx.UserId
                        : null)
            };

            var created = await _leads.CreateAsync(entity);
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ToDto(created));
        }

        // ─────────────────────────────────────────────────────
        // PUT /api/leads/{id}
        // ─────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Update(Guid id, UpdateLeadRequest request)
        {
            var updated = new Lead
            {
                Name = request.Name.Trim(),
                Email = (request.Email ?? "").Trim().ToLowerInvariant(),
                Phone = (request.Phone ?? "").Trim(),
                Source = (request.Source ?? "").Trim(),
                Status = request.Status,
                Priority = request.Priority,
                ExpectedValue = request.ExpectedValue,
                Notes = (request.Notes ?? "").Trim()
            };

            var result = await _leads.UpdateAsync(id, updated, _ctx.UserId, _ctx.Role);
            if (result == null) return NotFound();
            return Ok(ToDto(result));
        }

        // ─────────────────────────────────────────────────────
        // DELETE /api/leads/{id}  (Admin+)
        // ─────────────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _leads.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/leads/{id}/status
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> UpdateStatus(
            Guid id, UpdateLeadStatusRequest request)
        {
            var lead = await _leads.UpdateStatusAsync(
                id, request.Status, _ctx.UserId, _ctx.Role);

            if (lead == null) return NotFound();
            return Ok(ToDto(lead));
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/leads/{id}/assign  (Manager+)
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/assign")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Assign(Guid id, AssignLeadRequest request)
        {
            var user = await _leads.FindUserAsync(request.AssignedUserId);
            if (user == null)
                return BadRequest(new { message = "AssignedUserId not found." });

            var lead = await _leads.AssignAsync(id, request.AssignedUserId);
            if (lead == null) return NotFound();

            var refreshed = await _leads.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            return Ok(ToDto(refreshed!));
        }

        // ─────────────────────────────────────────────────────
        // POST /api/leads/{id}/convert
        // Only "Won" leads can be converted.
        // ─────────────────────────────────────────────────────
        [HttpPost("{id:guid}/convert")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Convert(Guid id)
        {
            try
            {
                var result = await _leads.ConvertAsync(id, _ctx.UserId, _ctx.Role);
                if (result == null) return NotFound();

                var (lead, customer) = result.Value;

                return Ok(new ConvertLeadResponse(
                    lead.Id,
                    customer.Id,
                    $"{customer.FirstName} {customer.LastName}".Trim(),
                    customer.Email));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─── Mapping ──────────────────────────────────────────
        private static LeadDto ToDto(Lead l) => new(
            l.Id,
            l.CustomerId,
            l.Name,
            l.Email,
            l.Phone,
            l.Source,
            l.Status,
            l.Priority,
            l.ExpectedValue,
            l.Notes,
            l.AssignedUserId,
            l.AssignedUser?.Name,
            l.CreatedAt,
            l.UpdatedAt);
    }
}