using CRM.domain.Entities;
using CRM.domain.Enums;

namespace CRM.infrastructure.Services
{
    public interface IActivityService
    {
        Task<IReadOnlyList<Activity>> ListAsync(
            Guid? customerId,
            Guid? leadId,
            ActivityType? type,
            Guid? callerUserId,
            string? callerRole,
            int page,
            int pageSize);

        Task<int> CountAsync(
            Guid? customerId,
            Guid? leadId,
            ActivityType? type,
            Guid? callerUserId,
            string? callerRole);

        Task<Activity?> GetByIdAsync(Guid id, Guid? callerUserId, string? callerRole);
        Task<Activity> CreateAsync(Activity activity);
        Task<Activity?> UpdateAsync(Guid id, Activity updated, Guid? callerUserId, string? callerRole);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CustomerExistsAsync(Guid id);
        Task<bool> LeadExistsAsync(Guid id);
    }
}