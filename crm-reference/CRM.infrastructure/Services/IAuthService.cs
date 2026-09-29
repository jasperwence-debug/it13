using CRM.domain.Entities;

namespace CRM.infrastructure.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(
            string name,
            string email,
            string password,
            string role);

        Task<User?> ValidateCredentialsAsync(
            string email,
            string password);
    }
}