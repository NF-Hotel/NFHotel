using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Reading the monthly occupancy and the explanation from a market analysis report (MIL-055).</summary>
public sealed class OccupancyObservationReaderTests
{
    private static MarketAnalysisContentDto Section(MarketAnalysisSectionDto section) => new([], [section]);

    private static MarketAnalysisSectionDto Occupancy(string status, params MarketAnalysisSeriesDto[] series) =>
        new("occupancy", "Occupancy", status, null, series, null);

    #region Reading the occupancy

    /// <summary>Positive: the monthly series in percent gives one observation per month, with the value as it is.</summary>
    [Fact]
    public void Read_MonthlySeriesInPercent_ReturnsOneObservationPerMonthAsPercent()
    {
        // Arrange
        var content = PriceSuggestionTestData.Content();

        // Act
        var observations = OccupancyObservationReader.Read(content);

        // Assert
        Assert.Equal(12, observations.Count);
        Assert.Equal(new OccupancyObservation(2019, 1, 30m), observations[0]);
        Assert.Equal(new OccupancyObservation(2019, 7, 90m), observations[6]);
    }

    /// <summary>Equivalence: a percent can pass 100, because the service caps nothing, and keeps its decimals.</summary>
    [Fact]
    public void Read_PercentAbove100WithDecimals_IsKept()
    {
        // Arrange
        var series = new MarketAnalysisSeriesDto("monthly", "Monthly", "%", [new("2019-07", 112.5), new("2019-08", 89.2)]);

        // Act
        var observations = OccupancyObservationReader.Read(Section(Occupancy("available", series)));

        // Assert
        Assert.Equal([new OccupancyObservation(2019, 7, 112.5m), new OccupancyObservation(2019, 8, 89.2m)], observations);
    }

    /// <summary>Equivalence: a series without the percent unit whose values are all fractions is multiplied by 100.</summary>
    [Fact]
    public void Read_FractionsWithoutUnit_AreMultipliedBy100()
    {
        // Arrange
        var series = new MarketAnalysisSeriesDto("monthly", "Monthly", string.Empty, [new("2019-07", 0.9), new("2019-08", 0.25)]);

        // Act
        var observations = OccupancyObservationReader.Read(Section(Occupancy("available", series)));

        // Assert
        Assert.Equal([new OccupancyObservation(2019, 7, 90m), new OccupancyObservation(2019, 8, 25m)], observations);
    }

    /// <summary>Equivalence: a series without a unit that has a value above 1 is percent, and with the percent unit values of 0..1 stay percent.</summary>
    [Theory]
    [InlineData("", 0.9, 80.0, 0.9)]
    [InlineData("%", 0.9, 0.5, 0.9)]
    public void Read_PercentSeries_IsNotScaled(string unit, double first, double second, double expectedFirst)
    {
        // Arrange
        var series = new MarketAnalysisSeriesDto("monthly", "Monthly", unit, [new("2019-07", first), new("2019-08", second)]);

        // Act
        var observations = OccupancyObservationReader.Read(Section(Occupancy("available", series)));

        // Assert
        Assert.Equal((decimal)expectedFirst, observations[0].OccupancyPercent);
        Assert.Equal((decimal)second, observations[1].OccupancyPercent);
    }

    /// <summary>Negative: points that cannot be read (a text label, a week, a month 13, a NaN, a negative value) are skipped and nothing throws.</summary>
    [Fact]
    public void Read_UnparseablePoints_AreSkipped()
    {
        // Arrange
        var series = new MarketAnalysisSeriesDto(
            "monthly",
            "Monthly",
            "%",
            [
                new("Overall", 40),
                new("2019-W26", 50),
                new("2019-13", 50),
                new("2019-07", double.NaN),
                new("2019-08", -5),
                new("2019-09", 77.5),
                new(" 2019-10 ", 61),
            ]);

        // Act
        var observations = OccupancyObservationReader.Read(Section(Occupancy("available", series)));

        // Assert
        Assert.Equal([new OccupancyObservation(2019, 9, 77.5m), new OccupancyObservation(2019, 10, 61m)], observations);
    }

    /// <summary>Equivalence: the series with the id monthly is preferred; otherwise the first series of months is used.</summary>
    [Fact]
    public void Read_SeveralSeries_PrefersMonthlyThenTheFirstSeriesOfMonths()
    {
        // Arrange
        var other = new MarketAnalysisSeriesDto("other", "Other", "%", [new("2019-01", 10)]);
        var monthly = new MarketAnalysisSeriesDto("monthly", "Monthly", "%", [new("2019-02", 20)]);
        var holidays = new MarketAnalysisSeriesDto("a+b", "Holidays", "%", [new("Holidays", 70), new("Other days", 40)]);

        // Act
        var preferred = OccupancyObservationReader.Read(Section(Occupancy("available", holidays, other, monthly)));
        var fallback = OccupancyObservationReader.Read(Section(Occupancy("available", holidays, other)));

        // Assert
        Assert.Equal([new OccupancyObservation(2019, 2, 20m)], preferred);
        Assert.Equal([new OccupancyObservation(2019, 1, 10m)], fallback);
    }

    /// <summary>Negative: no content, no occupancy section, an unavailable section or no readable point gives nothing.</summary>
    [Fact]
    public void Read_NothingUsable_ReturnsEmpty()
    {
        // Arrange
        var points = new MarketAnalysisSeriesDto("monthly", "Monthly", "%", [new("2019-01", 10)]);
        var noMonths = new MarketAnalysisSeriesDto("monthly", "Monthly", "%", [new("Overall", 10)]);
        var other = new MarketAnalysisSectionDto("lead_time", "Lead time", "available", null, [points], null);

        // Act and assert
        Assert.Empty(OccupancyObservationReader.Read(null));
        Assert.Empty(OccupancyObservationReader.Read(new MarketAnalysisContentDto([], [])));
        Assert.Empty(OccupancyObservationReader.Read(Section(other)));
        Assert.Empty(OccupancyObservationReader.Read(Section(Occupancy("unavailable", points))));
        Assert.Empty(OccupancyObservationReader.Read(Section(Occupancy("available", noMonths))));
        Assert.Empty(OccupancyObservationReader.Read(Section(Occupancy("available"))));
    }

    #endregion

    #region Reading the explanation

    /// <summary>Positive: the executive summary of an available insight on the occupancy is returned trimmed.</summary>
    [Fact]
    public void ReadExplanation_AvailableInsight_ReturnsTheSummary()
    {
        // Arrange
        var content = PriceSuggestionTestData.Content(explanation: "  Summer is full.  ");

        // Act
        var text = OccupancyObservationReader.ReadExplanation(content);

        // Assert
        Assert.Equal("Summer is full.", text);
    }

    /// <summary>Negative: no insight, an unavailable one, or an empty summary gives null.</summary>
    [Fact]
    public void ReadExplanation_NoUsableInsight_ReturnsNull()
    {
        // Arrange
        var unavailable = new MarketAnalysisSectionDto(
            "occupancy",
            "Occupancy",
            "available",
            null,
            [],
            new MarketAnalysisInsightDto("unavailable", "TIMEOUT", "text", []));
        var empty = unavailable with { Insight = new MarketAnalysisInsightDto("available", null, "  ", []) };

        // Act and assert
        Assert.Null(OccupancyObservationReader.ReadExplanation(null));
        Assert.Null(OccupancyObservationReader.ReadExplanation(PriceSuggestionTestData.Content()));
        Assert.Null(OccupancyObservationReader.ReadExplanation(Section(unavailable)));
        Assert.Null(OccupancyObservationReader.ReadExplanation(Section(empty)));
    }

    #endregion
}
