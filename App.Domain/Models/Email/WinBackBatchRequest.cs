using System.Collections.Generic;

namespace App.Domain.Models.Email
{
    /// <summary>
    /// Batch request to trigger win-back emails to multiple qualifying customers.
    /// </summary>
    public class WinBackBatchRequest
    {
        public List<int> CustomerIds { get; set; } = new();
        public string CampaignType { get; set; } = "Automated Retention Policy";
        public string PromoCode { get; set; } = "WINBACK10";
        public int DiscountPercentage { get; set; } = 10;
        public string? Subject { get; set; }
        public string? CustomMessage { get; set; }
    }

    /// <summary>
    /// Summary outcome of a batch win-back dispatch.
    /// </summary>
    public class WinBackBatchResult
    {
        public int TotalRequested { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<EmailResult> Results { get; set; } = new();
    }
}
