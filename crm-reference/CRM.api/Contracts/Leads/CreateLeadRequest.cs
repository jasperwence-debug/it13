using System.ComponentModel.DataAnnotations;
using CRM.domain.Enums;

namespace CRM.api.Contracts.Leads
{
    public record CreateLeadRequest(
        [Required, MaxLength(200)] string Name,
        [EmailAddress, MaxLength(256)] string Email,
        [MaxLength(50)] string Phone,
        [MaxLength(100)] string Source,
        LeadStatus Status,
        LeadPriority Priority,
        [Range(0, 999_999_999)] decimal ExpectedValue,
        [MaxLength(2000)] string Notes,
        Guid? AssignedUserId);
}