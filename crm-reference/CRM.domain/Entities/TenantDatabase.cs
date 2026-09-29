using System;

namespace CRM.domain.Entities
{
    public class TenantDatabase
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation Property
        public Tenant Tenant { get; set; } = null!;
    }
}