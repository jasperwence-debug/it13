using System.ComponentModel.DataAnnotations;
using CRM.domain.Enums;

namespace CRM.api.Contracts.CustomerInquiries
{
    public record UpdateCustomerInquiryRequest(
        [Required, MaxLength(200)] string Subject,
        [Required, MaxLength(4000)] string Description,
        [Required] CustomerInquiryPriority Priority);
}