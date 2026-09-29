using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class FollowUpApiClient : ApiClientBase
    {
        public Task<PagedResponse<FollowUpDto>?> ListAsync(
            string? status = null,
            bool? overdue = null,
            int page = 1,
            int pageSize = 50)
        {
            var url = $"/api/follow-ups?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (overdue.HasValue) url += $"&overdue={overdue.Value.ToString().ToLowerInvariant()}";
            return GetAsync<PagedResponse<FollowUpDto>>(url);
        }

        public Task<FollowUpDto?> CreateAsync(CreateFollowUpRequest req)
            => PostAsync<FollowUpDto>("/api/follow-ups", req);

        public Task UpdateStatusAsync(Guid id, string status)
            => PatchAsync($"/api/follow-ups/{id}/status", new { status });
    }
}