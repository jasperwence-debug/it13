namespace CRM.winforms.Models
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Company { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "";
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ─── Churn / retention metadata ─────────────────────
        public DateTime? LastActivityAt { get; set; }
        public int DaysSinceLastActivity { get; set; }
        public string ChurnRisk { get; set; } = "Low";

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class CreateCustomerRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Company { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "Active";
        public Guid? AssignedUserId { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Company { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "Active";
    }
}