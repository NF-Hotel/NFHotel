using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Home.Neighbourhood;

/// <summary>The code behind one pin on the map: the place it stands for and where its name goes.</summary>
public partial class MapPin : ComponentBase
{
    /// <summary>The place the pin stands for.</summary>
    [Parameter]
    [EditorRequired]
    public NeighbourhoodPlace Place { get; set; } = null!;

    /// <summary>The number in the dot, the same as in the list under the map.</summary>
    [Parameter]
    [EditorRequired]
    public int Number { get; set; }

    /// <summary>The classes of the pin, from the viewmodel, so the chosen pin is drawn filled.</summary>
    [Parameter]
    [EditorRequired]
    public string PinClass { get; set; } = string.Empty;

    /// <summary>Raised when the pin is clicked or tapped.</summary>
    [Parameter]
    public EventCallback OnSelect { get; set; }

    private string LabelClass => Place.LabelOnLeft ? "m-pin-label m-pin-label-left" : "m-pin-label";

    private int LabelX => Place.LabelOnLeft ? -18 : 18;
}
