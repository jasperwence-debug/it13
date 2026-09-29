namespace CRM.domain.Dtos.Reports
{
    public record OverviewReportDto(
        // Customer metrics
        int TotalCustomers,
        int ActiveCustomers,
        int NewCustomersThisMonth,

        // Lead metrics
        int TotalLeads,
        int ActiveLeads,
        decimal PipelineValue,

        // Follow-up metrics
        int PendingFollowUps,
        int OverdueFollowUps,
        int CompletedFollowUpsThisMonth,

        // Inquiry metrics
        int OpenInquiries,
        int OverdueInquiries,

        // Conversions
        int ConvertedLeadsThisMonth,
        double ConversionRate,   // 0.0 to 1.0

        // Retention
        double RetentionRate,    // 0.0 to 1.0
        int AtRiskCustomers,

        // Meta
        DateTime RangeFrom,
        DateTime RangeTo,
        string Scope             // "Personal" | "Team" | "Tenant"
    );
}