namespace CRM.api.Contracts.Reports
{
    public record TeamMemberPerformanceDto(
        Guid UserId,
        string Name,
        string Role,
        int AssignedCustomers,
        int AssignedLeads,
        int ConvertedLeads,
        int ActivitiesLogged,
        decimal PipelineValue,
        double ConversionRate
    );

    public record TeamPerformanceDto(
        IReadOnlyList<TeamMemberPerformanceDto> Members,
        int TotalMembers,
        DateTime RangeFrom,
        DateTime RangeTo
    );
}