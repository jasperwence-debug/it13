namespace CRM.winforms.Models
{
    /// <summary>
    /// Row in the Retention Action Center's At-Risk grid.
    /// Has bulk-select + win-back state tracking (differs from the
    /// read-only AtRiskCustomerDto used in Reports).
    /// </summary>
    public class AtRiskActionCustomerDto
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Company { get; set; } = "";
        public int DaysSinceLastActivity { get; set; }
        public DateTime LastActivityDate { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public bool HasOpenWinBack { get; set; }
        public Guid? WinBackFollowUpId { get; set; }

        /// <summary>UI-only: checkbox state for bulk selection.</summary>
        public bool Selected { get; set; }
    }

    public class RetentionSummaryDto
    {
        public int AtRiskCount { get; set; }
        public int ContactedCount { get; set; }
        public int RecoveredCount { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public double RetentionRate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int ThresholdDays { get; set; }
    }

    /// <summary>
    /// Row in the Retention Action Center's Recovered grid.
    /// Differs from ReportModels' AtRiskCustomerDto by tracking recovery metadata.
    /// </summary>
    public class RecoveredActionCustomerDto
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Company { get; set; } = "";
        public DateTime WinBackCreatedAt { get; set; }
        public DateTime FirstActivityAfterWinBack { get; set; }
        public int DaysToRecover { get; set; }
    }

    public class BulkAssignRequest
    {
        public List<Guid> CustomerIds { get; set; } = new();
        public Guid AssignedUserId { get; set; }
    }

    public class CreateWinBackRequest
    {
        public List<Guid> CustomerIds { get; set; } = new();
        public Guid? AssignedUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
    }

    public class WinBackResultDto
    {
        public int Created { get; set; }
        public int SkippedExisting { get; set; }
        public List<Guid> CreatedFollowUpIds { get; set; } = new();
    }
}