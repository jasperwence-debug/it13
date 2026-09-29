using CRM.domain.Enums;

namespace CRM.api.Contracts.CustomerInquiries
{
    public record UpdateInquiryStatusRequest(
        CustomerInquiryStatus Status,
        string? Resolution);
}