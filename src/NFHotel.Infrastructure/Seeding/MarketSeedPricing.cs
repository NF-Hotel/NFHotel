namespace NFHotel.Infrastructure.Seeding;

/// <summary>
/// The price per night of a fake booking (MIL-026). <c>nf_hotel_bookings.csv</c> has no price, and the market analysis needs
/// one, so it is made from the room type, the month of arrival and the market segment. It depends only on the row, so the
/// same file gives the same prices. These are not real prices.
/// </summary>
public static class MarketSeedPricing
{
    private const decimal BasePrice = 70m;
    private const decimal PricePerRoomTypeStep = 12m;
    private const decimal HighSeasonFactor = 1.35m;
    private const decimal ShoulderSeasonFactor = 1.15m;
    private const string ComplementarySegment = "Complementary";

    /// <summary>The price of one night.</summary>
    /// <param name="row">The booking.</param>
    /// <returns>The price with two decimals; 0 for a complementary booking, as in the sample of the markets API.</returns>
    public static decimal PricePerNight(MarketSeedRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (string.Equals(row.MarketSegment, ComplementarySegment, StringComparison.Ordinal))
        {
            return 0m;
        }

        var step = row.RoomType.Length == 1 && row.RoomType[0] is >= 'A' and <= 'Z' ? row.RoomType[0] - 'A' : 0;
        var factor = row.Arrival.Month switch
        {
            >= 6 and <= 8 => HighSeasonFactor,
            5 or 9 => ShoulderSeasonFactor,
            _ => 1m,
        };

        return Math.Round((BasePrice + (step * PricePerRoomTypeStep)) * factor, 2, MidpointRounding.AwayFromZero);
    }
}
