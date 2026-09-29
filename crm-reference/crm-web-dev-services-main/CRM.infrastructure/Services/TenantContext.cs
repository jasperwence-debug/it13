using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CRM.infrastructure.Services
{
    public class TenantContext : ITenantContext
    {
        public Guid? TenantId { get; }
        public Guid? UserId { get; }
        public string? Email { get; }
        public string? Role { get; }
        public bool IsAuthenticated { get; }

        public TenantContext(IHttpContextAccessor accessor)
        {
            var user = accessor.HttpContext?.User;
            IsAuthenticated = user?.Identity?.IsAuthenticated == true;
            if (!IsAuthenticated) return;

            if (Guid.TryParse(user!.FindFirst("tenantId")?.Value, out var tid))
                TenantId = tid;

            if (Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var uid))
                UserId = uid;

            Email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            Role = user.FindFirst("role")?.Value;
        }

        public bool IsInRole(params string[] roles)
            => !string.IsNullOrEmpty(Role)
               && roles.Any(r => string.Equals(r, Role, StringComparison.OrdinalIgnoreCase));
    }
}