using CRM.domain.Dtos.Reports;

namespace CRM.infrastructure.Services
{
    public interface IReportService
    {
        Task<OverviewReportDto> GetOverviewAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole);

        Task<PipelineReportDto> GetPipelineAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole);

        Task<ActivityReportDto> GetActivityAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole);

        Task<RetentionReportDto> GetRetentionAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole,
            int atRiskDays = 60);

        Task<ConversionReportDto> GetConversionAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole);

        Task<TeamPerformanceDto> GetTeamPerformanceAsync(
            DateTime from, DateTime to,
            Guid? callerUserId, string? callerRole);
    }
}