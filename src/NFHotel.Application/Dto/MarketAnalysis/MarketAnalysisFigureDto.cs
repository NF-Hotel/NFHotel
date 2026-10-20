namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One headline figure of the report, for example the cancellation rate.</summary>
/// <param name="Label">What the figure is.</param>
/// <param name="Value">The figure.</param>
/// <param name="Unit">The unit, or empty.</param>
public sealed record MarketAnalysisFigureDto(string Label, double Value, string Unit);
