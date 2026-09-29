using CRM.domain.Enums;

namespace CRM.api.Contracts.Leads
{
    public record UpdateLeadStatusRequest(LeadStatus Status);
}