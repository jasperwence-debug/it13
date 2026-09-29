using System.ComponentModel.DataAnnotations;

namespace CRM.api.Contracts.Users
{
    public record UpdateUserRequest(
        [Required, MaxLength(150)] string Name,
        [Required, EmailAddress, MaxLength(256)] string Email,
        [Required] string Role);
}