using System.ComponentModel.DataAnnotations;

namespace CRM.api.Contracts.Users
{
    public record ChangePasswordRequest(
        [Required, MinLength(6), MaxLength(100)] string NewPassword);
}