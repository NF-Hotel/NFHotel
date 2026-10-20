namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// One booking as the input contract of the markets API wants it (ADR-0001 of <c>Notebook_Market_Analysis</c>). The client
/// writes the names in snake_case and leaves a <c>null</c> out, which the markets API counts as a missing value.
/// </summary>
/// <param name="BookingId">The booking id.</param>
/// <param name="Hotel">The hotel name.</param>
/// <param name="IsCanceled">True when the booking was cancelled.</param>
/// <param name="LeadTime">The days from the booking date to the arrival, or <c>null</c> when the booking date is not known.</param>
/// <param name="BookingDate">The date the booking was made, or <c>null</c>.</param>
/// <param name="ArrivalDate">The arrival date.</param>
/// <param name="StaysInWeekendNights">The booked nights that fall on a Saturday or a Sunday.</param>
/// <param name="StaysInWeekNights">The other booked nights.</param>
/// <param name="Adults">The adults, or <c>null</c>.</param>
/// <param name="Children">The children, or <c>null</c>.</param>
/// <param name="Babies">The babies, or <c>null</c>.</param>
/// <param name="Meal">The meal plan, or <c>null</c>.</param>
/// <param name="Country">The country of the guest.</param>
/// <param name="MarketSegment">How the booking was sold, or <c>null</c>.</param>
/// <param name="IsRepeatedGuest">True when the guest has stayed before, or <c>null</c>.</param>
/// <param name="PreviousCancellations">The earlier cancellations of the guest, or <c>null</c>.</param>
/// <param name="AssignedRoomType">The room type, or <c>null</c>.</param>
/// <param name="BookingChanges">The changes made to the booking, or <c>null</c>.</param>
/// <param name="DepositType">The kind of deposit, or <c>null</c>.</param>
/// <param name="CustomerType">The kind of customer, or <c>null</c>.</param>
/// <param name="RequiredCarParkingSpaces">The parking spaces, or <c>null</c>.</param>
/// <param name="TotalOfSpecialRequests">The special requests, or <c>null</c>.</param>
/// <param name="PricePerNight">The price of one night, or <c>null</c>.</param>
public sealed record MarketBookingDto(
    int BookingId,
    string Hotel,
    bool IsCanceled,
    int? LeadTime,
    DateOnly? BookingDate,
    DateOnly ArrivalDate,
    int StaysInWeekendNights,
    int StaysInWeekNights,
    int? Adults,
    int? Children,
    int? Babies,
    string? Meal,
    string Country,
    string? MarketSegment,
    bool? IsRepeatedGuest,
    int? PreviousCancellations,
    string? AssignedRoomType,
    int? BookingChanges,
    string? DepositType,
    string? CustomerType,
    int? RequiredCarParkingSpaces,
    int? TotalOfSpecialRequests,
    decimal? PricePerNight);
