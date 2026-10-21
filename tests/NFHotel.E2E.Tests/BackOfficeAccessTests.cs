namespace NFHotel.E2E.Tests;

/// <summary>
/// The back office roles in a browser (MIL-027 task 5, MIL-016, Access Control Matrix): the redirect to the login, the
/// pages each role may and may not open, the sign-out, and the limit on failed sign-ins.
/// </summary>
public sealed class BackOfficeAccessTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    #region Without a session

    [E2EFact]
    public async Task AdminPage_WithoutSession_RedirectsToTheLogin()
    {
        await Pages.OpenAsync(Page, "admin/guests");

        Assert.Equal("/admin/login", Pages.PathOf(Page));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Staff sign-in" })).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task SignIn_SixthFailureInARow_ShowsTooManyAttempts()
    {
        // A user name of its own, so the limit never locks out an account that another test signs in with.
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            await StaffSignIn.SubmitAsync(Page, "e2e-rate-limit", "wrong-password-" + attempt);
        }

        await Assertions.Expect(Page.GetByText("Too many failed attempts")).ToBeVisibleAsync();
        Assert.Equal("/admin/login", Pages.PathOf(Page));
    }

    #endregion

    #region Own password

    [E2EFact]
    public async Task ChangePassword_WrongCurrentPassword_ShowsTheRefusalAndKeepsTheSession()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Receptionist);
        await Pages.OpenAsync(Page, "admin/account/password");
        await Page.Locator("#current-password").FillAsync("not the current password");
        await Page.Locator("#new-password").FillAsync("E2E never used password " + Guid.NewGuid().ToString("N"));
        await Page.Locator("#repeat-password").FillAsync(await Page.Locator("#new-password").InputValueAsync());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Change password" }).ClickAsync();

        await Assertions.Expect(Page.GetByText("The current password is wrong.")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" })).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task StaffPage_AsReceptionist_IsRefused()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Receptionist);
        await Pages.OpenAsync(Page, "admin/staff");

        Assert.Equal("/admin/denied", Pages.PathOf(Page));
    }

    [E2EFact]
    public async Task StaffAndAuditPages_AsManager_ListTheAccountsAndTheirSignIns()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Manager);
        await Pages.OpenAsync(Page, "admin/staff");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Staff" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Cell, new() { Name = E2ESettings.UserName(StaffRole.Manager), Exact = true })).ToBeVisibleAsync();

        await Pages.OpenAsync(Page, "admin/staff/audit");
        await Assertions.Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "SignedIn", Exact = true }).First).ToBeVisibleAsync();
    }

    #endregion

    #region Roles

    [E2EFact]
    public async Task Receptionist_OpensBookingsAndSales_SeesBookingsAndIsRefusedSales()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Receptionist);

        await AssertAllowedAsync("admin/bookings");
        await AssertRefusedAsync("admin/revenue");
    }

    [E2EFact]
    public async Task Housekeeping_SignsIn_LandsOnRoomsAndIsRefusedBookingsAndGuests()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Housekeeping);

        Assert.Equal("/admin/rooms", Pages.PathOf(Page));
        await AssertRefusedAsync("admin/bookings");
        // The guest page is the only page with passport numbers, so Housekeeping never sees one.
        await AssertRefusedAsync("admin/guests");
    }

    [E2EFact]
    public async Task Manager_OpensEveryBackOfficePage_IsRefusedNone()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Manager);

        foreach (var path in new[] { "admin", "admin/bookings", "admin/guests", "admin/rooms", "admin/revenue" })
        {
            await AssertAllowedAsync(path);
        }
    }

    [E2EFact]
    public async Task SignOut_AfterSignIn_EndsTheSession()
    {
        await StaffSignIn.AsAsync(Page, StaffRole.Receptionist);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Pages.OpenAsync(Page, "admin/bookings");
        Assert.Equal("/admin/login", Pages.PathOf(Page));
    }

    #endregion

    #region Helpers

    private async Task AssertAllowedAsync(string path)
    {
        var response = await Pages.OpenAsync(Page, path);

        Assert.True(response is { Status: 200 }, $"/{path} answered {response?.Status}.");
        Assert.Equal("/" + path, Pages.PathOf(Page));
    }

    private async Task AssertRefusedAsync(string path)
    {
        await Pages.OpenAsync(Page, path);

        Assert.Equal("/admin/denied", Pages.PathOf(Page));
    }

    #endregion
}
