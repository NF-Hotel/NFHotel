using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the header of a card: title, hint, tinted icon and actions.</summary>
public partial class CardHead : ComponentBase
{
    /// <summary>The name of the section. Becomes the <c>h2</c> of the card.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The id of the heading, for <c>aria-labelledby</c> on the card.</summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>One line that says what the section is for.</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>The icon from the sprite, for example <c>login</c>. No icon, no tinted square.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>The tint of the icon: <c>teal</c> (default), <c>amber</c>, <c>ok</c>, <c>danger</c>, <c>slate</c> or <c>navy</c>.</summary>
    [Parameter]
    public string Tint { get; set; } = "teal";

    /// <summary>A number shown after the title, for example how many rows the card holds.</summary>
    [Parameter]
    public int? Count { get; set; }

    /// <summary>Optional actions — typically a link to the full list.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    private string TintClass => Tint is "teal" or "" ? string.Empty : $"tint-{Tint}";
}
