using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Components.Shared;

/// <summary>
/// The code behind the one-message alert: its severity, its message and the role it is read out with.
/// </summary>
public partial class AlertBox : ComponentBase
{
    /// <summary>The severity of the message. Controls colour and read-out.</summary>
    [Parameter]
    public AlertKind Kind { get; set; } = AlertKind.Info;

    /// <summary>The Danish message. An empty message without content hides the component.</summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>Optional extra content, for example a link or a button below the message.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Errors are announced immediately; the rest wait until the screen reader is finished.
    /// </summary>
    private string Role => Kind == AlertKind.Error ? "alert" : "status";
}
