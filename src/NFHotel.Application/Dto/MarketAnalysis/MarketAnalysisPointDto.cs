namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One bar or point of a chart series.</summary>
/// <param name="Label">The category, for example a country.</param>
/// <param name="Value">The value.</param>
/// <param name="SmallSample">True when the figure rests on too few bookings for a firm reading, as the service flags it.</param>
public sealed record MarketAnalysisPointDto(string Label, double Value, bool SmallSample = false);
