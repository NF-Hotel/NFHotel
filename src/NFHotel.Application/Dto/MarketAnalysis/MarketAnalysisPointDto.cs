namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One bar or point of a chart series.</summary>
/// <param name="Label">The category, for example a country.</param>
/// <param name="Value">The value.</param>
public sealed record MarketAnalysisPointDto(string Label, double Value);
