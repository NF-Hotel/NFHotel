using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Configuration;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;

namespace NFHotel.Api.Tests;

/// <summary>The API refuses a call without the right bearer token and keeps the health endpoint open (ADR-0004).</summary>
public sealed class ApiAuthenticationTests : IClassFixture<ApiFactory>
{
    private const string AnyApiPath = "/api/rooms";

    private readonly ApiFactory _factory;

    public ApiAuthenticationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region The token

    /// <summary>Positive: the right token is accepted.</summary>
    [Fact]
    public async Task Get_RightToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateAuthorizedClient();

        // Act
        using var response = await client.GetAsync(AnyApiPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: a call without a token gets 401 and the error code, and nothing is changed.</summary>
    [Fact]
    public async Task Get_NoToken_ReturnsUnauthorizedWithTheErrorCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(AnyApiPath, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(["api.unauthorized"], body!.Errors);
    }

    /// <summary>Negative: a wrong token, a token that is only a prefix of the right one, and a token with extra text all get 401.</summary>
    [Theory]
    [InlineData("wrong-token-wrong-token-wrong-token-0123")]
    [InlineData("test-token-0123456789")]
    [InlineData("test-token-0123456789-test-token-0123456789-extra")]
    [InlineData("")]
    public async Task Get_WrongToken_ReturnsUnauthorized(string token)
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Act
        using var response = await client.GetAsync(AnyApiPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Negative: the right token under another scheme is not accepted.</summary>
    [Fact]
    public async Task Get_RightTokenUnderBasicScheme_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Basic", ApiFactory.Token);

        // Act
        using var response = await client.GetAsync(AnyApiPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Edge: the scheme name is case-insensitive, as HTTP says.</summary>
    [Fact]
    public async Task Get_SchemeInLowerCase_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("bearer", ApiFactory.Token);

        // Act
        using var response = await client.GetAsync(AnyApiPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: every verb is checked, a write without a token changes nothing.</summary>
    [Fact]
    public async Task Post_NoToken_ReturnsUnauthorizedAndCreatesNothing()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new NFHotel.Application.Dto.Rooms.CreateRoomRequest("901", 9, NFHotel.Domain.ValueObjects.RoomSize.Suite, 4);

        // Act
        using var response = await client.PostAsJsonAsync(ApiRoutes.Rooms, request, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(_factory.World.Rooms.Items, room => room.RoomNumber == "901");
    }

    #endregion

    #region Health

    /// <summary>Positive: the health endpoint answers without a token, for the container health check.</summary>
    [Fact]
    public async Task Get_HealthWithoutToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Start-up

    /// <summary>Negative boundary: a token one character short of the minimum stops the start-up.</summary>
    [Fact]
    public void Read_TokenBelowTheMinimum_Throws()
    {
        // Arrange
        var configuration = Configuration(new string('a', ApiAuthentication.MinimumTokenLength - 1));

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => ApiToken.Read(configuration));
    }

    /// <summary>Boundary: a token of exactly the minimum length is accepted.</summary>
    [Fact]
    public void Read_TokenAtTheMinimum_IsAccepted()
    {
        // Arrange
        var configuration = Configuration(new string('a', ApiAuthentication.MinimumTokenLength));

        // Act
        var token = ApiToken.Read(configuration);

        // Assert
        Assert.Equal(ApiAuthentication.MinimumTokenLength, token.Value.Length);
    }

    /// <summary>Negative: no token at all stops the start-up.</summary>
    [Fact]
    public void Read_NoToken_Throws()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => ApiToken.Read(configuration));
    }

    #endregion

    #region Helpers

    private static IConfiguration Configuration(string token) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:Token"] = token })
            .Build();

    #endregion
}