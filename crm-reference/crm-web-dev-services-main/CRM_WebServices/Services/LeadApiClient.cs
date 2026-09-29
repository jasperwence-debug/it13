using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class LeadApiClient : ApiClientBase
    {
        public Task<PagedResponse<LeadDto>?> ListAsync(
            string? search = null,
            string? status = null,
            string? priority = null,
            int page = 1,
            int pageSize = 20)
        {
            var url = $"/api/leads?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrWhiteSpace(priority)) url += $"&priority={Uri.EscapeDataString(priority)}";
            return GetAsync<PagedResponse<LeadDto>>(url);
        }

        public Task<LeadDto?> GetAsync(Guid id)
            => GetAsync<LeadDto>($"/api/leads/{id}");

        public Task<LeadDto?> CreateAsync(CreateLeadRequest req)
            => PostAsync<LeadDto>("/api/leads", req);

        public Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest req)
            => PutAsync<LeadDto>($"/api/leads/{id}", req);

        public Task UpdateStatusAsync(Guid id, string status)
            => PatchAsync($"/api/leads/{id}/status", new { status });

        public Task DeleteAsync(Guid id)
            => DeleteRequestAsync($"/api/leads/{id}");

        public Task<LeadDto?> AssignAsync(Guid id, Guid assignedUserId)
            => PatchAsync<LeadDto>(
                $"/api/leads/{id}/assign",
                new { assignedUserId });

        public Task<ConvertLeadResponse?> ConvertAsync(Guid id)
            => PostAsync<ConvertLeadResponse>($"/api/leads/{id}/convert", new { });
    }

    public class ConvertLeadResponse
    {
        public Guid LeadId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
    }
}