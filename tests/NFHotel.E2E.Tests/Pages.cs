namespace NFHotel.E2E.Tests;

/// <summary>Small steps that every test takes in a Blazor page.</summary>
public static class Pages
{
    /// <summary>
    /// Opens a path of the stack and waits until the network is quiet, so an interactive page has its circuit before
    /// the test clicks. Playwright waits; the test never sleeps.
    /// </summary>
    public static async Task<IResponse?> OpenAsync(IPage page, string path)
    {
        var response = await page.GotoAsync(path);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return response;
    }

    /// <summary>
    /// Fills a field and leaves it: Blazor's <c>@bind</c> takes the value on the change event, which fires when the
    /// field loses focus.
    /// </summary>
    public static async Task FillAsync(ILocator field, string value)
    {
        await field.FillAsync(value);
        await field.PressAsync("Tab");
    }

    /// <summary>The path of the page without the address of the stack and without the query, for example <c>/admin/login</c>.</summary>
    public static string PathOf(IPage page) => new Uri(page.Url).AbsolutePath;
}
