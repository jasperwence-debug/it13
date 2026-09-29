namespace App.Domain.Models.Email
{
    /// <summary>
    /// Payload requested to trigger a personalized win-back re-engagement email.
    /// </summary>
    public class WinBackEmailRequest
    {
        public int? CustomerId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string PromoCode { get; set; } = "WINBACK10";
        public int DiscountPercentage { get; set; } = 10;
        public int? DaysInactive { get; set; }
        public string? LastServiceType { get; set; }
        public string CampaignType { get; set; } = "30-Day Re-engagement";
        public string? CustomMessage { get; set; }
        public string? ServiceLocation { get; set; }
    }
}
