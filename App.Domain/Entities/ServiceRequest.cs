namespace App.Domain.Entities
{
    public class ServiceRequest
    {
        public int ServiceRequestId { get; set; }

        // Foreign key to Lead (optional)
        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        // Foreign key to Customer (required)
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Foreign key to Service (optional)
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        // Service info
        public string RequestedService { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public DateTime PreferredDate { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public string AssignedSalesStaff { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested";
        public decimal? ActualPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}