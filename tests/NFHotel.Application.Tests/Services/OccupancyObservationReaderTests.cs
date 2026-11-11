using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Reading the explanation from a market analysis report (MIL-055); the figures come from the service (MIL-071).</summary>
public sealed class OccupancyObservationReaderTests
{
    private static MarketAnalysisContentDto Section(MarketAnalysisSectionDto section) => new([], [section]);

    /// <summary>Positive: the executive summary of an available insight on the occupancy is returned trimmed.</summary>
    [Fact]
    public void ReadExplanation_AvailableInsight_ReturnsTheSummary()
    {
        // Arrange
        MarketAnalysisContentDto content = PriceSuggestionTestData.Content(explanation: "  Summer is full.  ");

        // Act
        string? text = OccupancyObservationReader.ReadExplanation(content);

        // Assert
        Assert.Equal("Summer is full.", text);
    }

    /// <summary>Negative: no insight, an unavailable one, or an empty summary gives null.</summary>
    [Fact]
    public void ReadExplanation_NoUsableInsight_ReturnsNull()
    {
        // Arrange
        MarketAnalysisSectionDto unavailable = new(
            "occupancy",
            "Occupancy",
            "available",
            null,
            [],
            new MarketAnalysisInsightDto("unavailable", "TIMEOUT", "text", []));
        MarketAnalysisSectionDto empty = unavailable with { Insight = new MarketAnalysisInsightDto("available", null, "  ", []) };

        // Act and assert
        Assert.Null(OccupancyObservationReader.ReadExplanation(null));
        Assert.Null(OccupancyObservationReader.ReadExplanation(PriceSuggestionTestData.Content()));
        Assert.Null(OccupancyObservationReader.ReadExplanation(Section(unavailable)));
        Assert.Null(OccupancyObservationReader.ReadExplanation(Section(empty)));
    }
}
