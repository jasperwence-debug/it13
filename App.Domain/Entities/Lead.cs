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

        /// <summary>
        /// Lifecycle status: New, Contacted, Quoted, Won, Lost, Converted
        /// </summary>
        public string Status { get; set; } = "New";

        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
        public string? LostReason { get; set; }

        /// <summary>
        /// Traceability link to Customer created/linked during conversion.
        /// </summary>
        public int? ConvertedCustomerId { get; set; }

        /// <summary>
        /// Timestamp when the lead was converted.
        /// </summary>
        public DateTime? ConvertedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}