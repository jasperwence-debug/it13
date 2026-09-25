namespace App.Domain.Enums
{
    /// <summary>
    /// Tenant Subscription Tiers according to the Final Laboratory Exam CRM specifications:
    /// - Micro (Tenant A): Main Transaction & Data Collection
    /// - Small (Tenant B): Business Intelligence & Actions
    /// - Medium (Tenant C): Branching, Business Intelligence & Actions
    /// </summary>
    public enum SubscriptionTier
    {
        Micro = 1,
        Small = 2,
        Medium = 3
    }
}
