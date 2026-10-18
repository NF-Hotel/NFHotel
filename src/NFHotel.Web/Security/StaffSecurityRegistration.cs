using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;

namespace NFHotel.Web.Security;

/// <summary>Registers the sign-in of the back office in the web application (ADR-0005).</summary>
public static class StaffSecurityRegistration
{
    /// <summary>The path of the login page.</summary>
    public const string LoginPath = "/admin/login";

    /// <summary>The page that tells a signed-in user that the role lacks the permission.</summary>
    public const string AccessDeniedPath = "/admin/denied";

    /// <summary>The path that signs out.</summary>
    public const string LogoutPath = "/admin/logout";

    /// <summary>
    /// Adds the cookie, one policy per <see cref="Permissions"/> built from <see cref="RolePermissions"/>, the cascading
    /// authentication state and the source of the staff token for the use-case runner.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="requireTls">True when TLS is in front of the application: the cookie is then always <c>Secure</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStaffSignIn(this IServiceCollection services, bool requireTls = false)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(StaffClaims.CookieScheme)
            .AddCookie(StaffClaims.CookieScheme, options =>
            {
                options.Cookie.Name = "nfhotel.staff";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;

                // Always Secure behind the TLS proxy (ADR-0006); Secure when the request is HTTPS otherwise.
                options.Cookie.SecurePolicy = requireTls ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.LoginPath = LoginPath;
                options.ReturnUrlParameter = "returnUrl";
                options.AccessDeniedPath = AccessDeniedPath;
                options.SlidingExpiration = false;
            });

        services.AddAuthorizationBuilder().AddPolicies();
        services.AddCascadingAuthenticationState();
        services.AddScoped<IStaffTokenSource, AuthenticationStateStaffTokenSource>();
        return services;
    }

    /// <summary>Maps the sign-out endpoint.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static WebApplication MapStaffSignOut(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(LogoutPath, async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context).ConfigureAwait(false))
            {
                return Results.BadRequest();
            }

            await context.SignOutAsync(StaffClaims.CookieScheme).ConfigureAwait(false);
            return Results.Redirect(LoginPath);
        });
        return app;
    }

    private static void AddPolicies(this Microsoft.AspNetCore.Authorization.AuthorizationBuilder builder)
    {
        foreach (var permission in Permissions.All)
        {
            builder.AddPolicy(permission, policy => policy
                .AddAuthenticationSchemes(StaffClaims.CookieScheme)
                .RequireAssertion(context => StaffClaims.Holds(context.User, permission)));
        }
    }
}