using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the caption under a chart: the caption it shows.</summary>
public partial class ChartCaption : ComponentBase
{
    /// <summary>The caption to show.</summary>
    [Parameter, EditorRequired]
    public ChartCaptionModel Model { get; set; } = new(string.Empty, string.Empty, [], null, null, string.Empty);
}
