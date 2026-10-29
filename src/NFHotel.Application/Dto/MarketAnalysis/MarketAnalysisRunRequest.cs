namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// What the website API sends to start a run of the market analysis service (<c>RunRequest</c> of
/// <c>openapi/market-contract.yaml</c> of that service): the bookings, the rooms of the hotel (the bookings do not say how many
/// there are) and the period of the analysis.
/// </summary>
/// <param name="RoomCount">The rooms of the hotel; at least one.</param>
/// <param name="Bookings">The bookings; at least one.</param>
/// <param name="From">The first arrival date the Manager chose, or <c>null</c>.</param>
/// <param name="To">The last arrival date the Manager chose, or <c>null</c>.</param>
public sealed record MarketAnalysisRunRequest(
    int RoomCount,
    IReadOnlyList<MarketBookingDto> Bookings,
    DateOnly? From = null,
    DateOnly? To = null);
