using CRM.domain.Enums;

namespace CRM.api.Contracts.CustomerInquiries
{
    public record CustomerInquiryDto(
        Guid Id,
        Guid CustomerId,
        string? CustomerName,
        CustomerInquiryType Type,
        string Subject,
        string Description,
        CustomerInquiryStatus Status,
        CustomerInquiryPriority Priority,
        string? Resolution,
        DateTime? ResolvedAt,
        Guid? ResolvedByUserId,
        string? ResolvedByUserName,
        Guid? AssignedUserId,
        string? AssignedUserName,
        Guid CreatedByUserId,
        string? CreatedByUserName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsOverdue);
}