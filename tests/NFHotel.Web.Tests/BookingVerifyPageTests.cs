using System.Net;
using System.Text.RegularExpressions;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The page of the link in the mail of a booking request, <c>/booking/verify</c>, through the real pipeline of the web
/// application (MIL-088, ADR-0052 decisions 3 and 5): opening the link changes nothing, only the button with the
/// antiforgery token sends the token, in the body, and a room booked signs the guest in and shows the receipt. The API is
/// a substitute; what reaches it is recorded.
/// </summary>
public sealed partial class BookingVerifyPageTests : IClassFixture<BookingRequestWebFactory>
{
    #region Setup

    private const string GuestCookie = "nfhotel.guest=";

    /// <summary>The one text of a link that does not work.</summary>
    private const string LinkInvalidText = "This link doesn't work any more. Ask for the booking again, and we'll email you a new link.";

    private readonly BookingRequestWebFactory _factory;

    public BookingVerifyPageTests(BookingRequestWebFactory factory)
    {
        _factory = factory;
    }

    private static CancellationToken Call => TestContext.Current.CancellationToken;

    #endregion

    #region Opening the link

    /// <summary>
    /// Security (ADR-0052 decision 3): opening the link shows the page, not the redirect of an old address of the sign-in,
    /// with one form whose button is disabled until the script has the token, and calls no one; so a mail scanner that
    /// opens the link books nothing.
    /// </summary>
    [Fact]
    public async Task Get_Opened_ShowsTheDisabledFormAndCallsNothing()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        _factory.Requests.ClearReceivedCalls();

        // Act
        using HttpResponseMessage response = await browser.GetAsync(GuestSignInRegistration.VerifyPath, Call);
        string html = await response.Content.ReadAsStringAsync(Call);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(FormTag().Matches(html));
        Assert.Matches(DisabledButton(), html);
        Assert.Equal(string.Empty, WebFactory.InputAttribute(html, "booking-verify-token", "value"));
        Assert.Empty(_factory.Requests.ReceivedCalls());
    }

    /// <summary>
    /// Positive (ADR-0052 decision 3): the page says why it needs script, and loads its script from a file of the site,
    /// which the Content Security Policy allows (MIL-033).
    /// </summary>
    [Fact]
    public async Task Get_Opened_LoadsItsScriptFromAFileAndSaysWhyItNeedsIt()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        string html = WebUtility.HtmlDecode(await browser.GetStringAsync(GuestSignInRegistration.VerifyPath, Call));

        // Assert
        Assert.Contains("This page needs JavaScript to book your room.", html, StringComparison.Ordinal);
        Assert.Matches(ScriptOfThePage(), html);
    }

    /// <summary>
    /// Positive (ADR-0052 decision 3): the script of the page is served as JavaScript, and it copies the token from the
    /// address into the field and takes it out of the address bar.
    /// </summary>
    [Fact]
    public async Task Script_OfThePage_IsServedAndTakesTheTokenOutOfTheAddress()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        string html = await browser.GetStringAsync(GuestSignInRegistration.VerifyPath, Call);
        string source = "/" + ScriptOfThePage().Match(html).Groups["src"].Value.TrimStart('/');

        // Act
        using HttpResponseMessage response = await browser.GetAsync(source, Call);
        string script = await response.Content.ReadAsStringAsync(Call);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);
        Assert.Contains("history.replaceState", script, StringComparison.Ordinal);
        Assert.Contains("booking-verify-token", script, StringComparison.Ordinal);
    }

    /// <summary>
    /// Security (ADR-0052 decision 3): the page, the answer to its button and the refusal of a post without the
    /// antiforgery token all carry no referrer and no store.
    /// </summary>
    [Fact]
    public async Task Answers_OfThePage_CarryNoReferrerAndNoStore()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        using FormUrlEncodedContent bare = new(new Dictionary<string, string> { ["token"] = BookingRequestWebFactory.UsedToken });

        // Act
        using HttpResponseMessage page = await browser.GetAsync(GuestSignInRegistration.VerifyPath, Call);
        using HttpResponseMessage booked = await PressAsync(browser, BookingRequestWebFactory.BookingToken);
        using HttpResponseMessage refused = await browser.PostAsync(GuestSignInRegistration.VerifyPath, bare, Call);

        // Assert
        Assert.All([page, booked, refused], response =>
        {
            Assert.Equal(VerifyPageHeaders.ReferrerPolicy, Assert.Single(response.Headers.GetValues("Referrer-Policy")));
            Assert.Equal(VerifyPageHeaders.CacheControl, response.Headers.CacheControl?.ToString());
        });
    }

    #endregion

    #region Pressing the button

    /// <summary>
    /// Positive (ADR-0052 decision 5): the button sends the token in the body, and a room booked sets the guest cookie as
    /// the sign-in does and sends the browser to the own booking with the result in the address. The cookie does not show
    /// the guest token.
    /// </summary>
    [Fact]
    public async Task Post_LinkThatWorks_SignsTheGuestInAndGoesToTheOwnBooking()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        _factory.Requests.ClearReceivedCalls();

        // Act
        using HttpResponseMessage response = await PressAsync(browser, BookingRequestWebFactory.BookingToken);
        string cookie = SetCookie(response, GuestCookie);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/my-booking?done=booked", LocalPath(response));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/my-booking", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(GuestWebFactory.GuestToken, cookie, StringComparison.Ordinal);
        await _factory.Requests.Received(1).VerifyAsync(
            new VerifyBookingRequest(BookingRequestWebFactory.BookingToken), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Positive (ADR-0052 decision 5): after the button, the own booking shows the receipt with the reference and says that
    /// the room is booked; a reload shows it again and presses nothing.
    /// </summary>
    [Fact]
    public async Task Post_LinkThatWorks_OwnBookingShowsTheReceiptWithTheReference()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        using HttpResponseMessage response = await PressAsync(browser, BookingRequestWebFactory.BookingToken);
        _factory.Requests.ClearReceivedCalls();

        // Act
        string html = WebUtility.HtmlDecode(await browser.GetStringAsync(LocalPath(response), Call));

        // Assert
        Assert.Contains(GuestWebFactory.Reference, html, StringComparison.Ordinal);
        Assert.Contains("your room is booked.", html, StringComparison.Ordinal);
        Assert.Empty(_factory.Requests.ReceivedCalls());
    }

    /// <summary>
    /// Equivalence (ADR-0052 decision 3): a link that is unknown, used or lapsed gets the one text and the way to the
    /// booking page, and no guest cookie.
    /// </summary>
    [Fact]
    public async Task Post_LinkThatDoesNotWork_ShowsTheOneTextAndSetsNoCookie()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await PressAsync(browser, BookingRequestWebFactory.UsedToken);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Call));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(LinkInvalidText, html, StringComparison.Ordinal);
        Assert.Contains("href=\"/book\"", html, StringComparison.Ordinal);
        Assert.Empty(FormTag().Matches(html));
        Assert.False(HasSetCookie(response, GuestCookie));
    }

    /// <summary>Error handling (ADR-0052 decision 5): a room that went between the mail and the button says so, with the way to book again.</summary>
    [Fact]
    public async Task Post_RoomGone_SaysSoWithTheWayToTheBookingPage()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();

        // Act
        using HttpResponseMessage response = await PressAsync(browser, BookingRequestWebFactory.RoomGoneToken);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Call));

        // Assert
        Assert.Contains("That room type is no longer free for your dates.", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/book\"", html, StringComparison.Ordinal);
        Assert.False(HasSetCookie(response, GuestCookie));
    }

    /// <summary>Security (ASVS 3.5.3): a post without the antiforgery token is refused before the API is called.</summary>
    [Fact]
    public async Task Post_WithoutAntiforgery_IsRefusedAndCallsNothing()
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        _factory.Requests.ClearReceivedCalls();
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["_handler"] = "booking-verify",
            ["token"] = BookingRequestWebFactory.BookingToken,
        });

        // Act
        using HttpResponseMessage response = await browser.PostAsync(GuestSignInRegistration.VerifyPath, form, Call);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Requests.ReceivedCalls());
    }

    /// <summary>Boundary: an empty token and one longer than the API reads get the one text, and the API is not called.</summary>
    /// <param name="length">The length of the token.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(VerifyBookingRequest.MaxTokenLength + 1)]
    public async Task Post_TokenEmptyOrTooLong_ShowsTheOneTextAndCallsNothing(int length)
    {
        // Arrange
        using HttpClient browser = _factory.CreateBrowser();
        _factory.Requests.ClearReceivedCalls();

        // Act
        using HttpResponseMessage response = await PressAsync(browser, new string('a', length));
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Call));

        // Assert
        Assert.Contains(LinkInvalidText, html, StringComparison.Ordinal);
        Assert.Empty(_factory.Requests.ReceivedCalls());
    }

    #endregion

    #region Helpers

    /// <summary>Opens the page, puts the token in the hidden field as the script does, and presses the button.</summary>
    private static async Task<HttpResponseMessage> PressAsync(HttpClient browser, string token)
    {
        string html = await browser.GetStringAsync(GuestSignInRegistration.VerifyPath, Call);
        Dictionary<string, string> fields = WebFactory.HiddenFields(html);
        fields[WebFactory.InputName(html, "booking-verify-token")] = token;
        using FormUrlEncodedContent form = new(fields);
        return await browser.PostAsync(GuestSignInRegistration.VerifyPath, form, Call);
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

    [GeneratedRegex("<form[^>]*id=\"booking-verify-form\"", RegexOptions.IgnoreCase)]
    private static partial Regex FormTag();

    [GeneratedRegex("<button[^>]*id=\"booking-verify-button\"[^>]*\\bdisabled\\b", RegexOptions.IgnoreCase)]
    private static partial Regex DisabledButton();

    [GeneratedRegex("<script src=\"(?<src>[^\"]*js/booking-verify[^\"]*\\.js)\"", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptOfThePage();

    #endregion
}
