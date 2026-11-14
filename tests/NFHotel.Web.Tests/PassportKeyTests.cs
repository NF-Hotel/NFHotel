using System.Net;

namespace NFHotel.Web.Tests;

/// <summary>
/// The web application holds no passport key: it gets its use cases from the API, and only the API decrypts a passport
/// number (ADR-0004, MIL-075). So it starts and serves its pages with no key in its configuration.
/// </summary>
public sealed class PassportKeyTests
{
    /// <summary>Positive: with no passport key, the web application starts and serves the front page and the sign-in page.</summary>
    /// <param name="path">The page.</param>
    [Theory]
    [InlineData("/")]
    [InlineData("/admin/login")]
    public async Task Get_PageWithoutPassportKey_IsServed(string path)
    {
        // Arrange
        using NoPassportKeyFactory factory = new();
        using HttpClient browser = factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await browser.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The web application with an empty passport key, which the API treats as a missing key. The empty value also hides
    /// a key that the environment of the test runner might hold.
    /// </summary>
    private sealed class NoPassportKeyFactory : WebFactory
    {
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new("Encryption:PassportKey", string.Empty),
        ];
    }
}
