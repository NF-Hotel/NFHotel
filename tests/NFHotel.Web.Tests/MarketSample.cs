using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.Web.Tests;

/// <summary>The real result of the market analysis service (5,000 bookings, result schema 1.1), read through the mapper, for the tests of the charts.</summary>
internal static class MarketSample
{
    /// <summary>Reads the sample the way the API stores it.</summary>
    /// <returns>The nine analyses of the sample.</returns>
    public static IReadOnlyList<MarketAnalysisSectionDto> Sections()
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));
        using JsonDocument document = JsonDocument.Parse(json);

        return MarketAnalysisResultMapper.Map(document.RootElement)?.Content?.Sections
            ?? throw new InvalidOperationException("The sample is not a result.");
    }

    /// <summary>Finds one analysis of the sample.</summary>
    /// <param name="id">The id of the analysis, for example <c>occupancy</c>.</param>
    /// <returns>The analysis.</returns>
    public static MarketAnalysisSectionDto Section(string id) => Sections().Single(section => section.Id == id);
}
