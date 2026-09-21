namespace App.API
{
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

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
    }
}