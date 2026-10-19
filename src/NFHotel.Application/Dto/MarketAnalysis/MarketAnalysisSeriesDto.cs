namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One chart of an analysis: a titled list of points.</summary>
/// <param name="Id">A stable key of the series, the path of the group figures in the findings.</param>
/// <param name="Title">The title of the chart.</param>
/// <param name="Unit">The unit of the values, for example <c>%</c> for a rate or <c>bookings</c> for a count; empty when there is none.</param>
/// <param name="Points">The points in display order.</param>
public sealed record MarketAnalysisSeriesDto(
    string Id,
    string Title,
    string Unit,
    IReadOnlyList<MarketAnalysisPointDto> Points);
