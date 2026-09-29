namespace CRM.domain.Dtos.Retention
{
    public record AtRiskCustomerDto(
        Guid CustomerId,
        string Name,
        string Email,
        string Company,
        int DaysSinceLastActivity,
        DateTime LastActivityDate,
        Guid? AssignedUserId,
        string? AssignedUserName,
        bool HasOpenWinBack,          // true if there's already a pending win-back follow-up
        Guid? WinBackFollowUpId       // id of that follow-up, if any
    );
}