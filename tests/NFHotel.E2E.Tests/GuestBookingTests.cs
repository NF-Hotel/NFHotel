namespace NFHotel.E2E.Tests;

/// <summary>
/// The public booking flow in a browser (MIL-027 task 4): a guest picks dates, a room and fills in the form, and lands
/// on the receipt page (MIL-027 criterion 1: a receipt page that fails breaks the first test). The dates
/// are fixed far ahead, so the test reads no clock and the room of <see cref="BrowserFixture"/> is free on them.
/// </summary>
public sealed class GuestBookingTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string CheckIn = "2030-06-10";
    private const string CheckOut = "2030-06-12";

    [E2EFact]
    public async Task Book_FreeRoomWithCompleteForm_ShowsTheReceipt()
    {
        await ChooseTestRoomAsync(CheckIn, CheckOut);
        await FillGuestAsync("ada@example.com");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send booking request" }).ClickAsync();

        // The flow ends on the receipt page of the new booking, whose number is the key to it.
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your booking" })).ToBeVisibleAsync();
        Assert.StartsWith("/booking/", Pages.PathOf(Page), StringComparison.Ordinal);
        await Assertions.Expect(Page.GetByText($"Room {BrowserFixture.TestRoomNumber}")).ToBeVisibleAsync();
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

    [E2EFact]
    public async Task Receipt_UnknownBookingNumber_AnswersWithoutAServerError()
    {
        var response = await Pages.OpenAsync(Page, "booking/NO-SUCH-BOOKING");

        Assert.NotNull(response);
        Assert.True(response.Status < 500, $"The receipt page answered {response.Status}.");
    }

    private async Task FillGuestAsync(string email)
    {
        await Pages.FillAsync(Page.Locator("#first-name"), "Ada");
        await Pages.FillAsync(Page.Locator("#last-name"), "Lovelace");
        await Pages.FillAsync(Page.Locator("#email"), email);
        await Pages.FillAsync(Page.Locator("#phone"), "+4512345678");
        await Pages.FillAsync(Page.Locator("#country"), "Denmark");
    }

    private async Task ChooseTestRoomAsync(string checkIn, string checkOut)
    {
        await Pages.OpenAsync(Page, "book");
        await Pages.FillAsync(Page.Locator("#check-in"), checkIn);
        await Pages.FillAsync(Page.Locator("#check-out"), checkOut);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Find available rooms" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = $"Choose room {BrowserFixture.TestRoomNumber}" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your details" })).ToBeVisibleAsync();
    }
}
