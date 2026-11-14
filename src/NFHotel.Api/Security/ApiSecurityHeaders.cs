namespace NFHotel.Api.Security;

/// <summary>
/// The header that tells a browser to take an answer of the API for the type that it declares, JSON, and never to
/// guess another (MIL-077). The API scan of ZAP found the answers without it (alert 10021); the sign-in and the rooms
/// reach a client through nginx (ADR-0015), which adds no header of its own. The other headers of the web pages
/// (MIL-033) govern documents that a browser renders, and the API serves none.
/// </summary>
public static class ApiSecurityHeaders
{
    /// <summary>The value of <c>X-Content-Type-Options</c>: the browser does not guess the type of the answer.</summary>
    public const string NoSniff = "nosniff";

    /// <summary>
    /// Writes the header when each response starts. Registered first in the pipeline, so a refusal of the token
    /// middleware and an answer of the exception handler get it too.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseApiSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            HttpResponse response = context.Response;
            response.OnStarting(() =>
            {
                response.Headers.XContentTypeOptions = NoSniff;
                return Task.CompletedTask;
            });

            await next(context).ConfigureAwait(false);
        });
    }
}
