using Microsoft.AspNetCore.Components;

using NFHotel.Web.Security;

namespace NFHotel.Web.Components;

/// <summary>The code behind the redirect to the login page: the return address and the redirect itself.</summary>
public partial class RedirectToLogin : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Sends the visitor to the login page with the page that was asked for as return address.</summary>
    protected override void OnInitialized()
    {
        var target = Navigation.ToBaseRelativePath(Navigation.Uri);
        var returnUrl = "/" + target;
        Navigation.NavigateTo($"{StaffSecurityRegistration.LoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
