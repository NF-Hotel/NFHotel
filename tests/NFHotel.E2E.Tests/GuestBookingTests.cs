namespace NFHotel.E2E.Tests;

/// <summary>
/// The public booking flow in a browser (MIL-027 task 4): a guest picks dates, a room type and fills in the form, and sees
/// the receipt (MIL-027 criterion 1: a receipt that fails breaks the first test). Since MIL-079 the receipt is step 4 of
/// the booking page, and the lookup page opens a booking only with its email; since MIL-018 that sign-in leads to the
/// guest's own booking, where the guest changes or cancels it; since MIL-085 the booking is known by a random reference
/// (ADR-0049). The dates are fixed far ahead, so the test reads no clock and the room of <see cref="BrowserFixture"/> is
/// free on them.
/// </summary>
public sealed class GuestBookingTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string CheckIn = "2030-06-10";
    private const string CheckOut = "2030-06-12";

    /// <summary>The text of a version 4 UUID in lower case (RFC 9562), the form of a reference (ADR-0049 decision 1).</summary>
    private const string Version4 = "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";

    #region The booking flow

    [E2EFact]
    public async Task Book_FreeRoomWithCompleteForm_ShowsTheReceipt()
    {
        await ChooseTestRoomAsync(CheckIn, CheckOut);
        await FillGuestAsync("ada@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();

        // The flow ends on its own receipt step: the guest who has just booked does not type the email again (MIL-079).
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your confirmation" })).ToBeVisibleAsync();
        Assert.Equal("/book", Pages.PathOf(Page));
        await Assertions.Expect(Page.GetByText("Single room · the room number is given at check-in")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("10 June 2030")).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task Book_GuestFormWithInvalidEmail_ShowsTheFieldErrorAndSendsNothing()
    {
        await ChooseTestRoomAsync("2030-07-01", "2030-07-03");
        await FillGuestAsync("not-an-email");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();

        await Assertions.Expect(Page.Locator("#email")).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your details" })).ToBeVisibleAsync();
        Assert.Equal("/book", Pages.PathOf(Page));
    }

    #endregion

    #region The guest sign-in and the own booking

    [E2EFact]
    public async Task FindBooking_ReferenceWithAnotherEmailThenItsOwn_SignsInOnlyWithItsOwn()
    {
        await ChooseTestRoomAsync("2030-08-04", "2030-08-06");
        await FillGuestAsync("grace@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();
        await Assertions.Expect(Page.Locator(".pub-receipt-reference")).ToBeVisibleAsync();
        string reference = (await Page.Locator(".pub-receipt-reference").InnerTextAsync()).Trim();
        Assert.Matches(Version4, reference);

        // The receipt links to the sign-in; neither the reference nor the email is in the address (MIL-079, ADR-0049).
        await Assertions.Expect(Page.Locator("main").GetByRole(AriaRole.Link, new() { Name = "Find your booking" }))
            .ToHaveAttributeAsync("href", "/booking");
        await Pages.OpenAsync(Page, "booking");
        await Pages.FillAsync(Page.Locator("#booking-reference"), reference.ToUpperInvariant());

        // Another email gets the answer of a reference that does not exist.
        await Pages.FillAsync(Page.Locator("#booking-email"), "someone.else@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Show my booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("We couldn't find that booking.")).ToBeVisibleAsync();

        // Its own email, in another letter case, signs the guest in to the own booking (MIL-018).
        await Pages.FillAsync(Page.Locator("#booking-email"), "GRACE@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Show my booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("4 August 2030")).ToBeVisibleAsync();
        Assert.Equal("/my-booking", Pages.PathOf(Page));
    }

    /// <summary>
    /// MIL-018 criterion 5 in the throwaway stack: a guest books, manages the booking just made, signs out, signs in with
    /// the reference and the email, and cancels; the guest cookie is HttpOnly, SameSite=Lax, only for the own booking
    /// pages, and Secure on HTTPS.
    /// </summary>
    [E2EFact]
    public async Task GuestSession_BookSignOutSignInAndCancel_ReachesTheOwnBooking()
    {
        await ChooseTestRoomAsync("2030-09-07", "2030-09-09");
        await FillGuestAsync("hedy@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();
        await Assertions.Expect(Page.Locator(".pub-receipt-reference")).ToBeVisibleAsync();
        string reference = (await Page.Locator(".pub-receipt-reference").InnerTextAsync()).Trim();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Manage your booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your booking" })).ToBeVisibleAsync();

        BrowserContextCookiesResult cookie = (await Page.Context.CookiesAsync()).Single(candidate => candidate.Name == "nfhotel.guest");
        Assert.True(cookie.HttpOnly);
        Assert.Equal(SameSiteAttribute.Lax, cookie.SameSite);
        Assert.Equal("/my-booking", cookie.Path);
        Assert.Equal(new Uri(Page.Url).Scheme == Uri.UriSchemeHttps, cookie.Secure);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await Assertions.Expect(Page.Locator("#booking-reference")).ToBeVisibleAsync();
        Assert.Equal("/booking", Pages.PathOf(Page));

        await Pages.FillAsync(Page.Locator("#booking-reference"), reference);
        await Pages.FillAsync(Page.Locator("#booking-email"), "hedy@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Show my booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("7 September 2030")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel my booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("Your booking is cancelled.")).ToBeVisibleAsync();
    }

    #endregion

    #region Old booking numbers

    /// <summary>ADR-0049 decision 5: an old link with a booking number ends on the empty sign-in, never on an error.</summary>
    [E2EFact]
    public async Task SignIn_OldLinkWithABookingNumber_EndsOnTheEmptySignIn()
    {
        IResponse? response = await Pages.OpenAsync(Page, "booking/FLZ-000001");

        Assert.NotNull(response);
        Assert.True(response.Status < 500, $"The sign-in answered {response.Status}.");
        Assert.Equal("/booking", Pages.PathOf(Page));
        await Assertions.Expect(Page.Locator("#booking-reference")).ToHaveValueAsync(string.Empty);
    }

    /// <summary>ADR-0049 decisions 2 and 5: a booking number of the old kind with the right email gets the one refusal.</summary>
    [E2EFact]
    public async Task SignIn_OldBookingNumberWithItsEmail_GetsTheOneRefusal()
    {
        await ChooseTestRoomAsync("2030-10-01", "2030-10-03");
        await FillGuestAsync("joan@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();
        await Assertions.Expect(Page.Locator(".pub-receipt-reference")).ToBeVisibleAsync();

        await Pages.OpenAsync(Page, "booking");
        await Pages.FillAsync(Page.Locator("#booking-reference"), "FLZ-000001");
        await Pages.FillAsync(Page.Locator("#booking-email"), "joan@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Show my booking" }).ClickAsync();

        await Assertions.Expect(Page.GetByText("We couldn't find that booking.")).ToBeVisibleAsync();
        Assert.Equal("/booking", Pages.PathOf(Page));
    }

    #endregion

    #region Helpers

    private async Task FillGuestAsync(string email)
    {
        await Pages.FillAsync(Page.Locator("#first-name"), "Ada");
        await Pages.FillAsync(Page.Locator("#last-name"), "Lovelace");
        await Pages.FillAsync(Page.Locator("#email"), email);
        await Pages.FillAsync(Page.Locator("#phone"), "+4512345678");
        await Pages.SelectAsync(Page.Locator("#country"), "Denmark");
    }

    private async Task ChooseTestRoomAsync(string checkIn, string checkOut)
    {
        await Pages.OpenAsync(Page, "book");
        await Pages.FillAsync(Page.Locator("#check-in"), checkIn);
        await Pages.FillAsync(Page.Locator("#check-out"), checkOut);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Find available rooms" }).ClickAsync();
        // The room of BrowserFixture is a single room (the default of the room form); the guest chooses its type.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Choose Single room" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your details" })).ToBeVisibleAsync();
    }

    #endregion
}
