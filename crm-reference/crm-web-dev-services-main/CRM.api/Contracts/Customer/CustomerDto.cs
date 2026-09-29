namespace CRM.api.Contracts.Customers
{
    public record CustomerDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Company,
        string Address,
        string Status,
        Guid? AssignedUserId,
        string? AssignedUserName,
        DateTime CreatedAt,
        DateTime UpdatedAt,

        // ─── Churn / retention metadata ───────────────────
        DateTime? LastActivityAt,
        int DaysSinceLastActivity,
        string ChurnRisk        // "Low" | "Medium" | "High"
    );
}