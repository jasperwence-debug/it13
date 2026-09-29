namespace CRM.domain.Dtos.Reports
{
    public record PipelineStageDto(
        string Stage,
        int Count,
        decimal TotalValue,
        double PercentageOfTotal  // 0.0 to 1.0
    );

    public record PipelineReportDto(
        IReadOnlyList<PipelineStageDto> Stages,
        int TotalLeads,
        decimal TotalPipelineValue,
        DateTime GeneratedAt
    );
}