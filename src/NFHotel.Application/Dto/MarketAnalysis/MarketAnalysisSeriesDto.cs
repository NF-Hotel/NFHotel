namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>One chart of an analysis: a titled list of points.</summary>
/// <param name="Id">A stable key of the series, the path of the group figures in the findings.</param>
/// <param name="Title">The title of the chart.</param>
/// <param name="Unit">The unit of the values, for example <c>%</c> for a rate or <c>bookings</c> for a count; empty when there is none.</param>
/// <param name="Points">The points in display order.</param>
/// <param name="IsOrdered">True when the points have a natural order that a chart must keep: months, bands such as the lead time bands, or numbers such as the guests. Derived from the labels. A report stored before this field existed reads as false.</param>
/// <param name="IsPartsOfWhole">True when the points are the parts of one whole whose shares add up to 100 %, for example cancelled and not cancelled. Two separate rates (the occupancy on holidays and on other days) are false. A report stored before this field existed reads as false.</param>
/// <param name="ReferenceValue">The overall level of the series in the same unit, for example the occupancy of the whole period next to the occupancy of each month; <c>null</c> when the findings carry none.</param>
/// <param name="ReferenceLabel">The name of the overall level, for example <c>Overall</c>; <c>null</c> when there is no <paramref name="ReferenceValue"/>.</param>
public sealed record MarketAnalysisSeriesDto(
    string Id,
    string Title,
    string Unit,
    IReadOnlyList<MarketAnalysisPointDto> Points,
    bool IsOrdered = false,
    bool IsPartsOfWhole = false,
    double? ReferenceValue = null,
    string? ReferenceLabel = null);
