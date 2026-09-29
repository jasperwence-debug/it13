namespace CRM.winforms.Models
{
    public class FollowUpDto
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public Guid? LeadId { get; set; }
        public string? LeadName { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public bool IsOverdue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateFollowUpRequest
    {
        public Guid? CustomerId { get; set; }
        public Guid? LeadId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }
        public Guid? AssignedUserId { get; set; }
    }
}