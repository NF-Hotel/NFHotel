[assembly: AssemblyFixture(typeof(NFHotel.E2E.Tests.BrowserFixture))]

namespace NFHotel.E2E.Tests;

/// <summary>
/// One headless Chromium for the whole test run (MIL-027 task 3), and the room the guest tests book: a fresh stack
/// has no rooms, so the Manager creates one through the back office once, before the first test.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    /// <summary>The number of the room that the Manager creates for the guest tests.</summary>
    public const string TestRoomNumber = "901";

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
}
