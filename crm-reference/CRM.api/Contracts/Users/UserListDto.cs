namespace CRM.api.Contracts.Users
{
    public record UserListDto(
        Guid Id,
        string Name,
        string Email,
        string Role,
        bool IsActive,
        DateTime? LastLoginAt,
        DateTime CreatedAt);
}