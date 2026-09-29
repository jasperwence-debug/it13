using CRM.domain.Constants;
using CRM.domain.Dtos.Retention;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/retention")]
    [Authorize]
    public class RetentionController : ControllerBase
    {
        private readonly IRetentionService _retention;
        private readonly ITenantContext _ctx;

        public RetentionController(IRetentionService retention, ITenantContext ctx)
        {
            _retention = retention;
            _ctx = ctx;
        }

        // GET /api/retention/summary?thresholdDays=60
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] int thresholdDays = 60)
        {
            if (thresholdDays < 7) thresholdDays = 7;
            if (thresholdDays > 365) thresholdDays = 365;

            var dto = await _retention.GetSummaryAsync(
                thresholdDays, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // GET /api/retention/at-risk?thresholdDays=60&onlyUnassigned=false
        [HttpGet("at-risk")]
        public async Task<IActionResult> AtRisk(
            [FromQuery] int thresholdDays = 60,
            [FromQuery] bool onlyUnassigned = false)
        {
            if (thresholdDays < 7) thresholdDays = 7;
            if (thresholdDays > 365) thresholdDays = 365;

            var items = await _retention.GetAtRiskAsync(
                thresholdDays, onlyUnassigned, _ctx.UserId, _ctx.Role);
            return Ok(items);
        }

        // GET /api/retention/recovered?lookbackDays=90
        [HttpGet("recovered")]
        public async Task<IActionResult> Recovered([FromQuery] int lookbackDays = 90)
        {
            if (lookbackDays < 7) lookbackDays = 7;
            if (lookbackDays > 365) lookbackDays = 365;

            var items = await _retention.GetRecoveredAsync(
                lookbackDays, _ctx.UserId, _ctx.Role);
            return Ok(items);
        }

        // POST /api/retention/bulk-assign
        [HttpPost("bulk-assign")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> BulkAssign(BulkAssignRequest request)
        {
            if (request.CustomerIds == null || request.CustomerIds.Count == 0)
                return BadRequest(new { message = "At least one customerId is required." });

            try
            {
                int count = await _retention.BulkAssignAsync(
                    request.CustomerIds, request.AssignedUserId);
                return Ok(new { updated = count });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/retention/win-back
        [HttpPost("win-back")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> CreateWinBack(CreateWinBackRequest request)
        {
            if (request.CustomerIds == null || request.CustomerIds.Count == 0)
                return BadRequest(new { message = "At least one customerId is required." });

            try
            {
                var result = await _retention.CreateWinBacksAsync(
                    request.CustomerIds,
                    request.AssignedUserId,
                    request.DueDate,
                    request.Notes,
                    _ctx.UserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}