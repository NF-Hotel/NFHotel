namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The AI insight of one analysis: the executive summary and the improvement suggestions, or why there is none.</summary>
/// <param name="Status"><c>available</c>, <c>unavailable</c> or <c>not_applicable</c>, as the markets API names it.</param>
/// <param name="Reason">Why it is unavailable, for example <c>GUARDRAIL_REJECTED</c> or <c>TIMEOUT</c>; <c>null</c> when it is available.</param>
/// <param name="ExecutiveSummary">The executive summary, or <c>null</c>.</param>
/// <param name="Suggestions">Up to five suggestions; empty when the insight is not available.</param>
/// <param name="Observations">What the AI sees in each chart (result schema 1.1 of the run API); <c>null</c> when there are none, also in a report stored before they existed.</param>
public sealed record MarketAnalysisInsightDto(
    string Status,
    string? Reason,
    string? ExecutiveSummary,
    IReadOnlyList<MarketAnalysisSuggestionDto> Suggestions,
    IReadOnlyList<MarketAnalysisObservationDto>? Observations = null);
