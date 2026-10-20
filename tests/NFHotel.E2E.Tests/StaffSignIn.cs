namespace NFHotel.E2E.Tests;

/// <summary>Signs in to the back office through its login page, as a person does (MIL-016, ADR-0005).</summary>
public static class StaffSignIn
{
    /// <summary>Signs in with the test account of a role and waits for the landing page of that role.</summary>
    public static async Task AsAsync(IPage page, StaffRole role)
    {
        await SubmitAsync(page, E2ESettings.UserName(role), E2ESettings.Password(role));
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })).ToBeVisibleAsync();
    }

    /// <summary>Opens the login page and posts a user name and a password.</summary>
    public static async Task SubmitAsync(IPage page, string userName, string password)
    {
        await Pages.OpenAsync(page, "admin/login");
        await page.Locator("#login-user").FillAsync(userName);
        await page.Locator("#login-password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
