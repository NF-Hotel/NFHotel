namespace NFHotel.Domain.ValueObjects;

/// <summary>The fields of <see cref="NFHotel.Domain.Entities.BookingMarketDetail"/> other than the booking and its date.</summary>
/// <param name="PricePerNight">The price of one night, or <c>null</c>.</param>
/// <param name="Adults">The adults.</param>
/// <param name="Children">The children.</param>
/// <param name="Babies">The babies.</param>
/// <param name="Meal">The meal plan, or <c>null</c>.</param>
/// <param name="MarketSegment">How the booking was sold, or <c>null</c>.</param>
/// <param name="CustomerType">The kind of customer, or <c>null</c>.</param>
/// <param name="DepositType">The kind of deposit, or <c>null</c>.</param>
/// <param name="RoomType">The room type the guest was given, or <c>null</c>.</param>
/// <param name="IsRepeatedGuest">True when the guest has stayed before.</param>
/// <param name="PreviousCancellations">The earlier cancellations of the guest.</param>
/// <param name="BookingChanges">The changes made to the booking.</param>
/// <param name="SpecialRequests">The special requests.</param>
/// <param name="ParkingSpaces">The car parking spaces.</param>
public sealed record BookingMarketValues(
    decimal? PricePerNight,
    int Adults,
    int Children,
    int Babies,
    string? Meal,
    string? MarketSegment,
    string? CustomerType,
    string? DepositType,
    string? RoomType,
    bool IsRepeatedGuest,
    int PreviousCancellations,
    int BookingChanges,
    int SpecialRequests,
    int ParkingSpaces);
