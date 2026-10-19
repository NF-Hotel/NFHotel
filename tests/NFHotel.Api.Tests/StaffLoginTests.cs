using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>The sign-in endpoint: a token for the right password, one answer for every kind of refusal, and a stop after five failures (ADR-0005).</summary>
public sealed class StaffLoginTests : IClassFixture<ApiFactory>
{
    private const string Login = "/api/auth/login";
    private const string Password = "correct horse battery staple";

    private readonly ApiFactory _factory;

    public StaffLoginTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Success

    /// <summary>Positive: the right password gives 200 and a session with the role.</summary>
    [Fact]
    public async Task Login_RightPassword_ReturnsTheSessionWithTheRole()
    {
        // Arrange
        _factory.AddStaff("rita", Password, StaffRole.Receptionist);
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("rita", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StaffRole.Receptionist, body!.Value!.Role);
        Assert.False(string.IsNullOrEmpty(body.Value.Token));
    }

    /// <summary>Equivalence: the user name is matched in any case.</summary>
    [Fact]
    public async Task Login_UserNameInOtherCase_ReturnsOk()
    {
        // Arrange
        _factory.AddStaff("mona", Password, StaffRole.Manager);
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("  MoNa ", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Integration: the token that the sign-in gives is accepted on an endpoint of the role, and refused on one the role lacks.</summary>
    [Fact]
    public async Task Login_ThenCallWithTheToken_RoleDecidesWhatIsAllowed()
    {
        // Arrange
        _factory.AddStaff("hilda", Password, StaffRole.Housekeeping);
        using var signIn = _factory.CreateServiceClient();
        using var response = await signIn.PostAsJsonAsync(Login, new SignInRequest("hilda", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        var token = (await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!.Token;
        using var client = _factory.CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, token);

        // Act
        using var rooms = await client.GetAsync("/api/rooms", TestContext.Current.CancellationToken);
        using var guests = await client.GetAsync("/api/guests", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, rooms.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, guests.StatusCode);
    }

    #endregion

    #region Refusal

    /// <summary>Negative: a wrong password is 401 with the credentials code, and no token.</summary>
    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedWithoutAToken()
    {
        // Arrange
        _factory.AddStaff("walt", Password, StaffRole.Manager);
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("walt", "not the password"), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal([ErrorCodes.Staff.InvalidCredentials], body!.Errors);
        Assert.Null(body.Value);
    }

    /// <summary>Equivalence: an unknown user, a wrong password and an inactive account give the very same answer.</summary>
    [Fact]
    public async Task Login_UnknownUserWrongPasswordAndInactive_GiveTheSameAnswer()
    {
        // Arrange
        _factory.AddStaff("vera", Password, StaffRole.Manager);
        _factory.AddStaff("ivan", Password, StaffRole.Manager).Deactivate();
        using var client = _factory.CreateServiceClient();

        // Act
        var unknown = await AnswerAsync(client, new SignInRequest("nobody-here", Password));
        var wrong = await AnswerAsync(client, new SignInRequest("vera", "wrong password!"));
        var inactive = await AnswerAsync(client, new SignInRequest("ivan", Password));

        // Assert
        Assert.Equal(unknown, wrong);
        Assert.Equal(unknown, inactive);
        Assert.Contains(ErrorCodes.Staff.InvalidCredentials, unknown, StringComparison.Ordinal);
    }

    /// <summary>Negative: the sign-in itself needs the token of the web application.</summary>
    [Fact]
    public async Task Login_WithoutServiceToken_ReturnsUnauthorizedWithTheServiceCode()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest("anyone", Password), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Api.Unauthorized], body!.Errors);
    }

    #endregion

    #region Throttle

    /// <summary>Boundary: five failures are answered with 401, the sixth attempt with 429, even with the right password.</summary>
    [Fact]
    public async Task Login_SixthAttemptAfterFiveFailures_ReturnsTooManyRequests()
    {
        // Arrange
        _factory.AddStaff("trudy", Password, StaffRole.Manager);
        using var client = _factory.CreateServiceClient();
        var statuses = new List<HttpStatusCode>();

        // Act
        for (var attempt = 0; attempt < 5; attempt++)
        {
            statuses.Add((await PostAsync(client, new SignInRequest("trudy", $"wrong password {attempt}"))).StatusCode);
        }

        var sixth = await PostAsync(client, new SignInRequest("trudy", Password));
        var body = await sixth.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.Equal([ErrorCodes.Staff.TooManyAttempts], body!.Errors);
    }

    /// <summary>Boundary: four failures and then the right password still sign in, and clear the count.</summary>
    [Fact]
    public async Task Login_FourFailuresThenTheRightPassword_ReturnsOk()
    {
        // Arrange
        _factory.AddStaff("pete", Password, StaffRole.Manager);
        using var client = _factory.CreateServiceClient();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await PostAsync(client, new SignInRequest("pete", $"wrong password {attempt}"));
        }

        // Act
        var response = await PostAsync(client, new SignInRequest("pete", Password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Equivalence: failures of one user name do not stop another user name.</summary>
    [Fact]
    public async Task Login_OtherUserAfterFiveFailuresOfOne_ReturnsOk()
    {
        // Arrange
        _factory.AddStaff("locked-out", Password, StaffRole.Manager);
        _factory.AddStaff("bystander", Password, StaffRole.Manager);
        using var client = _factory.CreateServiceClient();
        for (var attempt = 0; attempt < 6; attempt++)
        {
            await PostAsync(client, new SignInRequest("locked-out", $"wrong password {attempt}"));
        }

        // Act
        var response = await PostAsync(client, new SignInRequest("bystander", Password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helpers

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, SignInRequest request) =>
        client.PostAsJsonAsync(Login, request, ApiJson.Options, TestContext.Current.CancellationToken);

    private static async Task<string> AnswerAsync(HttpClient client, SignInRequest request)
    {
        using var response = await PostAsync(client, request);
        return $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}";
    }

    #endregion
}
