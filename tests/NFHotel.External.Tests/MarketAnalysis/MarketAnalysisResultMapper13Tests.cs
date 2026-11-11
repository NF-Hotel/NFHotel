using System.Text.Json;
using System.Text.Json.Nodes;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The mapper of result schema 1.3 of the market analysis service (MIL-071 task 2): the price suggestions with their history, the weeks
/// without enough history and the ideas. The sample is a copy of the sample of the service.
/// </summary>
public sealed class MarketAnalysisResultMapper13Tests
{
    /// <summary>Positive: the suggestions are read with their dates, change, percentage, reason, holiday, history and text.</summary>
    [Fact]
    public void Map_Version13_ReadsTheSuggestions()
    {
        // Act
        MarketAnalysisPriceSuggestionsDto prices = Map(WithSample()).Content!.PriceSuggestions!;

        // Assert
        Assert.Equal((new DateOnly(2026, 11, 11), new DateOnly(2027, 1, 10)), (prices.AsOf, prices.HorizonEnd));
        Assert.Equal(2, prices.Suggestions.Count);
        MarketAnalysisPriceSuggestionDto christmas = prices.Suggestions[0];
        Assert.Equal(("all", new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "surcharge", 20m), (christmas.RoomSize, christmas.FirstDate, christmas.LastDate, christmas.Change, christmas.Percent));
        Assert.Equal(("holiday", "Christmas", 0.8d), (christmas.Reason, christmas.Holiday, christmas.Threshold));
        Assert.Equal([(2024, 0.85d), (2025, 0.88d)], christmas.History.Select(year => (year.Year, year.Occupancy)));
        Assert.True(christmas.Explanation.AiWritten);
        Assert.Null(prices.Suggestions[1].Holiday);
        Assert.False(prices.Suggestions[1].Explanation.AiWritten);
    }

    /// <summary>Positive: the weeks without enough history and the ideas are read, the ideas marked as written by the AI.</summary>
    [Fact]
    public void Map_Version13_ReadsTheGapsAndTheIdeas()
    {
        // Act
        MarketAnalysisPriceSuggestionsDto prices = Map(WithSample()).Content!.PriceSuggestions!;

        // Assert
        MarketAnalysisPriceGapDto gap = Assert.Single(prices.NotEnoughHistory);
        Assert.Equal((new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 29), 1), (gap.FirstDate, gap.LastDate, gap.YearsFound));
        MarketAnalysisPriceTextDto idea = Assert.Single(prices.Ideas);
        Assert.True(idea.AiWritten);
    }

    /// <summary>Negative: a result of schema 1.2 has no price suggestions, and the rest is still read.</summary>
    [Fact]
    public void Map_Version12_HasNoSuggestions()
    {
        // Act
        MarketAnalysisResultDto result = Map(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "TestData", "market-analysis-result-1.2.json")));

        // Assert
        Assert.Null(result.Content!.PriceSuggestions);
        Assert.NotEmpty(result.Content.Sections);
    }

    /// <summary>Negative: a suggestion without its dates or without its text cannot be shown and is left out; the others stay.</summary>
    [Theory]
    [InlineData("first_date")]
    [InlineData("percent")]
    [InlineData("explanation")]
    public void Map_SuggestionIncomplete_IsLeftOut(string missing)
    {
        // Arrange
        JsonNode document = JsonNode.Parse(WithSample())!;
        ((JsonObject)document["price_suggestions"]!["suggestions"]![0]!).Remove(missing);

        // Act
        MarketAnalysisPriceSuggestionsDto prices = Map(document.ToJsonString()).Content!.PriceSuggestions!;

        // Assert
        Assert.Equal("single", Assert.Single(prices.Suggestions).RoomSize);
    }

    /// <summary>Boundary: price suggestions without dates of the horizon are not read as a set.</summary>
    [Fact]
    public void Map_NoHorizon_GivesNone()
    {
        // Arrange
        JsonNode document = JsonNode.Parse(WithSample())!;
        ((JsonObject)document["price_suggestions"]!).Remove("horizon_end");

        // Act & Assert
        Assert.Null(Map(document.ToJsonString()).Content!.PriceSuggestions);
    }

    private static string WithSample()
    {
        JsonNode document = JsonNode.Parse(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "TestData", "market-analysis-result-1.2.json")))!;
        document["price_suggestions"] = JsonNode.Parse(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "TestData", "price-suggestions-sample.json")));

        return document.ToJsonString();
    }

    private static MarketAnalysisResultDto Map(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return MarketAnalysisResultMapper.Map(document.RootElement) ?? throw new InvalidOperationException("Not a result.");
    }
}
