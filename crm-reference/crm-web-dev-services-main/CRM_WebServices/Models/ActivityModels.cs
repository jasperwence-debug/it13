namespace CRM.winforms.Models
{
    public class ActivityDto
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public Guid? LeadId { get; set; }
        public string? LeadName { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string ActivityType { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime ActivityDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateActivityRequest
    {
        public Guid? CustomerId { get; set; }
        public Guid? LeadId { get; set; }
        public string ActivityType { get; set; } = "Note";
        public string Description { get; set; } = "";
        public DateTime? ActivityDate { get; set; }
    }
}