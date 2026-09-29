using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class CustomerApiClient : ApiClientBase
    {
        public Task<PagedResponse<CustomerDto>?> ListAsync(
            string? search = null, string? status = null)
        {
            var url = "/api/customers";
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (q.Count > 0) url += "?" + string.Join("&", q);

            return GetAllWrapped(url);
        }

        public Task<CustomerDto?> GetAsync(Guid id)
            => GetAsync<CustomerDto>($"/api/customers/{id}");

        public Task<CustomerDto?> CreateAsync(CreateCustomerRequest req)
            => PostAsync<CustomerDto>("/api/customers", req);

        public Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest req)
            => PutAsync<CustomerDto>($"/api/customers/{id}", req);

        public Task DeleteAsync(Guid id)
            => DeleteRequestAsync($"/api/customers/{id}");

        public Task<CustomerDto?> AssignAsync(Guid id, Guid assignedUserId)
            => PatchAsync<CustomerDto>(
                $"/api/customers/{id}/assign",
                new { assignedUserId });

        private async Task<PagedResponse<CustomerDto>?> GetAllWrapped(string url)
        {
            var items = await GetAsync<List<CustomerDto>>(url);
            return new PagedResponse<CustomerDto>
            {
                Page = 1,
                PageSize = items?.Count ?? 0,
                Total = items?.Count ?? 0,
                Items = items ?? new()
            };
        }
    }
}