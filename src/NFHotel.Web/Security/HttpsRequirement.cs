namespace NFHotel.Web.Security;

/// <summary>
/// Makes the web application insist on HTTPS when the deployment says that TLS is in front of it (<c>Tls:Require</c>,
/// ADR-0006). The proxy ends TLS and tells the application with <c>X-Forwarded-Proto</c>; a request that still arrives
/// as HTTP, for example straight on the container, is sent to HTTPS and never served.
/// </summary>
public static class HttpsRequirement
{
    /// <summary>The configuration key that turns the requirement on.</summary>
    public const string SettingName = "Tls:Require";

    /// <summary>Reads the setting; a missing or unreadable value is false (local development).</summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>True when HTTPS is required.</returns>
    public static bool IsRequired(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return bool.TryParse(configuration[SettingName], out var required) && required;
    }

    /// <summary>
    /// Redirects a request that is not HTTPS (after the forwarded headers have been applied) with a permanent redirect
    /// that keeps the method. Does nothing when <paramref name="required"/> is false.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="required">Whether HTTPS is required.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseHttpsRequirement(this IApplicationBuilder app, bool required)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!required)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            if (context.Request.IsHttps)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var request = context.Request;
            var target = $"https://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            context.Response.Redirect(target, permanent: true, preserveMethod: true);
        });
    }
}