namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One of the analyses of the report, for example lead time or cancellations.</summary>
/// <param name="Id">The name the markets API gives the analysis, for example <c>lead_time</c>.</param>
/// <param name="Title">The title for the page.</param>
/// <param name="Status"><c>available</c> or <c>unavailable</c>.</param>
/// <param name="Reason">Why the analysis is unavailable, for example the field it lacks; <c>null</c> when it is available.</param>
/// <param name="Series">The charts made from the group figures of its findings.</param>
/// <param name="Insight">The AI insight, or <c>null</c> when none was asked for.</param>
/// <param name="Formula">How the figures of the analysis were calculated, or <c>null</c> (a report stored before result schema 1.1 of the run API has none).</param>
/// <param name="Limitations">What the analysis cannot say; <c>null</c> when the result has none.</param>
/// <param name="Matrices">The matrices of the findings, for example the correlations; <c>null</c> when there are none.</param>
public sealed record MarketAnalysisSectionDto(
    string Id,
    string Title,
    string Status,
    string? Reason,
    IReadOnlyList<MarketAnalysisSeriesDto> Series,
    MarketAnalysisInsightDto? Insight,
    string? Formula = null,
    IReadOnlyList<string>? Limitations = null,
    IReadOnlyList<MarketAnalysisMatrixDto>? Matrices = null);
