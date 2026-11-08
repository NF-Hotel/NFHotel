using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>The code behind the contents of the report: which parts there are links to.</summary>
public partial class MarketAnalysisContents : ComponentBase
{
    private const string PagePath = "/admin/market-analysis";

    /// <summary>Whether the report has key figures to link to.</summary>
    [Parameter]
    public bool HasFigures { get; set; }

    /// <summary>Whether the report has an overall interpretation to link to.</summary>
    [Parameter]
    public bool HasOverall { get; set; }

    /// <summary>Whether the report has a forecast to link to.</summary>
    [Parameter]
    public bool HasForecast { get; set; }

    /// <summary>The analyses of the report, in the order of the page.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<MarketAnalysisSectionView> Sections { get; set; } = [];
}
