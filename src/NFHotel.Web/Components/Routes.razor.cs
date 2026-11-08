using System.Security.Claims;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Security;

namespace NFHotel.Web.Components;

/// <summary>The code behind the router: where a signed-in user lands when a page is not part of the role.</summary>
public partial class Routes : ComponentBase
{
    /// <summary>The page the role of the user starts on, for a signed-in user who is refused a page.</summary>
    private static string LandingPath(ClaimsPrincipal user) =>
        StaffClaims.RoleOf(user) is { } role
            ? RolePermissions.LandingPath(role)
            : "/";
}
