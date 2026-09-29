using System;
using CRM.domain.Enums;

namespace CRM.domain.Entities
{
    public class CustomerInquiry
    {
        public Guid Id { get; set; }

        // Link to the customer
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public CustomerInquiryType Type { get; set; } = CustomerInquiryType.Inquiry;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public CustomerInquiryStatus Status { get; set; } = CustomerInquiryStatus.Open;
        public CustomerInquiryPriority Priority { get; set; } = CustomerInquiryPriority.Medium;

        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public Guid? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }

        public Guid? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public Guid CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}