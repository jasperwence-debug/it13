using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.API.Controllers
{
    /// <summary>
    /// LAYER 3 — Service Requests / Work Orders Controller.
    ///
    /// Responsibilities:
    ///   POST /api/servicerequests              — Create a work order. Requires an EXISTING CustomerId.
    ///   GET  /api/servicerequests              — Return all active work orders for the grid.
    ///   GET  /api/servicerequests/customer/{id}— Work orders for one customer (modal detail).
    ///   GET  /api/servicerequests/staff        — Available technicians and field staff.
    ///   PUT  /api/servicerequests/{id}/dispatch— Manager/Admin dispatches & schedules work order.
    ///   PUT  /api/servicerequests/{id}/status  — Manager/Admin updates execution lifecycle status.
    /// </summary>
    [ApiController]
    [Route("api/servicerequests")]
    [Route("api/service-requests")]
    public class ServiceRequestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceRequestController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET /api/servicerequests
        // Returns all active work orders with customer name joined.
        // Read-only: AsNoTracking().
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetWorkOrders()
        {
            var orders = await _context.ServiceRequests
                .AsNoTracking()
                .Include(sr => sr.Customer)
                .Where(sr => sr.IsActive)
                .OrderByDescending(sr => sr.PreferredDate)
                .Select(sr => new WorkOrderDto
                {
                    ServiceRequestId = sr.ServiceRequestId,
                    CustomerId       = sr.CustomerId,
                    CustomerName     = sr.Customer != null ? (sr.Customer.CustomerName ?? "Unknown Client") : "Unknown Client",
                    ServiceType      = sr.RequestedService ?? "General Cleaning",
                    PreferredDate    = sr.PreferredDate,
                    AssignedStaff    = sr.AssignedSalesStaff ?? "Unassigned",
                    Status           = sr.Status ?? "Requested",
                    QuotedPrice      = sr.QuotedPrice,
                    ActualPrice      = sr.ActualPrice,
                    SpecialRequests  = sr.SpecialRequests,
                    Notes            = sr.Notes,
                    CreatedAt        = sr.CreatedAt,
                    Rating           = sr.Rating,
                    FeedbackNotes    = sr.FeedbackNotes,
                    InspectionStatus = sr.InspectionStatus,
                    InspectedBy      = sr.InspectedBy,
                    FeedbackDate     = sr.FeedbackDate
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ============================================================
        // GET /api/servicerequests/customer/{customerId}
        // Returns work orders for a specific customer (used by detail modal).
        // Read-only: AsNoTracking().
        // ============================================================
        [HttpGet("customer/{customerId:int}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var exists = await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.CustomerId == customerId && c.IsActive);

            if (!exists)
                return NotFound($"No active customer with id {customerId}.");

            var orders = await _context.ServiceRequests
                .AsNoTracking()
                .Where(sr => sr.CustomerId == customerId && sr.IsActive)
                .OrderByDescending(sr => sr.PreferredDate)
                .Select(sr => new WorkOrderDto
                {
                    ServiceRequestId = sr.ServiceRequestId,
                    CustomerId       = sr.CustomerId,
                    CustomerName     = string.Empty,   // not needed in modal — customer already known
                    ServiceType      = sr.RequestedService,
                    PreferredDate    = sr.PreferredDate,
                    AssignedStaff    = sr.AssignedSalesStaff,
                    Status           = sr.Status ?? "Requested",
                    QuotedPrice      = sr.QuotedPrice,
                    ActualPrice      = sr.ActualPrice,
                    SpecialRequests  = sr.SpecialRequests,
                    Notes            = sr.Notes,
                    CreatedAt        = sr.CreatedAt,
                    Rating           = sr.Rating,
                    FeedbackNotes    = sr.FeedbackNotes,
                    InspectionStatus = sr.InspectionStatus,
                    InspectedBy      = sr.InspectedBy,
                    FeedbackDate     = sr.FeedbackDate
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ============================================================
        // POST /api/servicerequests
        // Creates a new work order / booking request.
        // STRICT RULE: CustomerId MUST refer to an existing Customer.
        //              This endpoint NEVER creates or modifies Customers.
        // Enforces:
        //   - SalesStaff creates requests with status 'Requested'. Cannot set 'Scheduled'.
        //   - AssignedSalesStaff records who created the request.
        //   - QuotedPrice is captured.
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateWorkOrder(
            [FromBody] WorkOrderCreateDto dto,
            [FromHeader(Name = "X-User-Role")] string? userRole = null,
            [FromHeader(Name = "X-User-Name")] string? userName = null)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body cannot be null." });

            if (dto.CustomerId <= 0)
                return BadRequest(new { message = "CustomerId is required and must be a valid Customer ID." });

            if (string.IsNullOrWhiteSpace(dto.ServiceType))
                return BadRequest(new { message = "ServiceType is required." });

            if (dto.PreferredDate.Year < 1753)
                return BadRequest(new { message = "PreferredDate is required and must be a valid future date." });

            bool isSalesStaff = string.Equals(userRole, "SalesStaff", StringComparison.OrdinalIgnoreCase);

            // Sales staff can ONLY create 'Requested' status bookings
            if (isSalesStaff && !string.IsNullOrWhiteSpace(dto.Status) &&
                !string.Equals(dto.Status.Trim(), "Requested", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Sales staff cannot schedule work orders directly. Booking requests must be submitted with status 'Requested'." });
            }

            // Verify customer exists — do NOT create one
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId && c.IsActive);

            if (customer == null)
                return NotFound(new { message = $"No active customer found with CustomerId {dto.CustomerId}. Convert a Lead first or select an existing customer." });

            var safePreferredDate = dto.PreferredDate.Year < 1753
                ? DateTime.UtcNow.AddDays(1)
                : dto.PreferredDate;

            // Determine creator / sales staff name
            string staffName = !string.IsNullOrWhiteSpace(userName)
                ? userName.Trim()
                : (!string.IsNullOrWhiteSpace(dto.AssignedStaff) ? dto.AssignedStaff.Trim() : (isSalesStaff ? "SalesStaff" : "Staff"));

            string targetStatus = isSalesStaff
                ? "Requested"
                : (string.IsNullOrWhiteSpace(dto.Status) ? "Requested" : dto.Status.Trim());

            var workOrder = new ServiceRequest
            {
                CustomerId         = dto.CustomerId,
                RequestedService   = dto.ServiceType.Trim(),
                PreferredDate      = safePreferredDate,
                AssignedSalesStaff = staffName,
                Status             = targetStatus,
                QuotedPrice        = dto.QuotedPrice,
                SpecialRequests    = dto.SpecialRequests?.Trim(),
                Notes              = dto.Notes?.Trim(),
                BookingDate        = DateTime.UtcNow,
                IsActive           = true,
                CreatedAt          = DateTime.UtcNow
            };

            _context.ServiceRequests.Add(workOrder);
            await _context.SaveChangesAsync();

            var resultDto = new WorkOrderDto
            {
                ServiceRequestId = workOrder.ServiceRequestId,
                CustomerId       = workOrder.CustomerId,
                CustomerName     = customer.CustomerName,
                ServiceType      = workOrder.RequestedService,
                PreferredDate    = workOrder.PreferredDate,
                AssignedStaff    = workOrder.AssignedSalesStaff,
                Status           = workOrder.Status,
                QuotedPrice      = workOrder.QuotedPrice,
                ActualPrice      = workOrder.ActualPrice,
                SpecialRequests  = workOrder.SpecialRequests,
                Notes            = workOrder.Notes,
                CreatedAt        = workOrder.CreatedAt
            };

            return Created($"/api/servicerequests/{workOrder.ServiceRequestId}", resultDto);
        }

        // ============================================================
        // GET /api/servicerequests/staff
        // Returns list of available technicians and field crew members.
        // Read-only: AsNoTracking().
        // ============================================================
        [HttpGet("staff")]
        public async Task<IActionResult> GetAvailableStaff()
        {
            var defaultStaff = new List<string>
            {
                "Pedro Reyes", "Maria Santos", "Mark Anthony", "Sarah Jane", "Michael John", "Jessica Mae"
            };

            try
            {
                var assignedStaffInOrders = await _context.ServiceRequests
                    .AsNoTracking()
                    .Where(sr => !string.IsNullOrEmpty(sr.AssignedSalesStaff) &&
                                 sr.AssignedSalesStaff != "Unassigned" &&
                                 sr.AssignedSalesStaff != "Staff" &&
                                 sr.AssignedSalesStaff != "SalesStaff")
                    .Select(sr => sr.AssignedSalesStaff)
                    .Distinct()
                    .ToListAsync();

                var userStaff = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Role == "Staff" || u.Role == "Manager" || u.Role == "Technician")
                    .Select(u => u.Username)
                    .ToListAsync();

                var combined = defaultStaff
                    .Concat(assignedStaffInOrders)
                    .Concat(userStaff)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                return Ok(combined);
            }
            catch
            {
                return Ok(defaultStaff);
            }
        }

        // ============================================================
        // PUT /api/servicerequests/{id}/dispatch
        // Dispatches and schedules a work order with an assigned technician/crew.
        // Requires Manager, Admin, or SuperAdmin role.
        // Transition: Requested -> Scheduled
        // ============================================================
        [HttpPut("{id:int}/dispatch")]
        public async Task<IActionResult> DispatchWorkOrder(
            int id,
            [FromBody] WorkOrderDispatchDto dto,
            [FromHeader(Name = "X-User-Role")] string? userRole = null,
            [FromHeader(Name = "X-User-Name")] string? userName = null)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body cannot be null." });

            bool isSalesStaff = string.Equals(userRole, "SalesStaff", StringComparison.OrdinalIgnoreCase);
            if (isSalesStaff)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Access denied: Sales staff cannot dispatch or schedule work orders. Only Managers and Admins can perform dispatch operations."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.AssignedStaff))
                return BadRequest(new { message = "AssignedStaff is required to dispatch a work order." });

            if (dto.ScheduledDate.Year < 1753)
                return BadRequest(new { message = "A valid ScheduledDate is required." });

            var workOrder = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id && sr.IsActive);

            if (workOrder == null)
                return NotFound(new { message = $"No active work order found with ID {id}." });

            if (string.Equals(workOrder.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = $"Cannot dispatch a work order that is already '{workOrder.Status}'." });
            }

            workOrder.AssignedSalesStaff = dto.AssignedStaff.Trim();
            workOrder.PreferredDate = dto.ScheduledDate;
            workOrder.Status = "Scheduled";

            string managerIdentifier = !string.IsNullOrWhiteSpace(userName) ? userName.Trim() : "Manager";
            var auditNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Dispatched by {managerIdentifier} to {workOrder.AssignedSalesStaff}";
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                auditNote += $" - {dto.Notes.Trim()}";
            }

            workOrder.Notes = string.IsNullOrWhiteSpace(workOrder.Notes)
                ? auditNote
                : $"{workOrder.Notes}\n{auditNote}";

            await _context.SaveChangesAsync();

            var resultDto = new WorkOrderDto
            {
                ServiceRequestId = workOrder.ServiceRequestId,
                CustomerId       = workOrder.CustomerId,
                CustomerName     = workOrder.Customer?.CustomerName ?? "Unknown Client",
                ServiceType      = workOrder.RequestedService,
                PreferredDate    = workOrder.PreferredDate,
                AssignedStaff    = workOrder.AssignedSalesStaff,
                Status           = workOrder.Status,
                QuotedPrice      = workOrder.QuotedPrice,
                ActualPrice      = workOrder.ActualPrice,
                SpecialRequests  = workOrder.SpecialRequests,
                Notes            = workOrder.Notes,
                CreatedAt        = workOrder.CreatedAt
            };

            return Ok(resultDto);
        }

        // ============================================================
        // PUT /api/servicerequests/{id}/status
        // Updates execution lifecycle status: Scheduled -> InProgress -> Completed / Cancelled / Rescheduled.
        // Requires Manager, Admin, or SuperAdmin role.
        // ============================================================
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateWorkOrderStatus(
            int id,
            [FromBody] WorkOrderStatusUpdateDto dto,
            [FromHeader(Name = "X-User-Role")] string? userRole = null,
            [FromHeader(Name = "X-User-Name")] string? userName = null)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body cannot be null." });

            bool isSalesStaff = string.Equals(userRole, "SalesStaff", StringComparison.OrdinalIgnoreCase);
            if (isSalesStaff)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Access denied: Sales staff cannot modify work order operational status. Only Managers and Admins can update execution lifecycle."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Status is required." });

            var targetStatus = BookingStatusHelper.FromString(dto.Status);

            var workOrder = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id && sr.IsActive);

            if (workOrder == null)
                return NotFound(new { message = $"No active work order found with ID {id}." });

            var currentStatus = BookingStatusHelper.FromString(workOrder.Status);

            // Terminal state checks
            if (currentStatus == BookingStatus.Cancelled && targetStatus != BookingStatus.Cancelled)
            {
                return BadRequest(new { message = "Cannot transition an already Cancelled work order." });
            }

            if (currentStatus == BookingStatus.Completed && targetStatus != BookingStatus.Completed)
            {
                return BadRequest(new { message = "Cannot transition an already Completed work order." });
            }

            // Target state specific logic
            if (targetStatus == BookingStatus.Completed)
            {
                if (currentStatus == BookingStatus.Requested)
                {
                    return BadRequest(new { message = "Work order cannot be completed directly from 'Requested'. It must be dispatched and scheduled first." });
                }

                if (dto.ActualPrice.HasValue && dto.ActualPrice.Value >= 0)
                {
                    workOrder.ActualPrice = dto.ActualPrice.Value;
                }
                else if (!workOrder.ActualPrice.HasValue && workOrder.QuotedPrice.HasValue)
                {
                    workOrder.ActualPrice = workOrder.QuotedPrice.Value;
                }
            }
            else if (targetStatus == BookingStatus.Rescheduled)
            {
                if (dto.ScheduledDate.HasValue && dto.ScheduledDate.Value.Year >= 1753)
                {
                    workOrder.PreferredDate = dto.ScheduledDate.Value;
                }
            }

            workOrder.Status = targetStatus.ToString();

            string managerIdentifier = !string.IsNullOrWhiteSpace(userName) ? userName.Trim() : "Manager";
            var auditNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status changed to '{workOrder.Status}' by {managerIdentifier}";
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                auditNote += $" - {dto.Notes.Trim()}";
            }

            workOrder.Notes = string.IsNullOrWhiteSpace(workOrder.Notes)
                ? auditNote
                : $"{workOrder.Notes}\n{auditNote}";

            await _context.SaveChangesAsync();

            var resultDto = new WorkOrderDto
            {
                ServiceRequestId = workOrder.ServiceRequestId,
                CustomerId       = workOrder.CustomerId,
                CustomerName     = workOrder.Customer?.CustomerName ?? "Unknown Client",
                ServiceType      = workOrder.RequestedService,
                PreferredDate    = workOrder.PreferredDate,
                AssignedStaff    = workOrder.AssignedSalesStaff,
                Status           = workOrder.Status,
                QuotedPrice      = workOrder.QuotedPrice,
                ActualPrice      = workOrder.ActualPrice,
                SpecialRequests  = workOrder.SpecialRequests,
                Notes            = workOrder.Notes,
                CreatedAt        = workOrder.CreatedAt,
                Rating           = workOrder.Rating,
                FeedbackNotes    = workOrder.FeedbackNotes,
                InspectionStatus = workOrder.InspectionStatus,
                InspectedBy      = workOrder.InspectedBy,
                FeedbackDate     = workOrder.FeedbackDate
            };

            return Ok(resultDto);
        }

        // ============================================================
        // PUT /api/servicerequests/{id}/feedback
        // Quality Assurance & Customer Feedback (Phase 6).
        // Captures rating (1-5), feedback notes, and inspection status
        // for completed work orders.
        // ============================================================
        [HttpPut("{id:int}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            int id,
            [FromBody] ServiceFeedbackDto dto,
            [FromHeader(Name = "X-User-Role")] string? userRole = null,
            [FromHeader(Name = "X-User-Name")] string? userName = null)
        {
            if (dto == null)
                return BadRequest("Invalid feedback payload.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5 stars.");

            var workOrder = await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id && sr.IsActive);

            if (workOrder == null)
                return NotFound($"Work order with ID {id} was not found.");

            if (!string.Equals(workOrder.Status, BookingStatus.Completed.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Feedback and Quality Inspections can only be recorded for 'Completed' work orders.");
            }

            workOrder.Rating = dto.Rating;
            workOrder.FeedbackNotes = dto.FeedbackNotes?.Trim();
            workOrder.InspectionStatus = !string.IsNullOrWhiteSpace(dto.InspectionStatus) ? dto.InspectionStatus.Trim() : "Passed";
            workOrder.InspectedBy = !string.IsNullOrWhiteSpace(dto.InspectedBy) ? dto.InspectedBy.Trim() : (!string.IsNullOrWhiteSpace(userName) ? userName.Trim() : "Supervisor");
            workOrder.FeedbackDate = DateTime.UtcNow;

            var feedbackAudit = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] QA & Feedback recorded by {workOrder.InspectedBy}: Rating {workOrder.Rating}/5, Inspection: {workOrder.InspectionStatus}";
            if (!string.IsNullOrWhiteSpace(workOrder.FeedbackNotes))
            {
                feedbackAudit += $", Notes: \"{workOrder.FeedbackNotes}\"";
            }

            workOrder.Notes = string.IsNullOrWhiteSpace(workOrder.Notes)
                ? feedbackAudit
                : $"{workOrder.Notes}\n{feedbackAudit}";

            await _context.SaveChangesAsync();

            var result = new WorkOrderDto
            {
                ServiceRequestId = workOrder.ServiceRequestId,
                CustomerId       = workOrder.CustomerId,
                CustomerName     = workOrder.Customer?.CustomerName ?? "Unknown Client",
                ServiceType      = workOrder.RequestedService,
                PreferredDate    = workOrder.PreferredDate,
                AssignedStaff    = workOrder.AssignedSalesStaff,
                Status           = workOrder.Status,
                QuotedPrice      = workOrder.QuotedPrice,
                ActualPrice      = workOrder.ActualPrice,
                SpecialRequests  = workOrder.SpecialRequests,
                Notes            = workOrder.Notes,
                CreatedAt        = workOrder.CreatedAt,
                Rating           = workOrder.Rating,
                FeedbackNotes    = workOrder.FeedbackNotes,
                InspectionStatus = workOrder.InspectionStatus,
                InspectedBy      = workOrder.InspectedBy,
                FeedbackDate     = workOrder.FeedbackDate
            };

            return Ok(result);
        }
    }
}
