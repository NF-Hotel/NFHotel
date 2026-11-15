using System.Net;

using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The guest pages through the real pipeline of the web application (MIL-018, ADR-0047 decision 6, UC-010): the sign-in
/// on <c>/booking</c>, the guest cookie, the own booking on <c>/my-booking</c> with its two changes, the session from the
/// booking flow, and the sign-out. The API is a substitute; what reaches it is recorded.
/// </summary>
public sealed class GuestPagesTests : IClassFixture<GuestWebFactory>
{
    private const string GuestCookie = "nfhotel.guest=";

    private readonly GuestWebFactory _factory;

    public GuestPagesTests(GuestWebFactory factory)
    {
        _factory = factory;
    }

    #region The sign-in and the cookie

    /// <summary>
    /// Positive: the number and the email set the guest cookie, HttpOnly, SameSite=Lax, only for the own booking pages,
    /// expiring with the guest token, and the browser goes to the own booking. The cookie does not show the token.
    /// </summary>
    [Fact]
    public async Task SignIn_RightNumberAndEmail_SetsTheGuestCookieAndGoesToTheOwnBooking()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, GuestWebFactory.Email);
        string cookie = SetCookie(response, GuestCookie);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(GuestSignInRegistration.MyBookingPath, LocalPath(response));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/my-booking", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=Fri, 01 Jan 2100 00:00:00 GMT", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(GuestWebFactory.GuestToken, cookie, StringComparison.Ordinal);
    }

    /// <summary>Negative: a wrong email shows the one refusal on the page, and sets no guest cookie.</summary>
    [Fact]
    public async Task SignIn_WrongEmail_ShowsTheRefusalAndSetsNoCookie()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, "bo@example.com");
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Check the booking number and the email you booked with.", html, StringComparison.Ordinal);
        Assert.False(HasSetCookie(response, GuestCookie));
    }

    /// <summary>Positive (MIL-018 criterion 5, in process): behind the TLS proxy the guest cookie is Secure as well as HttpOnly.</summary>
    [Fact]
    public async Task SignIn_BehindTheProxy_SetsASecureHttpOnlyGuestCookie()
    {
        // Arrange
        using TlsGuestFactory factory = new();
        using HttpClient browser = factory.CreateBrowser();
        browser.BaseAddress = new Uri("https://localhost/");
        browser.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        // Act
        using HttpResponseMessage response = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, GuestWebFactory.Email);
        string cookie = SetCookie(response, GuestCookie);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("secure", cookie.Replace("httponly", string.Empty, StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region The own booking

    /// <summary>Negative: without the guest cookie the own booking page sends the browser to the sign-in.</summary>
    [Fact]
    public async Task MyBooking_WithoutTheGuestCookie_GoesToTheSignIn()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await browser.GetAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(GuestSignInRegistration.SignInPath, LocalPath(response));
    }

    /// <summary>Security: a staff session is no guest session; the own booking page sends a member of staff to the sign-in.</summary>
    [Fact]
    public async Task MyBooking_StaffCookieOnly_GoesToTheSignIn()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        using HttpResponseMessage staff = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);

        // Act
        using HttpResponseMessage response = await browser.GetAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, staff.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(GuestSignInRegistration.SignInPath, LocalPath(response));
    }

    /// <summary>
    /// Security: the signed-in guest sees the own booking, and the page speaks to the API as the guest and only as the
    /// guest, even in a browser that is also signed in to the back office. The token never reaches the page.
    /// </summary>
    [Fact]
    public async Task MyBooking_GuestAndStaffInOneBrowser_ShowsTheBookingAndSpeaksOnlyAsTheGuest()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        using HttpResponseMessage staff = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        using HttpResponseMessage guest = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, GuestWebFactory.Email);
        _factory.Callers.Clear();

        // Act
        using HttpResponseMessage response = await browser.GetAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(GuestWebFactory.BookingNumber, html, StringComparison.Ordinal);
        Assert.Contains("Anna Jensen", html, StringComparison.Ordinal);
        Assert.Contains("Cancel my booking", html, StringComparison.Ordinal);
        Assert.Contains("Save the new dates", html, StringComparison.Ordinal);
        Assert.DoesNotContain(GuestWebFactory.GuestToken, html, StringComparison.Ordinal);
        Assert.Equal([((string?)null, (string?)GuestWebFactory.GuestToken)], _factory.Callers);
    }

    /// <summary>
    /// Positive: the cancel form cancels the booking in the cookie and sends the browser back with the result in the
    /// address, so a reload shows it and does not post again.
    /// </summary>
    [Fact]
    public async Task Cancel_SignedInGuest_CancelsTheBookingAndRedirectsWithTheResult()
    {
        // Arrange
        using HttpClient browser = await SignedInGuestAsync();
        Dictionary<string, string> fields = await FormFieldsAsync(browser, "guest-cancel");
        _factory.Bookings.ClearReceivedCalls();

        // Act
        using FormUrlEncodedContent form = new(fields);
        using HttpResponseMessage response = await browser.PostAsync(GuestSignInRegistration.MyBookingPath, form, TestContext.Current.CancellationToken);
        string notice = await browser.GetStringAsync(LocalPath(response), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/my-booking?done=cancelled", LocalPath(response));
        await _factory.Bookings.Received(1).CancelAsync(GuestWebFactory.BookingId, Arg.Any<CancellationToken>());
        Assert.Contains("Your booking is cancelled.", notice, StringComparison.Ordinal);
    }

    /// <summary>Positive: the change form moves the booking in the cookie to the new dates, in its own room.</summary>
    [Fact]
    public async Task Reschedule_SignedInGuest_AsksForTheNewDatesInTheSameRoom()
    {
        // Arrange
        using HttpClient browser = await SignedInGuestAsync();
        string html = await browser.GetStringAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);
        Dictionary<string, string> fields = await FormFieldsAsync(browser, "guest-reschedule");
        fields[WebFactory.InputName(html, "change-check-in")] = "2026-04-20";
        fields[WebFactory.InputName(html, "change-check-out")] = "2026-04-23";
        _factory.Bookings.ClearReceivedCalls();

        // Act
        using FormUrlEncodedContent form = new(fields);
        using HttpResponseMessage response = await browser.PostAsync(GuestSignInRegistration.MyBookingPath, form, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/my-booking?done=rescheduled", LocalPath(response));
        await _factory.Bookings.Received(1).RescheduleAsync(
            new RescheduleBookingRequest(GuestWebFactory.BookingId, new DateOnly(2026, 4, 20), new DateOnly(2026, 4, 23), GuestWebFactory.RoomId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Security: the guest cookie alone opens no back office page; the back office asks for the staff sign-in.</summary>
    [Fact]
    public async Task AdminPage_GuestCookieOnly_RedirectsToTheStaffLogin()
    {
        // Arrange
        using HttpClient browser = await SignedInGuestAsync();

        // Act
        using HttpResponseMessage response = await browser.GetAsync("/admin", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/admin/login", LocalPath(response), StringComparison.Ordinal);
    }

    #endregion

    #region The session from the booking flow, and the sign-out

    /// <summary>
    /// Positive: a ticket of the booking flow, posted with the antiforgery token, sets the guest cookie once; the same
    /// ticket again sends the browser to the sign-in.
    /// </summary>
    [Fact]
    public async Task SessionTicket_FromTheBookingFlow_SetsTheGuestCookieOnce()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        string ticket = _factory.Services.GetRequiredService<GuestSessionTickets>()
            .Issue(new GuestSessionDto(GuestWebFactory.GuestToken, GuestWebFactory.ExpiresAt, GuestWebFactory.BookingId));
        Dictionary<string, string> fields = await AntiforgeryFieldAsync(browser);
        fields[GuestSignInRegistration.TicketField] = ticket;

        // Act
        using FormUrlEncodedContent first = new(fields);
        using HttpResponseMessage started = await browser.PostAsync(GuestSignInRegistration.SessionPath, first, TestContext.Current.CancellationToken);
        using FormUrlEncodedContent second = new(fields);
        using HttpResponseMessage again = await browser.PostAsync(GuestSignInRegistration.SessionPath, second, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(GuestSignInRegistration.MyBookingPath, LocalPath(started));
        Assert.Contains("httponly", SetCookie(started, GuestCookie), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(GuestSignInRegistration.SignInPath, LocalPath(again));
    }

    /// <summary>Security: the session and the sign-out endpoints refuse a post without the antiforgery token.</summary>
    /// <param name="path">The endpoint.</param>
    [Theory]
    [InlineData(GuestSignInRegistration.SessionPath)]
    [InlineData(GuestSignInRegistration.SignOutPath)]
    public async Task GuestEndpoint_PostWithoutAntiforgery_ReturnsBadRequest(string path)
    {
        // Arrange
        using HttpClient browser = await SignedInGuestAsync();
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { [GuestSignInRegistration.TicketField] = "x" });

        // Act
        using HttpResponseMessage response = await browser.PostAsync(path, form, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Positive: the sign-out ends the guest session only; a member of staff signed in in the same browser stays signed
    /// in to the back office (ADR-0047 decision 6).
    /// </summary>
    [Fact]
    public async Task SignOut_GuestAndStaffInOneBrowser_EndsOnlyTheGuestSession()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        using HttpResponseMessage staff = await WebFactory.SignInAsync(browser, "mona", WebFactory.Password);
        using HttpResponseMessage guest = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, GuestWebFactory.Email);
        Dictionary<string, string> fields = await AntiforgeryFieldAsync(browser);

        // Act
        using FormUrlEncodedContent form = new(fields);
        using HttpResponseMessage signedOut = await browser.PostAsync(GuestSignInRegistration.SignOutPath, form, TestContext.Current.CancellationToken);
        using HttpResponseMessage ownBooking = await browser.GetAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);
        using HttpResponseMessage backOffice = await browser.GetAsync("/admin/rooms", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(GuestSignInRegistration.SignInPath, LocalPath(signedOut));
        Assert.Contains("expires=Thu, 01 Jan 1970", SetCookie(signedOut, GuestCookie), StringComparison.OrdinalIgnoreCase);
        Assert.False(HasSetCookie(signedOut, "nfhotel.staff="));
        Assert.Equal(GuestSignInRegistration.SignInPath, LocalPath(ownBooking));
        Assert.Equal(HttpStatusCode.OK, backOffice.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<HttpClient> SignedInGuestAsync()
    {
        HttpClient browser = _factory.CreateBrowser();
        using HttpResponseMessage response = await GuestWebFactory.SignInAsGuestAsync(browser, GuestWebFactory.BookingNumber, GuestWebFactory.Email);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return browser;
    }

    /// <summary>The hidden fields of the own booking page, with the name of the form to post.</summary>
    private static async Task<Dictionary<string, string>> FormFieldsAsync(HttpClient browser, string formName)
    {
        string html = await browser.GetStringAsync(GuestSignInRegistration.MyBookingPath, TestContext.Current.CancellationToken);
        Dictionary<string, string> fields = WebFactory.HiddenFields(html);
        fields["_handler"] = formName;
        return fields;
    }

    /// <summary>The antiforgery field of a page of the browser, for the two endpoints that are not pages.</summary>
    private static async Task<Dictionary<string, string>> AntiforgeryFieldAsync(HttpClient browser)
    {
        string html = await browser.GetStringAsync(GuestSignInRegistration.SignInPath, TestContext.Current.CancellationToken);
        Dictionary<string, string> hidden = WebFactory.HiddenFields(html);
        return new Dictionary<string, string> { ["__RequestVerificationToken"] = hidden["__RequestVerificationToken"] };
    }

    /// <summary>The address a redirect points to, without the scheme and the host of the test server.</summary>
    private static string LocalPath(HttpResponseMessage response)
    {
        Uri location = response.Headers.Location ?? throw new InvalidOperationException("The answer has no Location.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }

    private static bool HasSetCookie(HttpResponseMessage response, string prefix) =>
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
        && cookies.Any(value => value.StartsWith(prefix, StringComparison.Ordinal));

    private static string SetCookie(HttpResponseMessage response, string prefix) =>
        response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>The guest pages behind the TLS proxy, as in the deployment (ADR-0006).</summary>
    private sealed class TlsGuestFactory : GuestWebFactory
    {
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new(HttpsRequirement.SettingName, "true"),
            new("FORWARDEDHEADERS_ENABLED", "true"),
        ];
    }

    #endregion
}
