using System.Security.Claims;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Security;

/// <summary>
/// The claims of a signed-in member of staff in the web application (ADR-0005): the user name, the role and the staff
/// token for the API. The cookie holds them; the policies and the menu read the role from them.
/// </summary>
public static class StaffClaims
{
    /// <summary>The name of the cookie scheme.</summary>
    public const string CookieScheme = "NFHotelStaff";

    /// <summary>The claim that carries the staff token for the API.</summary>
    public const string TokenClaim = "staff_token";

    /// <summary>Builds the principal that goes into the cookie.</summary>
    /// <param name="session">The session the API gave at the sign-in.</param>
    /// <returns>The principal.</returns>
    public static ClaimsPrincipal CreatePrincipal(StaffSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, session.UserName),
                new Claim(ClaimTypes.Role, session.Role.ToString()),
                new Claim(TokenClaim, session.Token),
            ],
            CookieScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Reads the role of a principal.</summary>
    /// <param name="user">The principal.</param>
    /// <returns>The role, or <c>null</c> when nobody is signed in or the claim is not a known role.</returns>
    public static StaffRole? RoleOf(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.Identity?.IsAuthenticated == true
            && StaffRoleParser.TryParse(user.FindFirst(ClaimTypes.Role)?.Value, out var role)
                ? role
                : null;
    }

    /// <summary>Tells whether the principal holds a permission.</summary>
    /// <param name="user">The principal.</param>
    /// <param name="permission">The permission.</param>
    /// <returns>True when the role of the principal holds it.</returns>
    public static bool Holds(ClaimsPrincipal user, string permission) =>
        RoleOf(user) is { } role && RolePermissions.HasPermission(role, permission);
}