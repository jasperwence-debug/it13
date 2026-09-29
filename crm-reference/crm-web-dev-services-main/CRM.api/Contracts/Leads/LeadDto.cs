using CRM.domain.Enums;

namespace CRM.api.Contracts.Leads
{
    public record LeadDto(
        Guid Id,
        Guid? CustomerId,
        string Name,
        string Email,
        string Phone,
        string Source,
        LeadStatus Status,
        LeadPriority Priority,
        decimal ExpectedValue,
        string Notes,
        Guid? AssignedUserId,
        string? AssignedUserName,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}