namespace CRM.infrastructure.Services
{
    public interface ITenantContext
    {
        Guid? TenantId { get; }
        Guid? UserId { get; }
        string? Email { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(params string[] roles);
    }
}