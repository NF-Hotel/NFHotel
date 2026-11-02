using System.Globalization;
using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Domain.Entities;

namespace NFHotel.TestSupport;

/// <summary>Builds market analysis reports with an occupancy by month, and stores one as the latest report (MIL-055).</summary>
public static class PriceSuggestionTestData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The occupancy of twelve calendar months of 2019 for the hand figures of the tests: June, July and August at 90 %
    /// (high season at 80 %), January and February at 30 % (low season at 40 %) and the rest at 60 %.
    /// </summary>
    public static readonly double[] SeasonalYear = [30, 30, 60, 60, 60, 90, 90, 90, 60, 60, 60, 60];

    /// <summary>Builds the content of a report whose occupancy section has a monthly series of 2019.</summary>
    /// <param name="percents">The twelve values, January first.</param>
    /// <param name="unit">The unit of the series.</param>
    /// <param name="explanation">The executive summary of the insight, or <c>null</c> for no insight.</param>
    /// <returns>The content.</returns>
    public static MarketAnalysisContentDto Content(double[]? percents = null, string unit = "%", string? explanation = null)
    {
        var values = percents ?? SeasonalYear;
        var points = values
            .Select((value, index) => new MarketAnalysisPointDto(string.Create(CultureInfo.InvariantCulture, $"2019-{index + 1:00}"), value))
            .ToArray();
        var insight = explanation is null
            ? null
            : new MarketAnalysisInsightDto("available", null, explanation, []);

        return new MarketAnalysisContentDto(
            [],
            [new MarketAnalysisSectionDto("occupancy", "Occupancy", "available", null, [new MarketAnalysisSeriesDto("monthly", "Monthly", unit, points, true)], insight)]);
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
