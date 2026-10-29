using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.External.MarketAnalysis;

/// <summary>
/// Finds the charts in the <c>findings</c> of an analysis of the market analysis service. The findings are free-form JSON, but every
/// group figure in them is an object with <c>group</c>, <c>numerator</c> and <c>denominator</c> (ADR-0002 of that
/// repository), either directly in a list or wrapped as <c>{"figure": {...}}</c> next to other fields of the group. A list of
/// such items becomes one series: a share in percent when it has a numerator and a denominator, otherwise a <c>value</c> or
/// a <c>count</c>. Lists of another shape are left out of the charts. The series are ordered by depth in the findings and
/// then by the number of points, so the overview comes first. How a long series is cut depends on its group labels. A series
/// whose labels are all months (<c>yyyy-MM</c>) is a time series, ordered oldest first by the service: when it has more than
/// <see cref="MaxPoints"/> points it keeps the latest <see cref="MaxPoints"/> however long it is, and its title says so
/// (for example "(latest 30 of 103 months)"), so the chart does not pretend to show the whole history; its id is unchanged.
/// A weekly series (labels like <c>2019-W26</c>) of more than <see cref="MaxChartPoints"/> points is too dense for a bar chart
/// and is left out. Every other series (a category such as the country, largest first) of more than
/// <see cref="MaxChartPoints"/> points is left out, and one of 31 to 60 points is cut to its first <see cref="MaxPoints"/>. Two group figures that sit directly in the findings (the
/// occupancy on holidays and on other days) are one series, and a <c>matrix</c> with its <c>fields</c> (the correlations) is
/// read by <see cref="ExtractMatrices"/> as a matrix and not as a series.
/// </summary>
public static partial class FindingsSeries
{
    /// <summary>The most points of one series that are drawn.</summary>
    public const int MaxPoints = 30;

    /// <summary>A series with more points than this is not drawn at all.</summary>
    public const int MaxChartPoints = 60;

    /// <summary>The most series of one analysis.</summary>
    public const int MaxSeries = 8;

    private const string GroupName = "group";
    private const string FigureName = "figure";
    private const string StatusName = "status";
    private const string UnavailableStatus = "unavailable";
    private const string FieldsName = "fields";
    private const string MatrixName = "matrix";
    private const string OverallName = "overall";
    private const double WholeTolerance = 1e-9;

    // Lists the service names after what they hold: bands and buckets are ordered however their labels read.
    private static readonly HashSet<string> OrderedNames = new(StringComparer.Ordinal) { "bands", "buckets" };

    private static readonly HashSet<string> GenericNames = new(StringComparer.Ordinal) { "groups", "splits", "periods", "distributions", "buckets" };
    private const double PercentFactor = 100d;

    /// <summary>Reads the series out of the findings of one analysis.</summary>
    /// <param name="findings">The <c>findings</c> object.</param>
    /// <returns>The series in the order they are found; empty when the findings hold no group figures.</returns>
    public static IReadOnlyList<MarketAnalysisSeriesDto> Extract(JsonElement findings)
    {
        var found = new List<(int Depth, MarketAnalysisSeriesDto Series)>();
        Walk(findings, [], found);
        if (RootFigures(findings) is { } compared)
        {
            found.Add((0, compared));
        }

        return [.. found
            .Where(entry => IsMonthly(entry.Series) || entry.Series.Points.Count <= MaxChartPoints)
            .OrderBy(entry => entry.Depth)
            .ThenBy(entry => entry.Series.Points.Count)
            .Select(entry => Shorten(entry.Series))
            .Take(MaxSeries)];
    }

    private static bool IsMonthly(MarketAnalysisSeriesDto series) =>
        series.Points.Count > 0 && series.Points.All(point => MonthLabel().IsMatch(point.Label));

    // A long time series keeps its latest points and says so in the title; any other long series keeps its first points.
    private static MarketAnalysisSeriesDto Shorten(MarketAnalysisSeriesDto series)
    {
        var count = series.Points.Count;
        if (count <= MaxPoints)
        {
            return series;
        }

        // A cut series no longer adds up to its whole, whichever end is kept.
        return IsMonthly(series)
            ? series with { Title = $"{series.Title} (latest {MaxPoints} of {count} months)", Points = [.. series.Points.TakeLast(MaxPoints)], IsPartsOfWhole = false }
            : series with { Points = [.. series.Points.Take(MaxPoints)], IsPartsOfWhole = false };
    }

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.CultureInvariant)]
    private static partial Regex MonthLabel();

    // A number, a range of numbers (the lead time bands 0-7 and 8-30), an open end (91+, 7+) or an ISO week (2018-W26) has a natural order.
    [GeneratedRegex(@"^(\d+(-\d+|\+)?|\d{4}-W\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex OrderedLabel();

    /// <summary>
    /// Reads the matrix of an analysis that has one, such as the correlations: a <c>matrix</c> of rows of numbers (a number or
    /// <c>null</c>) next to the <c>fields</c> that name its rows and columns. The shape decides, not the name of the analysis.
    /// </summary>
    /// <param name="findings">The <c>findings</c> object.</param>
    /// <returns>The matrices; empty when the findings hold none or the matrix is not square.</returns>
    public static IReadOnlyList<MarketAnalysisMatrixDto> ExtractMatrices(JsonElement findings)
    {
        if (findings.ValueKind != JsonValueKind.Object
            || !findings.TryGetProperty(FieldsName, out var fields) || fields.ValueKind != JsonValueKind.Array
            || !findings.TryGetProperty(MatrixName, out var matrix) || matrix.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var names = fields.EnumerateArray().Select(field => field.ValueKind == JsonValueKind.String ? field.GetString() ?? string.Empty : field.ToString()).ToList();
        var rows = new List<IReadOnlyList<double?>>();
        foreach (var row in matrix.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != names.Count)
            {
                return [];
            }

            rows.Add([.. row.EnumerateArray().Select(cell => cell.ValueKind == JsonValueKind.Number ? cell.GetDouble() : default(double?))]);
        }

        if (names.Count == 0 || rows.Count != names.Count)
        {
            return [];
        }

        var method = findings.TryGetProperty("method", out var found) && found.ValueKind == JsonValueKind.String ? found.GetString() ?? string.Empty : string.Empty;

        return [new MarketAnalysisMatrixDto(MatrixName, Humanize(MatrixName), method, names, rows)];
    }

    // Two or more group figures that sit directly in the findings (the occupancy on holidays and on other days) are compared
    // in one chart. A single one (the overall figure) is a tile for the page to draw, not a series. The rule applies to the
    // root only: inside a group, a figure next to its comparison figure is not a pair to draw together.
    private static MarketAnalysisSeriesDto? RootFigures(JsonElement findings)
    {
        if (findings.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var names = new List<string>();
        var points = new List<MarketAnalysisPointDto>();
        foreach (var property in findings.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object
                && property.Value.TryGetProperty(GroupName, out var group)
                && group.ValueKind == JsonValueKind.String
                && Figure(property.Value) is { } figure)
            {
                names.Add(property.Name);
                points.Add(new MarketAnalysisPointDto(Humanize(property.Name), figure.Value, SmallSample(property.Value)));
            }
        }

        return points.Count < 2
            ? null
            : new MarketAnalysisSeriesDto(string.Join('+', names), ComparisonTitle(names), "%", points);
    }

    private static string ComparisonTitle(List<string> names)
    {
        var words = names.Select(name => name.Replace('_', ' ')).ToList();
        return char.ToUpperInvariant(words[0][0]) + string.Join(" and ", words)[1..];
    }

    private static string Humanize(string name)
    {
        var text = name.Replace('_', ' ');
        return text.Length == 0 ? name : char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static void Walk(JsonElement node, List<string> path, List<(int Depth, MarketAnalysisSeriesDto Series)> series)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                // A part of the findings that the service marks unavailable (for example the estimated value without a price)
                // holds placeholders, not figures to draw.
                if (node.TryGetProperty(StatusName, out var status) && status.ValueKind == JsonValueKind.String && status.GetString() == UnavailableStatus)
                {
                    break;
                }

                var first = series.Count;
                foreach (var property in node.EnumerateObject())
                {
                    path.Add(property.Name);
                    Walk(property.Value, path, series);
                    path.RemoveAt(path.Count - 1);
                }

                AddReference(node, path.Count + 1, series, first);
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

    // The overall figure that sits next to a time series (the occupancy of the whole period next to that of each month) is the level
    // the months are read against. Another series, such as the lead time bands whose overall figure is 100 %, has no such reference.
    private static void AddReference(JsonElement node, int seriesDepth, List<(int Depth, MarketAnalysisSeriesDto Series)> series, int first)
    {
        if (!node.TryGetProperty(OverallName, out var overall)
            || overall.ValueKind != JsonValueKind.Object
            || !overall.TryGetProperty(GroupName, out _)
            || Figure(overall) is not { } figure)
        {
            return;
        }

        for (var index = first; index < series.Count; index++)
        {
            var (depth, found) = series[index];
            if (depth == seriesDepth && IsMonthly(found) && found.Unit == figure.Unit)
            {
                series[index] = (depth, found with { ReferenceValue = figure.Value, ReferenceLabel = "Overall" });
            }
        }
    }

    // A set of shares is the parts of one whole when every point is a share of the same denominator and the numerators add up to it.
    private static bool AddsUpToWhole(List<(double Numerator, double Denominator)> shares, int pointCount) =>
        shares.Count == pointCount
        && shares.Count > 0
        && shares.All(share => share.Denominator > 0 && share.Denominator == shares[0].Denominator)
        && Math.Abs(shares.Sum(share => share.Numerator) - shares[0].Denominator) < WholeTolerance;

    private static MarketAnalysisSeriesDto? ToSeries(JsonElement array, List<string> path)
    {
        var points = new List<MarketAnalysisPointDto>();
        var shares = new List<(double Numerator, double Denominator)>();
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
            points.Add(new MarketAnalysisPointDto(Label(group), figure.Value, SmallSample(figureItem)));
            if (Number(figureItem, "numerator") is { } numerator && Number(figureItem, "denominator") is { } denominator)
            {
                shares.Add((numerator, denominator));
            }
        }

        if (points.Count == 0)
        {
            return null;
        }

        var id = string.Join('.', path);
        var isOrdered = points.All(point => OrderedLabel().IsMatch(point.Label) || MonthLabel().IsMatch(point.Label))
            || (path.Count > 0 && OrderedNames.Contains(path[^1]));

        return new MarketAnalysisSeriesDto(id, Title(path), unit, points, isOrdered, AddsUpToWhole(shares, points.Count));
    }

    private static bool SmallSample(JsonElement item) =>
        item.TryGetProperty("small_sample", out var flag) && flag.ValueKind == JsonValueKind.True;

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
