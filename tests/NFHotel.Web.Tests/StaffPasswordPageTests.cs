using System.Net;

namespace NFHotel.Web.Tests;

/// <summary>
/// The change-password page, the forced change and the Manager pages through the real pipeline of the web application
/// (UC-005, UC-006, ADR-0017).
/// </summary>
public sealed class StaffPasswordPageTests : IClassFixture<WebFactory>
{
    private const string ChangePath = "/admin/account/password";

    private readonly WebFactory _factory;

    public StaffPasswordPageTests(WebFactory factory)
    {
        _factory = factory;
    }

    #region The forced change

    /// <summary>State transition: an account that must change its password is sent to the change page from every back office page.</summary>
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/rooms")]
    [InlineData("/admin/staff")]
    public async Task Get_AdminPageWhileMustChange_RedirectsToTheChangePage(string path)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, WebFactory.MustChangeUser, WebFactory.Password);

        // Act
        using var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(ChangePath, LocalPath(response));
    }

    /// <summary>Positive: the change page opens for that account and says why.</summary>
    [Fact]
    public async Task Get_ChangePageWhileMustChange_ShowsTheNotice()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, WebFactory.MustChangeUser, WebFactory.Password);

        // Act
        var html = await browser.GetStringAsync(ChangePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Choose a new password to continue.", html, StringComparison.Ordinal);
    }

    /// <summary>Integration: a successful change replaces the cookie, so the back office opens afterwards.</summary>
    [Fact]
    public async Task Post_ChangeWithTheRightPassword_OpensTheBackOffice()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, WebFactory.MustChangeUser, WebFactory.Password);

        // Act
        using var changed = await PostChangeAsync(browser, WebFactory.Password, WebFactory.NewPassword, WebFactory.NewPassword);
        using var after = await browser.GetAsync("/admin", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        Assert.Equal("/admin", LocalPath(changed));
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    #endregion

    #region Refusals

    /// <summary>Negative: a wrong current password shows its text and keeps the user on the page, with no password echoed back.</summary>
    [Fact]
    public async Task Post_ChangeWithAWrongCurrentPassword_ShowsTheRefusal()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        using var response = await PostChangeAsync(browser, "not the password", WebFactory.NewPassword, WebFactory.NewPassword);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The current password is wrong.", html, StringComparison.Ordinal);
        Assert.DoesNotContain(WebFactory.NewPassword, html, StringComparison.Ordinal);
    }

    /// <summary>Negative: two different new passwords are refused by the form before any call.</summary>
    [Fact]
    public async Task Post_ChangeWithDifferentRepeat_ShowsTheValidationText()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        using var response = await PostChangeAsync(browser, WebFactory.Password, WebFactory.NewPassword, "something else entirely");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("The two new passwords are not the same.", html, StringComparison.Ordinal);
    }

    #endregion

    #region Who may open what

    /// <summary>Equivalence: every role may open the change page.</summary>
    [Theory]
    [InlineData("rita")]
    [InlineData("mona")]
    [InlineData("hilda")]
    public async Task Get_ChangePage_IsOpenToEveryRole(string user)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, user, WebFactory.Password);

        // Act
        using var response = await browser.GetAsync(ChangePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Negative: without a session the change page redirects to the login.</summary>
    [Fact]
    public async Task Get_ChangePageWithoutSession_RedirectsToTheLogin()
    {
        using var browser = _factory.CreateBrowser();
        using var response = await browser.GetAsync(ChangePath, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/admin/login", LocalPath(response), StringComparison.Ordinal);
    }

    /// <summary>Negative: a Receptionist and a Housekeeping user are told that the role has no access to Staff and Audit.</summary>
    [Theory]
    [InlineData("rita", "/admin/staff")]
    [InlineData("rita", "/admin/staff/audit")]
    [InlineData("hilda", "/admin/staff")]
    [InlineData("hilda", "/admin/staff/audit")]
    public async Task Get_StaffPagesAsAnotherRole_RedirectsToAccessDenied(string user, string path)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        using var signedIn = await WebFactory.SignInAsync(browser, user, WebFactory.Password);

        // Act
        using var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/admin/denied", LocalPath(response), StringComparison.Ordinal);
    }

    /// <summary>Positive: a Manager opens both pages and sees the menu items; a Receptionist sees neither.</summary>
    [Fact]
    public async Task Get_StaffPagesAsAManager_OpenAndShowTheMenu()
    {
        // Arrange
        using var manager = _factory.CreateBrowser();
        using var receptionist = _factory.CreateBrowser();
        using var managerSignIn = await WebFactory.SignInAsync(manager, "mona", WebFactory.Password);
        using var receptionistSignIn = await WebFactory.SignInAsync(receptionist, "rita", WebFactory.Password);

        // Act
        using var staff = await manager.GetAsync("/admin/staff", TestContext.Current.CancellationToken);
        using var audit = await manager.GetAsync("/admin/staff/audit", TestContext.Current.CancellationToken);
        var managerMenu = await manager.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);
        var receptionistMenu = await receptionist.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, staff.StatusCode);
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.Contains("href=\"/admin/staff\"", managerMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/staff\"", receptionistMenu, StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/account/password\"", receptionistMenu, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static string LocalPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location ?? throw new InvalidOperationException("The answer has no Location.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }

    private static async Task<HttpResponseMessage> PostChangeAsync(HttpClient browser, string current, string fresh, string repeat)
    {
        var html = await browser.GetStringAsync(ChangePath, TestContext.Current.CancellationToken);
        var fields = WebFactory.HiddenFields(html);
        fields[WebFactory.InputName(html, "current-password")] = current;
        fields[WebFactory.InputName(html, "new-password")] = fresh;
        fields[WebFactory.InputName(html, "repeat-password")] = repeat;
        return await browser.PostAsync(ChangePath, new FormUrlEncodedContent(fields), TestContext.Current.CancellationToken);
    }

    #endregion
}
