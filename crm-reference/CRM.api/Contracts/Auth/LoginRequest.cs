namespace CRM.api.Contracts.Auth
{
    public record LoginRequest(
        string Email,
        string Password,
        Guid TenantId);
}