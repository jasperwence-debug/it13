namespace App.Domain.Entities
{
    public class ServiceRequest
    {
        public int ServiceRequestId { get; set; }
        public int Id
        {
            get => ServiceRequestId;
            set => ServiceRequestId = value;
        }

        // Foreign key to Lead (optional)
        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        // Foreign key to Customer (required)
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Foreign key to Service (optional)
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        // Foreign key to Branch (optional — used by Tenant C Medium Enterprise multi-branch routing)
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // Service info
        public string RequestedService { get; set; } = string.Empty;
        public string ServiceType
        {
            get => RequestedService;
            set => RequestedService = value;
        }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public DateTime PreferredDate { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public string AssignedSalesStaff { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested";
        public decimal? QuotedPrice { get; set; }
        public decimal? ActualPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quality Assurance & Customer Feedback (Phase 6)
        public int? Rating { get; set; } // 1 to 5 stars
        public string? FeedbackNotes { get; set; }
        public string? InspectionStatus { get; set; } // "Passed", "NeedsRework", "Pending"
        public string? InspectedBy { get; set; }
        public DateTime? FeedbackDate { get; set; }
    }
}