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
/// A signed-in guest (MIL-018, ADR-0047) passes only an endpoint that names a <see cref="GuestPermission"/> which the
/// Guest role holds; the use case then checks that the booking is the guest's own. A guest token on any other endpoint
/// gets 403, and a guest token that is expired or tampered gets the 401 of a refused guest sign-in.
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

    /// <summary>
    /// The permission of the Guest role that lets a signed-in guest call the endpoint for their own booking, or
    /// <c>null</c> when the endpoint is for staff only (MIL-018).
    /// </summary>
    public string? GuestPermission { get; set; }

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
    /// The answer to a call without a valid staff token (MIL-018): without a guest token it is the 401 of ADR-0005; a
    /// refused guest token gets the one refusal of the guest sign-in; a valid guest token passes only where the Guest role
    /// holds the guest permission of the endpoint, and gets 403 everywhere else.
    /// </summary>
    /// <param name="http">The request.</param>
    /// <returns>The refusal, or <c>null</c> to let the guest through to the use case.</returns>
    private ObjectResult? GuestRefusal(HttpContext http)
    {
        if (GuestAuthentication.IsRefused(http))
        {
            return GuestAuthentication.Refusal();
        }

        if (GuestAuthentication.BookingIdOf(http) is null)
        {
            return Refuse(StatusCodes.Status401Unauthorized, ErrorCodes.Api.StaffUnauthorized);
        }

        return GuestPermission is not null && RolePermissions.HasPermission(Roles.Guest, GuestPermission)
            ? null
            : Refuse(StatusCodes.Status403Forbidden, ErrorCodes.Api.Forbidden);
    }

    private static ObjectResult Refuse(int status, string code) =>
        new(new ApiResponse([code])) { StatusCode = status };
}
