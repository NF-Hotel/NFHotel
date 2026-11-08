using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the heading of a page: its title, subheading and actions.</summary>
public partial class PageHeader : ComponentBase
{
    /// <summary>The name of the page. Becomes the <c>h1</c> of the page.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional subheading that explains what the page is for.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>Optional actions — typically a primary button, for example "Create booking".</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }
}
