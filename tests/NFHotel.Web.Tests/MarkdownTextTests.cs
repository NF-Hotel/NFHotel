using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Tests;

/// <summary>The component that shows an AI text written in Markdown: formatted, with no raw HTML run and no link to click (MIL-026 task 23).</summary>
public sealed class MarkdownTextTests
{
    /// <summary>Positive: paragraphs, a bulleted list and bold text are drawn as HTML.</summary>
    [Fact]
    public async Task Render_ParagraphsListAndBold_AreHtml()
    {
        // Act
        string html = await RenderAsync("First **strong** paragraph.\n\nSecond paragraph.\n\n- one\n- two\n");

        // Assert
        Assert.Contains("<p>First <strong>strong</strong> paragraph.</p>", html, StringComparison.Ordinal);
        Assert.Contains("<p>Second paragraph.</p>", html, StringComparison.Ordinal);
        Assert.Contains("<ul>", html, StringComparison.Ordinal);
        Assert.Contains("<li>one</li>", html, StringComparison.Ordinal);
        Assert.Contains("<li>two</li>", html, StringComparison.Ordinal);
        Assert.Contains("class=\"markdown-text\"", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a script in the text is shown as text and is not an element.</summary>
    [Fact]
    public async Task Render_ScriptTag_IsShownAsTextAndNotRun()
    {
        // Act
        string html = await RenderAsync("Look: <script>alert(1)</script> and <img src=x onerror=alert(2)>\n\n<div onclick=\"x()\">block</div>");

        // Assert
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<div onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a link keeps its text and is not an anchor, also with a script address; an image and an autolink are not loaded or clickable.</summary>
    [Theory]
    [InlineData("See [the report](https://example.com/report) now.", "See the report now.", "example.com/report")]
    [InlineData("Run [this](javascript:alert(1)).", "Run this.", "javascript")]
    [InlineData("An image ![alt text](https://example.com/x.png) here.", "An image alt text here.", "x.png")]
    [InlineData("Visit <https://example.com/auto> today.", "Visit https://example.com/auto today.", "href")]
    public async Task Render_Link_IsTextAndNotClickable(string markdown, string expectedText, string absent)
    {
        // Act
        string html = await RenderAsync(markdown);

        // Assert
        Assert.DoesNotContain("<a ", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.Contains(expectedText, html, StringComparison.Ordinal);
        if (absent != "href")
        {
            Assert.DoesNotContain(absent, html, StringComparison.Ordinal);
        }
    }

    /// <summary>Boundary: a heading in the text is lowered below the headings of the page.</summary>
    [Fact]
    public async Task Render_Heading_IsLowered()
    {
        // Act
        string html = await RenderAsync("# Big\n\n## Medium");

        // Assert
        Assert.DoesNotContain("<h1", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<h2", html, StringComparison.Ordinal);
        Assert.Contains("<h4>Big</h4>", html, StringComparison.Ordinal);
        Assert.Contains("<h5>Medium</h5>", html, StringComparison.Ordinal);
    }

    /// <summary>Boundary: no text, white space and null draw nothing.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public async Task Render_EmptyText_DrawsNothing(string? markdown)
    {
        // Act
        string html = await RenderAsync(markdown);

        // Assert
        Assert.Equal(string.Empty, html);
    }

    /// <summary>Positive: the evidence of an AI text with a bold figure is shown bold and with no asterisks, in the muted class (MIL-026 task 27).</summary>
    [Fact]
    public async Task Render_EvidenceWithBoldFigure_IsBoldAndHasNoAsterisks()
    {
        // Act
        string html = await RenderAsync("Evidence: No Contract (Single) cancellation share: **30.9%** from 36,373 cancelled bookings (117787 bookings)", "markdown-text muted");

        // Assert
        Assert.Contains("<strong>30.9%</strong>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("**", html, StringComparison.Ordinal);
        Assert.Contains("class=\"markdown-text muted\"", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a script in the evidence is shown as text and is not run.</summary>
    [Fact]
    public async Task Render_EvidenceWithScript_IsShownAsText()
    {
        // Act
        string html = await RenderAsync("Evidence: <script>alert(1)</script> 12.5%", "markdown-text muted");

        // Assert
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(string? markdown, string? cssClass = null)
    {
        await using ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await renderer.RenderComponentAsync<MarkdownText>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(MarkdownText.Text)] = markdown,
                [nameof(MarkdownText.CssClass)] = cssClass ?? "markdown-text",
            }));
            return output.ToHtmlString();
        });
    }
}
