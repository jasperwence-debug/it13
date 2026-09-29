using CRM.domain.Entities;

namespace CRM.infrastructure.Services
{
    public interface ICustomerService
    {
        Task<IReadOnlyList<Customer>> ListAsync(
            string? search,
            string? status,
            Guid? callerUserId,
            string? callerRole);

        Task<Customer?> GetByIdAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole);

        Task<Customer> CreateAsync(
            Customer customer);

        Task<Customer?> UpdateAsync(
            Guid id,
            Customer updated,
            Guid? callerUserId,
            string? callerRole);

        Task<bool> DeleteAsync(
            Guid id);

        Task<Customer?> AssignAsync(
            Guid id,
            Guid assignedUserId);

        Task<User?> FindUserAsync(Guid userId);
    }
}