namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One of the analyses of the report, for example lead time or cancellations.</summary>
/// <param name="Id">The name the markets API gives the analysis, for example <c>lead_time</c>.</param>
/// <param name="Title">The title for the page.</param>
/// <param name="Status"><c>available</c> or <c>unavailable</c>.</param>
/// <param name="Reason">Why the analysis is unavailable, for example the field it lacks; <c>null</c> when it is available.</param>
/// <param name="Series">The charts made from the group figures of its findings.</param>
/// <param name="Insight">The AI insight, or <c>null</c> when none was asked for.</param>
public sealed record MarketAnalysisSectionDto(
    string Id,
    string Title,
    string Status,
    string? Reason,
    IReadOnlyList<MarketAnalysisSeriesDto> Series,
    MarketAnalysisInsightDto? Insight);
