using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Security;

/// <summary>
/// Lets an endpoint through only for a signed-in guest whose role holds the permission (ADR-0049 decision 3): the guest
/// token names the one booking the endpoint works on, so the route takes no key. A call without a guest token, or with
/// one that is expired, tampered, signed with another key or not a guest token, gets the one refusal of the guest
/// sign-in; a call with a valid staff token gets 403, because a route of a guest is never a route of staff, and so does a
/// guest when the Guest role lacks the permission. The use case is not called in any of these cases.
/// </summary>
/// <param name="permission">The permission of the Guest role that the endpoint needs (the Access Control Matrix).</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiresGuestAttribute(string permission) : Attribute, IAuthorizationFilter
{
    #region Properties

    /// <summary>The permission of the Guest role that the endpoint needs.</summary>
    public string Permission { get; } = permission;

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpContext http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated == true)
        {
            context.Result = Forbidden();
            return;
        }

        if (GuestAuthentication.IsRefused(http) || GuestAuthentication.BookingIdOf(http) is null)
        {
            context.Result = GuestAuthentication.Refusal();
            return;
        }

        if (!RolePermissions.HasPermission(Roles.Guest, Permission))
        {
            context.Result = Forbidden();
        }
    }

    #endregion Public methods

    #region Private methods

    private static ObjectResult Forbidden() =>
        new(new ApiResponse([ErrorCodes.Api.Forbidden])) { StatusCode = StatusCodes.Status403Forbidden };

    #endregion Private methods
}
