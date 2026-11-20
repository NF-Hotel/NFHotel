[assembly: AssemblyFixture(typeof(NFHotel.E2E.Tests.BrowserFixture))]

namespace NFHotel.E2E.Tests;

/// <summary>
/// One headless Chromium for the whole test run (MIL-027 task 3), and the room the guest tests book: a fresh stack
/// has no room of the tests, so the Manager creates one through the back office once, before the first test. A second
/// run against the same stack finds the room and signs no one in (MIL-088).
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    /// <summary>The number of the room that the Manager creates for the guest tests.</summary>
    public const string TestRoomNumber = "901";

    /// <summary>The arrival of the search that looks for the room; no test books these dates.</summary>
    private const string SearchCheckIn = "2031-08-04";

    /// <summary>The departure of the search that looks for the room.</summary>
    private const string SearchCheckOut = "2031-08-06";

    private IPlaywright? _playwright;

    /// <summary>The browser that every test opens its own context in.</summary>
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        if (!E2ESettings.IsConfigured && !E2ESettings.IsRequired)
        {
            return;
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        await CreateTestRoomAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_playwright is null)
        {
            return;
        }

        await Browser.DisposeAsync();
        _playwright.Dispose();
    }

    /// <summary>Opens a context for one test: the stack's certificate is self-signed, so its errors are ignored.</summary>
    public Task<IBrowserContext> NewContextAsync() =>
        Browser.NewContextAsync(new() { BaseURL = E2ESettings.BaseUrl.ToString(), IgnoreHTTPSErrors = true });

    private async Task CreateTestRoomAsync()
    {
        await using var context = await NewContextAsync();
        var page = await context.NewPageAsync();

        // The tests of the verification by email run after the other browser tests, against the same stack and database
        // with only the API made again (MIL-088): the room is there, and the first Manager has a password that only the
        // earlier run knew. A guest's search finds the room without a sign-in.
        if (await IsTestRoomOfferedAsync(page))
        {
            return;
        }

        await StaffSignIn.AsAsync(page, StaffRole.Manager);
        await Pages.OpenAsync(page, "admin/rooms");
        // An empty room list repeats the button in its empty state; either opens the same form.
        await page.GetByRole(AriaRole.Button, new() { Name = "New room" }).First.ClickAsync();
        await Pages.FillAsync(page.Locator("#room-number"), TestRoomNumber);
        await Pages.FillAsync(page.Locator("#room-floor"), "9");
        await Pages.FillAsync(page.Locator("#room-capacity"), "2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save room" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TestRoomNumber, Exact = true })).ToBeVisibleAsync();
    }

    /// <summary>True when a guest's search offers the single room of the tests, which an earlier run on the stack made.</summary>
    private static async Task<bool> IsTestRoomOfferedAsync(IPage page)
    {
        await Pages.OpenAsync(page, "book");
        await Pages.FillAsync(page.Locator("#check-in"), SearchCheckIn);
        await Pages.FillAsync(page.Locator("#check-out"), SearchCheckOut);
        await page.GetByRole(AriaRole.Button, new() { Name = "Find available rooms" }).ClickAsync();
        // The check of the stack (check-e2e-stack.sh) makes a Suite of its own before the tests, so the search can offer
        // other room types, or none. The step of the room types holds its list, or the text that none is free, once its
        // button back to the dates is enabled again.
        var types = page.GetByRole(AriaRole.Region, new() { Name = "Choose a room type" });
        await Assertions.Expect(types.GetByRole(AriaRole.Button, new() { Name = "Back to dates" })).ToBeEnabledAsync();
        return await types.GetByRole(AriaRole.Button, new() { Name = "Choose Single room" }).IsVisibleAsync();
    }
}
