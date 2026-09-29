namespace CRM.domain.Dtos.Reports
{
    public record ConversionBySourceDto(
        string Source,
        int TotalLeads,
        int Converted,
        double ConversionRate
    );

    public record ConversionReportDto(
        int TotalLeads,
        int ConvertedLeads,
        double OverallConversionRate,
        double AverageDaysToConvert,
        IReadOnlyList<ConversionBySourceDto> BySource,
        DateTime RangeFrom,
        DateTime RangeTo
    );
}