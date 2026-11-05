using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the text alternative of a chart: the values of the table.</summary>
public partial class ChartAlternative : ComponentBase
{
    /// <summary>The title of the chart, the caption of the table.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The unit of the values.</summary>
    [Parameter]
    public string Unit { get; set; } = string.Empty;

    /// <summary>The points of the chart.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<MarketAnalysisPointDto> Points { get; set; } = [];

    /// <summary>The heading of the first column.</summary>
    [Parameter]
    public string FirstColumn { get; set; } = "Group";
}
