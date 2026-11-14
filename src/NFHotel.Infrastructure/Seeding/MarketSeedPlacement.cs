namespace NFHotel.Infrastructure.Seeding;

/// <summary>Where a fake booking goes.</summary>
/// <param name="Row">The row of the file.</param>
/// <param name="Room">The 1-based number of its room.</param>
/// <param name="Guest">The 1-based number of its guest.</param>
public sealed record MarketSeedPlacement(MarketSeedRow Row, int Room, int Guest);
