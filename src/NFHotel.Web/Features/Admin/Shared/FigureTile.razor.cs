using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind a figure tile: the figure, its unit and what it is the figure of.</summary>
public partial class FigureTile : ComponentBase
{
    /// <summary>The title of the series.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>What the figure is the figure of, for example the group of the point.</summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>The figure.</summary>
    [Parameter, EditorRequired]
    public double Value { get; set; }

    /// <summary>The unit of the figure, <c>%</c> for a rate.</summary>
    [Parameter]
    public string Unit { get; set; } = string.Empty;

    /// <summary>True when the figure rests on too few bookings for a firm reading.</summary>
    [Parameter]
    public bool SmallSample { get; set; }
}
