namespace CRM.api.Contracts.Users
{
    public record UserDetailDto(
        Guid Id,
        string Name,
        string Email,
        string Role,
        bool IsActive,
        DateTime? LastLoginAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,

        // Snapshot of activity (nice for the detail screen)
        int AssignedCustomers,
        int AssignedLeads,
        int LoggedActivities,
        int OpenFollowUps);
}