using System.ComponentModel.DataAnnotations;
using CRM.domain.Enums;

namespace CRM.api.Contracts.Activities
{
    public record CreateActivityRequest(
        Guid? CustomerId,
        Guid? LeadId,
        ActivityType ActivityType,
        [Required, MaxLength(2000)] string Description,
        DateTime? ActivityDate);
}