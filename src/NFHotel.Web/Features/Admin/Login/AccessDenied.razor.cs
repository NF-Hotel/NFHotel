using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Admin.Login;

/// <summary>The code behind the access denied page: the landing page of the role of the signed-in user.</summary>
public partial class AccessDenied : ComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationState { get; set; } = null!;

    private string _landing = "/";

    /// <summary>Finds the landing page of the role of the signed-in user.</summary>
    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationState.GetAuthenticationStateAsync();
        _landing = StaffClaims.RoleOf(state.User) is { } role ? RolePermissions.LandingPath(role) : "/";
    }
}
