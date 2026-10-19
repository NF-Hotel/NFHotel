namespace NFHotel.Infrastructure.Seeding;

/// <summary>
/// Fills the hotel with bookings up to a date by repeating the bookings of the file back to back (MIL-026). The file covers a
/// period of whole months (2018-07 to 2020-08, 26 months); copy 1 is the same bookings 26 months later, copy 2 52 months later,
/// and so on until the date is passed. The bookings that would arrive after the date are left out. The booking numbers of a copy
/// are those of the file plus a whole multiple of the highest number, so they stay unique. The fake bookings repeat in
/// every copy, which is fine for development but is not a forecast.
/// </summary>
public static class MarketSeedExpander
{
    /// <summary>Repeats the rows until their arrival dates reach <paramref name="through"/>.</summary>
    /// <param name="rows">The rows of the file.</param>
    /// <param name="through">The last arrival date to include, or <c>null</c> to keep the rows as they are.</param>
    /// <returns>The rows of the file and of the copies, ordered by booking number.</returns>
    public static IReadOnlyList<MarketSeedRow> Expand(IReadOnlyList<MarketSeedRow> rows, DateOnly? through)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (through is not { } last || rows.Count == 0)
        {
            return rows;
        }

        var first = rows.Min(row => row.Arrival);
        var latest = rows.Max(row => row.Arrival);
        var months = ((latest.Year - first.Year) * 12) + (latest.Month - first.Month) + 1;
        var numberStep = rows.Max(row => row.Number);

        var expanded = new List<MarketSeedRow>(rows.Where(row => row.Arrival <= last));
        for (var copy = 1; first.AddMonths(copy * months) <= last; copy++)
        {
            var shift = copy * months;
            foreach (var row in rows)
            {
                var arrival = row.Arrival.AddMonths(shift);
                if (arrival <= last)
                {
                    expanded.Add(row with
                    {
                        Number = row.Number + (copy * numberStep),
                        BookedAt = row.BookedAt.AddMonths(shift),
                        Arrival = arrival,
                    });
                }
            }
        }

        return expanded;
    }
}
