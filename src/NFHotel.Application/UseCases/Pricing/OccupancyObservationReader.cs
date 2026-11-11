using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Reads the text of the AI insight on the occupancy from a market analysis report (MIL-055). It reads no figure: the price
/// suggestions come from the market analysis service (MIL-071).
/// </summary>
public static class OccupancyObservationReader
{
    /// <summary>The id of the occupancy section of the markets API.</summary>
    public const string SectionId = "occupancy";

    private const string AvailableStatus = "available";

    /// <summary>Reads the executive summary of the AI insight of the occupancy section.</summary>
    /// <param name="content">The content of the report.</param>
    /// <returns>The summary, or <c>null</c> when the insight is missing, not available or empty.</returns>
    public static string? ReadExplanation(MarketAnalysisContentDto? content)
    {
        MarketAnalysisInsightDto? insight = content?.Sections?
            .FirstOrDefault(section => string.Equals(section.Id, SectionId, StringComparison.Ordinal))?.Insight;
        if (insight is null || !string.Equals(insight.Status, AvailableStatus, StringComparison.Ordinal))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(insight.ExecutiveSummary) ? null : insight.ExecutiveSummary.Trim();
    }
}
