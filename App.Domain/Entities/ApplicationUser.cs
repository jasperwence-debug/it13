using Microsoft.AspNetCore.Identity;

namespace App.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}