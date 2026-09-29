namespace CRM.winforms.Models
{
    // ─── Overview ─────────────────────────────────────────
    public class OverviewReportDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalLeads { get; set; }
        public int ActiveLeads { get; set; }
        public decimal PipelineValue { get; set; }
        public int PendingFollowUps { get; set; }
        public int OverdueFollowUps { get; set; }
        public int CompletedFollowUpsThisMonth { get; set; }
        public int OpenInquiries { get; set; }
        public int OverdueInquiries { get; set; }
        public int ConvertedLeadsThisMonth { get; set; }
        public double ConversionRate { get; set; }
        public double RetentionRate { get; set; }
        public int AtRiskCustomers { get; set; }
        public DateTime RangeFrom { get; set; }
        public DateTime RangeTo { get; set; }
        public string Scope { get; set; } = "";
    }

    // ─── Pipeline ─────────────────────────────────────────
    public class PipelineStageDto
    {
        public string Stage { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class PipelineReportDto
    {
        public List<PipelineStageDto> Stages { get; set; } = new();
        public int TotalLeads { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    // ─── Activity ─────────────────────────────────────────
    public class ActivityTypeBreakdownDto
    {
        public string ActivityType { get; set; } = "";
        public int Count { get; set; }
    }

    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class UserActivityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public int TotalActivities { get; set; }
        public int Calls { get; set; }
        public int Emails { get; set; }
        public int Meetings { get; set; }
        public int Notes { get; set; }
        public int Tasks { get; set; }
    }

    public class ActivityReportDto
    {
        public int TotalActivities { get; set; }
        public List<ActivityTypeBreakdownDto> ByType { get; set; } = new();
        public List<DailyActivityDto> ByDay { get; set; } = new();
        public List<UserActivityDto> ByUser { get; set; } = new();
        public DateTime RangeFrom { get; set; }
        public DateTime RangeTo { get; set; }
    }

    // ─── Retention ────────────────────────────────────────
    public class AtRiskCustomerDto
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Company { get; set; } = "";
        public int DaysSinceLastActivity { get; set; }
        public DateTime LastActivityDate { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class MonthlyRetentionDto
    {
        public string Month { get; set; } = "";
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
    }

    public class RetentionReportDto
    {
        public double RetentionRate { get; set; }
        public double ChurnRate { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int AtRiskCustomers { get; set; }
        public List<AtRiskCustomerDto> AtRiskList { get; set; } = new();
        public List<MonthlyRetentionDto> MonthlyTrend { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    // ─── Conversion ───────────────────────────────────────
    public class ConversionBySourceDto
    {
        public string Source { get; set; } = "";
        public int TotalLeads { get; set; }
        public int Converted { get; set; }
        public double ConversionRate { get; set; }
    }

    public class ConversionReportDto
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public double OverallConversionRate { get; set; }
        public double AverageDaysToConvert { get; set; }
        public List<ConversionBySourceDto> BySource { get; set; } = new();
        public DateTime RangeFrom { get; set; }
        public DateTime RangeTo { get; set; }
    }

    // ─── Team ─────────────────────────────────────────────
    public class TeamMemberPerformanceDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public int AssignedCustomers { get; set; }
        public int AssignedLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int ActivitiesLogged { get; set; }
        public decimal PipelineValue { get; set; }
        public double ConversionRate { get; set; }
    }

    public class TeamPerformanceDto
    {
        public List<TeamMemberPerformanceDto> Members { get; set; } = new();
        public int TotalMembers { get; set; }
        public DateTime RangeFrom { get; set; }
        public DateTime RangeTo { get; set; }
    }
}