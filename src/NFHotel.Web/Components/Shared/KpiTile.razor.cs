using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind a key figure: the figure, its note and where the tile leads.</summary>
public partial class KpiTile : ComponentBase
{
    /// <summary>What the figure counts, for example "Arrivals today".</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>The figure itself.</summary>
    [Parameter]
    [EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>A small addition after the figure, for example "2 checked in".</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>One line under the figure.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>The icon from the sprite.</summary>
    [Parameter]
    [EditorRequired]
    public string Icon { get; set; } = string.Empty;

    /// <summary>The tint of the icon: <c>teal</c> (default), <c>amber</c>, <c>ok</c>, <c>danger</c> or <c>slate</c>.</summary>
    [Parameter]
    public string Tint { get; set; } = "teal";

    /// <summary>Where the tile leads. Without it the tile is plain.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>A share from 0 to 100, drawn as a bar under the figure.</summary>
    [Parameter]
    public int? Percent { get; set; }

    /// <summary>True for the amber edge that marks the figure that needs doing first.</summary>
    [Parameter]
    public bool Accent { get; set; }

    private string TileClass => Accent ? "kpi accent" : "kpi";

    private string TintClass => Tint is "teal" or "" ? "tint kpi-icon" : $"tint kpi-icon tint-{Tint}";
}
