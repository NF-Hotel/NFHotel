namespace NFHotel.Web.Security;

/// <summary>
/// Sends a signed-in member of staff whose account must choose a new password to the change page, for every page request
/// below <c>/admin</c> except that page, the sign-in and the sign-out (ADR-0015). The API refuses their other calls
/// anyway; this keeps them from seeing pages that cannot load.
/// </summary>
public static class MustChangePasswordMiddleware
{
    /// <summary>The path of the change page.</summary>
    public const string ChangePasswordPath = "/admin/account/password";

    /// <summary>Adds the redirect to the pipeline; it must come after the authentication.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWithSegments(ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWithSegments(StaffSecurityRegistration.LogoutPath, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWithSegments(StaffSecurityRegistration.LoginPath, StringComparison.OrdinalIgnoreCase)
                && StaffClaims.MustChangePassword(context.User)
                && HttpMethods.IsGet(context.Request.Method))
            {
                context.Response.Redirect(ChangePasswordPath);
                return;
            }

            await next(context).ConfigureAwait(false);
        });
    }
}
