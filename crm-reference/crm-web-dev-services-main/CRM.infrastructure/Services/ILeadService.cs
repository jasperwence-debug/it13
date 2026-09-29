using CRM.domain.Entities;
using CRM.domain.Enums;

namespace CRM.infrastructure.Services
{
    public interface ILeadService
    {
        Task<IReadOnlyList<Lead>> ListAsync(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            Guid? callerUserId,
            string? callerRole,
            int page,
            int pageSize);

        Task<int> CountAsync(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            Guid? callerUserId,
            string? callerRole);

        Task<Lead?> GetByIdAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole);

        Task<Lead> CreateAsync(Lead lead);

        Task<Lead?> UpdateAsync(
            Guid id,
            Lead updated,
            Guid? callerUserId,
            string? callerRole);

        Task<bool> DeleteAsync(Guid id);

        Task<Lead?> UpdateStatusAsync(
            Guid id,
            LeadStatus status,
            Guid? callerUserId,
            string? callerRole);

        Task<Lead?> AssignAsync(Guid id, Guid assignedUserId);

        Task<(Lead lead, Customer customer)?> ConvertAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole);

        Task<User?> FindUserAsync(Guid userId);
    }
}