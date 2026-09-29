namespace CRM.winforms.Models
{
    public class LeadDto
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Source { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public decimal ExpectedValue { get; set; }
        public string Notes { get; set; } = "";
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateLeadRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Source { get; set; } = "";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string Notes { get; set; } = "";
        public Guid? AssignedUserId { get; set; }
    }

    public class UpdateLeadRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Source { get; set; } = "";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string Notes { get; set; } = "";
    }
}