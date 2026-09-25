using System;
using App.Domain.Enums;

namespace App.Domain.Entities
{
    /// <summary>
    /// Represents a commercial subscription plan allocated to a tenant company.
    /// Governs multi-tenant tier access (Micro, Small, Medium Enterprise) as required by the CRM spec.
    /// </summary>
    public class Subscription
    {
        public int SubscriptionId { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public SubscriptionTier Tier { get; set; } = SubscriptionTier.Micro;

        public string Status { get; set; } = "Active"; // Active, Trial, Suspended, Cancelled
        public string BillingCycle { get; set; } = "Monthly"; // Monthly, Quarterly, Annual
        public decimal MonthlyPrice { get; set; } = 2500m;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public int MaxUsers { get; set; } = 3;
        public int MaxBranches { get; set; } = 1;

        // Entitlements according to 4-tier rubric requirements
        public bool HasDataCollection { get; set; } = true;
        public bool HasTransactions { get; set; } = true;
        public bool HasBusinessIntelligence { get; set; } = false;
        public bool HasActions { get; set; } = false;
        public bool HasBranching { get; set; } = false;

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Automatically adjusts entitlements, limits, and pricing based on the assigned tier.
        /// </summary>
        public void ApplyTierDefaults(SubscriptionTier tier)
        {
            Tier = tier;
            switch (tier)
            {
                case SubscriptionTier.Micro: // Tenant A
                    MonthlyPrice = 2500m;
                    MaxUsers = 3;
                    MaxBranches = 1;
                    HasDataCollection = true;
                    HasTransactions = true;
                    HasBusinessIntelligence = false;
                    HasActions = false;
                    HasBranching = false;
                    break;

                case SubscriptionTier.Small: // Tenant B
                    MonthlyPrice = 5500m;
                    MaxUsers = 10;
                    MaxBranches = 1;
                    HasDataCollection = true;
                    HasTransactions = true;
                    HasBusinessIntelligence = true;
                    HasActions = true;
                    HasBranching = false;
                    break;

                case SubscriptionTier.Medium: // Tenant C (Medium Enterprise)
                    MonthlyPrice = 12000m;
                    MaxUsers = 50;
                    MaxBranches = 10;
                    HasDataCollection = true;
                    HasTransactions = true;
                    HasBusinessIntelligence = true;
                    HasActions = true;
                    HasBranching = true;
                    break;
            }
        }
    }
}
