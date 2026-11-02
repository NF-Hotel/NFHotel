namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Turns the monthly occupancy of the market analysis, the base rates and the settings into price suggestions for the
/// next twelve months (MIL-055). Pure: it reads no clock, store, AI text or network, so the same input gives the same output.
/// </summary>
public static class PriceSuggestionRule
{
    private const int MonthsInHorizon = 12;

    /// <summary>Makes the suggestions.</summary>
    /// <param name="observations">The monthly occupancy of the market analysis.</param>
    /// <param name="baseRates">The base rate per room size.</param>
    /// <param name="settings">The settings of the rule.</param>
    /// <param name="today">Today; the horizon starts with the month after it.</param>
    /// <returns>The suggestions ordered by first day and room size; empty without a basis.</returns>
    /// <exception cref="ArgumentException">The settings make no sense.</exception>
    public static IReadOnlyList<PriceSuggestion> Suggest(
        IReadOnlyList<OccupancyObservation> observations,
        IReadOnlyList<RoomRateValue> baseRates,
        PricingSettings settings,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(baseRates);
        ArgumentNullException.ThrowIfNull(settings);

        CalendarSeasons seasons = SeasonRule.ByCalendarMonth(observations, settings);
        if (!seasons.HasBasis)
        {
            return [];
        }

        DateOnly firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        (DateOnly First, DateOnly Last, MonthSeason Month)[] horizon = Enumerable.Range(0, MonthsInHorizon)
            .Select(firstMonth.AddMonths)
            .Select(first => (First: first, Last: first.AddMonths(1).AddDays(-1), Month: seasons.Months[first.Month - 1]))
            .ToArray();

        List<PriceSuggestion> suggestions = new();
        foreach (RoomRateValue? rate in baseRates.GroupBy(r => r.Size).Select(g => g.First()))
        {
            int start = 0;
            while (start < horizon.Length)
            {
                Season season = horizon[start].Month.Season;
                int end = start;
                while (end + 1 < horizon.Length && horizon[end + 1].Month.Season == season)
                {
                    end++;
                }

                if (season != Season.Normal)
                {
                    (DateOnly First, DateOnly Last, MonthSeason Month)[] run = horizon[start..(end + 1)];
                    decimal occupancy = run.Average(m => m.Month.OccupancyPercent ?? 0m);
                    PriceSuggestion? suggestion = Build(rate, season, run[0].First, run[^1].Last, occupancy, settings);
                    if (suggestion is not null)
                    {
                        suggestions.Add(suggestion);
                    }
                }

                start = end + 1;
            }
        }

        return suggestions.OrderBy(s => s.FirstDay).ThenBy(s => s.Size).ToArray();
    }

    private static PriceSuggestion? Build(
        RoomRateValue rate, Season season, DateOnly first, DateOnly last, decimal occupancy, PricingSettings settings)
    {
        bool isHigh = season == Season.High;
        if (!isHigh && !settings.DiscountSizes.Contains(rate.Size))
        {
            return null;
        }

        decimal wanted = isHigh ? settings.SurchargePercent : settings.DiscountPercent;
        decimal percentage = Math.Min(wanted, settings.MaxChangePercent);
        bool limited = percentage < wanted;
        decimal newPrice = PriceMath.ApplyPercentage(rate.NightlyPrice, isHigh ? percentage : -percentage);

        if (!isHigh && newPrice < settings.MinimumNightlyPrice)
        {
            // The percentage is what is approved and stored, so it must give the cut price: it is the largest discount that
            // keeps the minimum, and the price is the one that percentage gives (never below the minimum).
            percentage = PriceMath.LargestDiscountKeeping(rate.NightlyPrice, settings.MinimumNightlyPrice);
            limited = true;
            if (percentage <= 0m)
            {
                return null;
            }

            newPrice = PriceMath.ApplyPercentage(rate.NightlyPrice, -percentage);
        }

        return new PriceSuggestion(
            rate.Size,
            season,
            first,
            last,
            percentage,
            rate.NightlyPrice,
            newPrice,
            Math.Round(occupancy, 1, MidpointRounding.AwayFromZero),
            isHigh ? settings.HighThreshold : settings.LowThreshold,
            limited);
    }
}
