namespace NFHotel.E2E.Tests;

/// <summary>Signs in to the back office through its login page, as a person does (MIL-016, ADR-0005).</summary>
public static class StaffSignIn
{
    /// <summary>Signs in with the test account of a role and waits for the landing page of that role.</summary>
    public static async Task AsAsync(IPage page, StaffRole role)
    {
        await SubmitAsync(page, E2ESettings.UserName(role), E2ESettings.Password(role));
        if (Pages.PathOf(page) == ChangePasswordPath)
        {
            await ChooseNewPasswordAsync(page, role);
        }

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })).ToBeVisibleAsync();
    }

    /// <summary>The page that an account with a temporary password is sent to (ADR-0015).</summary>
    public const string ChangePasswordPath = "/admin/account/password";

    /// <summary>
    /// Changes the password of an account that must choose a new one, as the person does, and remembers the new password
    /// for the rest of the run. The first Manager of a fresh stack is such an account.
    /// </summary>
    public static async Task ChooseNewPasswordAsync(IPage page, StaffRole role)
    {
        var chosen = "E2E chosen password " + Guid.NewGuid().ToString("N");
        await page.Locator("#current-password").FillAsync(E2ESettings.Password(role));
        await page.Locator("#new-password").FillAsync(chosen);
        await page.Locator("#repeat-password").FillAsync(chosen);
        await page.GetByRole(AriaRole.Button, new() { Name = "Change password" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        E2ESettings.RememberChosenPassword(role, chosen);
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
