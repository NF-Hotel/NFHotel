using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The mapper of result schema 1.1 of the market analysis service against its sample, a result of the service for 5,000 bookings
/// (insights off, so no price and no insight) with one hand-made insight with two observations on the cancellations.
/// </summary>
public sealed class MarketAnalysisResultMapperTests
{
    private static readonly string[] NineAnalyses =
        ["lead_time", "holidays", "seasonality", "cancellations", "room_value", "guest_mix", "occupancy", "occupancy_on_holidays", "correlations"];

    #region Whole result

    /// <summary>Positive: the sample is read: schema 1.1 as major 1, the headline figures and the nine analyses in the order of the service.</summary>
    [Fact]
    public void Map_Sample_ReadsTheNineAnalysesAndTheFigures()
    {
        // Act
        var dto = Map(SampleJson());

        // Assert
        Assert.Equal((1, Guid.Parse("fa01a9e2-21eb-4084-bf13-8d52feeb0360"), null, null), (dto.SchemaVersion, dto.RunId, dto.Provider, dto.Model));
        Assert.Equal(NineAnalyses, dto.Content!.Sections.Select(section => section.Id));
        Assert.Equal(["Bookings analysed", "Bookings with a price of 0", "Duplicate booking ids"], dto.Content.Figures.Select(figure => figure.Label));
        Assert.Equal([5000d, 0d, 0d], dto.Content.Figures.Select(figure => figure.Value));
    }

    /// <summary>Positive: every analysis has a title for the page, and the three new ones have their own.</summary>
    [Fact]
    public void Map_Sample_GivesEverySectionATitle()
    {
        // Act
        var sections = Map(SampleJson()).Content!.Sections;

        // Assert
        Assert.Equal(
            ["Lead time", "Cambodian holidays", "Seasonality and booking pace", "Cancellations", "Room value and stay", "Guest and booking mix", "Occupancy", "Occupancy on holidays", "Correlations between booking fields"],
            sections.Select(section => section.Title));
    }

    /// <summary>Positive: the schema version is kept as the major number, so the runner can refuse an unknown one.</summary>
    [Theory]
    [InlineData("1.0", 1)]
    [InlineData("1.1", 1)]
    [InlineData("1.7", 1)]
    [InlineData("2.0", 2)]
    public void Map_SchemaVersion_IsTheMajorNumber(string version, int major)
    {
        // Act
        var dto = Map("{\"schema_version\":\"" + version + "\",\"status\":\"completed\",\"analyses\":{}}");

        // Assert
        Assert.Equal(major, dto.SchemaVersion);
    }

    /// <summary>Negative: a document without a schema version, a failed result and a document that is not an object are not results.</summary>
    [Theory]
    [InlineData("""{"status":"completed","analyses":{}}""")]
    [InlineData("""{"schema_version":"x","status":"completed"}""")]
    [InlineData("""{"schema_version":"1.1","status":"failed"}""")]
    [InlineData("[]")]
    public void Map_NotAResult_IsNull(string json)
    {
        // Act
        using var document = JsonDocument.Parse(json);

        // Assert
        Assert.Null(MarketAnalysisResultMapper.Map(document.RootElement));
    }

    /// <summary>Positive: a version 1.0 result has no formula, limitations or observations; the fields are empty and the rest is read.</summary>
    [Fact]
    public void Map_Version10_ReadsWithoutTheNewFields()
    {
        // Act
        var dto = Map("""{"schema_version":"1.0","result_id":"5b3f0e1a-0000-4000-8000-0000000000a1","generated_at":"2026-10-19T10:04:00.000Z","status":"completed","analyses":{"guest_mix":{"status":"available","findings":{}}},"notices":[]}""");

        // Assert
        var section = Assert.Single(dto.Content!.Sections);
        Assert.Equal(("Guest and booking mix", null, null, null), (section.Title, section.Insight, section.Formula, section.Limitations));
        Assert.Empty(section.Matrices!);
    }

    #endregion

    #region Findings

    /// <summary>Positive: lead time gives the overall bands first, then the splits, as percent shares.</summary>
    [Fact]
    public void Map_Sample_LeadTimeStartsWithTheBands()
    {
        // Act
        var lead = Section("lead_time");

        // Assert
        Assert.Equal("overall.bands", lead.Series[0].Id);
        Assert.Equal(["0-7", "8-30", "31-90", "91+"], lead.Series[0].Points.Select(point => point.Label));
        Assert.Equal(11.8, lead.Series[0].Points[0].Value);
        Assert.Contains("splits.by_market_segment.groups", lead.Series.Select(series => series.Id));
    }

    /// <summary>Positive: the cancellation splits are read from the wrapped figures, and a small sample is flagged.</summary>
    [Fact]
    public void Map_Sample_CancellationsReadTheSplitsAndFlagTheSmallSamples()
    {
        // Act
        var cancellations = Section("cancellations");

        // Assert
        var deposit = cancellations.Series.Single(series => series.Id == "splits.by_deposit_type.groups");
        Assert.Equal("%", deposit.Unit);
        Assert.Equal(["No Deposit", "Non Refund", "Refundable"], deposit.Points.Select(point => point.Label));
        Assert.Equal([41.5, 91.8, 12.9], deposit.Points.Select(point => point.Value));
        var customers = cancellations.Series.Single(series => series.Id == "splits.by_customer_type.groups");
        Assert.True(customers.Points.Single(point => point.Label == "Group Contract").SmallSample);
        Assert.False(customers.Points.Single(point => point.Label == "No Contract (Single)").SmallSample);
    }

    /// <summary>Positive: the holiday figures are not groups of one list, so they give no chart; the analysis is still read.</summary>
    [Fact]
    public void Map_Sample_HolidaysHaveNoSeriesButFormulaAndLimitations()
    {
        // Act
        var holidays = Section("holidays");

        // Assert
        Assert.Empty(holidays.Series);
        Assert.NotEmpty(holidays.Limitations!);
    }

    /// <summary>Positive: the occupancy gives the months as shares of the room nights, and the overall figure is not a series.</summary>
    [Fact]
    public void Map_Sample_OccupancyGivesTheMonthlySeries()
    {
        // Act
        var occupancy = Section("occupancy");

        // Assert
        var monthly = Assert.Single(occupancy.Series);
        Assert.Equal(("monthly", "%", 10), (monthly.Id, monthly.Unit, monthly.Points.Count));
        Assert.Equal(("2018-07", 89.2), (monthly.Points[0].Label, monthly.Points[0].Value));
    }

    /// <summary>Positive: the occupancy on holidays and on other days are two rates in one series of two points, not parts of a whole.</summary>
    [Fact]
    public void Map_Sample_OccupancyOnHolidaysGivesTwoRates()
    {
        // Act
        var series = Assert.Single(Section("occupancy_on_holidays").Series);

        // Assert
        Assert.Equal(("holiday_days+other_days", "Holiday days and other days", "%"), (series.Id, series.Title, series.Unit));
        Assert.Equal([("Holiday days", 27.9), ("Other days", 40.7)], series.Points.Select(point => (point.Label, point.Value)));
    }

    /// <summary>Positive: the correlations are a matrix of their own and give no series.</summary>
    [Fact]
    public void Map_Sample_CorrelationsAreAMatrix()
    {
        // Act
        var correlations = Section("correlations");

        // Assert
        Assert.Empty(correlations.Series);
        var matrix = Assert.Single(correlations.Matrices!);
        Assert.Equal(("matrix", "spearman", 12), (matrix.Id, matrix.Method, matrix.Fields.Count));
        Assert.Equal("lead_time", matrix.Fields[0]);
        Assert.Equal(12, matrix.Cells.Count);
        Assert.All(matrix.Cells, row => Assert.Equal(12, row.Count));

        // A field that never varies has no correlation: its row and column are null, also on the diagonal.
        Assert.All(Enumerable.Range(0, 12).Select(i => matrix.Cells[i][i]), cell => Assert.True(cell is null or 1.0));
        Assert.Contains(matrix.Cells.SelectMany(row => row), cell => cell is null);
    }

    /// <summary>Boundary: a matrix with a null cell keeps it, and a matrix that is not square or has no fields is left out.</summary>
    [Theory]
    [InlineData("""{"method":"spearman","fields":["a","b"],"matrix":[[1.0,null],[null,1.0]]}""", true)]
    [InlineData("""{"method":"spearman","fields":["a","b"],"matrix":[[1.0,0.5]]}""", false)]
    [InlineData("""{"method":"spearman","fields":["a","b"],"matrix":[[1.0],[0.5]]}""", false)]
    [InlineData("""{"method":"spearman","fields":[],"matrix":[]}""", false)]
    [InlineData("""{"fields":["a"]}""", false)]
    public void ExtractMatrices_Shape_DecidesWhetherThereIsAMatrix(string findings, bool found)
    {
        // Act
        using var document = JsonDocument.Parse(findings);
        var matrices = FindingsSeries.ExtractMatrices(document.RootElement);

        // Assert
        Assert.Equal(found, matrices.Count == 1);
        if (found)
        {
            Assert.Null(matrices[0].Cells[0][1]);
        }
    }

    /// <summary>Positive: an analysis that is unavailable keeps its reason, and the placeholders of its unavailable part are not drawn.</summary>
    [Fact]
    public void Map_Sample_RoomValueWithoutPriceIsUnavailableAndDrawsNoValue()
    {
        // Act
        var room = Section("room_value");

        // Assert
        Assert.Equal(("unavailable", "NO_PRICE"), (room.Status, room.Reason));
        Assert.Equal(["length_of_stay.buckets"], room.Series.Select(series => series.Id));
        Assert.NotNull(room.Formula);
    }

    #endregion

    #region Formula, limitations and insight

    /// <summary>Positive: every analysis of the sample carries its formula and its limitations.</summary>
    [Fact]
    public void Map_Sample_EverySectionHasAFormulaAndLimitations()
    {
        // Act
        var sections = Map(SampleJson()).Content!.Sections;

        // Assert
        Assert.All(sections, section =>
        {
            Assert.False(string.IsNullOrWhiteSpace(section.Formula));
            Assert.NotEmpty(section.Limitations!);
        });
        Assert.StartsWith("Lead time is the supplied lead_time in days.", Section("lead_time").Formula, StringComparison.Ordinal);
    }

    /// <summary>Positive: the hand-made insight is read, with its summary, its suggestion and its two observations.</summary>
    [Fact]
    public void Map_Sample_ReadsTheInsightWithItsObservations()
    {
        // Act
        var insight = Section("cancellations").Insight!;

        // Assert
        Assert.Equal("available", insight.Status);
        Assert.StartsWith("Almost half of the bookings", insight.ExecutiveSummary, StringComparison.Ordinal);
        Assert.Equal([5000], insight.Suggestions.Select(suggestion => suggestion.SampleSize));
        Assert.Equal(2, insight.Observations!.Count);
        Assert.Equal(["splits.by_deposit_type.groups", "splits.by_market_segment.groups"], insight.Observations.Select(observation => observation.Series));
        Assert.Contains("438 of 477", insight.Observations[0].Evidence, StringComparison.Ordinal);
    }

    /// <summary>Positive: the series of an observation is the id of a chart of the same analysis, so the page can put it under that chart.</summary>
    [Fact]
    public void Map_Sample_EveryObservationNamesAChartOfItsAnalysis()
    {
        // Act
        var cancellations = Section("cancellations");

        // Assert
        var charts = cancellations.Series.Select(series => series.Id).ToList();
        Assert.All(cancellations.Insight!.Observations!, observation => Assert.Contains(observation.Series, charts));
    }

    /// <summary>Positive: an analysis without an insight (insights off) has none, and an insight that is unavailable has no observations.</summary>
    [Fact]
    public void Map_InsightsOffOrUnavailable_HaveNoObservations()
    {
        // Act
        var off = Section("lead_time");
        var unavailable = Map("""{"schema_version":"1.1","status":"completed","analyses":{"occupancy":{"status":"available","findings":{},"insight":{"status":"unavailable","reason":"TIMEOUT"}}}}""")
            .Content!.Sections[0].Insight!;

        // Assert
        Assert.Null(off.Insight);
        Assert.Equal(("unavailable", "TIMEOUT", 0), (unavailable.Status, unavailable.Reason, unavailable.Observations!.Count));
    }

    /// <summary>Boundary: an observation without text or without the path of its series cannot be put under a chart and is left out.</summary>
    [Fact]
    public void Map_ObservationsWithoutTextOrSeries_AreLeftOut()
    {
        // Act
        var insight = Map("""
            {"schema_version":"1.1","status":"completed","analyses":{"occupancy":{"status":"available","findings":{},"insight":{"status":"available","observations":[
              {"series":"monthly","text":"Summer is fuller.","evidence":"2018-07: 89.2%"},
              {"series":"monthly","text":""},
              {"text":"No series."},
              {"series":"monthly","text":"No evidence field."}]}}}}
            """).Content!.Sections[0].Insight!;

        // Assert
        Assert.Equal([("Summer is fuller.", "2018-07: 89.2%"), ("No evidence field.", string.Empty)], insight.Observations!.Select(observation => (observation.Text, observation.Evidence)));
    }

    #endregion

    #region Helpers

    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));

    private static MarketAnalysisResultDto Map(string json)
    {
        using var document = JsonDocument.Parse(json);

        return MarketAnalysisResultMapper.Map(document.RootElement) ?? throw new InvalidOperationException("The document is not a result.");
    }

    private static MarketAnalysisSectionDto Section(string id) =>
        Map(SampleJson()).Content!.Sections.Single(section => section.Id == id);

    #endregion
}
