namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>The forecast of one measure.</summary>
/// <param name="Id">The name the service gives the measure: <c>arrivals</c>, <c>occupancy</c> or <c>room_revenue</c>.</param>
/// <param name="Unit">The unit, for example <c>bookings</c>, <c>%</c> or <c>price units</c>.</param>
/// <param name="Months">The forecast months in order.</param>
public sealed record MarketAnalysisForecastMeasureDto(string Id, string Unit, IReadOnlyList<MarketAnalysisForecastMonthDto> Months);
