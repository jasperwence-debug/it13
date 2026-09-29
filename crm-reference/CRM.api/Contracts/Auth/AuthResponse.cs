namespace CRM.api.Contracts.Auth
{
    public record AuthResponse(
        string Token,
        DateTime ExpiresAt,
        string Name,
        string Email,
        string Role);
}