using System.Net;

namespace NFHotel.Web.Tests;

/// <summary>
/// The back office behind the sign-in (ADR-0005), through the real pipeline of the web application: pages redirect to
/// the login without a session, the cookie holds the session, and a role sees only what it may.
/// </summary>
public sealed class AdminAccessTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    public AdminAccessTests(WebFactory factory)
    {
        _factory = factory;
    }

    #region Without a session

    /// <summary>Negative: every back office page redirects to the login page, with the page as return address, and shows nothing.</summary>
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/bookings")]
    [InlineData("/admin/rooms")]
    [InlineData("/admin/guests")]
    [InlineData("/admin/revenue")]
    [InlineData("/admin/benchmark")]
    public async Task Get_AdminPageWithoutSession_RedirectsToTheLogin(string path)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/admin/login?returnUrl={Uri.EscapeDataString(path)}", LocalPath(response));
    }

    /// <summary>Equivalence: the public pages need no sign-in.</summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/book")]
    [InlineData("/admin/login")]
    public async Task Get_PublicPage_ReturnsOkWithoutSession(string path)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>The login page no longer warns that access control is missing, and has the form.</summary>
    [Fact]
    public async Task Get_LoginPage_HasTheFormAndNoWarning()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        var html = await browser.GetStringAsync("/admin/login", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("id=\"login-user\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"login-password\"", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Access control is not in place", html, StringComparison.Ordinal);
    }

    #endregion

    #region Signing in

    /// <summary>Positive: the right credentials set an HttpOnly cookie and go to the landing page of the role.</summary>
    [Theory]
    [InlineData("rita", "/admin")]
    [InlineData("mona", "/admin")]
    [InlineData("hilda", "/admin/rooms")]
    public async Task SignIn_RightCredentials_SetsTheCookieAndGoesToTheLandingPage(string user, string landing)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, user, WebFactory.Password);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("nfhotel.staff=", StringComparison.Ordinal));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(landing, LocalPath(response));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The staff token is inside the encrypted cookie only; it is not in the cookie text or in the page.</summary>
    [Fact]
    public async Task SignIn_RightCredentials_DoesNotLeakTheStaffToken()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        var cookies = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        var page = await browser.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(WebFactory.StaffToken, cookies, StringComparison.Ordinal);
        Assert.DoesNotContain(WebFactory.StaffToken, page, StringComparison.Ordinal);
    }

    /// <summary>Negative: a wrong password shows the refusal on the login page, sets no session cookie and gives access to nothing.</summary>
    [Fact]
    public async Task SignIn_WrongPassword_ShowsTheRefusalAndSetsNoSession()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, "mona", "wrong password!");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var afterwards = await browser.GetAsync("/admin", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The user name or the password is wrong.", html, StringComparison.Ordinal);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(value => value.StartsWith("nfhotel.staff=", StringComparison.Ordinal)));
        Assert.Equal(HttpStatusCode.Redirect, afterwards.StatusCode);
    }

    /// <summary>Equivalence: after the sign-in, the page that was asked for is where the user lands.</summary>
    [Fact]
    public async Task SignIn_WithAReturnAddress_GoesThere()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, "rita", WebFactory.Password, "/admin/guests");

        // Assert
        Assert.Equal("/admin/guests", LocalPath(response));
    }

    /// <summary>Negative: a return address that leaves the site or the back office is ignored.</summary>
    [Theory]
    [InlineData("https://evil.example/admin")]
    [InlineData("//evil.example")]
    [InlineData("/book")]
    public async Task SignIn_WithAForeignReturnAddress_GoesToTheLandingPage(string returnUrl)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await WebFactory.SignInAsync(browser, "rita", WebFactory.Password, returnUrl);

        // Assert
        Assert.Equal("/admin", LocalPath(response));
    }

    #endregion

    #region Roles

    /// <summary>Positive: a signed-in Manager opens the revenue page.</summary>
    [Fact]
    public async Task Get_RevenueAsManager_ShowsThePage()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        using var response = await browser.GetAsync("/admin/revenue", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The system has no pricing model.", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a Receptionist is told the page is not permitted, and does not see its content.</summary>
    [Fact]
    public async Task Get_RevenueAsReceptionist_ShowsNotPermitted()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "rita", WebFactory.Password);

        // Act
        var html = await GetFollowingOneRedirectAsync(browser, "/admin/revenue");

        // Assert
        Assert.Contains("You don't have access to this page", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The system has no pricing model.", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a signed-in Manager opens the benchmark page.</summary>

    [Fact]

    public async Task Get_BenchmarkAsManager_ShowsThePage()

    {

        // Arrange

        using var browser = _factory.CreateBrowser();

        await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);



        // Act

        using var response = await browser.GetAsync("/admin/benchmark", TestContext.Current.CancellationToken);

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);



        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("Benchmark runs", html, StringComparison.Ordinal);

    }



    /// <summary>Negative: a Receptionist is told the benchmark page is not permitted, and does not see its content.</summary>

    [Fact]

    public async Task Get_BenchmarkAsReceptionist_ShowsNotPermitted()

    {

        // Arrange

        using var browser = _factory.CreateBrowser();

        await WebFactory.SignInAsync(browser, "rita", WebFactory.Password);



        // Act

        var html = await GetFollowingOneRedirectAsync(browser, "/admin/benchmark");



        // Assert

        Assert.Contains("You don't have access to this page", html, StringComparison.Ordinal);

        Assert.DoesNotContain("Every finished run of the local models", html, StringComparison.Ordinal);

    }



    /// <summary>Negative: Housekeeping is told the bookings, the guests and the dashboard are not permitted.</summary>
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/bookings")]
    [InlineData("/admin/guests")]
    [InlineData("/admin/revenue")]
    [InlineData("/admin/benchmark")]
    public async Task Get_PageAsHousekeeping_ShowsNotPermitted(string path)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "hilda", WebFactory.Password);

        // Act
        var html = await GetFollowingOneRedirectAsync(browser, path);

        // Assert
        Assert.Contains("You don't have access to this page", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: Housekeeping opens the rooms page, and the menu shows only the rooms.</summary>
    [Fact]
    public async Task Get_RoomsAsHousekeeping_ShowsThePageAndOnlyTheRoomsMenuItem()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "hilda", WebFactory.Password);

        // Act
        var html = await browser.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("href=\"/admin/rooms\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/guests\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/bookings\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/revenue\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/admin/benchmark\"", html, StringComparison.Ordinal);
        Assert.Contains("hilda", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a Manager sees every menu item.</summary>
    [Fact]
    public async Task Get_RoomsAsManager_ShowsEveryMenuItem()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        var html = await browser.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.All(
            new[] { "/admin\"", "/admin/bookings", "/admin/rooms", "/admin/guests", "/admin/revenue", "/admin/benchmark" },
            item => Assert.Contains($"href=\"{item}", html, StringComparison.Ordinal));
    }

    #endregion

    #region The denied page

    /// <summary>Negative: a role that lacks the permission is sent to the denied page, not to the login page.</summary>
    [Fact]
    public async Task Get_PageWithoutThePermission_RedirectsToTheDeniedPage()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "hilda", WebFactory.Password);

        // Act
        using var response = await browser.GetAsync("/admin/guests", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/admin/denied", LocalPath(response), StringComparison.Ordinal);
    }

    /// <summary>The denied page offers the start page of the role, and an anonymous visitor goes to the login instead.</summary>
    [Fact]
    public async Task Get_DeniedPage_OffersTheStartPageOfTheRoleAndRedirectsAnonymous()
    {
        // Arrange
        using var anonymous = _factory.CreateBrowser();
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "hilda", WebFactory.Password);

        // Act
        using var forAnonymous = await anonymous.GetAsync("/admin/denied", TestContext.Current.CancellationToken);
        var html = await browser.GetStringAsync("/admin/denied", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, forAnonymous.StatusCode);
        Assert.StartsWith("/admin/login", LocalPath(forAnonymous), StringComparison.Ordinal);
        Assert.Contains("href=\"/admin/rooms\"", html, StringComparison.Ordinal);
    }

    #endregion

    #region Signing out

    /// <summary>State transition: signing out ends the session, and the pages redirect again.</summary>
    [Fact]
    public async Task SignOut_AfterSignIn_EndsTheSession()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        var page = await browser.GetStringAsync("/admin/rooms", TestContext.Current.CancellationToken);
        var form = new FormUrlEncodedContent(WebFactory.HiddenFields(page));

        // Act
        using var signOut = await browser.PostAsync("/admin/logout", form, TestContext.Current.CancellationToken);
        using var afterwards = await browser.GetAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);
        Assert.Equal("/admin/login", LocalPath(signOut));
        Assert.Equal(HttpStatusCode.Redirect, afterwards.StatusCode);
    }

    /// <summary>Negative: a sign-out request without the antiforgery token is refused, so another site cannot sign a user out.</summary>
    [Fact]
    public async Task SignOut_WithoutTheAntiforgeryToken_IsRefused()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();
        await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        using var signOut = await browser.PostAsync("/admin/logout", new FormUrlEncodedContent(new Dictionary<string, string>()), TestContext.Current.CancellationToken);
        using var stillSignedIn = await browser.GetAsync("/admin/revenue", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    #endregion

    #region Helpers

    private static string LocalPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location ?? throw new InvalidOperationException("The answer has no Location.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }

    private static async Task<string> GetFollowingOneRedirectAsync(HttpClient browser, string path)
    {
        using var first = await browser.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        return await browser.GetStringAsync(LocalPath(first), TestContext.Current.CancellationToken);
    }

    #endregion
}
