using CRM.winforms.Models;

namespace CRM.winforms.Services
{
    public class ReportsApiClient : ApiClientBase
    {
        public Task<OverviewReportDto?> GetOverviewAsync(DateTime from, DateTime to)
            => GetAsync<OverviewReportDto>(BuildUrl("/api/reports/overview", from, to));

        public Task<PipelineReportDto?> GetPipelineAsync(DateTime from, DateTime to)
            => GetAsync<PipelineReportDto>(BuildUrl("/api/reports/pipeline", from, to));

        public Task<ActivityReportDto?> GetActivityAsync(DateTime from, DateTime to)
            => GetAsync<ActivityReportDto>(BuildUrl("/api/reports/activity", from, to));

        public Task<RetentionReportDto?> GetRetentionAsync(DateTime from, DateTime to, int atRiskDays = 60)
            => GetAsync<RetentionReportDto>(
                BuildUrl("/api/reports/retention", from, to) + $"&atRiskDays={atRiskDays}");

        public Task<ConversionReportDto?> GetConversionAsync(DateTime from, DateTime to)
            => GetAsync<ConversionReportDto>(BuildUrl("/api/reports/conversion", from, to));

        public Task<TeamPerformanceDto?> GetTeamPerformanceAsync(DateTime from, DateTime to)
            => GetAsync<TeamPerformanceDto>(BuildUrl("/api/reports/team-performance", from, to));

        private static string BuildUrl(string baseUrl, DateTime from, DateTime to)
            => $"{baseUrl}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
    }
}