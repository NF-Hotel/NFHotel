namespace NFHotel.Infrastructure.Seeding;

/// <summary>Where a fake booking goes.</summary>
/// <param name="Row">The row of the file.</param>
/// <param name="Room">The 1-based number of its room.</param>
/// <param name="Guest">The 1-based number of its guest.</param>
public sealed record MarketSeedPlacement(MarketSeedRow Row, int Room, int Guest);

/// <summary>The rooms, the guests and the bookings that the seed command writes.</summary>
/// <param name="RoomCount">The rooms to make.</param>
/// <param name="GuestCountries">The country of each guest to make; the guest number is the 1-based position.</param>
/// <param name="Bookings">The bookings with their room and guest.</param>
public sealed record MarketSeedPlan(
    int RoomCount,
    IReadOnlyList<string> GuestCountries,
    IReadOnlyList<MarketSeedPlacement> Bookings);
