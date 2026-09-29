namespace CRM.winforms.Models
{
    public class CustomerInquiryDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Type { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public Guid? ResolvedByUserId { get; set; }
        public string? ResolvedByUserName { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class CreateCustomerInquiryRequest
    {
        public Guid CustomerId { get; set; }
        public string Type { get; set; } = "Inquiry";
        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Priority { get; set; }
        public Guid? AssignedUserId { get; set; }
    }

    public class UpdateCustomerInquiryRequest
    {
        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "Medium";
    }

    public class UpdateInquiryStatusRequest
    {
        public string Status { get; set; } = "Open";
        public string? Resolution { get; set; }
    }
}