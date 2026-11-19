using Bunit;

using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Tests;

/// <summary>
/// Checks that a page shows the hostile strings of the tests as text (MIL-086 decision 5): the page holds no element and
/// no event attribute that a string would make, and its text holds each string as it is.
/// </summary>
internal static class HostileMarkup
{
    /// <summary>Asserts that each value is text of the page and that no value made an element or an attribute.</summary>
    /// <typeparam name="TPage">The page.</typeparam>
    /// <param name="page">The rendered page.</param>
    /// <param name="values">The values that the page shows.</param>
    /// <remarks>
    /// The parser of the test reads a line break of the markup as a line feed, as every HTML parser does, so a value is
    /// looked for with its line breaks as line feeds.
    /// </remarks>
    public static void AssertShownAsText<TPage>(IRenderedComponent<TPage> page, IEnumerable<string> values)
        where TPage : IComponent
    {
        Assert.Empty(page.FindAll("script"));
        Assert.Empty(page.FindAll("img"));
        Assert.Empty(page.FindAll("[onmouseover], [onerror]"));
        var text = string.Concat(page.Nodes.Select(node => node.TextContent));
        Assert.All(values, value => Assert.Contains(value.ReplaceLineEndings("\n"), text, StringComparison.Ordinal));
    }
}
