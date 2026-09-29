using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class ActivityApiClient : ApiClientBase
    {
        public Task<PagedResponse<ActivityDto>?> ListAsync(
            Guid? customerId = null,
            Guid? leadId = null,
            string? type = null,
            int page = 1,
            int pageSize = 50)
        {
            var url = $"/api/activities?page={page}&pageSize={pageSize}";
            if (customerId.HasValue) url += $"&customerId={customerId}";
            if (leadId.HasValue) url += $"&leadId={leadId}";
            if (!string.IsNullOrWhiteSpace(type)) url += $"&type={Uri.EscapeDataString(type)}";
            return GetAsync<PagedResponse<ActivityDto>>(url);
        }

        public Task<ActivityDto?> CreateAsync(CreateActivityRequest req)
            => PostAsync<ActivityDto>("/api/activities", req);
    }
}