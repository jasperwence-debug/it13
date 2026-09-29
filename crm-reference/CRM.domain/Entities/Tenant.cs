using System;
using System.Collections.Generic;

namespace CRM.domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Agency Name
        public string ContactEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Subscription/Billing Info (Simplified for now)
        public string SubscriptionTier { get; set; } = "Basic";
        public DateTime? SubscriptionExpiry { get; set; }

        // Navigation Properties
        public ICollection<TenantDatabase> TenantDatabases { get; set; } = new List<TenantDatabase>();
    }
}