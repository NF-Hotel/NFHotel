using System.Net;

using NFHotel.Application.Dto.Staff;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.ApiClient.Tests;

/// <summary>The staff account calls over HTTP: the routes, the methods, the staff token and the codes that survive (ADR-0017).</summary>
public sealed class StaffAdminGatewayTests
{
    private static readonly Uri Base = new("http://api.test/");

    #region Routes

    /// <summary>Positive: the list is a GET of the staff route and reads the accounts.</summary>
    [Fact]
    public async Task ListAsync_Success_GetsTheStaffRouteWithTheStaffToken()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":[{"id":3,"userName":"rita","role":1,"isActive":true,"mustChangePassword":false}],"errors":[]}""");
        var service = new ApiStaffAdminService(Gateway(handler, new StaffCallContext { Token = "staff-token" }));

        // Act
        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/api/staff", handler.PathAndQuery);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("staff-token", handler.StaffToken);
        Assert.Equal("rita", Assert.Single(result.Value).UserName);
    }

    /// <summary>Equivalence: each action is a POST to its own route.</summary>
    [Theory]
    [InlineData("disable", "/api/staff/7/disable")]
    [InlineData("enable", "/api/staff/7/enable")]
    [InlineData("reset", "/api/staff/7/password")]
    public async Task Action_Success_PostsToItsRoute(string action, string path)
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"errors":[]}""");
        var service = new ApiStaffAdminService(Gateway(handler));

        // Act
        var result = action switch
        {
            "disable" => await service.DisableAsync(7, TestContext.Current.CancellationToken),
            "enable" => await service.EnableAsync(7, TestContext.Current.CancellationToken),
            _ => await service.ResetPasswordAsync(7, new ResetPasswordRequest("a temporary phrase"), TestContext.Current.CancellationToken),
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(path, handler.PathAndQuery);
        Assert.Equal(HttpMethod.Post, handler.Method);
    }

    /// <summary>Positive: a create posts the request, and the audit read asks for the page and its size.</summary>
    [Fact]
    public async Task CreateAndAudit_Success_UseTheStaffRoutes()
    {
        // Arrange
        var created = StubHandler.Json(HttpStatusCode.OK, """{"errors":[]}""");
        var audit = StubHandler.Json(HttpStatusCode.OK, """{"value":{"items":[],"page":2,"pageSize":50,"total":0},"errors":[]}""");

        // Act
        var createResult = await new ApiStaffAdminService(Gateway(created)).CreateAsync(new CreateStaffRequest("lena", "a temporary phrase", StaffRole.Receptionist), TestContext.Current.CancellationToken);
        var auditResult = await new ApiStaffAdminService(Gateway(audit)).ListAuditAsync(2, 50, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(createResult.IsSuccess);
        Assert.Equal("/api/staff", created.PathAndQuery);
        Assert.Contains("\"userName\":\"lena\"", created.Body, StringComparison.Ordinal);
        Assert.Equal("/api/staff/audit?page=2&pageSize=50", audit.PathAndQuery);
        Assert.Equal(2, auditResult.Value.Page);
    }

    /// <summary>Positive: the own password change posts to its route with the staff token and returns the new session.</summary>
    [Fact]
    public async Task ChangePasswordAsync_Success_PostsToTheOwnPasswordRoute()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"token":"new","expiresAt":"2030-01-01T10:00:00+00:00","userName":"rita","role":1,"mustChangePassword":false},"errors":[]}""");
        var service = new ApiStaffAuthService(Gateway(handler, new StaffCallContext { Token = "old" }));

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest("current one", "the new one"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/api/staff/me/password", handler.PathAndQuery);
        Assert.Equal("old", handler.StaffToken);
        Assert.Equal("new", result.Value.Token);
    }

    #endregion

    #region Refusals and edges

    /// <summary>Negative: the codes of a refusal keep their own text, and 401 and 429 are not turned into a service-token fault.</summary>
    [Theory]
    [InlineData(HttpStatusCode.Conflict, "staff.last_manager")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "staff.password_too_common")]
    [InlineData(HttpStatusCode.Unauthorized, "api.staff_unauthorized")]
    [InlineData(HttpStatusCode.TooManyRequests, "staff.too_many_requests")]
    public async Task DisableAsync_Refused_KeepsTheCode(HttpStatusCode status, string code)
    {
        // Arrange
        var service = new ApiStaffAdminService(Gateway(StubHandler.Json(status, $$"""{"errors":["{{code}}"]}""")));

        // Act
        var result = await service.DisableAsync(3, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>The client forwards the address of the browser to the API in the forwarded-for header.</summary>
    [Fact]
    public async Task ListAsync_WithAClientAddress_ForwardsIt()
    {
        // Arrange
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"value":[],"errors":[]}""");
        var service = new ApiStaffAdminService(Gateway(handler, new StaffCallContext { ClientAddress = "203.0.113.7" }));

        // Act
        await service.ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("203.0.113.7", handler.ForwardedFor);
    }

    /// <summary>Negative: a key below one and a missing request are programming errors.</summary>
    [Fact]
    public async Task Calls_BadArguments_Throw()
    {
        var service = new ApiStaffAdminService(Gateway(StubHandler.Json(HttpStatusCode.OK, """{"errors":[]}""")));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DisableAsync(0, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ResetPasswordAsync(1, null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler, StaffCallContext? staff = null) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base }, staff);

    #endregion
}
