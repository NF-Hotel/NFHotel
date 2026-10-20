using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The staff account endpoints through HTTP (UC-005, UC-006, ADR-0015): the own password, account management by a Manager,
/// the end of old sessions, the forced change and the limits.
/// </summary>
public sealed class StaffAccountEndpointTests : IClassFixture<ApiFactory>
{
    private const string Login = "/api/auth/login";
    private const string MyPassword = "/api/staff/me/password";
    private const string Password = "correct horse battery staple";
    private const string Fresh = "another long passphrase 42";
    private const string Temporary = "temporary passphrase 7";

    private readonly ApiFactory _factory;

    public StaffAccountEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region The own password

    /// <summary>Positive: a change gives a new session; the old token is refused, the new one works, and only the new password signs in.</summary>
    [Fact]
    public async Task ChangeOwnPassword_Valid_EndsTheOldSessionAndStartsANewOne()
    {
        // Arrange
        _factory.AddStaff("own-change", Password, StaffRole.Receptionist);
        var oldToken = await SignInAsync("own-change", Password);
        using var client = ClientWith(oldToken);

        // Act
        using var response = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest(Password, Fresh), ApiJson.Options, TestContext.Current.CancellationToken);
        var session = (await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
        var oldStatus = await StatusAsync(oldToken, "/api/rooms");
        var newStatus = await StatusAsync(session.Token, "/api/rooms");
        var oldLogin = await LoginStatusAsync("own-change", Password);
        var newLogin = await LoginStatusAsync("own-change", Fresh);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldStatus);
        Assert.Equal(HttpStatusCode.OK, newStatus);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin);
        Assert.Equal(HttpStatusCode.OK, newLogin);
    }

    /// <summary>Negative: a wrong current password gives 422 with its code, and the password stays.</summary>
    [Fact]
    public async Task ChangeOwnPassword_WrongCurrentPassword_Returns422AndKeepsThePassword()
    {
        // Arrange
        _factory.AddStaff("own-wrong", Password, StaffRole.Manager);
        using var client = ClientWith(await SignInAsync("own-wrong", Password));

        // Act
        using var response = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest("not it at all 12", Fresh), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal([ErrorCodes.Staff.CurrentPasswordInvalid], await CodesAsync(response));
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync("own-wrong", Password));
    }

    /// <summary>Boundary: five wrong current passwords are answered, the sixth try gets 429 even with the right password.</summary>
    [Fact]
    public async Task ChangeOwnPassword_SixthAttemptInTheWindow_Returns429()
    {
        // Arrange
        _factory.AddStaff("own-limit", Password, StaffRole.Housekeeping);
        using var client = ClientWith(await SignInAsync("own-limit", Password));
        var wrong = new ChangePasswordRequest("not it at all 12", Fresh);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var refused = await client.PostAsJsonAsync(MyPassword, wrong, ApiJson.Options, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        }

        // Act
        using var response = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest(Password, Fresh), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal([ErrorCodes.Staff.TooManyAttempts], await CodesAsync(response));
    }

    /// <summary>Equivalence: the new password is refused for each rule of the policy with the code of the rule.</summary>
    [Theory]
    [InlineData("short", ErrorCodes.Staff.PasswordInvalid)]
    [InlineData("correct horse battery staple", ErrorCodes.Staff.PasswordSameAsCurrent)]
    [InlineData("password1234", ErrorCodes.Staff.PasswordTooCommon)]
    public async Task ChangeOwnPassword_NewPasswordBreaksARule_Returns422WithTheCode(string fresh, string code)
    {
        // Arrange
        var name = $"own-rule-{code[(code.LastIndexOf('.') + 1)..]}";
        _factory.AddStaff(name, Password, StaffRole.Receptionist);
        using var client = ClientWith(await SignInAsync(name, Password));

        // Act
        using var response = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest(Password, fresh), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal([code], await CodesAsync(response));
    }

    /// <summary>Negative: a call without a staff token is refused before the use case.</summary>
    [Fact]
    public async Task ChangeOwnPassword_NoStaffToken_Returns401()
    {
        using var client = _factory.CreateServiceClient();
        using var response = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest(Password, Fresh), ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region The forced change

    /// <summary>State transition: an account that must change its password reaches only the change; the change then opens the rest.</summary>
    [Fact]
    public async Task MustChangeAccount_CanOnlyChangeThePassword_UntilItHas()
    {
        // Arrange
        var account = _factory.AddStaff("forced", Password, StaffRole.Manager);
        account.ResetPassword(account.PasswordHash);
        var forced = await SignInSessionAsync("forced", Password);

        // Act
        var before = await StatusAsync(forced.Token, "/api/rooms");
        using var client = ClientWith(forced.Token);
        using var change = await client.PostAsJsonAsync(MyPassword, new ChangePasswordRequest(Password, Fresh), ApiJson.Options, TestContext.Current.CancellationToken);
        var session = (await change.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
        var after = await StatusAsync(session.Token, "/api/rooms");

        // Assert
        Assert.True(forced.MustChangePassword);
        Assert.Equal(HttpStatusCode.Forbidden, before);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.False(session.MustChangePassword);
        Assert.Equal(HttpStatusCode.OK, after);
    }

    #endregion

    #region Management by a Manager

    /// <summary>Negative: a Receptionist and a Housekeeping user get 403 on every Manager endpoint.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task ManagerEndpoints_OtherRoles_Return403(StaffRole role)
    {
        // Arrange
        using var client = _factory.CreateStaffClient(role);
        var target = _factory.AccountOf(StaffRole.Manager).Id;
        var json = JsonContent.Create(new CreateStaffRequest("x", Temporary, StaffRole.Receptionist), options: ApiJson.Options);

        // Act and assert
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff/audit", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/staff", json, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/staff/{target}/disable", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/staff/{target}/enable", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/staff/{target}/password", new ResetPasswordRequest(Temporary), ApiJson.Options, TestContext.Current.CancellationToken)).StatusCode);
    }

    /// <summary>Integration: a Manager creates, disables, enables and resets; each effect shows on the sign-in and on the old token.</summary>
    [Fact]
    public async Task Manager_CreatesDisablesEnablesAndResets_AnAccount()
    {
        // Arrange
        _factory.AddStaff("boss-flow", Password, StaffRole.Manager);
        using var manager = ClientWith(await SignInAsync("boss-flow", Password));

        // Act: create
        using var created = await manager.PostAsJsonAsync("/api/staff", new CreateStaffRequest("New.Person", Temporary, StaffRole.Receptionist), ApiJson.Options, TestContext.Current.CancellationToken);
        var firstSession = await SignInSessionAsync("new.person", Temporary);
        var id = _factory.World.Staff.Items.Single(member => member.UserName == "new.person").Id;

        // Act: disable
        using var disabled = await manager.PostAsync($"/api/staff/{id}/disable", null, TestContext.Current.CancellationToken);
        var tokenAfterDisable = await StatusAsync(firstSession.Token, "/api/staff/me/none");
        var loginAfterDisable = await LoginStatusAsync("new.person", Temporary);

        // Act: enable, then reset
        using var enabled = await manager.PostAsync($"/api/staff/{id}/enable", null, TestContext.Current.CancellationToken);
        using var reset = await manager.PostAsJsonAsync($"/api/staff/{id}/password", new ResetPasswordRequest("second temporary phrase"), ApiJson.Options, TestContext.Current.CancellationToken);
        var oldTemporary = await LoginStatusAsync("new.person", Temporary);
        var newTemporary = await SignInSessionAsync("new.person", "second temporary phrase");

        // Assert
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.True(firstSession.MustChangePassword);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, tokenAfterDisable);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterDisable);
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldTemporary);
        Assert.True(newTemporary.MustChangePassword);
    }

    /// <summary>Integration: a disabled account's token is refused at once on a real endpoint.</summary>
    [Fact]
    public async Task DisabledAccount_TokenIssuedBefore_Returns401OnTheNextCall()
    {
        // Arrange
        _factory.AddStaff("boss-disable", Password, StaffRole.Manager);
        _factory.AddStaff("leaver", Password, StaffRole.Receptionist);
        var leaverToken = await SignInAsync("leaver", Password);
        using var manager = ClientWith(await SignInAsync("boss-disable", Password));
        var id = _factory.World.Staff.Items.Single(member => member.UserName == "leaver").Id;

        // Act
        var before = await StatusAsync(leaverToken, "/api/rooms");
        using var disabled = await manager.PostAsync($"/api/staff/{id}/disable", null, TestContext.Current.CancellationToken);
        var after = await StatusAsync(leaverToken, "/api/rooms");

        // Assert
        Assert.Equal(HttpStatusCode.OK, before);
        Assert.Equal(HttpStatusCode.Unauthorized, after);
    }

    /// <summary>Negative: a Manager cannot disable their own account, and a taken name is a conflict.</summary>
    [Fact]
    public async Task Manager_DisableSelfAndDuplicateName_AreConflicts()
    {
        // Arrange
        var self = _factory.AddStaff("boss-self", Password, StaffRole.Manager);
        using var manager = ClientWith(await SignInAsync("boss-self", Password));

        // Act
        using var disableSelf = await manager.PostAsync($"/api/staff/{self.Id}/disable", null, TestContext.Current.CancellationToken);
        using var duplicate = await manager.PostAsJsonAsync("/api/staff", new CreateStaffRequest("boss-self", Temporary, StaffRole.Receptionist), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, disableSelf.StatusCode);
        Assert.Equal([ErrorCodes.Staff.CannotDisableSelf], await CodesAsync(disableSelf));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    /// <summary>Negative: an unknown account is 404, and a weak temporary password is 422.</summary>
    [Fact]
    public async Task Manager_UnknownAccountAndWeakPassword_AreRefused()
    {
        // Arrange
        _factory.AddStaff("boss-refuse", Password, StaffRole.Manager);
        var someone = _factory.AddStaff("someone-refuse", Password, StaffRole.Receptionist);
        using var manager = ClientWith(await SignInAsync("boss-refuse", Password));

        // Act
        using var unknown = await manager.PostAsync("/api/staff/99999/disable", null, TestContext.Current.CancellationToken);
        using var weak = await manager.PostAsJsonAsync($"/api/staff/{someone.Id}/password", new ResetPasswordRequest("password1234"), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, weak.StatusCode);
        Assert.Equal([ErrorCodes.Staff.PasswordTooCommon], await CodesAsync(weak));
    }

    /// <summary>Integration: the list holds no hash, and the audit page shows the acts with no secret in it.</summary>
    [Fact]
    public async Task ListAndAudit_ShowTheActs_WithoutAnySecret()
    {
        // Arrange
        _factory.AddStaff("boss-audit", Password, StaffRole.Manager);
        using var manager = ClientWith(await SignInAsync("boss-audit", Password));
        using var created = await manager.PostAsJsonAsync("/api/staff", new CreateStaffRequest("audited", Temporary, StaffRole.Housekeeping), ApiJson.Options, TestContext.Current.CancellationToken);

        // Act
        var listText = await manager.GetStringAsync("/api/staff", TestContext.Current.CancellationToken);
        var auditText = await manager.GetStringAsync("/api/staff/audit?page=1&pageSize=200", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("audited", listText, StringComparison.Ordinal);
        Assert.Contains("\"targetName\":\"audited\"", auditText, StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"Success\"", auditText, StringComparison.Ordinal);
        foreach (var secret in new[] { Password, Temporary, Fresh, "fake-hash", "passwordHash" })
        {
            Assert.DoesNotContain(secret, listText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secret, auditText, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Boundary: the 31st write of one Manager in a minute gets 429; another Manager is not affected.</summary>
    [Fact]
    public async Task ManagerWrites_ThirtyFirstInAMinute_Returns429()
    {
        // Arrange
        var target = _factory.AddStaff("rate-target", Password, StaffRole.Receptionist);
        _factory.AddStaff("rate-boss", Password, StaffRole.Manager);
        _factory.AddStaff("rate-other", Password, StaffRole.Manager);
        using var boss = ClientWith(await SignInAsync("rate-boss", Password));
        using var other = ClientWith(await SignInAsync("rate-other", Password));
        for (var write = 0; write < 30; write++)
        {
            using var allowed = await boss.PostAsync($"/api/staff/{target.Id}/enable", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        // Act
        using var limited = await boss.PostAsync($"/api/staff/{target.Id}/enable", null, TestContext.Current.CancellationToken);
        using var untouched = await other.PostAsync($"/api/staff/{target.Id}/enable", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal([ErrorCodes.Staff.TooManyRequests], await CodesAsync(limited));
        Assert.Equal(HttpStatusCode.OK, untouched.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<StaffSessionDto> SignInSessionAsync(string userName, string password)
    {
        using var client = _factory.CreateServiceClient();
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest(userName, password), ApiJson.Options, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<StaffSessionDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
    }

    private async Task<string> SignInAsync(string userName, string password) => (await SignInSessionAsync(userName, password)).Token;

    private async Task<HttpStatusCode> LoginStatusAsync(string userName, string password)
    {
        using var client = _factory.CreateServiceClient();
        using var response = await client.PostAsJsonAsync(Login, new SignInRequest(userName, password), ApiJson.Options, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private HttpClient ClientWith(string staffToken)
    {
        var client = _factory.CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, staffToken);
        return client;
    }

    private async Task<HttpStatusCode> StatusAsync(string staffToken, string path)
    {
        using var client = ClientWith(staffToken);
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static async Task<string[]> CodesAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        return [.. body!.Errors];
    }

    #endregion
}
