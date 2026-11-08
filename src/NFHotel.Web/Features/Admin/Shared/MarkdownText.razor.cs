using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>The code behind the Markdown text: the text, its class and the safe HTML made of it.</summary>
public partial class MarkdownText : ComponentBase
{
    private string _html = string.Empty;

    /// <summary>The Markdown to show.</summary>
    [Parameter, EditorRequired]
    public string? Text { get; set; }

    /// <summary>The class of the element; <c>markdown-text</c> by default, <c>markdown-text muted</c> for the evidence under a text.</summary>
    [Parameter]
    public string CssClass { get; set; } = "markdown-text";

    /// <inheritdoc />
    protected override void OnParametersSet() => _html = MarkdownConverter.ToHtml(Text);
}
