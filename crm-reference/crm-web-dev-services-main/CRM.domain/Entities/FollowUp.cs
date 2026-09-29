using System;
using CRM.domain.Enums;

namespace CRM.domain.Entities
{
    public class FollowUp
    {
        public Guid Id { get; set; }

        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid? LeadId { get; set; }
        public Lead? Lead { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}