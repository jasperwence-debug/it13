using System;
using System.Collections.Generic;
using CRM.domain.Enums;

namespace CRM.domain.Entities
{
    public class Lead
    {
        public Guid Id { get; set; }

        // Optional link to an existing Customer
        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public LeadStatus Status { get; set; } = LeadStatus.New;
        public LeadPriority Priority { get; set; } = LeadPriority.Medium;

        public decimal ExpectedValue { get; set; }
        public string Notes { get; set; } = string.Empty;

        public Guid? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    }
}