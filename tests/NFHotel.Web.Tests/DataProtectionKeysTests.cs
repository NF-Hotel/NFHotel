using System.Net;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>
/// The Data Protection keys survive a new container when they are kept in a shared folder (ADR-0031): a second instance
/// of the web application reads the staff cookie and accepts a form that the first one served.
/// </summary>
public sealed class DataProtectionKeysTests : IDisposable
{
    private readonly DirectoryInfo _keys = Directory.CreateTempSubdirectory("nfhotel-keys-");

    /// <inheritdoc />
    public void Dispose() => _keys.Delete(recursive: true);

    #region The setting

    /// <summary>Positive: with the setting, the keys are written as files in the folder, under the fixed application name.</summary>
    [Fact]
    public void AddPersistentDataProtection_KeysPathSet_WritesTheKeysToTheFolder()
    {
        // Arrange
        using var provider = Provider(_keys.FullName);

        // Act
        var protector = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
        _ = protector.Protect("value");

        // Assert
        Assert.NotEmpty(_keys.GetFiles("key-*.xml"));
    }

    /// <summary>Edge: without the setting nothing is written to the folder, and protection still works (development).</summary>
    [Fact]
    public void AddPersistentDataProtection_NoKeysPath_UsesTheDefault()
    {
        // Arrange
        using var provider = Provider(null);

        // Act
        var protector = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
        var value = protector.Unprotect(protector.Protect("value"));

        // Assert
        Assert.Equal("value", value);
        Assert.Empty(_keys.GetFiles());
    }

    #endregion

    #region Across instances

    /// <summary>Integration: a staff cookie made by one instance signs the user in at a second instance with the same folder.</summary>
    [Fact]
    public async Task StaffCookie_SecondInstanceWithSameFolder_IsStillSignedIn()
    {
        // Arrange
        using var first = new KeysFactory(_keys.FullName);
        using var second = new KeysFactory(_keys.FullName);
        var cookie = await SignInCookieAsync(first);
        using var browser = second.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });

        // Act
        using var response = await GetWithCookieAsync(browser, "/admin", cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: a second instance with its own keys, as before ADR-0031, does not read the cookie and asks for a sign-in.</summary>
    [Fact]
    public async Task StaffCookie_SecondInstanceWithOtherKeys_AsksForSignIn()
    {
        // Arrange
        var otherKeys = Directory.CreateTempSubdirectory("nfhotel-keys-");
        try
        {
            using var first = new KeysFactory(_keys.FullName);
            using var second = new KeysFactory(otherKeys.FullName);
            var cookie = await SignInCookieAsync(first);
            using var browser = second.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });

            // Act
            using var response = await GetWithCookieAsync(browser, "/admin", cookie);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/admin/login", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        }
        finally
        {
            otherKeys.Delete(recursive: true);
        }
    }

    /// <summary>Integration: a form served by one instance is accepted by a second instance with the same folder (antiforgery).</summary>
    [Fact]
    public async Task LoginForm_ServedByFirstPostedToSecond_IsAccepted()
    {
        // Arrange
        using var first = new KeysFactory(_keys.FullName);
        using var second = new KeysFactory(_keys.FullName);
        using var firstBrowser = first.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        using var page = await firstBrowser.GetAsync("/admin/login", TestContext.Current.CancellationToken);
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var fields = WebFactory.HiddenFields(html);
        fields[WebFactory.InputName(html, "login-user")] = "mona";
        fields[WebFactory.InputName(html, "login-password")] = WebFactory.Password;
        using var secondBrowser = second.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        using var post = new HttpRequestMessage(HttpMethod.Post, "/admin/login") { Content = new FormUrlEncodedContent(fields) };
        post.Headers.Add("Cookie", CookieHeader(page));

        // Act
        using var response = await secondBrowser.SendAsync(post, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("nfhotel.staff=", StringComparison.Ordinal));
    }

    #endregion

    private static ServiceProvider Provider(string? keysPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DataProtectionRegistration.KeysPathSetting] = keysPath })
            .Build();

        return new ServiceCollection().AddPersistentDataProtection(configuration).BuildServiceProvider();
    }

    private static async Task<string> SignInCookieAsync(WebFactory factory)
    {
        using var browser = factory.CreateBrowser();
        using var response = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return CookieHeader(response);
    }

    private static async Task<HttpResponseMessage> GetWithCookieAsync(HttpClient browser, string path, string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", cookie);
        return await browser.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string CookieHeader(HttpResponseMessage response) =>
        string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]));

    /// <summary>The web application with its keys in a given folder, as the image sets it.</summary>
    private sealed class KeysFactory(string keysPath) : WebFactory
    {
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
            [new(DataProtectionRegistration.KeysPathSetting, keysPath)];
    }
}
