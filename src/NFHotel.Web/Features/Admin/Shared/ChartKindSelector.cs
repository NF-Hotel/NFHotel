using System.Text.RegularExpressions;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>
/// The one rule that gives each series of the market analysis report the kind of chart that fits its shape (MIL-026,
/// criterion 18). The rule reads what the series carries (its points, the order of its labels, whether its figures are parts of
/// one whole) and never the name of an analysis. The checks run in this order, the first that fits wins:
/// <list type="number">
/// <item>One point is a tile.</item>
/// <item>A value below zero keeps the grouped bars, because the other kinds draw from zero upwards.</item>
/// <item>Months are a line; months that span <see cref="HeatmapMinYears"/> years or more with at least <see cref="HeatmapMinPoints"/>
/// points are a heat map of months against years; two months are two columns.</item>
/// <item>A series of ISO weeks (<c>2018-W26</c>) with at least <see cref="MinLinePoints"/> points is a line as well.</item>
/// <item>Ordered bands or numbers are columns in their order, even when they add up to 100 % (the lead time bands).</item>
/// <item>Two to <see cref="MaxDonutParts"/> parts of one whole are a donut. Separate rates, such as the occupancy on holidays and
/// on other days, are never a donut.</item>
/// <item>More than <see cref="MaxDonutParts"/> categories are ranked bars, largest first.</item>
/// <item>Two to <see cref="MaxDonutParts"/> separate rates are columns side by side.</item>
/// </list>
/// </summary>
public static partial class ChartKindSelector
{
    /// <summary>The most parts a donut shows; more are ranked bars.</summary>
    public const int MaxDonutParts = 4;

    /// <summary>The fewest points a line needs; two points are one segment, which is a comparison and not a course.</summary>
    public const int MinLinePoints = 3;

    /// <summary>A series of months needs at least this many points to be laid out as months against years.</summary>
    public const int HeatmapMinPoints = 24;

    /// <summary>A series of months must span at least this many calendar years to be laid out as months against years.</summary>
    public const int HeatmapMinYears = 3;

    /// <summary>Chooses the kind of chart for a series.</summary>
    /// <param name="series">The series.</param>
    /// <returns>The kind of chart; <see cref="ChartKind.GroupedBars"/> when the series fits no other kind.</returns>
    public static ChartKind Select(MarketAnalysisSeriesDto series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var points = series.Points;
        if (points.Count == 1)
        {
            return ChartKind.Tile;
        }

        if (points.Count == 0 || points.Any(point => point.Value < 0 || double.IsNaN(point.Value)))
        {
            return ChartKind.GroupedBars;
        }

        if (MonthGrid.AreMonths(points))
        {
            return Months(points);
        }

        if (series.IsOrdered)
        {
            return points.Count >= MinLinePoints && points.All(point => WeekLabel().IsMatch(point.Label)) ? ChartKind.Line : ChartKind.Columns;
        }

        if (series.IsPartsOfWhole && points.Count <= MaxDonutParts)
        {
            return ChartKind.Donut;
        }

        return points.Count > MaxDonutParts ? ChartKind.RankedBars : ChartKind.Columns;
    }

    private static ChartKind Months(IReadOnlyList<MarketAnalysisPointDto> points)
    {
        if (points.Count >= HeatmapMinPoints && MonthGrid.YearCount(points) >= HeatmapMinYears)
        {
            return ChartKind.Heatmap;
        }

        return points.Count >= MinLinePoints ? ChartKind.Line : ChartKind.Columns;
    }

    [GeneratedRegex(@"^\d{4}-W\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex WeekLabel();
}
