namespace CRM.domain.Dtos.Retention
{
    public record BulkAssignRequest(
        IReadOnlyList<Guid> CustomerIds,
        Guid AssignedUserId
    );
}