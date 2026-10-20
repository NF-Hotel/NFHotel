namespace NFHotel.Infrastructure.Seeding;

/// <summary>One row of <c>nf_hotel_bookings.csv</c>, the fake bookings of the hotel (MIL-026).</summary>
/// <param name="Number">The booking id of the file; it becomes the booking id of the database.</param>
/// <param name="IsCanceled">True when the booking was cancelled.</param>
/// <param name="BookedAt">The booking date.</param>
/// <param name="Arrival">The arrival date.</param>
/// <param name="Nights">The nights; at least 1 (the file has bookings of 0 nights, which the database does not allow).</param>
/// <param name="Adults">The adults.</param>
/// <param name="Children">The children.</param>
/// <param name="Babies">The babies.</param>
/// <param name="Meal">The meal plan.</param>
/// <param name="Country">The country of the guest.</param>
/// <param name="MarketSegment">How the booking was sold.</param>
/// <param name="IsRepeatedGuest">True when the guest has stayed before.</param>
/// <param name="PreviousCancellations">The earlier cancellations of the guest.</param>
/// <param name="RoomType">The room type letter.</param>
/// <param name="BookingChanges">The changes made to the booking.</param>
/// <param name="DepositType">The kind of deposit.</param>
/// <param name="CustomerType">The kind of customer.</param>
/// <param name="ParkingSpaces">The car parking spaces.</param>
/// <param name="SpecialRequests">The special requests.</param>
public sealed record MarketSeedRow(
    int Number,
    bool IsCanceled,
    DateOnly BookedAt,
    DateOnly Arrival,
    int Nights,
    int Adults,
    int Children,
    int Babies,
    string Meal,
    string Country,
    string MarketSegment,
    bool IsRepeatedGuest,
    int PreviousCancellations,
    string RoomType,
    int BookingChanges,
    string DepositType,
    string CustomerType,
    int ParkingSpaces,
    int SpecialRequests)
{
    /// <summary>The departure date: the arrival plus the nights.</summary>
    public DateOnly Departure => Arrival.AddDays(Nights);
}
