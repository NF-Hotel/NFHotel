using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// What a series carries so that the page can choose its chart (MIL-026 task 17): whether its points are ordered, whether they are
/// the parts of one whole, and the overall level next to a series of months. Derived from the shape of the findings and never from
/// the name of an analysis.
/// </summary>
public sealed class FindingsSeriesShapeTests
{
    #region Parts of a whole

    /// <summary>Positive: the groups of one split whose numerators add up to the one denominator are parts of a whole.</summary>
    [Fact]
    public void Extract_SharesThatAddUpToTheWhole_ArePartsOfAWhole()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"groups":[{"group":"a","numerator":30,"denominator":100},{"group":"b","numerator":70,"denominator":100}]}""");

        // Assert
        Assert.True(series.IsPartsOfWhole);
        Assert.False(series.IsOrdered);
    }

    /// <summary>Negative: rates with their own denominators (the cancellation share of each group) are not parts of a whole.</summary>
    [Fact]
    public void Extract_RatesWithTheirOwnDenominators_AreNotPartsOfAWhole()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"groups":[{"group":"a","numerator":30,"denominator":50},{"group":"b","numerator":10,"denominator":100}]}""");

        // Assert
        Assert.False(series.IsPartsOfWhole);
    }

    /// <summary>Negative: shares of one denominator that leave a part out (they add up to less than the whole) are not parts of a whole.</summary>
    [Fact]
    public void Extract_SharesThatDoNotAddUp_AreNotPartsOfAWhole()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"groups":[{"group":"a","numerator":30,"denominator":100},{"group":"b","numerator":60,"denominator":100}]}""");

        // Assert
        Assert.False(series.IsPartsOfWhole);
    }

    /// <summary>Negative: a count has no denominator, so it is not a part of a whole.</summary>
    [Fact]
    public void Extract_Counts_AreNotPartsOfAWhole()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"groups":[{"group":"a","count":3},{"group":"b","count":7}]}""");

        // Assert
        Assert.False(series.IsPartsOfWhole);
    }

    /// <summary>Boundary: a series cut to its latest 30 months no longer adds up to its whole.</summary>
    [Fact]
    public void Extract_CutSeries_IsNotPartsOfAWhole()
    {
        // Arrange
        IEnumerable<string> months = Enumerable.Range(0, 40).Select(index => $$"""{"group":"{{2015 + (index / 12)}}-{{(index % 12) + 1:00}}","numerator":1,"denominator":40}""");

        // Act
        MarketAnalysisSeriesDto series = Single($$"""{"periods":[{{string.Join(',', months)}}]}""");

        // Assert
        Assert.Equal(FindingsSeries.MaxPoints, series.Points.Count);
        Assert.False(series.IsPartsOfWhole);
    }

    #endregion

    #region Order

    /// <summary>Positive: bands, numbers, open ends and months are ordered, however the findings call them.</summary>
    [Theory]
    [InlineData("0-7", "8-30", "91+")]
    [InlineData("1", "2", "7+")]
    [InlineData("2018-07", "2018-08", "2018-09")]
    [InlineData("2018-W26", "2018-W27", "2018-W28")]
    public void Extract_LabelsWithANaturalOrder_AreOrdered(string first, string second, string third)
    {
        // Act
        MarketAnalysisSeriesDto series = Single(Groups(first, second, third));

        // Assert
        Assert.True(series.IsOrdered);
    }

    /// <summary>Negative: categories (a segment, a country) are not ordered.</summary>
    [Fact]
    public void Extract_Categories_AreNotOrdered()
    {
        // Act
        MarketAnalysisSeriesDto series = Single(Groups("Online TA", "Direct", "Corporate"));

        // Assert
        Assert.False(series.IsOrdered);
    }

    /// <summary>Positive: a list the service calls bands or buckets is ordered even when a label is not a number.</summary>
    [Fact]
    public void Extract_ListNamedBands_IsOrdered()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"bands":[{"group":"short","numerator":1,"denominator":2},{"group":"long","numerator":1,"denominator":2}]}""");

        // Assert
        Assert.True(series.IsOrdered);
    }

    #endregion

    #region Overall level

    /// <summary>Positive: the overall figure that sits next to the months is the reference level of that series.</summary>
    [Fact]
    public void Extract_OverallNextToMonths_IsTheReference()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"overall":{"group":"all","numerator":404,"denominator":1000},"monthly":[{"group":"2018-07","numerator":1,"denominator":4},{"group":"2018-08","numerator":2,"denominator":4}]}""");

        // Assert
        Assert.Equal(40.4, series.ReferenceValue);
        Assert.Equal("Overall", series.ReferenceLabel);
    }

    /// <summary>Negative: an overall figure next to bands, or one that is wrapped in a figure, is no reference line.</summary>
    [Fact]
    public void Extract_OverallNextToBands_HasNoReference()
    {
        // Act
        MarketAnalysisSeriesDto series = Single("""{"overall":{"figure":{"group":"all","numerator":4,"denominator":4},"bands":[{"group":"0-7","numerator":1,"denominator":4},{"group":"8-30","numerator":3,"denominator":4}]}}""");

        // Assert
        Assert.Null(series.ReferenceValue);
        Assert.Null(series.ReferenceLabel);
    }

    #endregion

    #region Stored reports

    /// <summary>Compatibility: a series stored before the new fields existed still reads, with the defaults.</summary>
    [Fact]
    public void Deserialize_StoredSeriesWithoutTheNewFields_UsesTheDefaults()
    {
        // Arrange
        const string Stored = """{"Id":"a","Title":"A","Unit":"%","Points":[{"Label":"x","Value":1,"SmallSample":false}]}""";

        // Act
        MarketAnalysisSeriesDto series = JsonSerializer.Deserialize<MarketAnalysisSeriesDto>(Stored)!;

        // Assert
        Assert.False(series.IsOrdered);
        Assert.False(series.IsPartsOfWhole);
        Assert.Null(series.ReferenceValue);
        Assert.Null(series.ReferenceLabel);
    }

    #endregion

    #region Real sample

    /// <summary>Positive: in the real sample the cancelled and not cancelled bookings are parts of a whole, the cancellation shares of the deposit types are not, and the months of the occupancy carry the overall level.</summary>
    [Fact]
    public void Map_Sample_CarriesTheShapeOfEachSeries()
    {
        // Act
        IReadOnlyList<MarketAnalysisSeriesDto> lead = Sample("lead_time").Series;
        IReadOnlyList<MarketAnalysisSeriesDto> cancellations = Sample("cancellations").Series;
        MarketAnalysisSeriesDto occupancy = Assert.Single(Sample("occupancy").Series);
        MarketAnalysisSeriesDto holidays = Assert.Single(Sample("occupancy_on_holidays").Series);

        // Assert
        Assert.True(lead.Single(series => series.Id == "splits.by_cancellation_status.groups").IsPartsOfWhole);
        Assert.True(lead.Single(series => series.Id == "overall.bands").IsOrdered);
        Assert.False(cancellations.Single(series => series.Id == "splits.by_deposit_type.groups").IsPartsOfWhole);
        Assert.True(cancellations.Single(series => series.Id == "splits.by_lead_time_band.groups").IsOrdered);
        Assert.True(occupancy.IsOrdered);
        Assert.Equal(40.4, occupancy.ReferenceValue);
        Assert.False(holidays.IsPartsOfWhole);
        Assert.False(holidays.IsOrdered);
        Assert.Equal(2, holidays.Points.Count);
    }

    #endregion

    #region Helpers

    private static string Groups(params string[] labels) =>
        "{\"groups\":[" + string.Join(',', labels.Select(label => $$"""{"group":"{{label}}","numerator":1,"denominator":3}""")) + "]}";

    private static MarketAnalysisSeriesDto Single(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return Assert.Single(FindingsSeries.Extract(document.RootElement));
    }

    private static MarketAnalysisSectionDto Sample(string id)
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));
        using JsonDocument document = JsonDocument.Parse(json);

        return MarketAnalysisResultMapper.Map(document.RootElement)!.Content!.Sections.Single(section => section.Id == id);
    }

    #endregion
}
