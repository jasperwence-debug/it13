using System.ComponentModel.DataAnnotations;

namespace CRM.api.Contracts.Users
{
    public record CreateUserRequest(
        [Required, MaxLength(150)] string Name,
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required, MinLength(6), MaxLength(100)] string Password,
        [Required] string Role);
}