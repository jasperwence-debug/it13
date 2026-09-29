using System.ComponentModel.DataAnnotations;

namespace CRM.api.Contracts.FollowUps
{
    public record UpdateFollowUpRequest(
        [Required, MaxLength(200)] string Title,
        [MaxLength(2000)] string Description,
        [Required] DateTime DueDate);
}