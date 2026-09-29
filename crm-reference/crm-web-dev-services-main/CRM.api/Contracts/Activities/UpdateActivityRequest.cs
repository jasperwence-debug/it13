using System.ComponentModel.DataAnnotations;
using CRM.domain.Enums;

namespace CRM.api.Contracts.Activities
{
    public record UpdateActivityRequest(
        ActivityType ActivityType,
        [Required, MaxLength(2000)] string Description,
        DateTime? ActivityDate);
}