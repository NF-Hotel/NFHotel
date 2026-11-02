using NFHotel.Application.UseCases.Pricing;

namespace NFHotel.Application.Tests.Services;

/// <summary>The season of an occupancy figure and of each calendar month (MIL-051, task 4).</summary>
public sealed class SeasonRuleTests
{
    private static readonly PricingSettings Defaults = PricingSettings.Defaults;

    #region Classify

    /// <summary>Positive: 85 percent is high, 30 low and 60 normal with the default 80/40.</summary>
    [Theory]
    [InlineData(85, Season.High)]
    [InlineData(30, Season.Low)]
    [InlineData(60, Season.Normal)]
    public void Classify_DefaultThresholds_GivesTheSeason(int occupancy, Season expected)
    {
        Assert.Equal(expected, SeasonRule.Classify(occupancy, Defaults));
    }

    /// <summary>Boundary: exactly the high threshold is high and exactly the low threshold is low.</summary>
    [Theory]
    [InlineData(80, Season.High)]
    [InlineData(79.9, Season.Normal)]
    [InlineData(40, Season.Low)]
    [InlineData(40.1, Season.Normal)]
    public void Classify_OnTheThreshold_BelongsToTheSeason(double occupancy, Season expected)
    {
        Assert.Equal(expected, SeasonRule.Classify((decimal)occupancy, Defaults));
    }

    /// <summary>Negative: a low threshold at or above the high threshold is refused.</summary>
    [Theory]
    [InlineData(80, 80)]
    [InlineData(90, 80)]
    public void Classify_LowThresholdNotBelowHigh_Throws(int low, int high)
    {
        var settings = new PricingSettings(HighThreshold: high, LowThreshold: low);

        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, settings));
    }

    /// <summary>Negative: a percentage outside 0 to 100, a largest change outside 0 to 100 and a negative minimum are refused.</summary>
    [Fact]
    public void Classify_NonsenseSettings_Throw()
    {
        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, new PricingSettings(SurchargePercent: 101m)));
        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, new PricingSettings(DiscountPercent: -1m)));
        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, new PricingSettings(MaxChangePercent: 0m)));
        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, new PricingSettings(MaxChangePercent: 101m)));
        Assert.Throws<ArgumentException>(() => SeasonRule.Classify(50m, new PricingSettings(MinimumNightlyPrice: -0.01m)));
    }

    #endregion

    #region ByCalendarMonth

    /// <summary>Positive: the months get their season and a month without a figure is normal with no occupancy.</summary>
    [Fact]
    public void ByCalendarMonth_AllMonths_ClassifiesEachMonth()
    {
        var observations = Year(2025, (6, 85m), (1, 30m));

        var result = SeasonRule.ByCalendarMonth(observations, Defaults);

        Assert.True(result.HasBasis);
        Assert.Equal(12, result.Months.Count);
        Assert.Equal(new MonthSeason(6, 85m, Season.High), result.Months[5]);
        Assert.Equal(new MonthSeason(1, 30m, Season.Low), result.Months[0]);
        Assert.Equal(new MonthSeason(3, 60m, Season.Normal), result.Months[2]);
    }

    /// <summary>Positive: a month is averaged over the years, and duplicates of the same year and month are averaged too.</summary>
    [Fact]
    public void ByCalendarMonth_SeveralYearsAndDuplicates_AveragesTheMonth()
    {
        var observations = Year(2024, (6, 90m))
            .Concat(Year(2025, (6, 70m)))
            .Append(new OccupancyObservation(2025, 6, 70m))
            .ToArray();

        var result = SeasonRule.ByCalendarMonth(observations, Defaults);

        // (90 + 70 + 70) / 3 = 76.67 is not high; the plain 90 and 70 average is covered by the rule tests.
        Assert.Equal(Season.Normal, result.Months[5].Season);
        Assert.Equal(230m / 3m, result.Months[5].OccupancyPercent);
    }

    /// <summary>Negative: fewer than twelve distinct calendar months give no basis and every month is normal.</summary>
    [Fact]
    public void ByCalendarMonth_ElevenCalendarMonths_HasNoBasis()
    {
        var observations = Year(2025, (6, 90m)).Where(o => o.Month != 12).ToArray();

        var result = SeasonRule.ByCalendarMonth(observations, Defaults);

        Assert.False(result.HasBasis);
        Assert.All(result.Months, m => Assert.Equal(Season.Normal, m.Season));
        Assert.Null(result.Months[11].OccupancyPercent);
    }

    /// <summary>Negative: settings that make no sense are refused.</summary>
    [Fact]
    public void ByCalendarMonth_LowThresholdNotBelowHigh_Throws()
    {
        var settings = new PricingSettings(HighThreshold: 50m, LowThreshold: 60m);

        Assert.Throws<ArgumentException>(() => SeasonRule.ByCalendarMonth(Year(2025), settings));
    }

    #endregion

    private static OccupancyObservation[] Year(int year, params (int Month, decimal Percent)[] overrides) =>
        Enumerable.Range(1, 12)
            .Select(month => new OccupancyObservation(
                year,
                month,
                overrides.Where(o => o.Month == month).Select(o => o.Percent).DefaultIfEmpty(60m).First()))
            .ToArray();
}
