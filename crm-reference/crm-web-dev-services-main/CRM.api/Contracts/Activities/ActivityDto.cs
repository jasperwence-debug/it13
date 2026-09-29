using CRM.domain.Enums;

namespace CRM.api.Contracts.Activities
{
    public record ActivityDto(
        Guid Id,
        Guid? CustomerId,
        string? CustomerName,
        Guid? LeadId,
        string? LeadName,
        Guid UserId,
        string UserName,
        ActivityType ActivityType,
        string Description,
        DateTime ActivityDate,
        DateTime CreatedAt);
}