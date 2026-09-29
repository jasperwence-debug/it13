namespace CRM.api.Contracts.Users
{
    public record UserSummaryDto(Guid Id, string Name, string Email, string Role);
}