using System.Globalization;
using System.Text.RegularExpressions;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>
/// Reads the occupancy by month, and the text of its AI insight, from a market analysis report (MIL-051). Pure.
/// </summary>
/// <remarks>
/// Where the figures are: the section with the id <see cref="SectionId"/> (the <c>occupancy</c> analysis of the markets API),
/// available, and in it the series of the monthly figures. The real service delivers it as the series <c>monthly</c> with the
/// unit <c>%</c> and one point per month, labelled <c>yyyy-MM</c> (for example <c>2019-07</c>), whose value is already a
/// percent (an occupancy of 89.2 % is 89.2, and it can pass 100 because nothing is capped). A series without the unit
/// <c>%</c> whose values are all between 0 and 1 is read as fractions and multiplied by 100. A point whose label is not a
/// month, or whose value is not a number or is negative, is left out and never throws.
/// </remarks>
public static partial class OccupancyObservationReader
{
    /// <summary>The id of the occupancy analysis in the report.</summary>
    public const string SectionId = "occupancy";

    private const string MonthlySeriesId = "monthly";
    private const string AvailableStatus = "available";
    private const decimal Hundred = 100m;
    private const double LargestValue = 1_000_000d;

    /// <summary>Reads the monthly occupancy of the report.</summary>
    /// <param name="content">The content of the report.</param>
    /// <returns>One observation per readable month point; empty when the section or its monthly series is missing.</returns>
    public static IReadOnlyList<OccupancyObservation> Read(MarketAnalysisContentDto? content)
    {
        var section = SectionOf(content);
        if (section is null || !string.Equals(section.Status, AvailableStatus, StringComparison.Ordinal))
        {
            return [];
        }

        var candidates = (section.Series ?? [])
            .Select(series => (Series: series, Points: ReadPoints(series.Points)))
            .Where(item => item.Points.Count > 0)
            .ToArray();
        if (candidates.Length == 0)
        {
            return [];
        }

        // The monthly series by its id; another series of months is the fallback.
        var chosen = candidates.FirstOrDefault(item => string.Equals(item.Series.Id, MonthlySeriesId, StringComparison.Ordinal));
        if (chosen.Series is null)
        {
            chosen = candidates[0];
        }

        var isPercent = chosen.Series.Unit?.Contains('%', StringComparison.Ordinal) == true;
        var factor = !isPercent && chosen.Points.All(point => point.Value <= 1d) ? Hundred : 1m;

        return
        [
            .. chosen.Points.Select(point => new OccupancyObservation(point.Year, point.Month, ToDecimal(point.Value) * factor)),
        ];
    }

    /// <summary>Reads the executive summary of the AI insight on the occupancy.</summary>
    /// <param name="content">The content of the report.</param>
    /// <returns>The text, or <c>null</c> when the insight is missing, not available, or has no text.</returns>
    public static string? ReadExplanation(MarketAnalysisContentDto? content)
    {
        var insight = SectionOf(content)?.Insight;
        if (insight is null || !string.Equals(insight.Status, AvailableStatus, StringComparison.Ordinal))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(insight.ExecutiveSummary) ? null : insight.ExecutiveSummary.Trim();
    }

    private static MarketAnalysisSectionDto? SectionOf(MarketAnalysisContentDto? content) =>
        content?.Sections?.FirstOrDefault(section => string.Equals(section.Id, SectionId, StringComparison.Ordinal));

    private static List<(int Year, int Month, double Value)> ReadPoints(IReadOnlyList<MarketAnalysisPointDto>? points)
    {
        var read = new List<(int Year, int Month, double Value)>();
        foreach (var point in points ?? [])
        {
            var match = point.Label is null ? null : MonthLabel().Match(point.Label.Trim());
            if (match is null || !match.Success || !double.IsFinite(point.Value) || point.Value < 0d)
            {
                continue;
            }

            read.Add((
                int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture),
                point.Value));
        }

        return read;
    }

    // A huge double cannot become a decimal and is no occupancy, so it is capped instead of throwing.
    private static decimal ToDecimal(double value) => (decimal)Math.Min(value, LargestValue);

    [GeneratedRegex(@"^(?<year>\d{4})-(?<month>0[1-9]|1[0-2])(-\d{2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex MonthLabel();
}
