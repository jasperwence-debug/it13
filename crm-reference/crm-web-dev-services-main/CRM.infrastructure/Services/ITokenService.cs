using CRM.domain.Entities;

namespace CRM.infrastructure.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user, Guid tenantId);
    }
}