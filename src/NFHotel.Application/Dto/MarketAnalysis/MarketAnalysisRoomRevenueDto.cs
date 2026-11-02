namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// The room revenue of the room value analysis (result schema 1.2): price per night times nights of the bookings that are not
/// cancelled, before taxes, fees, discounts and extras. The currency is not known, so the amounts are in the price units of the input.
/// </summary>
/// <param name="Unit">The unit the service names, for example <c>price units of the input</c>.</param>
/// <param name="Limitation">What the figure cannot say, as the service words it.</param>
/// <param name="Total">The total room revenue.</param>
/// <param name="Mean">The mean for each booking with a price; <c>null</c> when no booking has a price.</param>
/// <param name="RecordsWithPrice">The bookings that went into the figures.</param>
/// <param name="RecordsWithoutPrice">The bookings that were left out for want of a price.</param>
/// <param name="Monthly">One point for each month of arrival that has a booking with a price, oldest first.</param>
public sealed record MarketAnalysisRoomRevenueDto(
    string Unit,
    string Limitation,
    double Total,
    double? Mean,
    int RecordsWithPrice,
    int RecordsWithoutPrice,
    IReadOnlyList<MarketAnalysisPointDto> Monthly);
