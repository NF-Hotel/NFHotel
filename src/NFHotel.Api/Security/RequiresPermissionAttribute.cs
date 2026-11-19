using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>
/// Lets an endpoint through only for a signed-in member of staff whose role holds the permission (ADR-0005):
/// 401 without a valid staff token, 403 when the role lacks the permission. The use case is not called in either case.
/// An endpoint without this attribute is public to every caller that holds the token of the web application.
/// <para>
/// A route of staff is never a route of a guest (ADR-0049 decision 3): a valid guest token gets 403 here, and a guest
/// token that is expired or tampered gets the 401 of a refused guest sign-in. The guest's own booking has its own routes,
/// behind <see cref="RequiresGuestAttribute"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="permission">The permission the endpoint needs.</param>
    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    /// <summary>The permission the endpoint needs.</summary>
    public string Permission { get; }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = GuestRefusal(context.HttpContext);
            return;
        }

        var allowed = StaffRoleParser.TryParse(user.FindFirst(JwtStaffTokenIssuer.RoleClaim)?.Value, out var role)
            && RolePermissions.HasPermission(role, Permission);
        if (!allowed)
        {
            context.Result = Refuse(StatusCodes.Status403Forbidden, ErrorCodes.Api.Forbidden);
            return;
        }

        // An account that must choose a new password may do nothing else until it has (ADR-0017).
        if (Permission != Permissions.StaffChangeOwnPassword
            && bool.TryParse(user.FindFirst(JwtStaffTokenIssuer.MustChangeClaim)?.Value, out var mustChange)
            && mustChange)
        {
            context.Result = Refuse(StatusCodes.Status403Forbidden, ErrorCodes.Staff.PasswordChangeRequired);
        }
    }

    /// <summary>
    /// The answer to a call without a valid staff token: without a guest token it is the 401 of ADR-0005; a refused guest
    /// token gets the one refusal of the guest sign-in (MIL-018); a valid guest token gets 403, because no route of staff
    /// admits a guest (ADR-0049 decision 3).
    /// </summary>
    /// <param name="http">The request.</param>
    /// <returns>The refusal.</returns>
    private static ObjectResult GuestRefusal(HttpContext http)
    {
        if (GuestAuthentication.IsRefused(http))
        {
            return GuestAuthentication.Refusal();
        }

        return GuestAuthentication.BookingIdOf(http) is null
            ? Refuse(StatusCodes.Status401Unauthorized, ErrorCodes.Api.StaffUnauthorized)
            : Refuse(StatusCodes.Status403Forbidden, ErrorCodes.Api.Forbidden);
    }

    private static ObjectResult Refuse(int status, string code) =>
        new(new ApiResponse([code])) { StatusCode = status };
}
