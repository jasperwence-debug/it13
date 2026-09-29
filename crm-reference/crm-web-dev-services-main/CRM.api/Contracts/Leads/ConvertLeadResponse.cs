namespace CRM.api.Contracts.Leads
{
    public record ConvertLeadResponse(
        Guid LeadId,
        Guid CustomerId,
        string CustomerName,
        string CustomerEmail);
}