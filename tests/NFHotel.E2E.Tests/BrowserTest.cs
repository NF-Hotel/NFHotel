namespace NFHotel.E2E.Tests;

/// <summary>
/// The base of every end-to-end test: a fresh browser context and page per test, so no cookie or session leaks
/// between tests, and a Playwright trace of the test written to the results folder; a failed test also gets a
/// screenshot (MIL-027 task 3). A test fails when the browser reports a Content Security Policy violation in its
/// console, so a page that needs inline code or a foreign source is found here, not by a guest (MIL-033 task 5).
/// </summary>
public abstract class BrowserTest(BrowserFixture fixture) : IAsyncLifetime
{
    private readonly List<string> _policyViolations = [];
    private IBrowserContext? _context;

    /// <summary>The page of this test.</summary>
    protected IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _context = await fixture.NewContextAsync();
        await _context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        Page = await _context.NewPageAsync();
        Page.Console += (_, message) =>
        {
            if (message.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                _policyViolations.Add(message.Text);
            }
        };
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

        // Thrown after the trace is written, so the trace shows the page that broke the policy.
        if (_policyViolations.Count > 0)
        {
            throw new InvalidOperationException(
                $"The browser reported {_policyViolations.Count} Content Security Policy violation(s): {string.Join(" | ", _policyViolations)}");
        }
    }

    private static string FileName(string testName) =>
        string.Concat(testName.Select(character => char.IsLetterOrDigit(character) || character is '.' or '_' ? character : '-'));
}
