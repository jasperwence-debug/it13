using CRM.api.Contracts.Customers;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customers;
        private readonly ITenantContext _ctx;

        public CustomerController(
            ICustomerService customers,
            ITenantContext ctx)
        {
            _customers = customers;
            _ctx = ctx;
        }

        // ─────────────────────────────────────────────────────
        // GET /api/customers?search=&status=
        // ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? status)
        {
            var rows = await _customers.ListAsync(
                search, status, _ctx.UserId, _ctx.Role);

            return Ok(rows.Select(ToDto));
        }

        // ─────────────────────────────────────────────────────
        // GET /api/customers/{id}
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var customer = await _customers.GetByIdAsync(
                id, _ctx.UserId, _ctx.Role);

            if (customer == null) return NotFound();
            return Ok(ToDto(customer));
        }

        // ─────────────────────────────────────────────────────
        // POST /api/customers
        // ─────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Create(CreateCustomerRequest request)
        {
            // Optional: validate assigned user exists
            if (request.AssignedUserId.HasValue)
            {
                var assigned = await _customers.FindUserAsync(
                    request.AssignedUserId.Value);
                if (assigned == null)
                    return BadRequest(new
                    {
                        message = "AssignedUserId does not match any user."
                    });
            }

            var entity = new Customer
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = request.Phone.Trim(),
                Company = request.Company.Trim(),
                Address = request.Address.Trim(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),

                //  Auto-assign to caller if:
                //   - no assignee was provided, AND
                //   - the caller is SalesStaff (so they see it immediately)
                AssignedUserId = request.AssignedUserId
        ?? (string.Equals(_ctx.Role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase)
            ? _ctx.UserId
            : null)
            };

            var created = await _customers.CreateAsync(entity);
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ToDto(created));
        }

        // ─────────────────────────────────────────────────────
        // PUT /api/customers/{id}
        // ─────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Update(
            Guid id, UpdateCustomerRequest request)
        {
            var updated = new Customer
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = request.Phone.Trim(),
                Company = request.Company.Trim(),
                Address = request.Address.Trim(),
                Status = request.Status.Trim()
            };

            var result = await _customers.UpdateAsync(
                id, updated, _ctx.UserId, _ctx.Role);

            if (result == null) return NotFound();
            return Ok(ToDto(result));
        }

        // ─────────────────────────────────────────────────────
        // DELETE /api/customers/{id}  (Admin & SuperAdmin only)
        // ─────────────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _customers.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/customers/{id}/assign  (Manager+)
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/assign")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Assign(
            Guid id, AssignCustomerRequest request)
        {
            var assignedUser = await _customers.FindUserAsync(request.AssignedUserId);
            if (assignedUser == null)
                return BadRequest(new
                {
                    message = "AssignedUserId does not match any user."
                });

            var customer = await _customers.AssignAsync(id, request.AssignedUserId);
            if (customer == null) return NotFound();

            // Reload with AssignedUser included
            var refreshed = await _customers.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            return Ok(ToDto(refreshed!));
        }

        // ─── Mapping helper ───────────────────────────────────
        private static CustomerDto ToDto(Customer c) => new(
        c.Id,
        c.FirstName,
        c.LastName,
        c.Email,
        c.Phone,
        c.Company,
        c.Address,
        c.Status,
        c.AssignedUserId,
        c.AssignedUser?.Name,
        c.CreatedAt,
        c.UpdatedAt,
        c.LastActivityAt,
        c.DaysSinceLastActivity,
        c.ChurnRisk);
    }
}