namespace CRM.domain.Dtos.Reports
{
    public record ActivityTypeBreakdownDto(
        string ActivityType,
        int Count
    );

    public record DailyActivityDto(
        DateTime Date,
        int Count
    );

    public record UserActivityDto(
        Guid UserId,
        string UserName,
        int TotalActivities,
        int Calls,
        int Emails,
        int Meetings,
        int Notes,
        int Tasks
    );

    public record ActivityReportDto(
        int TotalActivities,
        IReadOnlyList<ActivityTypeBreakdownDto> ByType,
        IReadOnlyList<DailyActivityDto> ByDay,
        IReadOnlyList<UserActivityDto> ByUser,
        DateTime RangeFrom,
        DateTime RangeTo
    );
}