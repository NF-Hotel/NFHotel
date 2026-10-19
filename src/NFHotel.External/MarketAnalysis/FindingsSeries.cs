using System.Globalization;
using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.External.MarketAnalysis;

/// <summary>
/// Finds the charts in the <c>findings</c> of an analysis of the markets API. The findings are free-form JSON, but every
/// group figure in them is an object with <c>group</c>, <c>numerator</c> and <c>denominator</c> (ADR-0002 of that
/// repository), either directly in a list or wrapped as <c>{"figure": {...}}</c> next to other fields of the group. A list of
/// such items becomes one series: a share in percent when it has a numerator and a denominator, otherwise a <c>value</c> or
/// a <c>count</c>. Lists of another shape are left out of the charts. The series are ordered by depth in the findings and
/// then by the number of points, so the overview comes first, and a series of more than <see cref="MaxChartPoints"/> points
/// (the weekly ones) is too dense for a bar chart and is left out.
/// </summary>
public static class FindingsSeries
{
    /// <summary>The most points of one series that are drawn.</summary>
    public const int MaxPoints = 30;

    /// <summary>A series with more points than this is not drawn at all.</summary>
    public const int MaxChartPoints = 60;

    /// <summary>The most series of one analysis.</summary>
    public const int MaxSeries = 8;

    private const string GroupName = "group";
    private const string FigureName = "figure";

    private static readonly HashSet<string> GenericNames = new(StringComparer.Ordinal) { "groups", "splits", "periods", "distributions", "buckets" };
    private const double PercentFactor = 100d;

    /// <summary>Reads the series out of the findings of one analysis.</summary>
    /// <param name="findings">The <c>findings</c> object.</param>
    /// <returns>The series in the order they are found; empty when the findings hold no group figures.</returns>
    public static IReadOnlyList<MarketAnalysisSeriesDto> Extract(JsonElement findings)
    {
        var found = new List<(int Depth, MarketAnalysisSeriesDto Series)>();
        Walk(findings, [], found);

        return [.. found
            .Where(entry => entry.Series.Points.Count <= MaxChartPoints)
            .OrderBy(entry => entry.Depth)
            .ThenBy(entry => entry.Series.Points.Count)
            .Select(entry => entry.Series.Points.Count > MaxPoints ? entry.Series with { Points = [.. entry.Series.Points.Take(MaxPoints)] } : entry.Series)
            .Take(MaxSeries)];
    }

    private static void Walk(JsonElement node, List<string> path, List<(int Depth, MarketAnalysisSeriesDto Series)> series)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in node.EnumerateObject())
                {
                    path.Add(property.Name);
                    Walk(property.Value, path, series);
                    path.RemoveAt(path.Count - 1);
                }

                break;

            case JsonValueKind.Array:
                if (ToSeries(node, path) is { } found)
                {
                    series.Add((path.Count, found));
                    break;
                }

                var index = 0;
                foreach (var item in node.EnumerateArray())
                {
                    path.Add(index.ToString(CultureInfo.InvariantCulture));
                    Walk(item, path, series);
                    path.RemoveAt(path.Count - 1);
                    index++;
                }

                break;

            default:
                break;
        }
    }

    private static MarketAnalysisSeriesDto? ToSeries(JsonElement array, List<string> path)
    {
        var points = new List<MarketAnalysisPointDto>();
        var unit = string.Empty;

        foreach (var item in array.EnumerateArray())
        {
            var figureItem = item.ValueKind == JsonValueKind.Object && item.TryGetProperty(FigureName, out var wrapped) && wrapped.ValueKind == JsonValueKind.Object
                ? wrapped
                : item;
            if (figureItem.ValueKind != JsonValueKind.Object || !figureItem.TryGetProperty(GroupName, out var group))
            {
                return null;
            }

            if (Figure(figureItem) is not { } figure)
            {
                continue;
            }

            unit = unit.Length == 0 ? figure.Unit : unit;
            points.Add(new MarketAnalysisPointDto(Label(group), figure.Value));
        }

        if (points.Count == 0)
        {
            return null;
        }

        var id = string.Join('.', path);
        return new MarketAnalysisSeriesDto(id, Title(path), unit, points);
    }

    private static (double Value, string Unit)? Figure(JsonElement item)
    {
        if (Number(item, "numerator") is { } numerator && Number(item, "denominator") is { } denominator)
        {
            return denominator > 0 ? (Math.Round(numerator / denominator * PercentFactor, 1), "%") : null;
        }

        if (Number(item, "value") is { } value)
        {
            return (value, string.Empty);
        }

        return Number(item, "count") is { } count ? (count, "bookings") : null;
    }

    // Money is a decimal string in the markets API (ADR-0002), so a number can be a string.
    private static double? Number(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetDouble(),
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string Label(JsonElement group) =>
        group.ValueKind == JsonValueKind.String ? group.GetString() ?? string.Empty : group.ToString();

    private static string Title(List<string> path)
    {
        var words = path
            .Where(part => !GenericNames.Contains(part) && !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            .TakeLast(2);
        var text = string.Join(" - ", words.Select(part => part.Replace('_', ' ')));

        return text.Length == 0 ? "Groups" : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
