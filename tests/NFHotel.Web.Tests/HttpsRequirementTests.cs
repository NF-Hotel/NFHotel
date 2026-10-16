using System.Net;

using Microsoft.Extensions.Configuration;

using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>Behind the TLS proxy the web application serves only HTTPS and marks the staff cookie Secure (ADR-0006).</summary>
public sealed class HttpsRequirementTests
{
    #region The setting

    /// <summary>Equivalence: only the text true turns the requirement on; a missing or unreadable value leaves it off.</summary>
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsRequired_Value_OnlyTrueTurnsItOn(string? value, bool expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [HttpsRequirement.SettingName] = value })
            .Build();

        // Act
        var required = HttpsRequirement.IsRequired(configuration);

        // Assert
        Assert.Equal(expected, required);
    }

    #endregion

    #region Behind the proxy

    /// <summary>Negative: a request that arrives as HTTP is sent to the same address on HTTPS, with the query, and nothing is served.</summary>
    [Fact]
    public async Task Get_RequestArrivingAsHttp_RedirectsToHttps()
    {
        // Arrange
        using var factory = new TlsFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync("/book?x=1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal("https://localhost/book?x=1", response.Headers.Location?.OriginalString);
    }

    /// <summary>Equivalence: the proxy marks the request as HTTPS, and the page is served.</summary>
    [Fact]
    public async Task Get_RequestForwardedAsHttps_IsServed()
    {
        // Arrange
        using var factory = new TlsFactory();
        using var browser = factory.CreateBrowser();
        browser.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await browser.GetAsync("/book", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Positive: the staff cookie has the Secure, HttpOnly and SameSite attributes behind the proxy.</summary>
    [Fact]
    public async Task SignIn_BehindTheProxy_SetsASecureCookie()
    {
        // Arrange
        using var factory = new TlsFactory();
        using var browser = factory.CreateBrowser();
        browser.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("nfhotel.staff=", StringComparison.Ordinal));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Negative: a sign-in that arrives as HTTP is redirected before the password is read, so no cookie is set.</summary>
    [Fact]
    public async Task SignIn_ArrivingAsHttp_IsRedirectedAndSetsNoCookie()
    {
        // Arrange
        using var factory = new TlsFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await browser.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>()), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    #endregion

    #region Local development

    /// <summary>Equivalence: without the setting an HTTP request is served, as in local development, and the cookie is not Secure on HTTP.</summary>
    [Fact]
    public async Task SignIn_WithoutTheSetting_WorksOverHttpWithAPlainCookie()
    {
        // Arrange
        using var factory = new WebFactory();
        using var browser = factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("nfhotel.staff=", StringComparison.Ordinal));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("secure", cookie.Replace("httponly", string.Empty, StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helpers

    private sealed class TlsFactory : WebFactory
    {
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new(HttpsRequirement.SettingName, "true"),
            new("FORWARDEDHEADERS_ENABLED", "true"),
        ];
    }

    #endregion
}