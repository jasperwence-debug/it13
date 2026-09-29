namespace CRM.domain.Dtos.Retention
{
    public record WinBackResultDto(
        int Created,
        int SkippedExisting,
        IReadOnlyList<Guid> CreatedFollowUpIds
    );
}