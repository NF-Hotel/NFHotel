using System.Globalization;
using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Domain.Entities;

namespace NFHotel.TestSupport;

/// <summary>Builds market analysis reports with price suggestions of the service, and stores one as the latest report (MIL-055, MIL-071).</summary>
public static class PriceSuggestionTestData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds the content of a report whose occupancy section has a monthly series of 2019, and whose price suggestions are the hand
    /// figures of the tests: a surcharge of 20 % on every size from 2027-06-01 to 2027-08-31 (history 88 and 92 %, threshold 80 %) and a
    /// discount of 15 % on the single room from 2028-01-01 to 2028-02-29 (history 28 and 32 %, threshold 40 %).
    /// </summary>
    /// <param name="withSuggestions">False for a report of the service with price suggestions but no suggestion in them.</param>
    /// <param name="explanation">The executive summary of the insight, or <c>null</c> for no insight.</param>
    /// <returns>The content.</returns>
    public static MarketAnalysisContentDto Content(bool withSuggestions = true, string? explanation = null)
    {
        MarketAnalysisPointDto[] points = Enumerable.Range(1, 12)
            .Select(month => new MarketAnalysisPointDto(string.Create(CultureInfo.InvariantCulture, $"2019-{month:00}"), 60d))
            .ToArray();
        MarketAnalysisInsightDto? insight = explanation is null
            ? null
            : new MarketAnalysisInsightDto("available", null, explanation, []);
        MarketAnalysisPriceSuggestionDto[] suggestions = withSuggestions
            ?
            [
                new MarketAnalysisPriceSuggestionDto(
                    "all",
                    new DateOnly(2027, 6, 1),
                    new DateOnly(2027, 8, 31),
                    "surcharge",
                    20m,
                    "high_history",
                    null,
                    0.8d,
                    [new MarketAnalysisPriceHistoryDto(2025, 0.88d), new MarketAnalysisPriceHistoryDto(2026, 0.92d)],
                    new MarketAnalysisPriceTextDto("The same days were busy in both earlier years.", true)),
                new MarketAnalysisPriceSuggestionDto(
                    "single",
                    new DateOnly(2028, 1, 1),
                    new DateOnly(2028, 2, 29),
                    "discount",
                    15m,
                    "low_history",
                    null,
                    0.4d,
                    [new MarketAnalysisPriceHistoryDto(2025, 0.28d), new MarketAnalysisPriceHistoryDto(2026, 0.32d)],
                    new MarketAnalysisPriceTextDto("Occupancy of these days was low in both earlier years.", false)),
            ]
            : [];

        return new MarketAnalysisContentDto(
            [],
            [new MarketAnalysisSectionDto("occupancy", "Occupancy", "available", null, [new MarketAnalysisSeriesDto("monthly", "Monthly", "%", points, true)], insight)],
            null,
            null,
            new MarketAnalysisPriceSuggestionsDto(
                new DateOnly(2027, 5, 1),
                new DateOnly(2027, 6, 30),
                "formula",
                [],
                suggestions,
                [],
                [new MarketAnalysisPriceTextDto("Offer a package for the quiet weeks.", true)]));
    }

    /// <summary>Stores the content as the report of a run that finished.</summary>
    /// <param name="world">The world of the test.</param>
    /// <param name="content">The content of the report.</param>
    public static void StoreLatestReport(ServiceWorld world, MarketAnalysisContentDto content)
    {
        ArgumentNullException.ThrowIfNull(world);

        world.MarketAnalysisRuns.Add(MarketAnalysisRun.Create(
            Guid.NewGuid(),
            1,
            new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 19, 10, 4, 0, TimeSpan.Zero),
            "ollama",
            "phi3.5:latest",
            JsonSerializer.Serialize(content, JsonOptions)));
    }
}
