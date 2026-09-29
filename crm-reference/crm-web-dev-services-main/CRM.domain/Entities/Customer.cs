using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.domain.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Foreign Key
        public Guid? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();

        // ─── Churn / retention metadata (NOT persisted) ─────────
        [NotMapped]
        public DateTime? LastActivityAt { get; set; }

        [NotMapped]
        public int DaysSinceLastActivity { get; set; }

        [NotMapped]
        public string ChurnRisk { get; set; } = "Low";
    }
}