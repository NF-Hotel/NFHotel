using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Tests;

/// <summary>The one rule that gives each series of the market analysis report its kind of chart (MIL-026, criteria 18 and 19).</summary>
public sealed class ChartKindSelectorTests
{
    #region Tile and donut

    /// <summary>Boundary: one point is a tile, two points are not.</summary>
    [Fact]
    public void Select_OnePoint_IsATile()
    {
        Assert.Equal(ChartKind.Tile, ChartKindSelector.Select(Whole("a")));
        Assert.NotEqual(ChartKind.Tile, ChartKindSelector.Select(Whole("a", "b")));
    }

    /// <summary>Boundary: two, three and four parts of one whole are a donut; five are ranked bars.</summary>
    [Theory]
    [InlineData(2, ChartKind.Donut)]
    [InlineData(3, ChartKind.Donut)]
    [InlineData(4, ChartKind.Donut)]
    [InlineData(5, ChartKind.RankedBars)]
    [InlineData(12, ChartKind.RankedBars)]
    public void Select_PartsOfAWhole_AreADonutOnlyUpToFourParts(int parts, ChartKind expected)
    {
        // Arrange
        MarketAnalysisSeriesDto series = Whole([.. Enumerable.Range(1, parts).Select(index => $"part {index}")]);

        // Act and assert
        Assert.Equal(expected, ChartKindSelector.Select(series));
    }

    /// <summary>Negative: two separate rates, such as the occupancy on holidays and on other days, are columns and never a donut.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Select_SeparateRates_AreColumnsAndNeverADonut(int rates)
    {
        // Arrange
        MarketAnalysisSeriesDto series = Series(false, false, [.. Enumerable.Range(1, rates).Select(index => $"rate {index}")]);

        // Act and assert
        Assert.Equal(ChartKind.Columns, ChartKindSelector.Select(series));
    }

    #endregion

    #region Ranked bars and columns

    /// <summary>Positive: more than four categories that are not ordered are ranked bars, parts of a whole or not.</summary>
    [Fact]
    public void Select_FiveCategories_AreRankedBars()
    {
        Assert.Equal(ChartKind.RankedBars, ChartKindSelector.Select(Series(false, false, "a", "b", "c", "d", "e")));
    }

    /// <summary>Positive: ordered bands are columns, also when they add up to 100 % and also when there are more than four.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(15)]
    public void Select_OrderedBands_AreColumnsInTheirOrder(int bands)
    {
        // Arrange
        MarketAnalysisSeriesDto series = Series(true, true, [.. Enumerable.Range(1, bands).Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture))]);

        // Act and assert
        Assert.Equal(ChartKind.Columns, ChartKindSelector.Select(series));
    }

    #endregion

    #region Months

    /// <summary>Boundary: one month is a tile, two months are two columns, three months are a line.</summary>
    [Theory]
    [InlineData(1, ChartKind.Tile)]
    [InlineData(2, ChartKind.Columns)]
    [InlineData(3, ChartKind.Line)]
    public void Select_FewMonths_FollowTheLimits(int months, ChartKind expected)
    {
        Assert.Equal(expected, ChartKindSelector.Select(Months(2018, 1, months)));
    }

    /// <summary>Positive: the 12 months of one year are a line.</summary>
    [Fact]
    public void Select_MonthsOfOneYear_AreALine()
    {
        Assert.Equal(ChartKind.Line, ChartKindSelector.Select(Months(2018, 1, 12)));
    }

    /// <summary>Positive: months against years are a heat map from 24 months over three calendar years.</summary>
    [Fact]
    public void Select_TwentyFourMonthsOverThreeYears_AreAHeatmap()
    {
        Assert.Equal(ChartKind.Heatmap, ChartKindSelector.Select(Months(2016, 12, 24)));
    }

    /// <summary>Boundary: 23 months over three years and 24 months over two years are still a line.</summary>
    [Fact]
    public void Select_BelowTheHeatmapLimits_AreALine()
    {
        Assert.Equal(ChartKind.Line, ChartKindSelector.Select(Months(2016, 12, 23)));
        Assert.Equal(ChartKind.Line, ChartKindSelector.Select(Months(2017, 1, 24)));
    }

    /// <summary>Positive: weeks are a line too.</summary>
    [Fact]
    public void Select_Weeks_AreALine()
    {
        // Arrange
        MarketAnalysisSeriesDto series = Series(true, false, "2018-W26", "2018-W27", "2018-W28", "2018-W29");

        // Act and assert
        Assert.Equal(ChartKind.Line, ChartKindSelector.Select(series));
    }

    #endregion

    #region Fallback

    /// <summary>Negative: a series with a value below zero keeps the grouped bars, because the other kinds draw from zero.</summary>
    [Fact]
    public void Select_NegativeValue_KeepsTheGroupedBars()
    {
        // Arrange
        MarketAnalysisSeriesDto series = new MarketAnalysisSeriesDto("s", "S", "%", [new("a", 1), new("b", -2), new("c", 3)]);

        // Act and assert
        Assert.Equal(ChartKind.GroupedBars, ChartKindSelector.Select(series));
    }

    /// <summary>Boundary: a series with no point keeps the grouped bars, which draw nothing.</summary>
    [Fact]
    public void Select_NoPoint_KeepsTheGroupedBars()
    {
        Assert.Equal(ChartKind.GroupedBars, ChartKindSelector.Select(new MarketAnalysisSeriesDto("s", "S", "%", [])));
    }

    #endregion

    #region Real sample

    /// <summary>
    /// Positive: each series of the real 5,000 booking result gets the expected kind. The table is also what the page draws:
    /// the occupancy is a line with the overall level, the two occupancy rates are columns and no series is a donut by its name.
    /// </summary>
    [Theory]
    [InlineData("lead_time", "overall.bands", ChartKind.Columns)]
    [InlineData("lead_time", "splits.by_cancellation_status.groups", ChartKind.Donut)]
    [InlineData("lead_time", "splits.by_customer_type.groups", ChartKind.Donut)]
    [InlineData("lead_time", "splits.by_market_segment.groups", ChartKind.RankedBars)]
    [InlineData("lead_time", "splits.by_arrival_month.groups", ChartKind.Line)]
    [InlineData("seasonality", "arrivals.monthly.periods", ChartKind.Line)]
    [InlineData("seasonality", "bookings.monthly.periods", ChartKind.Heatmap)]
    [InlineData("seasonality", "arrivals.iso_week.periods", ChartKind.Line)]
    [InlineData("cancellations", "splits.by_deposit_type.groups", ChartKind.Columns)]
    [InlineData("cancellations", "splits.by_lead_time_band.groups", ChartKind.Columns)]
    [InlineData("cancellations", "splits.by_customer_type.groups", ChartKind.Columns)]
    [InlineData("cancellations", "splits.by_special_requests.groups", ChartKind.Columns)]
    [InlineData("cancellations", "splits.by_market_segment.groups", ChartKind.RankedBars)]
    [InlineData("cancellations", "splits.by_booking_changes.groups", ChartKind.Columns)]
    [InlineData("cancellations", "splits.by_arrival_month.groups", ChartKind.Line)]
    [InlineData("room_value", "length_of_stay.buckets", ChartKind.Columns)]
    [InlineData("guest_mix", "distributions.is_repeated_guest.groups", ChartKind.Tile)]
    [InlineData("guest_mix", "distributions.required_car_parking_spaces.groups", ChartKind.Columns)]
    [InlineData("guest_mix", "distributions.meal.groups", ChartKind.RankedBars)]
    [InlineData("guest_mix", "distributions.total_of_special_requests.groups", ChartKind.Columns)]
    [InlineData("guest_mix", "distributions.assigned_room_type.groups", ChartKind.RankedBars)]
    [InlineData("guest_mix", "distributions.country.groups", ChartKind.RankedBars)]
    [InlineData("guest_mix", "distributions.total_guests.groups", ChartKind.Columns)]
    [InlineData("occupancy", "monthly", ChartKind.Line)]
    [InlineData("occupancy_on_holidays", "holiday_days+other_days", ChartKind.Columns)]
    public void Select_RealSample_GivesTheExpectedKind(string analysis, string seriesId, ChartKind expected)
    {
        // Arrange
        MarketAnalysisSeriesDto series = MarketSample.Section(analysis).Series.Single(found => found.Id == seriesId);

        // Act and assert
        Assert.Equal(expected, ChartKindSelector.Select(series));
    }

    /// <summary>Positive: the table above is complete, so a new series in the sample makes the table fail and be looked at.</summary>
    [Fact]
    public void Select_RealSample_EverySeriesIsInTheTable()
    {
        // Act
        int count = MarketSample.Sections().Sum(section => section.Series.Count);

        // Assert
        Assert.Equal(25, count);
    }

    /// <summary>Negative: in the real sample no donut has more than four parts or figures that are not parts of one whole (criterion 19).</summary>
    [Fact]
    public void Select_RealSample_NeverDrawsADonutForMoreThanFourPartsOrForSeparateRates()
    {
        // Arrange
        List<MarketAnalysisSeriesDto> donuts = MarketSample.Sections().SelectMany(section => section.Series).Where(series => ChartKindSelector.Select(series) == ChartKind.Donut).ToList();

        // Assert
        Assert.NotEmpty(donuts);
        Assert.All(donuts, series =>
        {
            Assert.True(series.IsPartsOfWhole);
            Assert.InRange(series.Points.Count, 2, ChartKindSelector.MaxDonutParts);
            Assert.InRange(series.Points.Sum(point => point.Value), 99.5, 100.5);
        });
    }

    #endregion

    #region Helpers

    private static MarketAnalysisSeriesDto Whole(params string[] labels) => Series(false, true, labels);

    private static MarketAnalysisSeriesDto Series(bool ordered, bool whole, params string[] labels) =>
        new("s", "S", "%", [.. labels.Select(label => new MarketAnalysisPointDto(label, 10))], ordered, whole);

    private static MarketAnalysisSeriesDto Months(int startYear, int startMonth, int count) =>
        Series(true, false, [.. Enumerable.Range(0, count).Select(index => Month(startYear, startMonth, index))]);

    private static string Month(int startYear, int startMonth, int offset)
    {
        int total = (startYear * 12) + (startMonth - 1) + offset;

        return $"{total / 12:0000}-{(total % 12) + 1:00}";
    }

    #endregion
}
