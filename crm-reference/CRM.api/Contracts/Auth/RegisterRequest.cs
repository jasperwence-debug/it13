namespace CRM.api.Contracts.Auth
{
    public record RegisterRequest(
        string Name,
        string Email,
        string Password,
        string Role);
}