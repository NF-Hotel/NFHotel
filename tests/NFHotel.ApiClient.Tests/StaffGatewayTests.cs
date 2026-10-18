using System.Net;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.ApiClient.Tests;

/// <summary>How the client carries the staff token and keeps the codes of the staff sign-in (ADR-0005).</summary>
public sealed class StaffGatewayTests
{
    private static readonly Uri Base = new("http://api.test/");

    #region The staff header

    /// <summary>Positive: a call in a scope that holds a staff token sends it in the staff header.</summary>
    [Fact]
    public async Task GetAsync_WithAStaffToken_SendsTheHeader()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        var gateway = Gateway(handler, new StaffCallContext { Token = "the-staff-token" });

        // Act
        await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("the-staff-token", handler.StaffToken);
    }

    /// <summary>Equivalence: a call without a staff token, such as the public booking pages make, sends no staff header.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAsync_WithoutAStaffToken_SendsNoHeader(string? token)
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        var gateway = Gateway(handler, new StaffCallContext { Token = token });

        // Act
        await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(handler.StaffToken);
    }

    /// <summary>Negative: the sign-in never sends a staff token, even if the scope holds one, so an old token cannot ride on a new sign-in.</summary>
    [Fact]
    public async Task PostAnonymousAsync_ScopeWithAToken_SendsNoStaffHeader()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        var gateway = Gateway(handler, new StaffCallContext { Token = "old-token" });

        // Act
        await gateway.PostAnonymousAsync<Sample>("api/auth/login", new Sample(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(handler.StaffToken);
    }

    #endregion

    #region 401 and 403

    /// <summary>Positive: a 401 that carries the staff code keeps it, so the screen can ask the user to sign in again.</summary>
    [Fact]
    public async Task GetAsync_UnauthorizedWithTheStaffCode_KeepsTheStaffCode()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.Unauthorized, """{"value":null,"errors":["api.staff_unauthorized"]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.StaffUnauthorized, result.FirstError?.Code);
    }

    /// <summary>Negative: a 401 with the service code, or with an unknown code, is still the refused service token.</summary>
    [Theory]
    [InlineData("""{"value":null,"errors":["api.unauthorized"]}""")]
    [InlineData("""{"value":null,"errors":["something.else"]}""")]
    [InlineData("""{"value":null,"errors":[]}""")]
    public async Task GetAsync_UnauthorizedWithAnyOtherBody_IsTheServiceToken(string body)
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.Unauthorized, body));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unauthorized, result.FirstError?.Code);
    }

    /// <summary>Positive: a 403 carries the forbidden code through.</summary>
    [Fact]
    public async Task GetAsync_Forbidden_ReturnsTheForbiddenCode()
    {
        // Arrange
        var gateway = Gateway(StubHandler.Json(HttpStatusCode.Forbidden, """{"value":null,"errors":["api.forbidden"]}"""));

        // Act
        var result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
    }

    #endregion

    #region The sign-in

    /// <summary>Positive: the sign-in posts the credentials to the login route and returns the session.</summary>
    [Fact]
    public async Task SignInAsync_Success_PostsToTheLoginRouteAndReturnsTheSession()
    {
        // Arrange
        var handler = StubHandler.Json(
            HttpStatusCode.OK,
            """{"value":{"token":"t","expiresAt":"2030-01-01T10:00:00+00:00","userName":"rita","role":2},"errors":[]}""");
        var service = new ApiStaffAuthService(Gateway(handler));

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", "pw"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/api/auth/login", handler.PathAndQuery);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(StaffRole.Manager, result.Value.Role);
        Assert.Equal("t", result.Value.Token);
    }

    /// <summary>Negative: a 401 with the credentials code and a 429 with the throttle code keep their own codes.</summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "staff.invalid_credentials")]
    [InlineData(HttpStatusCode.TooManyRequests, "staff.too_many_attempts")]
    public async Task SignInAsync_Refused_KeepsTheCode(HttpStatusCode status, string code)
    {
        // Arrange
        var service = new ApiStaffAuthService(Gateway(StubHandler.Json(status, $$"""{"value":null,"errors":["{{code}}"]}""")));

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", "pw"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>Negative: a missing request is a programming error.</summary>
    [Fact]
    public async Task SignInAsync_NullRequest_Throws()
    {
        // Arrange
        var service = new ApiStaffAuthService(Gateway(StubHandler.Json(HttpStatusCode.OK, "{}")));

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SignInAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler, StaffCallContext? staff = null) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base }, staff);

    private sealed record Sample(int N);

    #endregion
}