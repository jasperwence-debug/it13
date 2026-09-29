namespace CRM.api.Contracts.Customers
{
    public record UpdateCustomerRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Company,
        string Address,
        string Status);
}