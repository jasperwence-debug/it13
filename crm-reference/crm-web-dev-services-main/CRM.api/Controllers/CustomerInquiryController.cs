using CRM.api.Contracts.CustomerInquiries;
using CRM.domain.Constants;
using CRM.domain.Entities;
using CRM.domain.Enums;
using CRM.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers
{
    [ApiController]
    [Route("api/customer-inquiries")]
    [Authorize]
    public class CustomerInquiryController : ControllerBase
    {
        private readonly ICustomerInquiryService _inquiries;
        private readonly ITenantContext _ctx;

        public CustomerInquiryController(
            ICustomerInquiryService inquiries,
            ITenantContext ctx)
        {
            _inquiries = inquiries;
            _ctx = ctx;
        }

        // ─────────────────────────────────────────────────────
        // GET /api/customer-inquiries?customerId=&type=&status=&page=&pageSize=
        // ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? customerId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            CustomerInquiryType? typeEnum = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<CustomerInquiryType>(type, ignoreCase: true, out var parsed))
                    return BadRequest(new
                    {
                        message = $"Invalid type '{type}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<CustomerInquiryType>())}"
                    });
                typeEnum = parsed;
            }

            CustomerInquiryStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<CustomerInquiryStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest(new
                    {
                        message = $"Invalid status '{status}'. " +
                                  $"Valid values: {string.Join(", ", Enum.GetNames<CustomerInquiryStatus>())}"
                    });
                statusEnum = parsed;
            }

            var items = await _inquiries.ListAsync(
                customerId, typeEnum, statusEnum,
                _ctx.UserId, _ctx.Role, page, pageSize);

            var total = await _inquiries.CountAsync(
                customerId, typeEnum, statusEnum,
                _ctx.UserId, _ctx.Role);

            return Ok(new
            {
                page,
                pageSize,
                total,
                items = items.Select(ToDto)
            });
        }

        // ─────────────────────────────────────────────────────
        // GET /api/customer-inquiries/{id}
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var inquiry = await _inquiries.GetByIdAsync(id, _ctx.UserId, _ctx.Role);
            if (inquiry == null) return NotFound();
            return Ok(ToDto(inquiry));
        }

        // ─────────────────────────────────────────────────────
        // GET /api/customer-inquiries/by-customer/{customerId}
        // ─────────────────────────────────────────────────────
        [HttpGet("by-customer/{customerId:guid}")]
        public async Task<IActionResult> ByCustomer(Guid customerId)
        {
            var items = await _inquiries.ListAsync(
                customerId, null, null,
                _ctx.UserId, _ctx.Role, 1, 100);
            return Ok(items.Select(ToDto));
        }

        // ─────────────────────────────────────────────────────
        // POST /api/customer-inquiries
        // ─────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Create(CreateCustomerInquiryRequest request)
        {
            // Validate customer exists
            if (!await _inquiries.CustomerExistsAsync(request.CustomerId))
                return BadRequest(new { message = "CustomerId not found." });

            // Validate assignee exists (if provided)
            if (request.AssignedUserId.HasValue)
            {
                var user = await _inquiries.FindUserAsync(request.AssignedUserId.Value);
                if (user == null)
                    return BadRequest(new { message = "AssignedUserId not found." });
            }

            if (!_ctx.UserId.HasValue)
                return Unauthorized(new { message = "No authenticated user." });

            // Auto-assign to caller if SalesStaff and no assignee specified
            var assignedUserId = request.AssignedUserId
                ?? (string.Equals(_ctx.Role, Roles.SalesStaff, StringComparison.OrdinalIgnoreCase)
                    ? _ctx.UserId
                    : null);

            var entity = new CustomerInquiry
            {
                CustomerId = request.CustomerId,
                Type = request.Type,
                Subject = request.Subject.Trim(),
                Description = request.Description.Trim(),
                Priority = request.Priority
                    ?? (request.Type == CustomerInquiryType.Complaint
                        ? CustomerInquiryPriority.High
                        : CustomerInquiryPriority.Medium),
                Status = CustomerInquiryStatus.Open,
                AssignedUserId = assignedUserId,
                CreatedByUserId = _ctx.UserId.Value
            };

            var created = await _inquiries.CreateAsync(entity);
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ToDto(created));
        }

        // ─────────────────────────────────────────────────────
        // PUT /api/customer-inquiries/{id}
        // ─────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> Update(
            Guid id, UpdateCustomerInquiryRequest request)
        {
            try
            {
                var updated = new CustomerInquiry
                {
                    Subject = request.Subject.Trim(),
                    Description = request.Description.Trim(),
                    Priority = request.Priority
                };

                var result = await _inquiries.UpdateAsync(
                    id, updated, _ctx.UserId, _ctx.Role);

                if (result == null) return NotFound();
                return Ok(ToDto(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────
        // PATCH /api/customer-inquiries/{id}/status
        // ─────────────────────────────────────────────────────
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles =
            $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Manager},{Roles.SalesStaff}")]
        public async Task<IActionResult> UpdateStatus(
            Guid id, UpdateInquiryStatusRequest request)
        {
            try
            {
                var result = await _inquiries.UpdateStatusAsync(
                    id, request.Status, request.Resolution,
                    _ctx.UserId, _ctx.Role);

                if (result == null) return NotFound();
                return Ok(ToDto(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─── Mapping ──────────────────────────────────────────
        private static CustomerInquiryDto ToDto(CustomerInquiry x)
        {
            // Overdue: not resolved/closed, and older than 7 days (14 for non-complaints)
            bool isOpen = x.Status != CustomerInquiryStatus.Resolved &&
                          x.Status != CustomerInquiryStatus.Closed;

            int thresholdDays = x.Type == CustomerInquiryType.Complaint ? 7 : 14;
            bool isOverdue = isOpen &&
                             (DateTime.UtcNow - x.CreatedAt).TotalDays > thresholdDays;

            return new CustomerInquiryDto(
                x.Id,
                x.CustomerId,
                x.Customer != null
                    ? $"{x.Customer.FirstName} {x.Customer.LastName}".Trim()
                    : null,
                x.Type,
                x.Subject,
                x.Description,
                x.Status,
                x.Priority,
                x.Resolution,
                x.ResolvedAt,
                x.ResolvedByUserId,
                x.ResolvedByUser?.Name,
                x.AssignedUserId,
                x.AssignedUser?.Name,
                x.CreatedByUserId,
                x.CreatedByUser?.Name,
                x.CreatedAt,
                x.UpdatedAt,
                isOverdue);
        }
    }
}