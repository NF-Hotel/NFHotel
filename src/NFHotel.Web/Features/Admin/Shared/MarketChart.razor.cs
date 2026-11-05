using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>
/// The code behind one series of the report: the kind of chart, its caption and its groups, from the selector.
/// </summary>
public partial class MarketChart : ComponentBase
{
    /// <summary>The series to draw.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisSeriesDto Series { get; set; } = new(string.Empty, string.Empty, string.Empty, []);

    /// <summary>The analysis the series belongs to, for its limitations, its formula and the observations of its insight.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisSectionDto Section { get; set; } = new(string.Empty, string.Empty, string.Empty, null, [], null);

    private ChartKind Kind => ChartKindSelector.Select(Series);

    private ChartCaptionModel Caption => ChartCaptionText.ForSeries(Kind, Series, Section);

    private IReadOnlyList<string> SeriesNames => [string.IsNullOrWhiteSpace(Series.Unit) ? Series.Title : Series.Unit];

    private IReadOnlyList<ChartGroup> Groups =>
        [.. Series.Points.Select(point => new ChartGroup(point.Label, [new ChartBar(SeriesNames[0], point.Value)]))];
}
