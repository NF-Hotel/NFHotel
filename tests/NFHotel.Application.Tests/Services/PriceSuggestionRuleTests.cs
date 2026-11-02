using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Tests.Services;

/// <summary>The pure rule that turns monthly occupancy and base rates into price suggestions (MIL-055, task 4).</summary>
public sealed class PriceSuggestionRuleTests
{
    // The horizon of this day is 2026-02 .. 2027-01, so June to August lie in 2026.
    private static readonly DateOnly Today = new(2026, 1, 10);

    private static readonly RoomRateValue[] Rates =
    [
        new(RoomSize.Single, 100m),
        new(RoomSize.Double, 100m),
        new(RoomSize.Suite, 100m),
    ];

    #region Amounts

    /// <summary>Positive: a high month gives +20 percent to every size: 100 becomes 120.00.</summary>
    [Fact]
    public void Suggest_HighMonth_RaisesEverySize()
    {
        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), Rates, PricingSettings.Defaults, Today);

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(Season.High, s.Season));
        Assert.All(result, s => Assert.Equal(120.00m, s.NewPrice));
        Assert.All(result, s => Assert.Equal(20m, s.Percentage));
        Assert.All(result, s => Assert.Equal(80m, s.ThresholdPercent));
        Assert.All(result, s => Assert.Equal(90m, s.OccupancyPercent));
        Assert.All(result, s => Assert.False(s.WasLimited));
        Assert.Equal([RoomSize.Single, RoomSize.Double, RoomSize.Suite], result.Select(s => s.Size));
    }

    /// <summary>Positive: a low month gives -15 percent to the single room only: 100 becomes 85.00.</summary>
    [Fact]
    public void Suggest_LowMonth_LowersOnlyTheDiscountSizes()
    {
        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), Rates, PricingSettings.Defaults, Today);

        var suggestion = Assert.Single(result);
        Assert.Equal(RoomSize.Single, suggestion.Size);
        Assert.Equal(Season.Low, suggestion.Season);
        Assert.Equal(85.00m, suggestion.NewPrice);
        Assert.Equal(15m, suggestion.Percentage);
        Assert.Equal(40m, suggestion.ThresholdPercent);
        Assert.Equal(100m, suggestion.OldPrice);
    }

    /// <summary>Positive: a size in the discount sizes gets the discount, so a double room can be added.</summary>
    [Fact]
    public void Suggest_DiscountSizesWithDouble_LowersTheDoubleRoomToo()
    {
        var settings = new PricingSettings(DiscountSizesOrNull: [RoomSize.Single, RoomSize.Double]);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), Rates, settings, Today);

        Assert.Equal([RoomSize.Single, RoomSize.Double], result.Select(s => s.Size));
    }

    /// <summary>Boundary: a surcharge of 40 with a largest change of 30 gives 30 percent, 130.00, and says it was limited.</summary>
    [Fact]
    public void Suggest_SurchargeAboveTheCap_IsCutToTheCap()
    {
        var settings = new PricingSettings(SurchargePercent: 40m, MaxChangePercent: 30m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), Rates, settings, Today);

        Assert.All(result, s => Assert.Equal(30m, s.Percentage));
        Assert.All(result, s => Assert.Equal(130.00m, s.NewPrice));
        Assert.All(result, s => Assert.True(s.WasLimited));
    }

    /// <summary>Boundary: a minimum price of 90 stops the 85.00 at 90.00 and says it was limited.</summary>
    [Fact]
    public void Suggest_DiscountBelowTheMinimum_IsRaisedToTheMinimum()
    {
        var settings = new PricingSettings(MinimumNightlyPrice: 90m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), Rates, settings, Today);

        var suggestion = Assert.Single(result);
        Assert.Equal(90.00m, suggestion.NewPrice);
        Assert.True(suggestion.WasLimited);
    }

    /// <summary>Boundary: the minimum 90 raises 85.00 to 90.00 and the percentage is then 10, the one that gives 90.00, not the wanted 15.</summary>
    [Fact]
    public void Suggest_DiscountBelowTheMinimum_PercentageGivesTheCutPrice()
    {
        var settings = new PricingSettings(MinimumNightlyPrice: 90m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), Rates, settings, Today);

        var suggestion = Assert.Single(result);
        Assert.Equal(10m, suggestion.Percentage);
        Assert.Equal(90.00m, suggestion.NewPrice);
    }

    /// <summary>
    /// Boundary: over a grid of rates and minima the price that the approved percentage gives is the suggested price, is never
    /// below the minimum, and is above it by no more than a hundredth of a percent of the rate (plus half a cent of rounding).
    /// </summary>
    [Theory]
    [InlineData("80.00", "70.00")]
    [InlineData("99.99", "90.00")]
    [InlineData("99.99", "85.01")]
    [InlineData("99.99", "99.98")]
    [InlineData("100.00", "90.00")]
    [InlineData("100.00", "85.01")]
    [InlineData("123.45", "110.00")]
    [InlineData("123.45", "105.33")]
    [InlineData("250.00", "225.00")]
    [InlineData("250.00", "212.51")]
    [InlineData("250.00", "249.00")]
    public void Suggest_MinimumLimitsTheDiscount_ApprovedChangeGivesAtLeastTheMinimum(string rateText, string minimumText)
    {
        var rate = decimal.Parse(rateText, System.Globalization.CultureInfo.InvariantCulture);
        var minimum = decimal.Parse(minimumText, System.Globalization.CultureInfo.InvariantCulture);
        var settings = new PricingSettings(MinimumNightlyPrice: minimum);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), [new(RoomSize.Single, rate)], settings, Today);

        var suggestion = Assert.Single(result);

        // The price of the stored change: the formula written out here on purpose, so the test does not use the code it tests.
        var stored = Math.Round(rate * (1m - (Math.Round(suggestion.Percentage, 2) / 100m)), 2, MidpointRounding.AwayFromZero);
        Assert.True(suggestion.WasLimited);
        Assert.Equal(stored, suggestion.NewPrice);
        Assert.True(suggestion.NewPrice >= minimum, $"{suggestion.NewPrice} is below the minimum {minimum}");
        Assert.True(suggestion.NewPrice - minimum <= (rate * 0.0001m) + 0.005m, $"{suggestion.NewPrice} is too far above {minimum}");
        Assert.True(suggestion.Percentage is > 0m and < 15m);
        Assert.Equal(suggestion.Percentage, Math.Round(suggestion.Percentage, 2));
    }

    /// <summary>Negative: a minimum a hundredth below the rate leaves no discount that fits in 0.01 percent, so nothing is suggested.</summary>
    [Fact]
    public void Suggest_MinimumLeavesNoRoomForTheSmallestDiscount_GivesNoSuggestion()
    {
        var settings = new PricingSettings(MinimumNightlyPrice: 249.99m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), [new(RoomSize.Single, 250m)], settings, Today);

        Assert.Empty(result);
    }

    /// <summary>Negative: a rate already at the minimum gets no discount suggestion.</summary>
    [Fact]
    public void Suggest_RateAtTheMinimum_GivesNoDiscount()
    {
        var settings = new PricingSettings(MinimumNightlyPrice: 100m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 30m)), Rates, settings, Today);

        Assert.Empty(result);
    }

    /// <summary>Boundary: 99.99 plus 20 percent is 119.988, rounded away from zero to 119.99.</summary>
    [Fact]
    public void Suggest_PriceWithDecimals_RoundsToTwoDecimals()
    {
        RoomRateValue[] rates = [new(RoomSize.Double, 99.99m)];

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), rates, PricingSettings.Defaults, Today);

        Assert.Equal(119.99m, Assert.Single(result).NewPrice);
    }

    /// <summary>Boundary: a rounding midpoint goes up, 10.25 plus 10 percent is 11.275 and becomes 11.28.</summary>
    [Fact]
    public void Suggest_RoundingMidpoint_GoesAwayFromZero()
    {
        RoomRateValue[] rates = [new(RoomSize.Double, 10.25m)];
        var settings = new PricingSettings(SurchargePercent: 10m);

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), rates, settings, Today);

        Assert.Equal(11.28m, Assert.Single(result).NewPrice);
    }

    /// <summary>Negative: a size without a base rate gets no suggestion.</summary>
    [Fact]
    public void Suggest_SizeWithoutBaseRate_GetsNone()
    {
        RoomRateValue[] rates = [new(RoomSize.Suite, 200m)];

        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), rates, PricingSettings.Defaults, Today);

        Assert.Equal(RoomSize.Suite, Assert.Single(result).Size);
        Assert.Empty(PriceSuggestionRule.Suggest(Year(2025, (6, 90m)), [], PricingSettings.Defaults, Today));
    }

    #endregion

    #region Runs

    /// <summary>Positive: June to August high is one suggestion per size, from 06-01 to 08-31.</summary>
    [Fact]
    public void Suggest_ThreeHighMonthsInARow_IsOneSuggestionPerSize()
    {
        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m), (7, 85m), (8, 80m)), Rates, PricingSettings.Defaults, Today);

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(new DateOnly(2026, 6, 1), s.FirstDay));
        Assert.All(result, s => Assert.Equal(new DateOnly(2026, 8, 31), s.LastDay));
        Assert.All(result, s => Assert.Equal(85m, s.OccupancyPercent));
    }

    /// <summary>Positive: a run over the year end is one suggestion, to the last day of February.</summary>
    [Fact]
    public void Suggest_RunAcrossTheYearEnd_IsOneSuggestion()
    {
        var observations = Year(2025, (12, 90m), (1, 90m), (2, 90m));

        var result = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, new DateOnly(2026, 6, 15));

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(new DateOnly(2026, 12, 1), s.FirstDay));
        Assert.All(result, s => Assert.Equal(new DateOnly(2027, 2, 28), s.LastDay));
    }

    /// <summary>Boundary: in a leap year the run ends on 29 February.</summary>
    [Fact]
    public void Suggest_RunEndingInFebruaryOfALeapYear_EndsOnThe29th()
    {
        var observations = Year(2025, (12, 90m), (1, 90m), (2, 90m));

        var result = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, new DateOnly(2027, 6, 15));

        Assert.All(result, s => Assert.Equal(new DateOnly(2027, 12, 1), s.FirstDay));
        Assert.All(result, s => Assert.Equal(new DateOnly(2028, 2, 29), s.LastDay));
    }

    /// <summary>Positive: two separate runs are two suggestions per size, ordered by first day and then by size.</summary>
    [Fact]
    public void Suggest_TwoSeparateRuns_AreTwoSuggestionsOrderedByDayThenSize()
    {
        var result = PriceSuggestionRule.Suggest(Year(2025, (3, 90m), (7, 90m)), Rates, PricingSettings.Defaults, Today);

        Assert.Equal(6, result.Count);
        Assert.Equal(
            [
                (new DateOnly(2026, 3, 1), RoomSize.Single), (new DateOnly(2026, 3, 1), RoomSize.Double), (new DateOnly(2026, 3, 1), RoomSize.Suite),
                (new DateOnly(2026, 7, 1), RoomSize.Single), (new DateOnly(2026, 7, 1), RoomSize.Double), (new DateOnly(2026, 7, 1), RoomSize.Suite),
            ],
            result.Select(s => (s.FirstDay, s.Size)));
        Assert.All(result, s => Assert.Equal(s.FirstDay.AddMonths(1).AddDays(-1), s.LastDay));
    }

    /// <summary>Positive: a high month next to a low month are two runs, not one.</summary>
    [Fact]
    public void Suggest_HighMonthNextToLowMonth_AreSeparateRuns()
    {
        var result = PriceSuggestionRule.Suggest(Year(2025, (6, 90m), (7, 30m)), [new(RoomSize.Single, 100m)], PricingSettings.Defaults, Today);

        Assert.Equal([Season.High, Season.Low], result.Select(s => s.Season));
    }

    /// <summary>Positive: June 90 in one year and 70 in another average 80, which is high.</summary>
    [Fact]
    public void Suggest_JuneOverTwoYears_UsesTheAverage()
    {
        var observations = Year(2024, (6, 90m)).Concat(Year(2025, (6, 70m))).ToArray();

        var result = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today);

        Assert.Equal(3, result.Count);
        Assert.All(result, s => Assert.Equal(80m, s.OccupancyPercent));
    }

    #endregion

    #region Basis and horizon

    /// <summary>Negative: figures for only 11 distinct calendar months give no suggestion.</summary>
    [Fact]
    public void Suggest_ElevenCalendarMonths_GivesNothing()
    {
        var observations = Year(2025, (6, 90m)).Where(o => o.Month != 11).ToArray();

        Assert.Empty(PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today));
    }

    /// <summary>Positive: eleven months in one year and the twelfth in another make twelve calendar months, so it works.</summary>
    [Fact]
    public void Suggest_TwelveCalendarMonthsFromTwoYears_Works()
    {
        var observations = Year(2025, (6, 90m)).Where(o => o.Month != 11).Append(new OccupancyObservation(2024, 11, 60m)).ToArray();

        Assert.Equal(3, PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today).Count);
    }

    /// <summary>Positive: the same year and month twice is averaged: 100 and 60 make 80, which is high.</summary>
    [Fact]
    public void Suggest_DuplicateYearAndMonth_AreAveraged()
    {
        var observations = Year(2025, (6, 100m)).Append(new OccupancyObservation(2025, 6, 60m)).ToArray();

        var result = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today);

        Assert.All(result, s => Assert.Equal(80m, s.OccupancyPercent));
        Assert.Equal(3, result.Count);
    }

    /// <summary>Positive: on 2026-11-30 the horizon starts on 2026-12-01 and ends 2027-11-30.</summary>
    [Fact]
    public void Suggest_TodayInNovember_StartsTheHorizonInDecember()
    {
        var observations = Year(2025, (12, 90m), (11, 90m));

        var result = PriceSuggestionRule.Suggest(observations, [new(RoomSize.Double, 100m)], PricingSettings.Defaults, new DateOnly(2026, 11, 30));

        Assert.Equal([new DateOnly(2026, 12, 1), new DateOnly(2027, 11, 1)], result.Select(s => s.FirstDay));
        Assert.Equal(new DateOnly(2027, 11, 30), result[1].LastDay);
    }

    /// <summary>Positive: on 2026-12-15 the horizon starts on 2027-01-01, so a high December falls in 2027.</summary>
    [Fact]
    public void Suggest_TodayInDecember_StartsTheHorizonInJanuary()
    {
        var observations = Year(2025, (1, 90m), (12, 90m));

        var result = PriceSuggestionRule.Suggest(observations, [new(RoomSize.Double, 100m)], PricingSettings.Defaults, new DateOnly(2026, 12, 15));

        Assert.Equal([new DateOnly(2027, 1, 1)], result.Select(s => s.FirstDay).Take(1));
        Assert.Equal(new DateOnly(2027, 1, 31), result[0].LastDay);
        Assert.Equal(new DateOnly(2027, 12, 1), result[1].FirstDay);
    }

    #endregion

    #region Determinism

    /// <summary>Positive: the same input gives the same suggestions, element by element.</summary>
    [Fact]
    public void Suggest_SameInputTwice_GivesEqualSuggestions()
    {
        var observations = Year(2025, (3, 90m), (6, 30m), (7, 85m));

        var first = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today);
        var second = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today);

        Assert.NotEmpty(first);
        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    /// <summary>Positive: the order of the observations and of the rates does not change the result.</summary>
    [Fact]
    public void Suggest_ReorderedInput_GivesTheSameSuggestions()
    {
        var observations = Year(2025, (3, 90m), (6, 30m), (7, 85m));
        var expected = PriceSuggestionRule.Suggest(observations, Rates, PricingSettings.Defaults, Today);

        var actual = PriceSuggestionRule.Suggest(observations.Reverse().ToArray(), Rates.Reverse().ToArray(), PricingSettings.Defaults, Today);

        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    /// <summary>Negative: settings that make no sense are refused before anything is suggested.</summary>
    [Fact]
    public void Suggest_LowThresholdNotBelowHigh_Throws()
    {
        var settings = new PricingSettings(HighThreshold: 40m, LowThreshold: 40m);

        Assert.Throws<ArgumentException>(() => PriceSuggestionRule.Suggest(Year(2025), Rates, settings, Today));
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
