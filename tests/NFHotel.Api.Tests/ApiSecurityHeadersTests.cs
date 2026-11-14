using System.Net;

using Microsoft.Net.Http.Headers;

using NFHotel.Api.Contracts;

namespace NFHotel.Api.Tests;

/// <summary>
/// Every answer of the API tells a browser to take it for the type that it declares (MIL-077). The API scan of ZAP
/// found the answers without <c>X-Content-Type-Options</c> (alert 10021), and the sign-in and the rooms reach a client
/// through nginx (ADR-0015), which adds no header of its own.
/// </summary>
public sealed class ApiSecurityHeadersTests : IClassFixture<ApiFactory>
{
    private const string NoSniff = "nosniff";

    private readonly ApiFactory _factory;

    public ApiSecurityHeadersTests(ApiFactory factory) => _factory = factory;

    /// <summary>
    /// Regression: an answer, a refusal of the tokens, a refusal of hostile input and an unknown route each carry
    /// <c>X-Content-Type-Options: nosniff</c>; before the fix none of them did.
    /// </summary>
    /// <param name="path">The route, from the root of the API.</param>
    /// <param name="isSignedIn">Whether the client presents the service token and a Manager's staff token.</param>
    /// <param name="expected">The status of the answer.</param>
    [Theory]
    [InlineData(ApiRoutes.Health, true, HttpStatusCode.OK)]
    [InlineData(ApiRoutes.Rooms, true, HttpStatusCode.OK)]
    [InlineData(ApiRoutes.Rooms, false, HttpStatusCode.Unauthorized)]
    [InlineData(ApiRoutes.Guests + "?searchText=a%00b", true, HttpStatusCode.BadRequest)]
    [InlineData("api/no-such-route", true, HttpStatusCode.NotFound)]
    public async Task Get_AnyAnswer_TellsTheBrowserNotToSniff(string path, bool isSignedIn, HttpStatusCode expected)
    {
        // Arrange
        using var client = isSignedIn ? _factory.CreateAuthorizedClient() : _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HeaderNames.XContentTypeOptions, out var values), "the answer has no X-Content-Type-Options");
        Assert.Equal(NoSniff, Assert.Single(values));
    }
}
