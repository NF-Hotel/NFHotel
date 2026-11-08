using System.Globalization;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the grouped bars: the groups, the series and the SVG built from them.</summary>
public partial class GroupedBarChart : ComponentBase
{
    private const int Width = 640;
    private const int LabelWidth = 200;
    private const int PlotWidth = 340;
    private const int BarHeight = 14;
    private const int BarGap = 3;
    private const int GroupGap = 14;

    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    /// <summary>The title of the chart.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The text under the axis: what is measured and which direction is better.</summary>
    [Parameter, EditorRequired]
    public string AxisTitle { get; set; } = string.Empty;

    /// <summary>The names of the series, in the order of their colours.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> SeriesNames { get; set; } = [];

    /// <summary>The groups, with one bar per series each.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<ChartGroup> Groups { get; set; } = [];

    /// <summary>The number format of the values.</summary>
    [Parameter]
    public string ValueFormat { get; set; } = "0.##";

    private double Maximum => Groups.SelectMany(group => group.Bars).Select(bar => bar.Value ?? 0).DefaultIfEmpty(0).Max();

    private int Height =>
        4 + Groups.Sum(group => (Math.Max(1, group.Bars.Count) * (BarHeight + BarGap)) - BarGap + GroupGap) + 28;

    private string Description =>
        string.Join("; ", Groups.Select(group => $"{group.Label}: " + string.Join(", ", group.Bars.Select(bar => $"{bar.Series} {(bar.Value is { } value ? Format(value) : "n/a")}"))));

    private MarkupString BuildSvg()
    {
        var titleId = $"chart-title-{_id}";
        var descId = $"chart-desc-{_id}";
        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg class=\"chart-svg\" viewBox=\"0 0 {Width} {Height}\" role=\"img\" aria-labelledby=\"{titleId} {descId}\" preserveAspectRatio=\"xMinYMin meet\">");
        svg.Append(CultureInfo.InvariantCulture, $"<title id=\"{titleId}\">{Encode(Title)}</title><desc id=\"{descId}\">{Encode(Description)}</desc>");

        var top = 4;
        foreach (var group in Groups)
        {
            var groupHeight = (Math.Max(1, group.Bars.Count) * (BarHeight + BarGap)) - BarGap;
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-label\" x=\"{LabelWidth - 8}\" y=\"{top + (groupHeight / 2) + 4}\" text-anchor=\"end\">{Encode(Shorten(group.Label))}</text>");
            for (var index = 0; index < group.Bars.Count; index++)
            {
                var bar = group.Bars[index];
                var y = top + (index * (BarHeight + BarGap));
                var seriesIndex = Math.Max(0, IndexOfSeries(bar.Series));
                if (bar.Value is { } value)
                {
                    var length = Maximum <= 0 ? 0 : value / Maximum * PlotWidth;
                    var drawn = Math.Max(length, value > 0 ? 1 : 0);
                    svg.Append(CultureInfo.InvariantCulture, $"<rect class=\"chart-bar chart-series-{seriesIndex}\" x=\"{LabelWidth}\" y=\"{y}\" width=\"{Num(drawn)}\" height=\"{BarHeight}\" rx=\"2\"></rect>");
                    svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-value\" x=\"{Num(LabelWidth + Math.Max(length, 1) + 4)}\" y=\"{y + BarHeight - 2}\">{Encode(Format(value))}</text>");
                }
                else
                {
                    svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-value chart-missing\" x=\"{LabelWidth + 4}\" y=\"{y + BarHeight - 2}\">n/a</text>");
                }
            }

            top += groupHeight + GroupGap;
        }

        svg.Append(CultureInfo.InvariantCulture, $"<line class=\"chart-axis\" x1=\"{LabelWidth}\" y1=\"0\" x2=\"{LabelWidth}\" y2=\"{Height - 22}\"></line>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{LabelWidth}\" y=\"{Height - 6}\">{Encode(AxisTitle)}</text>");
        svg.Append("</svg>");

        return new MarkupString(svg.ToString());
    }

    private int IndexOfSeries(string series)
    {
        for (var index = 0; index < SeriesNames.Count; index++)
        {
            if (string.Equals(SeriesNames[index], series, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private string Format(double value) => value.ToString(ValueFormat, CultureInfo.InvariantCulture);

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string Num(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Shorten(string label) => label.Length <= 30 ? label : label[..29] + "…";
}
