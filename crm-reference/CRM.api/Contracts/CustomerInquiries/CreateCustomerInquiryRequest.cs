using System.ComponentModel.DataAnnotations;
using CRM.domain.Enums;

namespace CRM.api.Contracts.CustomerInquiries
{
    public record CreateCustomerInquiryRequest(
        [Required] Guid CustomerId,
        [Required] CustomerInquiryType Type,
        [Required, MaxLength(200)] string Subject,
        [Required, MaxLength(4000)] string Description,
        CustomerInquiryPriority? Priority,
        Guid? AssignedUserId);
}