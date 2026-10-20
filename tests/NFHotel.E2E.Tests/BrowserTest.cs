namespace NFHotel.E2E.Tests;

/// <summary>
/// The base of every end-to-end test: a fresh browser context and page per test, so no cookie or session leaks
/// between tests, and a Playwright trace of the test written to the results folder; a failed test also gets a
/// screenshot (MIL-027 task 3).
/// </summary>
public abstract class BrowserTest(BrowserFixture fixture) : IAsyncLifetime
{
    private IBrowserContext? _context;

    /// <summary>The page of this test.</summary>
    protected IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _context = await fixture.NewContextAsync();
        await _context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        Page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is null)
        {
            return;
        }

        var name = FileName(TestContext.Current.Test?.TestDisplayName ?? GetType().Name);
        Directory.CreateDirectory(E2ESettings.ResultsDirectory);
        if (TestContext.Current.TestState?.Result == TestResult.Failed)
        {
            await Page.ScreenshotAsync(new() { Path = Path.Combine(E2ESettings.ResultsDirectory, name + ".png"), FullPage = true });
        }

        await _context.Tracing.StopAsync(new() { Path = Path.Combine(E2ESettings.ResultsDirectory, name + ".trace.zip") });
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private static string FileName(string testName) =>
        string.Concat(testName.Select(character => char.IsLetterOrDigit(character) || character is '.' or '_' ? character : '-'));
}
