namespace NFHotel.Application.UseCases.Pricing;

/// <summary>Tells the season of an occupancy figure and of each calendar month of a market analysis (MIL-055). Pure.</summary>
public static class SeasonRule
{
    private const int MonthsInYear = 12;

    /// <summary>Classifies an occupancy figure; the two thresholds belong to their seasons.</summary>
    /// <param name="occupancyPercent">The occupancy in percent.</param>
    /// <param name="settings">The thresholds.</param>
    /// <returns>High at or above the high threshold, Low at or below the low threshold, else Normal.</returns>
    /// <exception cref="ArgumentException">The settings make no sense.</exception>
    public static Season Classify(decimal occupancyPercent, PricingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureValid();

        if (occupancyPercent >= settings.HighThreshold)
        {
            return Season.High;
        }

        return occupancyPercent <= settings.LowThreshold ? Season.Low : Season.Normal;
    }

    /// <summary>Averages the occupancy of each calendar month over the years of the report and classifies it.</summary>
    /// <param name="observations">The months of the report; several for the same year and month are averaged.</param>
    /// <param name="settings">The thresholds.</param>
    /// <returns>The twelve calendar months; without a basis (fewer than twelve distinct calendar months) every month is Normal.</returns>
    /// <exception cref="ArgumentException">The settings make no sense.</exception>
    public static CalendarSeasons ByCalendarMonth(IReadOnlyList<OccupancyObservation> observations, PricingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureValid();

        var averages = observations
            .Where(o => o.Month is >= 1 and <= MonthsInYear)
            .GroupBy(o => o.Month)
            .ToDictionary(g => g.Key, g => g.Average(o => o.OccupancyPercent));

        var hasBasis = averages.Count == MonthsInYear;
        var months = Enumerable.Range(1, MonthsInYear)
            .Select(month =>
            {
                decimal? occupancy = averages.TryGetValue(month, out var average) ? average : null;
                var season = hasBasis && occupancy is { } value ? Classify(value, settings) : Season.Normal;
                return new MonthSeason(month, occupancy, season);
            })
            .ToArray();

        return new CalendarSeasons(hasBasis, months);
    }
}
