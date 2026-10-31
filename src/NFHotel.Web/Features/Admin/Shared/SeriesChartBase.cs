using System.Globalization;
using System.Text;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>
/// What the SVG charts of one series of the market analysis report share: the parameters, the title and the description that
/// screen readers get, and the opening of the SVG. The SVG is built as text because the Razor compiler reserves the text element,
/// which SVG needs; every label is encoded by <see cref="ChartFormat.Encode"/>.
/// </summary>
public abstract class SeriesChartBase : ComponentBase
{
    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    /// <summary>The title of the chart.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The unit of the values: <c>%</c> for a rate, <c>bookings</c> for a count, or empty.</summary>
    [Parameter]
    public string Unit { get; set; } = string.Empty;

    /// <summary>The points of the series.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<MarketAnalysisPointDto> Points { get; set; } = [];

    /// <summary>The text under the axis: what is measured.</summary>
    protected string AxisText => Unit switch
    {
        "" => "value",
        "%" => "share, in %",
        _ => Unit,
    };

    /// <summary>The id of the title element of the SVG.</summary>
    protected string TitleId => $"chart-title-{_id}";

    /// <summary>The id of the description element of the SVG.</summary>
    protected string DescriptionId => $"chart-desc-{_id}";

    /// <summary>The id that an element of the SVG with a reference, such as a gradient, can use.</summary>
    protected string ElementId(string name) => $"chart-{name}-{_id}";

    /// <summary>The values in words, for the description of the SVG.</summary>
    protected string Description => string.Join("; ", Points.Select(point => $"{point.Label}: {PointText(point)}"));

    /// <summary>Writes the value of a point, with a star when it rests on few bookings.</summary>
    /// <param name="point">The point.</param>
    /// <returns>The text.</returns>
    protected string PointText(MarketAnalysisPointDto point) => ChartFormat.Value(point.Value, Unit) + (point.SmallSample ? "*" : string.Empty);

    /// <summary>Starts the SVG with its title and description.</summary>
    /// <param name="width">The width of the view box.</param>
    /// <param name="height">The height of the view box.</param>
    /// <returns>The builder, holding the opening of the SVG.</returns>
    protected StringBuilder OpenSvg(int width, int height)
    {
        StringBuilder svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg class=\"chart-svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-labelledby=\"{TitleId} {DescriptionId}\" preserveAspectRatio=\"xMinYMin meet\">");
        svg.Append(CultureInfo.InvariantCulture, $"<title id=\"{TitleId}\">{ChartFormat.Encode(Title)}</title><desc id=\"{DescriptionId}\">{ChartFormat.Encode(Description)}</desc>");

        return svg;
    }

    /// <summary>Ends the SVG.</summary>
    /// <param name="svg">The builder from <see cref="OpenSvg"/>.</param>
    /// <returns>The markup.</returns>
    protected static MarkupString CloseSvg(StringBuilder svg)
    {
        ArgumentNullException.ThrowIfNull(svg);

        return new MarkupString(svg.Append("</svg>").ToString());
    }
}
