using System.Net;
using System.Net.Sockets;

namespace NFHotel.E2E.Tests;

/// <summary>
/// The address of a caller through nginx (MIL-087 criterion 4, ADR-0051 decision 4, review RC-426 F7): an action of the
/// back office runs in a circuit, and a unit test cannot show that Blazor carries the address of the browser into it,
/// so the throwaway stack shows it, with nginx in front as in production.
/// </summary>
public sealed class CallerAddressTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    /// <summary>
    /// Security (RC-426 F7): an account that the manager creates in the back office, an action in a circuit, is audited
    /// under the address that nginx saw, the address of this browser's container on the network of the stack, and not
    /// under the address of the web application.
    /// </summary>
    [E2EFact]
    public async Task CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw()
    {
        // Arrange
        string browser = OwnAddress();
        string userName = "e2e-address-" + Guid.NewGuid().ToString("N")[..8];
        await StaffSignIn.AsAsync(Page, StaffRole.Manager);
        await Pages.OpenAsync(Page, "admin/staff");

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "New account" }).ClickAsync();
        await Pages.FillAsync(Page.Locator("#new-user"), userName);
        await Pages.SelectAsync(Page.Locator("#new-role"), nameof(StaffRole.Housekeeping));
        await Pages.FillAsync(Page.Locator("#new-password"), "E2E address password " + Guid.NewGuid().ToString("N"));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Cell, new() { Name = userName, Exact = true })).ToBeVisibleAsync();
        await Pages.OpenAsync(Page, "admin/staff/audit");

        // Assert
        ILocator created = Page.Locator("tbody tr")
            .Filter(new() { HasText = userName })
            .Filter(new() { HasText = "AccountCreated" });
        await Assertions.Expect(created.Locator("td").Last).ToHaveTextAsync(browser);
    }

    // The IPv4 address of this container, which runs on the network of the stack alone (scripts/run-e2e-tests.sh).
    private static string OwnAddress() =>
        Dns.GetHostAddresses(Dns.GetHostName()).First(address => address.AddressFamily == AddressFamily.InterNetwork).ToString();
}
