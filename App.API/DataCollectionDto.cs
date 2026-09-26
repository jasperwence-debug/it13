namespace App.API
{
    // ----------------------------------------------------------------
    // EXISTING: Used by POST /api/data-collection (preserved as-is)
    // ----------------------------------------------------------------
    public class DataCollectionDto
    {
        // Lead
        public string LeadName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }

        // Customer
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;

        // Service
        public string RequestedService { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public string AssignedSalesStaff { get; set; } = string.Empty;
    }

    // ----------------------------------------------------------------
    // EXISTING: Used by GET /api/customers/check (preserved as-is)
    // ----------------------------------------------------------------
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
    }

    // ================================================================
    // LAYER 1: LEADS — Inquiry-only DTOs
    // ================================================================

    /// <summary>
    /// Input DTO for POST /api/leads.
    /// Accepts Name, Phone, Email, and Source. No scheduling fields.
    /// </summary>
    public class LeadCreateDto
    {
        public string LeadName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }
        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
    }

    /// <summary>
    /// Input DTO for updating a Lead status (e.g. Quoted with price, Lost with reason).
    /// </summary>
    public class LeadStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal? QuotedPrice { get; set; }
        public string? LostReason { get; set; }
    }

    /// <summary>
    /// Output DTO returned by GET /api/leads.
    /// </summary>
    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }
        public string Status { get; set; } = "New";
        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
        public string? LostReason { get; set; }
        public int? ConvertedCustomerId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Input DTO for POST /api/leads/{id}/convert.
    /// Supports forcing creation or linking to a specific existing customer.
    /// </summary>
    public class LeadConvertRequestDto
    {
        public bool ForceCreate { get; set; } = false;
        public int? UseExistingCustomerId { get; set; }
        public string? ServiceAddress { get; set; }
    }

    /// <summary>
    /// Output DTO returned with 409 Conflict when a potential duplicate customer is found.
    /// </summary>
    public class LeadDuplicateMatchDto
    {
        public string Message { get; set; } = string.Empty;
        public int ExistingCustomerId { get; set; }
        public string ExistingCustomerName { get; set; } = string.Empty;
        public string ExistingContactInfo { get; set; } = string.Empty;
        public string? ExistingEmail { get; set; }
        public string ExistingLocation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Output DTO returned upon successful lead conversion.
    /// </summary>
    public class LeadConvertResultDto
    {
        public int CustomerId { get; set; }
        public int LeadId { get; set; }
        public bool WasExistingCustomer { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // ================================================================
    // LAYER 2: CUSTOMERS — Master profile with booking aggregates
    // ================================================================

    /// <summary>
    /// Output DTO for GET /api/customers — one row per Customer
    /// (never a flat 1-to-many join).
    /// </summary>
    public class CustomerSummaryDto
    {
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int? DaysSinceLastService { get; set; }
        public bool IsAtRisk { get; set; }
        public string RetentionStatus { get; set; } = "Active";
        public string? LatestService { get; set; }
        public DateTime? LatestDate { get; set; }
    }

    /// <summary>
    /// Output DTO for GET /api/analytics/at-risk-customers.
    /// Dedicated retention alert model for accounts without completed bookings in 60+ days.
    /// </summary>
    public class AtRiskCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
        public DateTime? LastCompletedDate { get; set; }
        public int DaysSinceLastService { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public string? LastServiceType { get; set; }
    }

    // ================================================================
    // LAYER 3: SERVICE REQUESTS / WORK ORDERS
    // ================================================================

    /// <summary>
    /// Input DTO for POST /api/servicerequests.
    /// Requires an existing CustomerId — never creates Customers.
    /// </summary>
    public class WorkOrderCreateDto
    {
        public int CustomerId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string AssignedStaff { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested";
        public decimal? QuotedPrice { get; set; }
        public string? SpecialRequests { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Output DTO for GET /api/servicerequests — work order grid row.
    /// </summary>
    public class WorkOrderDto
    {
        public int ServiceRequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string AssignedStaff { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? QuotedPrice { get; set; }
        public decimal? ActualPrice { get; set; }
        public string? SpecialRequests { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Quality Assurance & Feedback (Phase 6)
        public int? Rating { get; set; }
        public string? FeedbackNotes { get; set; }
        public string? InspectionStatus { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? FeedbackDate { get; set; }
    }

    /// <summary>
    /// Input DTO for PUT /api/servicerequests/{id}/dispatch.
    /// Used by Managers/Admins to assign field staff and confirm scheduled date.
    /// </summary>
    public class WorkOrderDispatchDto
    {
        public string AssignedStaff { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Input DTO for PUT /api/servicerequests/{id}/status.
    /// Used by Managers/Admins to advance work order through execution lifecycle:
    /// Scheduled -> InProgress -> Completed / Cancelled / Rescheduled.
    /// </summary>
    public class WorkOrderStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal? ActualPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime? ScheduledDate { get; set; }
    }

    /// <summary>
    /// Input DTO for PUT /api/servicerequests/{id}/feedback.
    /// Captures customer rating (1-5), feedback review, and supervisor QA inspection status.
    /// </summary>
    public class ServiceFeedbackDto
    {
        public int Rating { get; set; } // 1 to 5
        public string? FeedbackNotes { get; set; }
        public string? InspectionStatus { get; set; } // "Passed", "NeedsRework", "Pending"
        public string? InspectedBy { get; set; }
    }
}