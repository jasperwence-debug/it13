using CRM.domain.Enums;

namespace CRM.api.Contracts.FollowUps
{
    public record FollowUpDto(
        Guid Id,
        Guid? CustomerId,
        string? CustomerName,
        Guid? LeadId,
        string? LeadName,
        Guid UserId,
        string UserName,
        string Title,
        string Description,
        DateTime DueDate,
        FollowUpStatus Status,
        bool IsOverdue,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}