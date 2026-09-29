namespace CRM.api.Contracts.Customers
{
    public record CreateCustomerRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Company,
        string Address,
        string Status,
        Guid? AssignedUserId);
}