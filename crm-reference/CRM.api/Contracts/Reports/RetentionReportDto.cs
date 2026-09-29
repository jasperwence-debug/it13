namespace CRM.api.Contracts.Reports
{
    public record AtRiskCustomerDto(
        Guid CustomerId,
        string Name,
        string Email,
        string Company,
        int DaysSinceLastActivity,
        DateTime LastActivityDate,
        string? AssignedUserName
    );

    public record MonthlyRetentionDto(
        string Month,          // "2026-03"
        int NewCustomers,
        int ReturningCustomers
    );

    public record RetentionReportDto(
        double RetentionRate,
        double ChurnRate,
        int TotalCustomers,
        int ActiveCustomers,
        int AtRiskCustomers,
        IReadOnlyList<AtRiskCustomerDto> AtRiskList,
        IReadOnlyList<MonthlyRetentionDto> MonthlyTrend,
        DateTime GeneratedAt
    );
}