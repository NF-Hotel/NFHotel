namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The forecast of the next months (result schema 1.2): arrivals, occupancy and room revenue, calculated by the service from the
/// seasonal pattern of the history, not by an AI model.
/// </summary>
/// <param name="Formula">How the forecast is calculated.</param>
/// <param name="Limitations">What the forecast cannot say.</param>
/// <param name="HistoryMonths">The complete months the forecast rests on.</param>
/// <param name="MinimumHistoryMonths">The months of history the service wants for a reliable pattern.</param>
/// <param name="HistoryTooShort">True when the history has fewer months than the minimum.</param>
/// <param name="HorizonMonths">The months forecast; 0 when the history has no complete month.</param>
/// <param name="Measures">One forecast for each measure, in the order the service gives them.</param>
public sealed record MarketAnalysisForecastDto(
    string Formula,
    IReadOnlyList<string> Limitations,
    int HistoryMonths,
    int MinimumHistoryMonths,
    bool HistoryTooShort,
    int HorizonMonths,
    IReadOnlyList<MarketAnalysisForecastMeasureDto> Measures);
