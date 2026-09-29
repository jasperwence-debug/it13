using System.ComponentModel;

namespace CRM.domain.Enums
{
    [TypeConverter(typeof(StringEnumConverter<LeadStatus>))]
    public enum LeadStatus
    {
        New = 0,
        Contacted = 1,
        Qualified = 2,
        ProposalSent = 3,
        Negotiation = 4,
        Won = 5,
        Lost = 6
    }
}