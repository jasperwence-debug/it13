using System;

namespace App.Domain.Entities
{
    /// <summary>
    /// Represents an operational branch office or regional service hub.
    /// Core feature of Tenant C (Medium Enterprise) in the CRM 4-tier specification.
    /// Enables multi-location workforce dispatch, localized inventory, and regional performance reporting.
    /// </summary>
    public class Branch
    {
        public int BranchId { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string ManagerName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
