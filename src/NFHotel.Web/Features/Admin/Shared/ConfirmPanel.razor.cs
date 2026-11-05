using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the confirmation strip: its texts, its busy state and the two callbacks.</summary>
public partial class ConfirmPanel : ComponentBase
{
    /// <summary>The heading, for example "Cancel booking FLZ-000123?".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The explanation of what happens if the user continues.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>The text on the confirming button. Must name the action, not say "OK".</summary>
    [Parameter]
    public string ConfirmLabel { get; set; } = "Confirm";

    /// <summary>Disables both buttons while a call is in progress.</summary>
    [Parameter]
    public bool IsBusy { get; set; }

    /// <summary>Called when the user confirms.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Called when the user backs out.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }
}
