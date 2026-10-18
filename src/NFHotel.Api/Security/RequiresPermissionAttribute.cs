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
            context.Result = Refuse(StatusCodes.Status401Unauthorized, ErrorCodes.Api.StaffUnauthorized);
            return;
        }

        var allowed = StaffRoleParser.TryParse(user.FindFirst(JwtStaffTokenIssuer.RoleClaim)?.Value, out var role)
            && RolePermissions.HasPermission(role, Permission);
        if (!allowed)
        {
            context.Result = Refuse(StatusCodes.Status403Forbidden, ErrorCodes.Api.Forbidden);
        }
    }

    private static ObjectResult Refuse(int status, string code) =>
        new(new ApiResponse([code])) { StatusCode = status };
}