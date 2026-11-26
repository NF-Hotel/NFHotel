namespace NFHotel.Web.Security;

/// <summary>
/// The browser security headers of every response, pages, static files and redirects alike (MIL-033). The web
/// application sets them rather than nginx, because it knows its own pages and an integration test can check them
/// without the proxy.
/// </summary>
public static class SecurityHeaders
{
    /// <summary>
    /// The Content Security Policy: everything from this site only, with no inline code, no <c>eval</c> and no
    /// wildcard. The pages have no inline script or style (the import map and Blazor's default reconnect dialog are
    /// replaced), and the Blazor websocket is same origin, which <c>connect-src 'self'</c> covers. The directives that
    /// do not fall back to <c>default-src</c> are named. <c>base-uri</c> is <c>'self'</c>, not the <c>'none'</c> of
    /// ASVS 5.0 requirement 3.4.3, because Blazor needs the base element of <c>App.razor</c>; a base element that
    /// markup planted could only point at another path of this site (SAR-002).
    /// </summary>
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    /// <summary>
    /// The referrer policy of every answer: the browser sends the address of a page only to this site, and nothing
    /// to another site, so no address of the booking flow, its dates included, leaves the site in a <c>Referer</c>
    /// (ASVS 5.0 requirement 3.4.5, SAR-002). The page of the link in the mail of a booking request sends none at all
    /// (<see cref="VerifyPageHeaders.ReferrerPolicy"/>).
    /// </summary>
    public const string ReferrerPolicy = "same-origin";

    /// <summary>Turns off the browser features that the site does not use.</summary>
    public const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    /// <summary>The headers and their values, in the order they are written.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> All { get; } =
    [
        new("Content-Security-Policy", ContentSecurityPolicy),
        new("X-Content-Type-Options", "nosniff"),
        new("Referrer-Policy", ReferrerPolicy),
        new("Permissions-Policy", PermissionsPolicy),
        new("Cross-Origin-Opener-Policy", "same-origin"),
        new("Cross-Origin-Embedder-Policy", "require-corp"),
        new("Cross-Origin-Resource-Policy", "same-origin"),
    ];

    /// <summary>
    /// Writes the headers when each response starts. Registered first in the pipeline, so a redirect or an error page
    /// gets them too. A header that a later component set, such as Blazor's own <c>frame-ancestors</c> policy, is
    /// replaced, so the browser receives one complete policy.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            context.Response.OnStarting(() =>
            {
                foreach (var (name, value) in All)
                {
                    headers[name] = value;
                }

                return Task.CompletedTask;
            });

            await next(context).ConfigureAwait(false);
        });
    }
}
