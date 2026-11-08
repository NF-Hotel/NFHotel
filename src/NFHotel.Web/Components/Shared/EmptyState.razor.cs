using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the empty state: its title, its elaboration and the action it offers.</summary>
public partial class EmptyState : ComponentBase
{
    /// <summary>The main message, for example "No rooms match the filter".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = DisplayText.NoData;

    /// <summary>The elaboration: what the user can do to get something to look at.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Older name for <see cref="Description"/>.
    /// </summary>
    /// <remarks>
    /// TODO: remove when the last screens have been rewritten to <see cref="Description"/>.
    /// </remarks>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>Optional action, for example a button that clears the filter or creates a record.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The elaboration that is actually shown.</summary>
    private string? Text => string.IsNullOrWhiteSpace(Description) ? Hint : Description;
}
