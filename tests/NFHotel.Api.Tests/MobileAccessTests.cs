using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The mobile app holds no token of the web application (ADR-0015): the sign-in and the rooms routes accept the staff
/// token alone, every other route still needs the service token, and the sign-in is limited per address.
/// </summary>
public sealed class MobileAccessTests : IClassFixture<ApiFactory>
{
    private const string Login = "/api/auth/login";
    private const string Rooms = "/api/rooms";
    private const string Password = "correct horse battery staple";

    private readonly ApiFactory _factory;

    public MobileAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Sign-in without the service token

    /// <summary>Positive: the right password gives 200 and a token with no service token at all.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_RightPassword_ReturnsTheSession()
    {
        // Arrange
        _factory.AddStaff("mobile-rita", Password, StaffRole.Housekeeping);
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-rita", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StaffRole.Housekeeping, body!.Value!.Role);
    }

    /// <summary>Negative: a wrong password is 401 without the service token too.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        _factory.AddStaff("mobile-walt", Password, StaffRole.Manager);
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-walt", "not the password"), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Boundary: the sixth failed sign-in of a user name gives 429 without the service token.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_SixthFailure_ReturnsTooManyRequests()
    {
        // Arrange
        _factory.AddStaff("mobile-trudy", Password, StaffRole.Manager);
        using var client = _factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-trudy", $"wrong {attempt}"), ApiJson.Options, TestContext.Current.CancellationToken);
        }

        // Act
        using var sixth = await client.PostAsJsonAsync(Login, new SignInRequest("mobile-trudy", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
    }

    /// <summary>Boundary: many user names tried from one address are stopped after 20 failures, whatever the user name.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_ManyUserNamesFromOneAddress_ReturnsTooManyRequests()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("honest", Password, StaffRole.Manager);
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest($"guess-{attempt}", Password), ApiJson.Options, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        // Act
        using var next = await client.PostAsJsonAsync(Login, new SignInRequest("honest", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, next.StatusCode);
    }

    /// <summary>Equivalence: behind the proxy the address is the one in X-Forwarded-For, so one phone does not stop another.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_OtherForwardedAddress_IsNotStopped()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("other-phone", Password, StaffRole.Manager);
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var request = Request($"phone-guess-{attempt}", "203.0.113.7");
            using var failed = await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        // Act
        using var stoppedRequest = Request("other-phone", "203.0.113.7");
        using var otherRequest = Request("other-phone", "203.0.113.8");
        using var stopped = await client.SendAsync(stoppedRequest, TestContext.Current.CancellationToken);
        using var other = await client.SendAsync(otherRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, stopped.StatusCode);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    /// <summary>Equivalence: the web application, which signs everyone in from one address with the service token, is not counted per address.</summary>
    [Fact]
    public async Task Login_WithServiceToken_ManyFailuresOfOtherUserNames_StillSignsInTheRightOne()
    {
        // Arrange
        await using var factory = new ApiFactory();
        factory.AddStaff("web-user", Password, StaffRole.Manager);
        using var client = factory.CreateServiceClient();
        for (var attempt = 0; attempt < 25; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(Login, new SignInRequest($"web-guess-{attempt}", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        }

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("web-user", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage Request(string userName, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Login)
        {
            Content = JsonContent.Create(new SignInRequest(userName, Password), options: ApiJson.Options),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return request;
    }

    #endregion

    #region Rooms with the staff token alone

    /// <summary>Positive: a valid staff token alone reads the rooms.</summary>
    [Theory]
    [InlineData(StaffRole.Housekeeping)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    public async Task Get_Rooms_StaffTokenAlone_ReturnsOk(StaffRole role)
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(role));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: neither token is 401 with the service code.</summary>
    [Fact]
    public async Task Get_Rooms_NoToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a tampered staff token alone is 401.</summary>
    [Fact]
    public async Task Get_Rooms_TamperedStaffTokenAlone_ReturnsUnauthorized()
    {
        // Arrange
        var token = _factory.IssueStaffToken(StaffRole.Manager);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, token[..^3] + (token[^3] == 'A' ? "BBB" : "AAA"));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Negative: a wrong bearer token is not rescued by a valid staff token; the service token is checked when it is sent.</summary>
    [Fact]
    public async Task Get_Rooms_WrongServiceTokenAndValidStaffToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "wrong-token-0123456789-wrong-token-0123456789");
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Positive: Housekeeping starts and completes cleaning with the staff token alone.</summary>
    [Fact]
    public async Task Post_CleaningActions_HousekeepingStaffTokenAlone_ReturnsOk()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var room = factory.World.AddRoom("701", 7);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(StaffRole.Housekeeping));

        // Act
        using var due = await client.PostAsync($"{Rooms}/{room.Id}/mark-daily-cleaning-due", null, TestContext.Current.CancellationToken);
        using var start = await client.PostAsync($"{Rooms}/{room.Id}/start-cleaning", null, TestContext.Current.CancellationToken);
        using var complete = await client.PostAsync($"{Rooms}/{room.Id}/complete-cleaning", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, due.StatusCode);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
    }

    /// <summary>Negative: Housekeeping gets 403 on delete and on taking a room out of service.</summary>
    [Fact]
    public async Task Delete_Room_HousekeepingStaffTokenAlone_ReturnsForbidden()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var room = factory.World.AddRoom("702", 7);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(StaffRole.Housekeeping));

        // Act
        using var delete = await client.DeleteAsync($"{Rooms}/{room.Id}", TestContext.Current.CancellationToken);
        using var outOfService = await client.PostAsync($"{Rooms}/{room.Id}/take-out-of-service", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, outOfService.StatusCode);
    }

    #endregion

    #region Everything else still needs the service token

    /// <summary>Negative: a Manager staff token alone is 401 on every route group outside the list.</summary>
    [Theory]
    [InlineData("/api/bookings/1")]
    [InlineData("/api/bookings/available-rooms?checkInDate=2030-01-02&checkOutDate=2030-01-04")]
    [InlineData("/api/guests")]
    [InlineData("/api/benchmark/runs")]
    [InlineData("/api/market-analysis/latest")]
    [InlineData("/api/ai-defaults")]
    [InlineData("/api/auth/other")]
    public async Task Get_OtherRoutes_StaffTokenAlone_ReturnsUnauthorizedWithTheServiceCode(string path)
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a route that merely starts with the letters of a listed route is not listed.</summary>
    [Fact]
    public async Task Get_RouteWithListedPrefixInTheName_StaffTokenAlone_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync("/api/roomsXYZ", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
