using System.Globalization;

using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the donut chart: the geometry of the slices and the SVG built from them.</summary>
public partial class DonutChart : SeriesChartBase
{
    private const int Size = 200;
    private const int OuterRadius = 92;
    private const int InnerRadius = 54;
    private const int SeriesColours = 4;
    private const double MinLabelledShare = 0.08;

    private MarkupString BuildSvg()
    {
        var svg = OpenSvg(Size, Size);
        var slices = DonutGeometry.Slices([.. Points.Select(point => point.Value)], Size / 2d, Size / 2d, OuterRadius, InnerRadius);
        foreach (var slice in slices.Where(slice => slice.Path.Length > 0))
        {
            var point = Points[slice.Index];
            svg.Append(CultureInfo.InvariantCulture, $"<path class=\"chart-slice chart-series-{slice.Index % SeriesColours}\" d=\"{slice.Path}\"><title>{ChartFormat.Encode(point.Label)}: {ChartFormat.Encode(PointText(point))}</title></path>");
            if (slice.Share >= MinLabelledShare)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-on-slice\" x=\"{ChartFormat.Num(slice.LabelX)}\" y=\"{ChartFormat.Num(slice.LabelY + 4)}\" text-anchor=\"middle\">{ChartFormat.Encode(PointText(point))}</text>");
            }
        }

        return CloseSvg(svg);
    }
}
