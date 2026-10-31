using System.Globalization;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>One calendar year of a month grid: a value or <c>null</c> for each of the twelve months.</summary>
/// <param name="Year">The year.</param>
/// <param name="Values">Twelve values, January first; <c>null</c> where the series has no point for that month.</param>
/// <param name="SmallSamples">Twelve flags: true where the value rests on too few bookings.</param>
public sealed record MonthRow(int Year, IReadOnlyList<double?> Values, IReadOnlyList<bool> SmallSamples);

/// <summary>Reads a series of months (labels like <c>2018-07</c>) as months against years, for the heat map.</summary>
public static class MonthGrid
{
    /// <summary>The number of months in a year.</summary>
    public const int MonthsPerYear = 12;

    private const int LabelLength = 7;

    /// <summary>The short names of the months, January first.</summary>
    public static IReadOnlyList<string> MonthNames { get; } = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    /// <summary>Reads a label such as <c>2018-07</c>.</summary>
    /// <param name="label">The label.</param>
    /// <param name="year">The year, when the label is a month.</param>
    /// <param name="month">The month from 1 to 12, when the label is a month.</param>
    /// <returns>True when the label is a month.</returns>
    public static bool TryParse(string label, out int year, out int month)
    {
        year = 0;
        month = 0;

        return label is { Length: LabelLength } && label[4] == '-'
            && int.TryParse(label.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out year)
            && int.TryParse(label.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out month)
            && month is >= 1 and <= MonthsPerYear;
    }

    /// <summary>Tells whether every label of the points is a month.</summary>
    /// <param name="points">The points.</param>
    /// <returns>True when there is at least one point and all labels are months.</returns>
    public static bool AreMonths(IReadOnlyList<MarketAnalysisPointDto> points) =>
        points.Count > 0 && points.All(point => TryParse(point.Label, out _, out _));

    /// <summary>Counts the calendar years that the months touch.</summary>
    /// <param name="points">Points whose labels are months.</param>
    /// <returns>The number of distinct years.</returns>
    public static int YearCount(IReadOnlyList<MarketAnalysisPointDto> points) =>
        points.Select(point => TryParse(point.Label, out int year, out _) ? year : 0).Where(year => year > 0).Distinct().Count();

    /// <summary>Lays the points out as one row for each year, oldest first.</summary>
    /// <param name="points">Points whose labels are months; points with another label are left out.</param>
    /// <returns>The rows; empty when no label is a month.</returns>
    public static IReadOnlyList<MonthRow> Rows(IReadOnlyList<MarketAnalysisPointDto> points)
    {
        Dictionary<int, (double?[] Values, bool[] Small)> cells = new Dictionary<int, (double?[] Values, bool[] Small)>();
        foreach (MarketAnalysisPointDto point in points)
        {
            if (!TryParse(point.Label, out int year, out int month))
            {
                continue;
            }

            if (!cells.TryGetValue(year, out (double?[] Values, bool[] Small) row))
            {
                row = (new double?[MonthsPerYear], new bool[MonthsPerYear]);
                cells[year] = row;
            }

            row.Values[month - 1] = point.Value;
            row.Small[month - 1] = point.SmallSample;
        }

        return [.. cells.OrderBy(entry => entry.Key).Select(entry => new MonthRow(entry.Key, entry.Value.Values, entry.Value.Small))];
    }
}
