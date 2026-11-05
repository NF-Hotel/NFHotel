using System.Globalization;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the ranked bars: the order of the points and the SVG built from them.</summary>
public partial class RankedBarChart : SeriesChartBase
{
    private const int Width = 640;
    private const int LabelWidth = 200;
    private const int PlotWidth = 340;
    private const int RowHeight = 20;
    private const int BarHeight = 14;
    private const int LabelLength = 30;

    private IReadOnlyList<MarketAnalysisPointDto> Ranked =>
        [.. Points.OrderByDescending(point => point.Value).ThenBy(point => point.Label, StringComparer.Ordinal)];

    private MarkupString BuildSvg()
    {
        var ranked = Ranked;
        var height = 4 + (ranked.Count * RowHeight) + 28;
        var maximum = ranked.Max(point => point.Value);
        var svg = OpenSvg(Width, height);
        for (var index = 0; index < ranked.Count; index++)
        {
            var point = ranked[index];
            var y = 4 + (index * RowHeight);
            var length = maximum <= 0 ? 0 : point.Value / maximum * PlotWidth;
            var drawn = Math.Max(length, point.Value > 0 ? 1 : 0);
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-label\" x=\"{LabelWidth - 8}\" y=\"{y + BarHeight - 2}\" text-anchor=\"end\"><title>{ChartFormat.Encode(point.Label)}</title>{ChartFormat.Encode(ChartFormat.Shorten(point.Label, LabelLength))}</text>");
            svg.Append(CultureInfo.InvariantCulture, $"<rect class=\"chart-bar chart-series-0\" x=\"{LabelWidth}\" y=\"{y}\" width=\"{ChartFormat.Num(drawn)}\" height=\"{BarHeight}\" rx=\"2\"></rect>");
            svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-value\" x=\"{ChartFormat.Num(LabelWidth + Math.Max(length, 1) + 4)}\" y=\"{y + BarHeight - 2}\">{ChartFormat.Encode(PointText(point))}</text>");
        }

        svg.Append(CultureInfo.InvariantCulture, $"<line class=\"chart-axis\" x1=\"{LabelWidth}\" y1=\"0\" x2=\"{LabelWidth}\" y2=\"{height - 22}\"></line>");
        svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-axis-title\" x=\"{LabelWidth}\" y=\"{height - 6}\">{ChartFormat.Encode(AxisText)}, largest first</text>");

        return CloseSvg(svg);
    }
}
