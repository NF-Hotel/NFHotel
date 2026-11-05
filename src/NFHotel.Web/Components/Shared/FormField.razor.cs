using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind a form field: its label, its input, its error and its help text.</summary>
public partial class FormField : ComponentBase
{
    /// <summary>The label above the field. Danish, without colon.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>The id of the input in <see cref="ChildContent"/>. Binds the label to the field.</summary>
    [Parameter]
    [EditorRequired]
    public string For { get; set; } = string.Empty;

    /// <summary>The input itself — <c>&lt;input class="field-input"&gt;</c>, a select or similar.</summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Finished Danish error text. Empty means the field is in order.</summary>
    [Parameter]
    public string? Error { get; set; }

    /// <summary>Optional help text, for example the expected format.</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>Marks the field as required with an asterisk at the label.</summary>
    [Parameter]
    public bool IsRequired { get; set; }
}
