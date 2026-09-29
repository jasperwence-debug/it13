namespace CRM.winforms.Models
{
    public class UserSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";

        public override string ToString() => Name;
    }
}