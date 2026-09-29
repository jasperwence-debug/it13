using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class CustomerInquiryApiClient : ApiClientBase
    {
        public Task<PagedResponse<CustomerInquiryDto>?> ListAsync(
            string? type = null,
            string? status = null,
            Guid? customerId = null,
            int page = 1,
            int pageSize = 100)
        {
            var url = $"/api/customer-inquiries?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(type)) url += $"&type={Uri.EscapeDataString(type)}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (customerId.HasValue) url += $"&customerId={customerId}";
            return GetAsync<PagedResponse<CustomerInquiryDto>>(url);
        }

        public Task<CustomerInquiryDto?> GetAsync(Guid id)
            => GetAsync<CustomerInquiryDto>($"/api/customer-inquiries/{id}");

        public Task<List<CustomerInquiryDto>?> ListByCustomerAsync(Guid customerId)
            => GetAsync<List<CustomerInquiryDto>>($"/api/customer-inquiries/by-customer/{customerId}");

        public Task<CustomerInquiryDto?> CreateAsync(CreateCustomerInquiryRequest req)
            => PostAsync<CustomerInquiryDto>("/api/customer-inquiries", req);

        public Task<CustomerInquiryDto?> UpdateAsync(Guid id, UpdateCustomerInquiryRequest req)
            => PutAsync<CustomerInquiryDto>($"/api/customer-inquiries/{id}", req);

        public Task<CustomerInquiryDto?> UpdateStatusAsync(Guid id, string status, string? resolution)
            => PatchAsync<CustomerInquiryDto>(
                $"/api/customer-inquiries/{id}/status",
                new UpdateInquiryStatusRequest { Status = status, Resolution = resolution });
    }
}