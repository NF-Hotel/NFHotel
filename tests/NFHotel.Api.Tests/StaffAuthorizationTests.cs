using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The staff endpoints of the API need the token of the web application and a valid staff token whose role holds the
/// permission of the endpoint (ADR-0005): 401 without a valid token, 403 for a role without the permission, and in
/// both cases the use case is not called.
/// </summary>
public sealed class StaffAuthorizationTests : IClassFixture<ApiFactory>
{
    private const string Guests = "/api/guests";
    private const string Rooms = "/api/rooms";

    private readonly ApiFactory _factory;

    public StaffAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region No valid staff token

    /// <summary>Negative: a staff endpoint without a staff token is 401 with the staff code, not the service code.</summary>
    [Fact]
    public async Task Get_StaffEndpointWithoutStaffToken_ReturnsUnauthorizedWithTheStaffCode()
    {
        // Arrange
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.StaffUnauthorized], body!.Errors);
    }

    /// <summary>Negative: a valid staff token without the token of the web application is still 401 with the service code.</summary>
    [Fact]
    public async Task Get_StaffTokenWithoutServiceToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    /// <summary>Negative: a token whose signature was changed is refused.</summary>
    [Fact]
    public async Task Get_TamperedStaffToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = _factory.IssueStaffToken(StaffRole.Manager);
        var tampered = token[..^3] + (token[^3] == 'A' ? "BBB" : "AAA");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, tampered);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Negative: a token that expired is refused, even though the signature is right.</summary>
    [Fact]
    public async Task Get_ExpiredStaffToken_ReturnsUnauthorized()
    {
        // Arrange
        var expired = Craft(ApiFactory.SigningKey, "Manager", expires: new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, expired);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Negative: a token signed with another key is refused.</summary>
    [Fact]
    public async Task Get_TokenSignedWithAnotherKey_ReturnsUnauthorized()
    {
        // Arrange
        var foreign = Craft("another-signing-key-0123456789-another-key", "Manager");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, foreign);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Negative: a token without a signature at all (algorithm none) is refused.</summary>
    [Fact]
    public async Task Get_UnsignedToken_ReturnsUnauthorized()
    {
        // Arrange
        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var payload = Base64Url("""{"iss":"nfhotel-api","aud":"nfhotel-staff","role":"Manager","name":"x","exp":4102444800}""");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, $"{header}.{payload}.");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Negative: a token for another audience is refused.</summary>
    [Fact]
    public async Task Get_TokenForAnotherAudience_ReturnsUnauthorized()
    {
        // Arrange
        var other = Craft(ApiFactory.SigningKey, "Manager", audience: "someone-else");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, other);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Edge: the staff token in the Authorization header, where only the service token belongs, does not sign anyone in.</summary>
    [Fact]
    public async Task Get_StaffTokenInTheAuthorizationHeader_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", _factory.IssueStaffToken(StaffRole.Manager));

        // Act
        using var response = await client.GetAsync(Rooms, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Roles

    /// <summary>Negative: Housekeeping cannot read a guest, and the passport number is not in the answer.</summary>
    [Fact]
    public async Task GetGuest_Housekeeping_ReturnsForbiddenWithoutTheGuest()
    {
        // Arrange
        var guest = _factory.World.AddGuest();
        using var client = _factory.CreateStaffClient(StaffRole.Housekeeping);

        // Act
        using var response = await client.GetAsync($"{Guests}/{guest.Id}", TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
        Assert.DoesNotContain(guest.LastName, text, StringComparison.Ordinal);
    }

    /// <summary>Negative: Housekeeping cannot list bookings or search guests.</summary>
    [Theory]
    [InlineData("POST", "/api/bookings/overview")]
    [InlineData("GET", "/api/guests")]
    [InlineData("POST", "/api/bookings/1/confirm")]
    public async Task StaffEndpoint_Housekeeping_ReturnsForbidden(string method, string path)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(StaffRole.Housekeeping);

        // Act
        var status = await SendAsync(client, method, path);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    /// <summary>Negative: a Receptionist can read rooms but cannot create, delete or take one out of service.</summary>
    [Theory]
    [InlineData("POST", "/api/rooms")]
    [InlineData("DELETE", "/api/rooms/1")]
    [InlineData("POST", "/api/rooms/1/take-out-of-service")]
    [InlineData("POST", "/api/rooms/1/start-cleaning")]
    public async Task RoomWrite_Receptionist_ReturnsForbidden(string method, string path)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(StaffRole.Receptionist);

        // Act
        var status = await SendAsync(client, method, path);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    /// <summary>Positive: a Receptionist reads rooms and works with guests and bookings.</summary>
    [Theory]
    [InlineData("GET", "/api/rooms")]
    [InlineData("GET", "/api/guests")]
    [InlineData("POST", "/api/bookings/overview")]
    public async Task StaffEndpoint_Receptionist_IsLetThrough(string method, string path)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(StaffRole.Receptionist);

        // Act
        var status = await SendAsync(client, method, path);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, status);
        Assert.NotEqual(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Positive: Housekeeping reads rooms and sets their cleaning status, nothing else.</summary>
    [Theory]
    [InlineData("GET", "/api/rooms")]
    [InlineData("POST", "/api/rooms/1/start-cleaning")]
    [InlineData("POST", "/api/rooms/1/complete-service")]
    public async Task RoomEndpoint_Housekeeping_IsLetThrough(string method, string path)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(StaffRole.Housekeeping);

        // Act
        var status = await SendAsync(client, method, path);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, status);
        Assert.NotEqual(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Positive: a Manager is let through to every kind of staff endpoint.</summary>
    [Theory]
    [InlineData("POST", "/api/rooms")]
    [InlineData("DELETE", "/api/guests/1")]
    [InlineData("POST", "/api/rooms/1/send-to-maintenance")]
    [InlineData("POST", "/api/bookings/1/cancel")]
    public async Task StaffEndpoint_Manager_IsLetThrough(string method, string path)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(StaffRole.Manager);

        // Act
        var status = await SendAsync(client, method, path);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, status);
        Assert.NotEqual(HttpStatusCode.Unauthorized, status);
    }

    /// <summary>Negative: a valid token with a role that no longer exists holds nothing.</summary>
    [Fact]
    public async Task Get_TokenWithAnUnknownRole_ReturnsForbidden()
    {
        // Arrange
        var token = Craft(ApiFactory.SigningKey, "Director");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    /// <summary>Negative: a number that is a valid enum value but not a role name is refused, so "1" does not mean Receptionist.</summary>
    [Fact]
    public async Task Get_TokenWithTheRoleAsANumber_ReturnsForbidden()
    {
        // Arrange
        var token = Craft(ApiFactory.SigningKey, "2");

        // Act
        var status = await StatusWithStaffTokenAsync(Rooms, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    #endregion

    #region Public endpoints

    /// <summary>Positive: the public booking endpoints need only the token of the web application.</summary>
    [Fact]
    public async Task AvailableRooms_ServiceTokenOnly_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.GetAsync(
            $"{ApiRoutes.Bookings}/{ApiRoutes.AvailableRooms}?checkInDate=2030-03-01&checkOutDate=2030-03-03",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<HttpStatusCode> StatusWithStaffTokenAsync(string path, string staffToken)
    {
        using var client = _factory.CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, staffToken);
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static string Craft(string signingKey, string role, DateTime? expires = null, string audience = "nfhotel-staff")
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "nfhotel-api",
            Audience = audience,
            NotBefore = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Expires = expires ?? new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object> { ["role"] = role, ["name"] = "crafted", ["sub"] = "1" },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string Base64Url(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));

    #endregion
}