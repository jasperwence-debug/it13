namespace App.Domain.Entities
{
    public class Lead
    {
        public int LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}