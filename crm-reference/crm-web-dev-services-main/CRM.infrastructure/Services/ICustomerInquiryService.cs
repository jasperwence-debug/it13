using CRM.domain.Entities;
using CRM.domain.Enums;

namespace CRM.infrastructure.Services
{
    public interface ICustomerInquiryService
    {
        // ─── Queries ─────────────────────────────────────────
        Task<IReadOnlyList<CustomerInquiry>> ListAsync(
            Guid? customerId,
            CustomerInquiryType? type,
            CustomerInquiryStatus? status,
            Guid? callerUserId,
            string? callerRole,
            int page,
            int pageSize);

        Task<int> CountAsync(
            Guid? customerId,
            CustomerInquiryType? type,
            CustomerInquiryStatus? status,
            Guid? callerUserId,
            string? callerRole);

        Task<CustomerInquiry?> GetByIdAsync(
            Guid id,
            Guid? callerUserId,
            string? callerRole);

        // ─── Commands ────────────────────────────────────────
        Task<CustomerInquiry> CreateAsync(CustomerInquiry inquiry);

        Task<CustomerInquiry?> UpdateAsync(
            Guid id,
            CustomerInquiry updated,
            Guid? callerUserId,
            string? callerRole);

        Task<CustomerInquiry?> UpdateStatusAsync(
            Guid id,
            CustomerInquiryStatus status,
            string? resolution,
            Guid? callerUserId,
            string? callerRole);

        // ─── Helpers ─────────────────────────────────────────
        Task<bool> CustomerExistsAsync(Guid customerId);
        Task<User?> FindUserAsync(Guid userId);
    }
}