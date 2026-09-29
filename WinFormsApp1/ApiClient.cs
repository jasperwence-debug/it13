using System.Net;
using System.Net.Http.Json;
using App.Domain.Common;
using App.Domain.Models.Email;
using App.WinForms.Core;

namespace App.WinForms
{
    // ============================================================
    // EXISTING DTOs — used by DataCollectionView wizard & Dashboard
    // ============================================================
    public class DataCollectionDto
    {
        // IDs (returned by API, needed for Edit/Delete)
        public int ServiceRequestId { get; set; }
        public int? LeadId { get; set; }
        public int? CustomerId { get; set; }

        // Normalized Name Fields (max length 50 each)
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        // Lead
        public string LeadName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _leadName;
            set
            {
                _leadName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _leadName = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }

        // Customer
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _customerName;
            set
            {
                _customerName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _customerName = string.Empty;

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

        // Normalized Name Fields
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        public string CustomerName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _customerName;
            set
            {
                _customerName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _customerName = string.Empty;

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
    // LAYER 1 — LEAD DTOs
    // ============================================================

    /// <summary>Input DTO for POST /api/leads (inquiry-only).</summary>
    public class LeadCreateDto
    {
        // Normalized Name Fields (max length 50 each)
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        public string LeadName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _leadName;
            set
            {
                _leadName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _leadName = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }
        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedSalesStaff { get; set; }
    }

    /// <summary>Input DTO for updating Lead status, quote, or lost reason.</summary>
    public class LeadStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal? QuotedPrice { get; set; }
        public string? LostReason { get; set; }
    }

    /// <summary>Output DTO from GET /api/leads.</summary>
    public class LeadDto
    {
        public int LeadId { get; set; }

        // Normalized Name Fields
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        public string LeadName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _leadName;
            set
            {
                _leadName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _leadName = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
        public string LeadSource { get; set; } = string.Empty;
        public string ServiceOfInterest { get; set; } = string.Empty;
        public string? InquiryDetails { get; set; }
        public string Status { get; set; } = "New";
        public decimal? QuotedPrice { get; set; }
        public string? ServiceAddress { get; set; }
        public string? LostReason { get; set; }
        public int? ConvertedCustomerId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedSalesStaff { get; set; }
    }

    /// <summary>Input DTO for POST /api/leads/{id}/convert.</summary>
    public class LeadConvertRequestDto
    {
        public bool ForceCreate { get; set; } = false;
        public int? UseExistingCustomerId { get; set; }
        public string? ServiceAddress { get; set; }
    }

    /// <summary>Potential duplicate customer details from 409 Conflict.</summary>
    public class LeadDuplicateMatchDto
    {
        public string Message { get; set; } = string.Empty;
        public int ExistingCustomerId { get; set; }
        public string ExistingCustomerName { get; set; } = string.Empty;
        public string ExistingContactInfo { get; set; } = string.Empty;
        public string? ExistingEmail { get; set; }
        public string ExistingLocation { get; set; } = string.Empty;
    }

    /// <summary>Successful lead conversion result.</summary>
    public class LeadConvertResultDto
    {
        public int CustomerId { get; set; }
        public int LeadId { get; set; }
        public bool WasExistingCustomer { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Unified API client response for lead conversion.</summary>
    public class LeadConvertApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool DuplicateConflict { get; set; }
        public LeadDuplicateMatchDto? DuplicateInfo { get; set; }
        public LeadConvertResultDto? Result { get; set; }
    }

    // ============================================================
    // LAYER 2 — CUSTOMER SUMMARY DTO
    // ============================================================

    /// <summary>
    /// Output DTO from GET /api/customers.
    /// One row per customer with computed booking aggregates.
    /// </summary>
    public class CustomerSummaryDto
    {
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }

        // Normalized Name Fields
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        public string CustomerName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _customerName;
            set
            {
                _customerName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _customerName = string.Empty;

        public string CustomerType { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string ServiceLocation { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int? DaysSinceLastService { get; set; }
        public bool IsAtRisk { get; set; }
        public string RetentionStatus { get; set; } = "Active";
        public string? LatestService { get; set; }
        public DateTime? LatestDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedSalesStaff { get; set; }
        public bool HasNegativeFeedback { get; set; }
        public int? LatestRating { get; set; }
    }

    /// <summary>
    /// Output DTO for GET /api/analytics/at-risk-customers.
    /// </summary>
    public class AtRiskCustomerDto
    {
        public int CustomerId { get; set; }

        // Normalized Name Fields
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }

        public string FullName => NameNormalizer.FormatFullName(FirstName, MiddleName, LastName, Suffix);

        public string CustomerName
        {
            get => !string.IsNullOrWhiteSpace(FullName) ? FullName : _customerName;
            set
            {
                _customerName = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(value))
                {
                    var (f, m, l) = NameNormalizer.SplitSingleString(value);
                    FirstName = f;
                    MiddleName = m;
                    LastName = l;
                }
            }
        }
        private string _customerName = string.Empty;

        public string CustomerType { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string ServiceLocation { get; set; } = string.Empty;
        public DateTime? LastCompletedDate { get; set; }
        public int DaysSinceLastService { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public string? LastServiceType { get; set; }
    }

    // ============================================================
    // LAYER 3 — WORK ORDER DTOs
    // ============================================================

    /// <summary>Input DTO for POST /api/servicerequests.</summary>
    public class WorkOrderCreateDto
    {
        public int CustomerId { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string AssignedStaff { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested";
        public decimal? QuotedPrice { get; set; }
        public string? SpecialRequests { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Output DTO from GET /api/servicerequests — work order grid row.</summary>
    public class WorkOrderDto
    {
        public int ServiceRequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public string AssignedStaff { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? QuotedPrice { get; set; }
        public decimal? ActualPrice { get; set; }
        public string? SpecialRequests { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Quality Assurance & Customer Feedback (Phase 6)
        public int? Rating { get; set; }
        public string? FeedbackNotes { get; set; }
        public string? InspectionStatus { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? FeedbackDate { get; set; }
    }

    /// <summary>Input DTO for PUT /api/servicerequests/{id}/dispatch.</summary>
    public class WorkOrderDispatchDto
    {
        public string AssignedStaff { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Input DTO for PUT /api/servicerequests/{id}/status.</summary>
    public class WorkOrderStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal? ActualPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime? ScheduledDate { get; set; }
    }

    /// <summary>Input DTO for PUT /api/servicerequests/{id}/feedback.</summary>
    public class ServiceFeedbackDto
    {
        public int Rating { get; set; } // 1 to 5
        public string? FeedbackNotes { get; set; }
        public string? InspectionStatus { get; set; } // "Passed", "NeedsRework", "Pending"
        public string? InspectedBy { get; set; }
    }

    // ============================================================
    // MASTER TIER — SUBSCRIPTION DTOs
    // ============================================================
    public class SubscriptionDto
    {
        public int SubscriptionId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty; // Micro, Small, Medium
        public int TierValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public bool HasDataCollection { get; set; }
        public bool HasTransactions { get; set; }
        public bool HasBusinessIntelligence { get; set; }
        public bool HasActions { get; set; }
        public bool HasBranching { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SubscriptionUpdateDto
    {
        public int Tier { get; set; }
        public string Status { get; set; } = "Active";
        public string BillingCycle { get; set; } = "Monthly";
        public decimal? MonthlyPrice { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    public class SubscriptionCreateDto
    {
        public int CompanyId { get; set; }
        public int Tier { get; set; } = 1;
        public string Status { get; set; } = "Active";
        public string BillingCycle { get; set; } = "Monthly";
        public decimal? MonthlyPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    // ============================================================
    // TENANT C (MEDIUM ENTERPRISE) — BRANCH DTOs
    // ============================================================
    public class BranchDto
    {
        public int BranchId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ActiveWorkOrdersCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BranchCreateDto
    {
        public int CompanyId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
    }

    public class BranchUpdateDto
    {
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    // ============================================================
    // API CLIENT
    // ============================================================
    public class ApiClient
    {
        private const string BaseUrl = "http://localhost:5000";

        public static bool LastConnectionFailed { get; private set; }
        public static string? LastErrorMessage { get; private set; }

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
        // CUSTOMER LOOKUP / DEDUPLICATION (existing)
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
                    LastConnectionFailed = false;
                    LastErrorMessage = null;
                    return await response.Content.ReadFromJsonAsync<CustomerDto>();
                }

                return null;
            }
            catch (Exception ex)
            {
                LastConnectionFailed = true;
                LastErrorMessage = ex.Message;
                return null;
            }
        }

        // ------------------------------------------------------------
        // DASHBOARD / BI ANALYTICS (existing)
        // ------------------------------------------------------------
        public async Task<DashboardDto?> GetDashboardAsync(string? assignedStaff = null, int? assignedUserId = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (SessionManager.IsSalesStaff && SessionManager.CurrentUser != null)
                {
                    queryParams.Add($"assignedStaff={Uri.EscapeDataString(SessionManager.CurrentUser.Username)}");
                    queryParams.Add($"assignedUserId={SessionManager.CurrentUser.Id}");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(assignedStaff))
                        queryParams.Add($"assignedStaff={Uri.EscapeDataString(assignedStaff.Trim())}");
                    if (assignedUserId.HasValue)
                        queryParams.Add($"assignedUserId={assignedUserId.Value}");
                }

                var url = "/api/analytics/dashboard";
                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                var result = await _http.GetFromJsonAsync<DashboardDto>(url);
                LastConnectionFailed = false;
                LastErrorMessage = null;
                return result;
            }
            catch (Exception ex)
            {
                LastConnectionFailed = true;
                LastErrorMessage = ex.Message;
                return null;
            }
        }

        // ------------------------------------------------------------
        // LEGACY DATA COLLECTION (existing — preserved)
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

        // ============================================================
        // LAYER 1 — LEADS API METHODS
        // ============================================================

        /// <summary>Returns active or filtered leads for the Leads grid. Scoped by assigned user when provided or for Sales Staff.</summary>
        public async Task<List<LeadDto>> GetLeadsAsync(string? status = null, bool includeConverted = false, int? assignedUserId = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (includeConverted) queryParams.Add("includeConverted=true");
                if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

                // Sales staff is strictly scoped to their own leads
                if (SessionManager.IsSalesStaff && SessionManager.CurrentUser != null)
                {
                    queryParams.Add($"assignedUserId={SessionManager.CurrentUser.Id}");
                }
                else if (assignedUserId.HasValue)
                {
                    queryParams.Add($"assignedUserId={assignedUserId.Value}");
                }

                var url = "/api/leads";
                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var result = await _http.GetFromJsonAsync<List<LeadDto>>(url);
                return result ?? new List<LeadDto>();
            }
            catch
            {
                return new List<LeadDto>();
            }
        }

        /// <summary>
        /// Creates a new lead inquiry. Phone/Email are stored as "Phone | Email".
        /// NEVER creates ServiceRequests.
        /// </summary>
        public async Task<(bool Success, string Message, LeadDto? Lead)> CreateLeadAsync(LeadCreateDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/leads", dto);
                if (response.IsSuccessStatusCode)
                {
                    var lead = await response.Content.ReadFromJsonAsync<LeadDto>();
                    return (true, "Lead captured successfully!", lead);
                }
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nIs App.API running?\n\nDetails: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Converts a Lead to a Customer record.
        /// Handles role-based access, duplicate resolution, and returns rich result info.
        /// NEVER creates ServiceRequests.
        /// </summary>
        public async Task<LeadConvertApiResponse> ConvertLeadAsync(int leadId, bool forceCreate = false, int? useExistingCustomerId = null, string? serviceAddress = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leads/{leadId}/convert");
                if (!string.IsNullOrEmpty(SessionManager.CurrentRole))
                {
                    request.Headers.Add("X-User-Role", SessionManager.CurrentRole);
                }
                request.Content = JsonContent.Create(new LeadConvertRequestDto
                {
                    ForceCreate = forceCreate,
                    UseExistingCustomerId = useExistingCustomerId,
                    ServiceAddress = serviceAddress
                });

                var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LeadConvertResultDto>();
                    return new LeadConvertApiResponse
                    {
                        Success = true,
                        Message = result?.Message ?? "Lead successfully converted.",
                        Result = result
                    };
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var dup = await response.Content.ReadFromJsonAsync<LeadDuplicateMatchDto>();
                    return new LeadConvertApiResponse
                    {
                        Success = false,
                        DuplicateConflict = true,
                        DuplicateInfo = dup,
                        Message = dup?.Message ?? "A customer with matching contact details already exists."
                    };
                }

                var error = await response.Content.ReadAsStringAsync();
                return new LeadConvertApiResponse
                {
                    Success = false,
                    Message = $"Conversion failed ({(int)response.StatusCode}): {error}"
                };
            }
            catch (HttpRequestException ex)
            {
                return new LeadConvertApiResponse
                {
                    Success = false,
                    Message = $"Cannot reach API at {BaseUrl}.\n\nDetails: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new LeadConvertApiResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Updates a Lead's lifecycle status, quoted price, or lost reason.
        /// </summary>
        public async Task<(bool Success, string Message, LeadDto? Lead)> UpdateLeadStatusAsync(int leadId, LeadStatusUpdateDto dto)
        {
            try
            {
                var response = await _http.PatchAsJsonAsync($"/api/leads/{leadId}/status", dto);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<LeadDto>();
                    return (true, "Status updated.", updated);
                }
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to update status ({(int)response.StatusCode}): {error}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error updating lead status: {ex.Message}", null);
            }
        }

        // Internal helper for ConvertLead deserialization
        private class ConvertLeadResult
        {
            public int CustomerId { get; set; }
            public int LeadId { get; set; }
            public bool WasExistingCustomer { get; set; }
        }

        // ============================================================
        // LAYER 2 — CUSTOMERS API METHODS
        // ============================================================

        /// <summary>
        /// Returns all active customers — one row per customer
        /// with TotalBookings, LatestService, LatestDate aggregates.
        /// Scoped to assigned user for Sales Staff.
        /// </summary>
        public async Task<List<CustomerSummaryDto>> GetCustomersAsync(int? assignedUserId = null)
        {
            try
            {
                var url = "/api/customers";
                int? effectiveUserId = (SessionManager.IsSalesStaff && SessionManager.CurrentUser != null)
                    ? SessionManager.CurrentUser.Id
                    : assignedUserId;

                var queryParams = new List<string>();
                if (effectiveUserId.HasValue)
                {
                    queryParams.Add($"assignedUserId={effectiveUserId.Value}");
                }
                if (SessionManager.IsSalesStaff && !string.IsNullOrWhiteSpace(SessionManager.CurrentUser?.Username))
                {
                    queryParams.Add($"assignedStaff={Uri.EscapeDataString(SessionManager.CurrentUser.Username)}");
                }

                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var result = await _http.GetFromJsonAsync<List<CustomerSummaryDto>>(url);
                LastConnectionFailed = false;
                LastErrorMessage = null;
                return result ?? new List<CustomerSummaryDto>();
            }
            catch (Exception ex)
            {
                LastConnectionFailed = true;
                LastErrorMessage = ex.Message;
                return new List<CustomerSummaryDto>();
            }
        }

        /// <summary>
        /// Reassigns lead ownership to a specific staff member.
        /// </summary>
        public async Task<(bool Success, string Message)> AssignLeadAsync(int leadId, int assignedUserId, string assignedSalesStaff)
        {
            try
            {
                var resp = await _http.PatchAsJsonAsync($"/api/leads/{leadId}/assign", new { assignedUserId, assignedSalesStaff });
                return (resp.IsSuccessStatusCode, resp.IsSuccessStatusCode ? "Lead reassigned successfully." : "Failed to reassign lead.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Reassigns customer account ownership to a specific staff member.
        /// </summary>
        public async Task<(bool Success, string Message)> AssignCustomerAsync(int customerId, int assignedUserId, string assignedSalesStaff)
        {
            try
            {
                var resp = await _http.PatchAsJsonAsync($"/api/customers/{customerId}/assign", new { assignedUserId, assignedSalesStaff });
                return (resp.IsSuccessStatusCode, resp.IsSuccessStatusCode ? "Customer reassigned successfully." : "Failed to reassign customer.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ============================================================
        // LAYER 3 — WORK ORDERS API METHODS
        // ============================================================

        /// <summary>Returns active work orders for the grid, auto-scoped for Sales Staff.</summary>
        public async Task<List<WorkOrderDto>> GetWorkOrdersAsync(string? assignedStaff = null)
        {
            try
            {
                var url = "/api/servicerequests";
                if (string.IsNullOrEmpty(assignedStaff) && SessionManager.IsSalesStaff && SessionManager.CurrentUser != null)
                {
                    assignedStaff = SessionManager.CurrentUser.Username;
                }

                if (!string.IsNullOrEmpty(assignedStaff))
                {
                    url += $"?assignedStaff={Uri.EscapeDataString(assignedStaff)}";
                }

                var result = await _http.GetFromJsonAsync<List<WorkOrderDto>>(url);
                LastConnectionFailed = false;
                LastErrorMessage = null;
                return result ?? new List<WorkOrderDto>();
            }
            catch (Exception ex)
            {
                LastConnectionFailed = true;
                LastErrorMessage = ex.Message;
                return new List<WorkOrderDto>();
            }
        }

        /// <summary>Returns work orders for a specific customer (for detail modal).</summary>
        public async Task<List<WorkOrderDto>> GetWorkOrdersByCustomerAsync(int customerId)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<WorkOrderDto>>($"/api/servicerequests/customer/{customerId}");
                return result ?? new List<WorkOrderDto>();
            }
            catch
            {
                return new List<WorkOrderDto>();
            }
        }

        /// <summary>
        /// Creates a new work order or booking request. Requires an existing CustomerId.
        /// Sends authenticated user headers.
        /// </summary>
        public async Task<(bool Success, string Message, WorkOrderDto? WorkOrder)> CreateWorkOrderAsync(WorkOrderCreateDto dto)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/servicerequests");
                if (!string.IsNullOrEmpty(SessionManager.CurrentRole))
                {
                    request.Headers.Add("X-User-Role", SessionManager.CurrentRole);
                }
                if (SessionManager.CurrentUser != null && !string.IsNullOrEmpty(SessionManager.CurrentUser.Username))
                {
                    request.Headers.Add("X-User-Name", SessionManager.CurrentUser.Username);
                }
                request.Content = JsonContent.Create(dto);

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
                    return (true, "Booking request submitted successfully!", created);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nIs App.API running?\n\nDetails: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Dispatches and schedules a work order with an assigned technician/crew.
        /// Requires Manager, Admin, or SuperAdmin role.
        /// </summary>
        public async Task<(bool Success, string Message, WorkOrderDto? WorkOrder)> DispatchWorkOrderAsync(int workOrderId, WorkOrderDispatchDto dto)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/servicerequests/{workOrderId}/dispatch");
                if (!string.IsNullOrEmpty(SessionManager.CurrentRole))
                {
                    request.Headers.Add("X-User-Role", SessionManager.CurrentRole);
                }
                if (SessionManager.CurrentUser != null && !string.IsNullOrEmpty(SessionManager.CurrentUser.Username))
                {
                    request.Headers.Add("X-User-Name", SessionManager.CurrentUser.Username);
                }
                request.Content = JsonContent.Create(dto);

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
                    return (true, "Work order dispatched and scheduled successfully!", updated);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nIs App.API running?\n\nDetails: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Updates the execution lifecycle status (Scheduled -> InProgress -> Completed / Cancelled / Rescheduled).
        /// Requires Manager, Admin, or SuperAdmin role.
        /// </summary>
        public async Task<(bool Success, string Message, WorkOrderDto? WorkOrder)> UpdateWorkOrderStatusAsync(int workOrderId, WorkOrderStatusUpdateDto dto)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/servicerequests/{workOrderId}/status");
                if (!string.IsNullOrEmpty(SessionManager.CurrentRole))
                {
                    request.Headers.Add("X-User-Role", SessionManager.CurrentRole);
                }
                if (SessionManager.CurrentUser != null && !string.IsNullOrEmpty(SessionManager.CurrentUser.Username))
                {
                    request.Headers.Add("X-User-Name", SessionManager.CurrentUser.Username);
                }
                request.Content = JsonContent.Create(dto);

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
                    return (true, $"Work order status updated to '{dto.Status}' successfully!", updated);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"API error ({(int)response.StatusCode}): {error}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nIs App.API running?\n\nDetails: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Fetches the list of active technicians and field staff members.
        /// </summary>
        public async Task<List<string>> GetAvailableStaffAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<string>>("/api/servicerequests/staff");
                if (result != null && result.Count > 0)
                    return result;
            }
            catch
            {
                // Fallback to standard staff list
            }

            return new List<string>
            {
                "Pedro Reyes", "Maria Santos", "Mark Anthony", "Sarah Jane", "Michael John", "Jessica Mae"
            };
        }

        /// <summary>
        /// Fetches customers with no completed bookings in 60+ days for retention re-engagement.
        /// </summary>
        public async Task<List<AtRiskCustomerDto>> GetAtRiskCustomersAsync(string? assignedStaff = null, int? assignedUserId = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (SessionManager.IsSalesStaff && SessionManager.CurrentUser != null)
                {
                    queryParams.Add($"assignedStaff={Uri.EscapeDataString(SessionManager.CurrentUser.Username)}");
                    queryParams.Add($"assignedUserId={SessionManager.CurrentUser.Id}");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(assignedStaff))
                        queryParams.Add($"assignedStaff={Uri.EscapeDataString(assignedStaff.Trim())}");
                    if (assignedUserId.HasValue)
                        queryParams.Add($"assignedUserId={assignedUserId.Value}");
                }

                var url = "/api/analytics/at-risk-customers";
                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                var result = await _http.GetFromJsonAsync<List<AtRiskCustomerDto>>(url);
                return result ?? new List<AtRiskCustomerDto>();
            }
            catch
            {
                return new List<AtRiskCustomerDto>();
            }
        }

        /// <summary>
        /// Submits quality assurance inspection and customer rating/feedback for a completed work order.
        /// </summary>
        public async Task<(bool Success, string Message, WorkOrderDto? WorkOrder)> SubmitWorkOrderFeedbackAsync(int serviceRequestId, ServiceFeedbackDto dto)
        {
            try
            {
                var role = SessionManager.CurrentUser?.Role ?? Roles.Manager;
                var user = SessionManager.CurrentUser?.Username ?? "Supervisor";

                using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/servicerequests/{serviceRequestId}/feedback");
                request.Headers.Add("X-User-Role", role);
                request.Headers.Add("X-User-Name", user);
                request.Content = JsonContent.Create(dto);

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
                    return (true, "Feedback & QA recorded successfully!", updated);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to record feedback ({(int)response.StatusCode}): {error}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Cannot reach API at {BaseUrl}.\n\nDetails: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        // ============================================================
        // MASTER TIER — SUBSCRIPTION API METHODS
        // ============================================================
        public async Task<List<SubscriptionDto>> GetSubscriptionsAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<SubscriptionDto>>("/api/subscriptions");
                return result ?? new List<SubscriptionDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch subscriptions: {ex.Message}");
                return new List<SubscriptionDto>();
            }
        }

        public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<SubscriptionDto>($"/api/subscriptions/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateSubscriptionAsync(int id, SubscriptionUpdateDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/subscriptions/{id}", dto);
                if (response.IsSuccessStatusCode)
                    return (true, "Subscription updated successfully!");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to update subscription: {error}");
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, SubscriptionDto? Result)> CreateSubscriptionAsync(SubscriptionCreateDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/subscriptions", dto);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
                    return (true, "Subscription created successfully!", created);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to create subscription: {error}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteSubscriptionAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/subscriptions/{id}");
                if (response.IsSuccessStatusCode)
                    return (true, "Subscription deleted successfully!");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to delete subscription: {error}");
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}");
            }
        }

        // ============================================================
        // TENANT C (MEDIUM ENTERPRISE) — BRANCH API METHODS
        // ============================================================
        public async Task<List<BranchDto>> GetBranchesAsync(int? companyId = null)
        {
            try
            {
                string url = companyId.HasValue ? $"/api/branches?companyId={companyId.Value}" : "/api/branches";
                var result = await _http.GetFromJsonAsync<List<BranchDto>>(url);
                return result ?? new List<BranchDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch branches: {ex.Message}");
                return new List<BranchDto>();
            }
        }

        public async Task<BranchDto?> GetBranchByIdAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<BranchDto>($"/api/branches/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message, BranchDto? Result)> CreateBranchAsync(BranchCreateDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/branches", dto);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<BranchDto>();
                    return (true, "Branch created successfully!", created);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to create branch: {error}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateBranchAsync(int id, BranchUpdateDto dto)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/branches/{id}", dto);
                if (response.IsSuccessStatusCode)
                    return (true, "Branch updated successfully!");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to update branch: {error}");
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteBranchAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/branches/{id}");
                if (response.IsSuccessStatusCode)
                    return (true, "Branch deactivated successfully!");

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Failed to deactivate branch: {error}");
            }
            catch (Exception ex)
            {
                return (false, $"Error contacting server: {ex.Message}");
            }
        }

        // ============================================================
        // RETENTION & WIN-BACK SMTP EMAIL AUTOMATION
        // ============================================================

        public async Task<EmailResult> SendWinBackEmailAsync(WinBackEmailRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/retention/send-winback", request);
                var content = await response.Content.ReadFromJsonAsync<EmailResult>();

                if (content != null)
                {
                    return content;
                }

                if (response.IsSuccessStatusCode)
                {
                    return EmailResult.Success(request.RecipientEmail, "Win-back email dispatched successfully via SMTP.");
                }

                return EmailResult.Failed(request.RecipientEmail, $"Server returned {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                return EmailResult.Failed(request.RecipientEmail, $"Connection error: {ex.Message}", ex.ToString(),
                    "Ensure App.API is running on http://localhost:5000.");
            }
        }

        public async Task<WinBackBatchResult> BatchSendWinBackEmailAsync(WinBackBatchRequest batchRequest)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/retention/batch-winback", batchRequest);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<WinBackBatchResult>();
                    return result ?? new WinBackBatchResult { TotalRequested = batchRequest.CustomerIds.Count };
                }

                var errorResult = await response.Content.ReadFromJsonAsync<WinBackBatchResult>();
                return errorResult ?? new WinBackBatchResult
                {
                    TotalRequested = batchRequest.CustomerIds.Count,
                    TotalFailed = batchRequest.CustomerIds.Count,
                    Results = new List<EmailResult>
                    {
                        EmailResult.Failed(string.Empty, $"Batch request failed: {(int)response.StatusCode} {response.ReasonPhrase}")
                    }
                };
            }
            catch (Exception ex)
            {
                return new WinBackBatchResult
                {
                    TotalRequested = batchRequest.CustomerIds.Count,
                    TotalFailed = batchRequest.CustomerIds.Count,
                    Results = new List<EmailResult>
                    {
                        EmailResult.Failed(string.Empty, $"Network error dispatching batch win-back: {ex.Message}", ex.ToString())
                    }
                };
            }
        }

        public async Task<SmtpStatusDto?> GetSmtpStatusAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<SmtpStatusDto>("/api/retention/smtp-status");
            }
            catch
            {
                return null;
            }
        }

        public async Task<EmailResult> SendTestEmailAsync(string recipientEmail)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/retention/test-email", new { RecipientEmail = recipientEmail });
                var result = await response.Content.ReadFromJsonAsync<EmailResult>();
                return result ?? EmailResult.Failed(recipientEmail, "Empty response from server.");
            }
            catch (Exception ex)
            {
                return EmailResult.Failed(recipientEmail, $"Error sending test email: {ex.Message}");
            }
        }
    }

    public class SmtpStatusDto
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public bool IsValid { get; set; }
    }
}