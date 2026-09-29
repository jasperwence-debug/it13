using CRM.domain.Dtos.Retention;

namespace CRM.infrastructure.Services
{
    public interface IRetentionService
    {
        Task<RetentionSummaryDto> GetSummaryAsync(
            int thresholdDays,
            Guid? callerUserId,
            string? callerRole);

        Task<IReadOnlyList<AtRiskCustomerDto>> GetAtRiskAsync(
            int thresholdDays,
            bool onlyUnassigned,
            Guid? callerUserId,
            string? callerRole);

        Task<IReadOnlyList<RecoveredCustomerDto>> GetRecoveredAsync(
            int lookbackDays,
            Guid? callerUserId,
            string? callerRole);

        Task<int> BulkAssignAsync(
            IReadOnlyList<Guid> customerIds,
            Guid assignedUserId);

        Task<WinBackResultDto> CreateWinBacksAsync(
            IReadOnlyList<Guid> customerIds,
            Guid? assignedUserId,
            DateTime? dueDate,
            string? notes,
            Guid? callerUserId);
    }
}