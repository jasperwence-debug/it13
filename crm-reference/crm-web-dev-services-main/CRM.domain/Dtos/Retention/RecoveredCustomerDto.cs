namespace CRM.domain.Dtos.Retention
{
    public record RecoveredCustomerDto(
        Guid CustomerId,
        string Name,
        string Email,
        string Company,
        DateTime WinBackCreatedAt,
        DateTime FirstActivityAfterWinBack,
        int DaysToRecover
    );
}