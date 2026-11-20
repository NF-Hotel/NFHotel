namespace NFHotel.Web.Security;

/// <summary>
/// The headers of the page of the link in the mail of a booking request (MIL-088, ADR-0052 decision 3): no referrer, so
/// no address of the page goes to another site, and no store, so no cache keeps the page or the answer to its button.
/// </summary>
/// <remarks>
/// Set when each response starts, like <see cref="SecurityHeaders"/>, so the page, the answer to its post, the redirect
/// after a booking and the refusal of a post without an antiforgery token all carry them. The value of
/// <c>Cache-Control</c> replaces the <c>no-cache, no-store</c> that antiforgery writes on a page with a form.
/// </remarks>
public static class VerifyPageHeaders
{
    #region Fields

    /// <summary>The referrer policy of the page: the browser sends no <c>Referer</c> from it.</summary>
    public const string ReferrerPolicy = "no-referrer";

    /// <summary>The cache policy of the page: neither the browser nor a proxy keeps a copy.</summary>
    public const string CacheControl = "no-store";

    #endregion Fields

    #region Public methods

    /// <summary>Writes the two headers on every response to <see cref="GuestSignInRegistration.VerifyPath"/>.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseVerifyPageHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(GuestSignInRegistration.VerifyPath, StringComparison.OrdinalIgnoreCase))
            {
                IHeaderDictionary headers = context.Response.Headers;
                context.Response.OnStarting(() =>
                {
                    headers["Referrer-Policy"] = ReferrerPolicy;
                    headers.CacheControl = CacheControl;
                    return Task.CompletedTask;
                });
            }

            await next(context).ConfigureAwait(false);
        });
    }

    #endregion Public methods
}
