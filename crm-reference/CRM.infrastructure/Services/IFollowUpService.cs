using CRM.domain.Entities;
using CRM.domain.Enums;

namespace CRM.infrastructure.Services
{
    public interface IFollowUpService
    {
        Task<IReadOnlyList<FollowUp>> ListAsync(
            Guid? customerId, Guid? leadId, FollowUpStatus? status,
            bool? overdueOnly, Guid? callerUserId, string? callerRole,
            int page, int pageSize);

        Task<int> CountAsync(
            Guid? customerId, Guid? leadId, FollowUpStatus? status,
            bool? overdueOnly, Guid? callerUserId, string? callerRole);

        Task<FollowUp?> GetByIdAsync(Guid id, Guid? callerUserId, string? callerRole);
        Task<FollowUp> CreateAsync(FollowUp followUp);
        Task<FollowUp?> UpdateAsync(Guid id, FollowUp updated, Guid? callerUserId, string? callerRole);
        Task<FollowUp?> UpdateStatusAsync(Guid id, FollowUpStatus status, Guid? callerUserId, string? callerRole);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CustomerExistsAsync(Guid id);
        Task<bool> LeadExistsAsync(Guid id);
        Task<User?> FindUserAsync(Guid userId);
    }
}