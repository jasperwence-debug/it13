using CRM.api.Contracts.Activities;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/activities")]
    [Authorize]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _activities;
        private readonly ITenantContext _ctx;

        public ActivityController(IActivityService activities, ITenantContext ctx)
        {
            _activities = activities;
            _ctx = ctx;
        }

        // GET /api/activities?customerId=&leadId=&type=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? leadId,
            [FromQuery] string? type,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            ActivityType? typeEnum = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<ActivityType>(type, ignoreCase: true, out var parsed))
                    return BadRequest(new
                    {
                        message = $"Invalid type '{type}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<ActivityType>())}"
                    });
                typeEnum = parsed;
            }

            var items = await _activities.ListAsync(
                customerId, leadId, typeEnum,
                _ctx.UserId, _ctx.Role, page, pageSize);

            var total = await _activities.CountAsync(
                customerId, leadId, typeEnum, _ctx.UserId, _ctx.Role);

            return Ok(new { page, pageSize, total, items = items.Select(ToDto) });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var a = await _activities.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            if (a == null) return NotFound();
            return Ok(ToDto(a));
        }

        [HttpPost]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Create(CreateActivityRequest request)
        {
            // Business rule: must link to customer or lead
            if (!request.CustomerId.HasValue && !request.LeadId.HasValue)
                return BadRequest(new
                {
                    message = "Activity must be linked to a customer or a lead."
                });

            if (request.CustomerId.HasValue &&
                !await _activities.CustomerExistsAsync(request.CustomerId.Value))
                return BadRequest(new { message = "CustomerId not found." });

            if (request.LeadId.HasValue &&
                !await _activities.LeadExistsAsync(request.LeadId.Value))
                return BadRequest(new { message = "LeadId not found." });

            if (!_ctx.UserId.HasValue)
                return Unauthorized(new { message = "No authenticated user." });

            var entity = new Activity
            {
                CustomerId = request.CustomerId,
                LeadId = request.LeadId,
                UserId = _ctx.UserId.Value,
                ActivityType = request.ActivityType,
                Description = request.Description.Trim(),
                ActivityDate = request.ActivityDate ?? DateTime.UtcNow
            };

            var created = await _activities.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById),
                new { id = created.Id }, ToDto(created));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Update(Guid id, UpdateActivityRequest request)
        {
            var updated = new Activity
            {
                ActivityType = request.ActivityType,
                Description = request.Description.Trim(),
                ActivityDate = request.ActivityDate ?? DateTime.UtcNow
            };

            var result = await _activities.UpdateAsync(id, updated, _ctx.UserId, _ctx.Role);
            if (result == null) return NotFound();
            return Ok(ToDto(result));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _activities.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // Convenience endpoints
        [HttpGet("by-customer/{customerId:guid}")]
        public async Task<IActionResult> ByCustomer(Guid customerId)
        {
            var items = await _activities.ListAsync(
                customerId, null, null,
                _ctx.UserId, _ctx.Role, 1, 100);
            return Ok(items.Select(ToDto));
        }

        [HttpGet("by-lead/{leadId:guid}")]
        public async Task<IActionResult> ByLead(Guid leadId)
        {
            var items = await _activities.ListAsync(
                null, leadId, null,
                _ctx.UserId, _ctx.Role, 1, 100);
            return Ok(items.Select(ToDto));
        }

        private static ActivityDto ToDto(Activity a) => new(
            a.Id,
            a.CustomerId,
            a.Customer != null ? $"{a.Customer.FirstName} {a.Customer.LastName}".Trim() : null,
            a.LeadId,
            a.Lead?.Name,
            a.UserId,
            a.User?.Name ?? "(unknown)",
            a.ActivityType,
            a.Description,
            a.ActivityDate,
            a.CreatedAt);
    }
}