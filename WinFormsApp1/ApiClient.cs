using System.Net;
using System.Net.Http.Json;

namespace App.WinForms
{
    // ============================================================
    // DTO — matches JSON shape sent to / received from API
    // ============================================================
    public class DataCollectionDto
    {
        // IDs (returned by API, needed for Edit/Delete)
        public int ServiceRequestId { get; set; }
        public int? LeadId { get; set; }
        public int? CustomerId { get; set; }

        // Lead
        public string LeadName { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }

        // Customer
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;

        // Service Request
        public string RequestedService { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string? SpecialRequests { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public string AssignedSalesStaff { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
    }

    // ============================================================
    // DASHBOARD & BI ANALYTICS DTOs
    // ============================================================
    public class MonthlyTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopServiceDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardDto
    {
        public int TotalCustomers { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int ScheduledBookings { get; set; }
        public int RequestedBookings { get; set; }
        public double LeadConversionRate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingValue { get; set; }
        public double RepeatCustomerRate { get; set; }
        public int AtRiskCustomerCount { get; set; }
        public List<MonthlyTrendDto> MonthlyTrend { get; set; } = new();
        public List<TopServiceDto> TopServices { get; set; } = new();
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
    }

    // ============================================================
    // API client
    // ============================================================
    public class ApiClient
    {
        private const string BaseUrl = "http://localhost:5000";

        private readonly HttpClient _http;

        public ApiClient()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // ------------------------------------------------------------
        // CUSTOMER LOOKUP / DEDUPLICATION
        // ------------------------------------------------------------
        public async Task<CustomerDto?> CheckCustomerExistsAsync(string contactInfo)
        {
            if (string.IsNullOrWhiteSpace(contactInfo))
                return null;

            try
            {
                var encoded = Uri.EscapeDataString(contactInfo.Trim());
                var response = await _http.GetAsync($"/api/customers/check/{encoded}");

                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                {
                    response = await _http.GetAsync($"/api/customers/check?contact={encoded}");
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CustomerDto>();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------
        // DASHBOARD / BI ANALYTICS
        // ------------------------------------------------------------
        public async Task<DashboardDto?> GetDashboardAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<DashboardDto>("/api/analytics/dashboard");
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public async Task<(bool Success, string Message)> SaveAsync(DataCollectionDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/data-collection", dto);

                if (response.IsSuccessStatusCode)
                    return (true, "Record saved successfully!");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nIs App.API running?\n\nDetails: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        // ------------------------------------------------------------
        // READ — ALL
        // ------------------------------------------------------------
        public async Task<List<DataCollectionDto>> GetAllAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<DataCollectionDto>>("/api/data-collection");
                return result ?? new List<DataCollectionDto>();
            }
            catch
            {
                return new List<DataCollectionDto>();
            }
        }

        // ------------------------------------------------------------
        // READ — ONE
        // ------------------------------------------------------------
        public async Task<DataCollectionDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<DataCollectionDto>($"/api/data-collection/{id}");
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        public async Task<(bool Success, string Message)> UpdateAsync(int id, DataCollectionDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/data-collection/{id}", dto);

                if (response.IsSuccessStatusCode)
                    return (true, "Record updated successfully!");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (false, $"Record with id {id} not found.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nDetails: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        // ------------------------------------------------------------
        // DELETE (soft delete — sets IsActive = false)
        // ------------------------------------------------------------
        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/data-collection/{id}");

                if (response.IsSuccessStatusCode)
                    return (true, "Record deleted successfully.");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (false, $"Record with id {id} not found.");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nDetails: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }
    }
}