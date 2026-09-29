using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class RetentionApiClient : ApiClientBase
    {
        public Task<RetentionSummaryDto?> GetSummaryAsync(int thresholdDays = 60)
            => GetAsync<RetentionSummaryDto>(
                $"/api/retention/summary?thresholdDays={thresholdDays}");

        public Task<List<AtRiskActionCustomerDto>?> GetAtRiskAsync(
            int thresholdDays = 60,
            bool onlyUnassigned = false)
            => GetAsync<List<AtRiskActionCustomerDto>>(
                $"/api/retention/at-risk?thresholdDays={thresholdDays}&onlyUnassigned={(onlyUnassigned ? "true" : "false")}");

        public Task<List<RecoveredActionCustomerDto>?> GetRecoveredAsync(int lookbackDays = 90)
            => GetAsync<List<RecoveredActionCustomerDto>>(
                $"/api/retention/recovered?lookbackDays={lookbackDays}");

        public Task<object?> BulkAssignAsync(List<Guid> customerIds, Guid assignedUserId)
            => PostAsync<object>("/api/retention/bulk-assign", new BulkAssignRequest
            {
                CustomerIds = customerIds,
                AssignedUserId = assignedUserId
            });

        public Task<WinBackResultDto?> CreateWinBacksAsync(
            List<Guid> customerIds,
            Guid? assignedUserId = null,
            DateTime? dueDate = null,
            string? notes = null)
            => PostAsync<WinBackResultDto>("/api/retention/win-back", new CreateWinBackRequest
            {
                CustomerIds = customerIds,
                AssignedUserId = assignedUserId,
                DueDate = dueDate,
                Notes = notes
            });
    }
}