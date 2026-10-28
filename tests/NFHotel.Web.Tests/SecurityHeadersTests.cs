using System.Net;
using System.Text.RegularExpressions;

using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// Every response carries the browser security headers, and the pages hold no inline code that the Content Security
/// Policy would refuse (MIL-033, criterion 3).
/// </summary>
public sealed partial class SecurityHeadersTests
{
    #region The headers

    /// <summary>Positive: a page has the policy and the five other headers with their values.</summary>
    [Fact]
    public async Task Get_Page_HasEveryHeader()
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync("/book", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeaders(response);
    }

    /// <summary>Equivalence: a static file has the same headers as a page.</summary>
    [Fact]
    public async Task Get_StaticFile_HasEveryHeader()
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync("/favicon.png", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeaders(response);
    }

    /// <summary>Edge case: a redirect, written before any page runs, has the headers too.</summary>
    [Fact]
    public async Task Get_Redirect_HasEveryHeader()
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync("/admin", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertHeaders(response);
    }

    /// <summary>Positive: the policy names the directives with no fallback, and allows no inline code, eval or wildcard.</summary>
    [Fact]
    public void ContentSecurityPolicy_Directives_AllowNoInlineCodeOrForeignSource()
    {
        // Arrange
        var directives = SecurityHeaders.ContentSecurityPolicy.Split(';', StringSplitOptions.TrimEntries)
            .ToDictionary(directive => directive.Split(' ')[0], directive => directive);

        // Act
        var sources = string.Join(' ', directives.Values);

        // Assert
        Assert.All(["default-src", "object-src", "base-uri", "form-action", "frame-ancestors"], name => Assert.Contains(name, directives.Keys));
        Assert.Equal("object-src 'none'", directives["object-src"]);
        Assert.Equal("frame-ancestors 'none'", directives["frame-ancestors"]);
        Assert.DoesNotContain("unsafe-inline", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("*", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("data:", sources, StringComparison.Ordinal);
    }

    #endregion

    #region The pages

    /// <summary>Regression: the page sends one complete policy, not Blazor's own frame-ancestors 'self' as well.</summary>
    [Fact]
    public async Task Get_Page_SendsOnePolicy()
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync("/book", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
    }

    /// <summary>
    /// Negative: no page of the public site or the login has an inline script, an import map, a style element or a
    /// style attribute, which the policy refuses; and the page has the reconnect dialog of its own.
    /// </summary>
    /// <param name="path">The page.</param>
    [Theory]
    [InlineData("/book")]
    [InlineData("/admin/login")]
    public async Task Get_Page_HasNoInlineCode(string path)
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        var html = await browser.GetStringAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(InlineScript().Matches(html));
        Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(StyleAttribute().Matches(html));
        Assert.Contains("id=\"components-reconnect-modal\"", html, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static void AssertHeaders(HttpResponseMessage response)
    {
        foreach (var (name, value) in SecurityHeaders.All)
        {
            Assert.True(response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values), $"{name} is missing");
            Assert.Equal(value, Assert.Single(values));
        }
    }

    /// <summary>A script element without a src attribute: its code is inline.</summary>
    [GeneratedRegex(@"<script(?![^>]*\bsrc=)[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InlineScript();

    [GeneratedRegex(@"\sstyle\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex StyleAttribute();

    #endregion
}
