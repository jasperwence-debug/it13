namespace CRM.domain.Dtos.Retention
{
    public record CreateWinBackRequest(
        IReadOnlyList<Guid> CustomerIds,
        Guid? AssignedUserId,          // null = assign to caller
        DateTime? DueDate,             // null = 7 days from now
        string? Notes
    );
}