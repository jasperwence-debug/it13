namespace CRM.domain.Dtos.Retention
{
    public record RetentionSummaryDto(
        int AtRiskCount,
        int ContactedCount,           // at-risk customers with an open win-back follow-up
        int RecoveredCount,           // at-risk customers who had activity within 30 days of win-back
        int TotalCustomers,
        int ActiveCustomers,
        double RetentionRate,
        DateTime GeneratedAt,
        int ThresholdDays            // the threshold used to compute this summary
    );
}