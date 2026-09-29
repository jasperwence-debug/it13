using CRM.domain.Constants;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly ITenantContext _ctx;

        public ReportController(IReportService reports, ITenantContext ctx)
        {
            _reports = reports;
            _ctx = ctx;
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/overview?from=&to=
        // ─────────────────────────────────────────────────────
        [HttpGet("overview")]
        public async Task<IActionResult> Overview(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetOverviewAsync(f, t, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/pipeline?from=&to=
        // ─────────────────────────────────────────────────────
        [HttpGet("pipeline")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Pipeline(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetPipelineAsync(f, t, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/activity?from=&to=
        // ─────────────────────────────────────────────────────
        [HttpGet("activity")]
        public async Task<IActionResult> Activity(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetActivityAsync(f, t, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/retention?from=&to=&atRiskDays=
        // ─────────────────────────────────────────────────────
        [HttpGet("retention")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Retention(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int atRiskDays = 60)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetRetentionAsync(
                f, t, _ctx.UserId, _ctx.Role, atRiskDays);
            return Ok(dto);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/conversion?from=&to=
        // ─────────────────────────────────────────────────────
        [HttpGet("conversion")]
        public async Task<IActionResult> Conversion(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetConversionAsync(f, t, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/reports/team-performance?from=&to=
        // ─────────────────────────────────────────────────────
        [HttpGet("team-performance")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> TeamPerformance(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (f, t) = ResolveRange(from, to);
            var dto = await _reports.GetTeamPerformanceAsync(
                f, t, _ctx.UserId, _ctx.Role);
            return Ok(dto);
        }

        // ─── Helper ───────────────────────────────────────────
        // Default range: last 30 days if not specified
        private static (DateTime from, DateTime to) ResolveRange(DateTime? from, DateTime? to)
        {
            var t = to?.Date ?? DateTime.UtcNow.Date;
            var f = from?.Date ?? t.AddDays(-30);
            return (f, t.AddDays(1));   // inclusive of the "to" day
        }
    }
}