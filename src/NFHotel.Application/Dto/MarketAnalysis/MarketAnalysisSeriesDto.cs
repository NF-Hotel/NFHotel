namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One chart of the report: a titled list of points.</summary>
/// <param name="Id">A stable key of the series, for example <c>bookings_by_country</c>.</param>
/// <param name="Title">The title of the chart.</param>
/// <param name="Unit">The unit of the values, for example <c>bookings</c> or <c>%</c>; empty when there is none.</param>
/// <param name="Points">The points in display order.</param>
public sealed record MarketAnalysisSeriesDto(
    string Id,
    string Title,
    string Unit,
    IReadOnlyList<MarketAnalysisPointDto> Points);
