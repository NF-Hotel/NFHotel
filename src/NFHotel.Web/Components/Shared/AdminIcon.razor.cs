using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind one icon from the sprite.</summary>
public partial class AdminIcon : ComponentBase
{
    /// <summary>The name of the symbol without the <c>i-</c> prefix, for example <c>calendar</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>Extra classes, for example <c>icon-sm</c>.</summary>
    [Parameter]
    public string? Class { get; set; }
}
