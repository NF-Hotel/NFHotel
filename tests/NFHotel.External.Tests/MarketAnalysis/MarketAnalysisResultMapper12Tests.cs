using System.Text.Json;
using System.Text.Json.Nodes;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The mapper of result schema 1.2 of the market analysis service (MIL-026 tasks 24 and 25): room revenue, the improvement suggestions
/// with their series, the forecast and the overall interpretation. The fixture is a trimmed result of a real run.
/// </summary>
public sealed class MarketAnalysisResultMapper12Tests
{
    #region Room revenue

    /// <summary>Positive: the room revenue is read from the findings of the room value analysis, amounts from their decimal text.</summary>
    [Fact]
    public void Map_Version12_ReadsTheRoomRevenue()
    {
        // Act
        MarketAnalysisRoomRevenueDto revenue = Map(Fixture()).Content!.Sections.Single(section => section.Id == "room_value").RoomRevenue!;

        // Assert
        Assert.Equal(50769820.9, revenue.Total);
        Assert.Equal(447.1103, revenue.Mean);
        Assert.Equal((113551, 12), (revenue.RecordsWithPrice, revenue.RecordsWithoutPrice));
        Assert.Contains("before taxes, fees, discounts and extras", revenue.Limitation, StringComparison.Ordinal);
        Assert.Equal(["2018-07", "2018-08", "2018-09", "2018-10"], revenue.Monthly.Select(point => point.Label));
        Assert.Equal(707980.5, revenue.Monthly[0].Value);
    }

    /// <summary>Negative: an unavailable room revenue and a total that is not a number give none, and the analysis is still read.</summary>
    [Theory]
    [InlineData("status", "unavailable")]
    [InlineData("total", "not a number")]
    public void Map_RoomRevenueNotUsable_IsNull(string field, string value)
    {
        // Arrange
        JsonNode document = JsonNode.Parse(Fixture())!;
        document["analyses"]!["room_value"]!["findings"]!["room_revenue"]![field] = value;

        // Act
        MarketAnalysisSectionDto section = Map(document.ToJsonString()).Content!.Sections.Single();

        // Assert
        Assert.Null(section.RoomRevenue);
        Assert.Equal("available", section.Status);
    }

    /// <summary>Boundary: a booking set without a price has no mean, and the revenue is still read.</summary>
    [Fact]
    public void Map_RoomRevenueWithoutMean_HasANullMean()
    {
        // Arrange
        JsonNode document = JsonNode.Parse(Fixture())!;
        document["analyses"]!["room_value"]!["findings"]!["room_revenue"]!["mean"] = null;

        // Act
        MarketAnalysisRoomRevenueDto revenue = Map(document.ToJsonString()).Content!.Sections.Single().RoomRevenue!;

        // Assert
        Assert.Null(revenue.Mean);
    }

    #endregion

    #region Suggestions

    /// <summary>Positive: the improvement suggestions keep their Markdown, their evidence and the series they read.</summary>
    [Fact]
    public void Map_Version12_ReadsTheSuggestionsWithTheirSeries()
    {
        // Act
        MarketAnalysisInsightDto insight = Map(Fixture()).Content!.Sections.Single().Insight!;

        // Assert
        Assert.Equal(2, insight.Suggestions.Count);
        Assert.Equal("Review the **weekday** prices.", insight.Suggestions[0].Suggestion);
        Assert.Equal("room_revenue.monthly", insight.Suggestions[0].Series);
        Assert.Equal(43135, insight.Suggestions[0].SampleSize);
        Assert.Contains("**concentrated**", insight.ExecutiveSummary, StringComparison.Ordinal);
    }

    #endregion

    #region Forecast

    /// <summary>Positive: the forecast is read with its history, its limitations and one measure for each of the three, with the band.</summary>
    [Fact]
    public void Map_Version12_ReadsTheForecast()
    {
        // Act
        MarketAnalysisForecastDto forecast = Map(Fixture()).Content!.Forecast!;

        // Assert
        Assert.Equal((102, 24, false, 3), (forecast.HistoryMonths, forecast.MinimumHistoryMonths, forecast.HistoryTooShort, forecast.HorizonMonths));
        Assert.NotEmpty(forecast.Limitations);
        Assert.NotEmpty(forecast.Formula);
        Assert.Equal(["arrivals", "occupancy", "room_revenue"], forecast.Measures.Select(measure => measure.Id));
        MarketAnalysisForecastMeasureDto arrivals = forecast.Measures[0];
        Assert.Equal("bookings", arrivals.Unit);
        Assert.Equal(3, arrivals.Months.Count);
        Assert.Equal(new MarketAnalysisForecastMonthDto("2027-01", 998.63, 888.61, 1108.64), arrivals.Months[0]);
        Assert.Equal("%", forecast.Measures[1].Unit);
    }

    /// <summary>Boundary: a band end that is null stays null, and a history that is too short is flagged.</summary>
    [Fact]
    public void Map_ForecastWithoutBandAndTooShortHistory_KeepsBoth()
    {
        // Arrange
        JsonNode document = JsonNode.Parse(Fixture())!;
        document["forecast"]!["history_too_short"] = true;
        document["forecast"]!["measures"]!["arrivals"]!["months"]![0]!["low"] = null;

        // Act
        MarketAnalysisForecastDto forecast = Map(document.ToJsonString()).Content!.Forecast!;

        // Assert
        Assert.True(forecast.HistoryTooShort);
        Assert.Null(forecast.Measures[0].Months[0].Low);
        Assert.NotNull(forecast.Measures[0].Months[0].High);
    }

    /// <summary>Boundary: a measure without months is left out, and a forecast without measures is none.</summary>
    [Fact]
    public void Map_ForecastWithoutMonthsOrMeasures_LeavesThemOut()
    {
        // Arrange
        JsonNode empty = JsonNode.Parse(Fixture())!;
        empty["forecast"]!["measures"]!["occupancy"]!["months"] = new JsonArray();
        JsonNode none = JsonNode.Parse(Fixture())!;
        none["forecast"]!.AsObject().Remove("measures");

        // Act
        MarketAnalysisForecastDto forecast = Map(empty.ToJsonString()).Content!.Forecast!;
        MarketAnalysisResultDto result = Map(none.ToJsonString());

        // Assert
        Assert.Equal(["arrivals", "room_revenue"], forecast.Measures.Select(measure => measure.Id));
        Assert.Null(result.Content!.Forecast);
    }

    #endregion

    #region Overall

    /// <summary>Positive: the overall interpretation of an AI is read with its text, evidence and recommendations.</summary>
    [Fact]
    public void Map_Version12_ReadsTheOverallInterpretation()
    {
        // Act
        MarketAnalysisOverallDto overall = Map(Fixture()).Content!.Overall!;

        // Assert
        Assert.True(overall.IsAi);
        Assert.Equal("AI-generated", overall.Label);
        Assert.Contains("**same seasonal pattern**", overall.Text, StringComparison.Ordinal);
        Assert.Equal("50769820.9, 447.1103, 113551", overall.Evidence);
        Assert.Equal(2, overall.Recommendations.Count);
        Assert.Equal(113551, overall.Recommendations[0].SampleSize);
    }

    /// <summary>Positive: a text that the service calculated from the figures has the source figures and no recommendations.</summary>
    [Fact]
    public void Map_OverallFromFigures_IsNotAi()
    {
        // Arrange
        JsonNode document = JsonNode.Parse(Fixture())!;
        JsonObject overall = document["insights"]!["overall"]!.AsObject();
        overall["source"] = "figures";
        overall["label"] = "Calculated";
        overall.Remove("recommendations");
        overall.Remove("evidence");
        overall.Remove("generated_at");

        // Act
        MarketAnalysisOverallDto mapped = Map(document.ToJsonString()).Content!.Overall!;

        // Assert
        Assert.False(mapped.IsAi);
        Assert.Empty(mapped.Recommendations);
        Assert.Null(mapped.Evidence);
    }

    /// <summary>Negative: a 1.1 result, and an overall interpretation without text or without the status available, have none.</summary>
    [Theory]
    [InlineData("1.1")]
    [InlineData("text")]
    [InlineData("status")]
    public void Map_NoUsableOverall_IsNull(string scenario)
    {
        // Arrange
        JsonNode document = JsonNode.Parse(Fixture())!;
        JsonObject insights = document["insights"]!.AsObject();
        switch (scenario)
        {
            case "1.1":
                insights.Remove("overall");
                break;
            case "text":
                insights["overall"]!["text"] = string.Empty;
                break;
            default:
                insights["overall"]!["status"] = "unavailable";
                break;
        }

        // Act
        MarketAnalysisResultDto result = Map(document.ToJsonString());

        // Assert
        Assert.Null(result.Content!.Overall);
        Assert.Equal(1, result.SchemaVersion);
    }

    #endregion

    #region Version 1.1

    /// <summary>Positive: the real 1.1 sample maps as before, with none of the new fields.</summary>
    [Fact]
    public void Map_Version11Sample_HasNoNewFields()
    {
        // Arrange
        string json = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));

        // Act
        MarketAnalysisContentDto content = Map(json).Content!;

        // Assert
        Assert.Null(content.Overall);
        Assert.Null(content.Forecast);
        Assert.All(content.Sections, section => Assert.Null(section.RoomRevenue));
        Assert.Equal(9, content.Sections.Count);
    }

    #endregion

    private static string Fixture() =>
        File.ReadAllText(Path.Join(AppContext.BaseDirectory, "TestData", "market-analysis-result-1.2.json"));

    private static MarketAnalysisResultDto Map(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return MarketAnalysisResultMapper.Map(document.RootElement) ?? throw new InvalidOperationException("Not a result.");
    }
}
