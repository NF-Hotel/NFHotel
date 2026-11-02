namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The forecast of one month with the band of the history.</summary>
/// <param name="Month">The month, <c>yyyy-MM</c>.</param>
/// <param name="Value">The expected value.</param>
/// <param name="Low">The low end of the band, or <c>null</c> when it cannot be known.</param>
/// <param name="High">The high end of the band, or <c>null</c> when it cannot be known.</param>
public sealed record MarketAnalysisForecastMonthDto(string Month, double Value, double? Low, double? High);
