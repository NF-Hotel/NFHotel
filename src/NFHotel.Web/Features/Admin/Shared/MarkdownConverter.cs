using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace NFHotel.Web.Features.Admin.Shared;

/// <summary>
/// Turns the Markdown of an AI text into HTML that is safe to put in a page. The text comes from a local AI model, so it is not
/// trusted: raw HTML is not parsed and is shown as plain text (<see cref="MarkdownExtensions.DisableHtml"/> and the encoding of the
/// renderer), and links and images are replaced by their text, so the page has no anchor and loads nothing. Headings are lowered to
/// level 4 or below, so a text cannot break the structure of the page.
/// </summary>
public static class MarkdownConverter
{
    private const int LowestHeading = 4;
    private const int DeepestHeading = 6;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

    /// <summary>Converts Markdown to HTML.</summary>
    /// <param name="markdown">The Markdown; <c>null</c> or white space gives an empty text.</param>
    /// <returns>The HTML, with no raw HTML of the input, no anchor and no image.</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        MarkdownDocument document = Markdown.Parse(markdown, Pipeline);

        foreach (LinkInline link in document.Descendants<LinkInline>().ToList())
        {
            Unwrap(link);
        }

        foreach (AutolinkInline autolink in document.Descendants<AutolinkInline>().ToList())
        {
            autolink.ReplaceBy(new LiteralInline(autolink.Url));
        }

        foreach (HeadingBlock heading in document.Descendants<HeadingBlock>())
        {
            heading.Level = Math.Clamp(heading.Level + LowestHeading - 1, LowestHeading, DeepestHeading);
        }

        return Markdown.ToHtml(document, Pipeline);
    }

    // A link or an image keeps its text (the children) and loses its target.
    private static void Unwrap(LinkInline link)
    {
        Inline? child = link.FirstChild;
        while (child is not null)
        {
            Inline? next = child.NextSibling;
            child.Remove();
            link.InsertBefore(child);
            child = next;
        }

        link.Remove();
    }
}
