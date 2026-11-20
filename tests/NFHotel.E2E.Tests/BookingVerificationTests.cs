using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NFHotel.E2E.Tests;

/// <summary>
/// The booking verified by email in a browser (MIL-088, ADR-0052 and ADR-0053), against a stack with the verification on
/// and Mailpit catching its mails: the guest asks, reads the mail, opens the link, presses the button and sees the
/// receipt; a used or unknown link gets the one answer; "Send again" sends a second mail and the first link stops
/// working. The tests carry the trait <c>Mode=Verification</c>, so the runner runs them only against a stack in that mode
/// and every other browser test only against a stack with the verification off.
/// </summary>
/// <remarks>
/// Each test asks with an email of its own at example.com, so the limits per mailbox and the one open request per
/// mailbox of one test never touch another. The three tests ask four times from the address of the browser container,
/// within the five booking requests an hour of ADR-0051. Every token that a test uses is appended to
/// <c>verification-tokens.txt</c> in the results folder, so the runner can show that no log of the stack holds one.
/// A lapsed link is not tested here: it lapses after 30 minutes; the tests of the API cover it with a fixed clock.
/// </remarks>
[Trait("Mode", "Verification")]
public sealed partial class BookingVerificationTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    #region Fields

    /// <summary>The subject of the mail with the link (ADR-0052 decision 4).</summary>
    private const string RequestSubject = "Verify your booking at NF Hotel";

    /// <summary>The subject of the mail with the reference (ADR-0052 decision 5).</summary>
    private const string ReceiptSubject = "Your booking at NF Hotel";

    /// <summary>The one answer to a link that is unknown, used or lapsed (ADR-0052 decision 3).</summary>
    private const string LinkInvalidText = "This link doesn't work any more.";

    /// <summary>The text of a version 4 UUID in lower case (RFC 9562), the form of a reference (ADR-0049 decision 1).</summary>
    private const string Version4 = "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";

    /// <summary>The file in the results folder with every token the tests used, one per line.</summary>
    private const string TokenFileName = "verification-tokens.txt";

    /// <summary>Lets one test at a time append to the file of the tokens.</summary>
    private static readonly SemaphoreSlim TokenFileLock = new(1, 1);

    #endregion Fields

    #region The booking verified by email

    /// <summary>
    /// Positive (ADR-0052 decisions 1 to 5): the guest asks and sees the inbox step; the mail holds the dates, the room
    /// type, the link and the time it lapses, and nothing the guest typed; the button behind the link books the room and
    /// shows the receipt with the reference, which the second mail carries too; and the reference with the email finds
    /// the booking again.
    /// </summary>
    [E2EFact]
    public async Task Verify_LinkFromTheMail_BooksTheRoomAndFindsItWithTheReference()
    {
        // Arrange
        CancellationToken cancel = TestContext.Current.CancellationToken;
        string email = UniqueEmail();
        using MailCatcher mails = new();
        await AskAsync("2031-03-10", "2031-03-12", email);

        // Act
        string mail = await mails.WaitForTextAsync(email, RequestSubject, 1, cancel);
        string token = await TokenOfAsync(mail);
        await PressTheButtonAsync(token);

        // Assert: the mail, read without its token, which is random and could hold any letters
        string words = mail.Replace(token, string.Empty, StringComparison.Ordinal);
        string[] typed = ["Ada", "Lovelace", "+4512345678", "Denmark", email];
        Assert.Contains($"Arrival: {MailDay("2031-03-10")}", words, StringComparison.Ordinal);
        Assert.Contains($"Departure: {MailDay("2031-03-12")} (2 nights)", words, StringComparison.Ordinal);
        Assert.Contains("Room: Single room", words, StringComparison.Ordinal);
        Assert.Matches(LinkLapses(), words);
        Assert.All(typed, text => Assert.DoesNotContain(text, words, StringComparison.OrdinalIgnoreCase));

        // Assert: the receipt
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your booking" })).ToBeVisibleAsync();
        Assert.Equal("/my-booking", Pages.PathOf(Page));
        await Assertions.Expect(Page.GetByText("your room is booked", new() { Exact = false })).ToBeVisibleAsync();
        string reference = (await Page.Locator(".pub-receipt-reference").InnerTextAsync()).Trim();
        Assert.Matches(Version4, reference);
        string receipt = await mails.WaitForTextAsync(email, ReceiptSubject, 1, cancel);
        Assert.Contains($"Reference: {reference}", receipt, StringComparison.Ordinal);

        // Assert: the reference and the email find the booking again (UC-010)
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await Assertions.Expect(Page.Locator("#booking-reference")).ToBeVisibleAsync();
        await Pages.FillAsync(Page.Locator("#booking-reference"), reference);
        await Pages.FillAsync(Page.Locator("#booking-email"), email);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Show my booking" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("10 March 2031")).ToBeVisibleAsync();
        Assert.Equal("/my-booking", Pages.PathOf(Page));
    }

    /// <summary>
    /// Security (ADR-0052 decisions 3 and 5): a link works once; pressed again it gets the one answer, as an unknown token
    /// does, with the way back to the booking page.
    /// </summary>
    [E2EFact]
    public async Task Verify_UsedOrUnknownLink_GetsTheOneAnswer()
    {
        // Arrange
        CancellationToken cancel = TestContext.Current.CancellationToken;
        string email = UniqueEmail();
        using MailCatcher mails = new();
        await AskAsync("2031-04-14", "2031-04-16", email);
        string token = await TokenOfAsync(await mails.WaitForTextAsync(email, RequestSubject, 1, cancel));
        await PressTheButtonAsync(token);
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your booking" })).ToBeVisibleAsync();
        string unknown = Base64Url(RandomNumberGenerator.GetBytes(32));
        await RememberTokenAsync(unknown);

        // Act and assert: the used link
        await PressTheButtonAsync(token);
        await Assertions.Expect(Page.GetByText(LinkInvalidText)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Go to the booking page" })).ToHaveAttributeAsync("href", "/book");

        // Act and assert: the unknown link
        await PressTheButtonAsync(unknown);
        await Assertions.Expect(Page.GetByText(LinkInvalidText)).ToBeVisibleAsync();
    }

    /// <summary>
    /// State transition (ADR-0052 decision 6): "Send again" sends a second mail with a new link, and the link of the first
    /// mail stops working; the new link books the room.
    /// </summary>
    [E2EFact]
    public async Task SendAgain_FromTheInbox_SendsANewLinkAndTheFirstStopsWorking()
    {
        // Arrange
        CancellationToken cancel = TestContext.Current.CancellationToken;
        string email = UniqueEmail();
        using MailCatcher mails = new();
        await AskAsync("2031-05-19", "2031-05-21", email);
        string first = await TokenOfAsync(await mails.WaitForTextAsync(email, RequestSubject, 1, cancel));

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send again" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("We've sent you a new link.", new() { Exact = false })).ToBeVisibleAsync();
        string second = await TokenOfAsync(await mails.WaitForTextAsync(email, RequestSubject, 2, cancel));

        // Assert
        Assert.NotEqual(first, second);
        await PressTheButtonAsync(first);
        await Assertions.Expect(Page.GetByText(LinkInvalidText)).ToBeVisibleAsync();
        await PressTheButtonAsync(second);
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your booking" })).ToBeVisibleAsync();
    }

    #endregion

    #region Helpers

    /// <summary>A fresh address at example.com for one test, so its mails and its limits are its own.</summary>
    private static string UniqueEmail() => $"e2e-{Guid.NewGuid():N}@example.com";

    /// <summary>A day as the mails write it, for example <c>Monday 10 March 2031</c>.</summary>
    private static string MailDay(string date) =>
        DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Writes bytes as base64url without padding, the form of a token (ADR-0052 decision 2).</summary>
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Reads the token of the link in a mail and appends it to the file of the tokens.</summary>
    private static async Task<string> TokenOfAsync(string mail)
    {
        Match link = LinkOfTheMail().Match(mail);
        Assert.True(link.Success, "The mail has no link to /booking/verify with a token of 43 characters.");
        string token = link.Groups["token"].Value;
        await RememberTokenAsync(token);
        return token;
    }

    /// <summary>Appends a token to the file of the tokens in the results folder, which is made when it is missing.</summary>
    private static async Task RememberTokenAsync(string token)
    {
        Directory.CreateDirectory(E2ESettings.ResultsDirectory);
        await TokenFileLock.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            await File.AppendAllTextAsync(
                Path.Join(E2ESettings.ResultsDirectory, TokenFileName), token + "\n", TestContext.Current.CancellationToken);
        }
        finally
        {
            TokenFileLock.Release();
        }
    }

    /// <summary>Picks the test room for the dates, fills the details with an email and asks; the page shows the inbox step.</summary>
    private async Task AskAsync(string checkIn, string checkOut, string email)
    {
        await Pages.OpenAsync(Page, "book");
        await Pages.FillAsync(Page.Locator("#check-in"), checkIn);
        await Pages.FillAsync(Page.Locator("#check-out"), checkOut);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Find available rooms" }).ClickAsync();
        // The room of BrowserFixture is a single room; the guest chooses its type.
        await Page.GetByRole(AriaRole.Button, new() { Name = "Choose Single room" }).ClickAsync();
        await Pages.FillAsync(Page.Locator("#first-name"), "Ada");
        await Pages.FillAsync(Page.Locator("#last-name"), "Lovelace");
        await Pages.FillAsync(Page.Locator("#email"), email);
        await Pages.FillAsync(Page.Locator("#phone"), "+4512345678");
        await Pages.SelectAsync(Page.Locator("#country"), "Denmark");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Check your inbox" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText($"We've sent an email with a link to {email}.")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Opens the page of a link on the stack, as a click on the link in the mail does, and presses its button once the
    /// script has taken the token from the address, which then holds none. The caller waits for the answer.
    /// </summary>
    /// <remarks>
    /// The page is left first: from the page of the link, an address that differs only after <c>#</c> would be a jump
    /// within the page, which loads nothing and runs no script.
    /// </remarks>
    private async Task PressTheButtonAsync(string token)
    {
        await Page.GotoAsync("about:blank");
        await Pages.OpenAsync(Page, $"booking/verify#{token}");
        ILocator button = Page.GetByRole(AriaRole.Button, new() { Name = "Book the room" });
        await Assertions.Expect(button).ToBeEnabledAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(AddressOfThePage());
        await button.ClickAsync();
    }

    /// <summary>The link of the mail: an https address of the page of the link with a token of 43 characters after <c>#</c>.</summary>
    [GeneratedRegex(@"^https://\S+/booking/verify#(?<token>[A-Za-z0-9_-]{43})\r?$", RegexOptions.Multiline)]
    private static partial Regex LinkOfTheMail();

    /// <summary>The address of the page of the link once its script has run: no <c>#</c> and no token.</summary>
    [GeneratedRegex(@"/booking/verify$")]
    private static partial Regex AddressOfThePage();

    /// <summary>The line of the mail with the time the link lapses, in UTC.</summary>
    [GeneratedRegex(@"The link works once, until \d{2}:\d{2} UTC on [A-Z][a-z]+day \d{1,2} [A-Z][a-z]+ \d{4}\.")]
    private static partial Regex LinkLapses();

    #endregion
}
