using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the error list: the finished texts it shows.</summary>
public partial class ErrorAlert : ComponentBase
{
    /// <summary>The Danish error texts to show. An empty list hides the component.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
