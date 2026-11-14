using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The caption under one chart of the report (MIL-026 task 21).</summary>
/// <param name="What">What the chart shows.</param>
/// <param name="Why">Why this kind of chart was chosen.</param>
/// <param name="Limits">What the chart cannot say: the limits of its kind, then the limitations the analysis carries.</param>
/// <param name="Formula">How the figures were calculated, or <c>null</c> when the result has none.</param>
/// <param name="Observation">The observation the AI wrote about this chart, or <c>null</c> when there is none.</param>
/// <param name="NoObservation">The short text shown when there is no observation, saying why when the insight says so.</param>
public sealed record ChartCaptionModel(
    string What,
    string Why,
    IReadOnlyList<string> Limits,
    string? Formula,
    MarketAnalysisObservationDto? Observation,
    string NoObservation);
