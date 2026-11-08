using Microsoft.AspNetCore.Antiforgery;

namespace NFHotel.Web.Security;

/// <summary>
/// The light or dark theme of the back office: a cookie that AdminLayout writes to <c>data-theme</c>.
/// Without the cookie the shell follows the system (app.css section 12). No JavaScript, so the
/// Content Security Policy stays <c>script-src 'self'</c> (MIL-033).
/// </summary>
public static class AdminTheme
{
    /// <summary>The path the theme form posts to.</summary>
    public const string TogglePath = "/admin/theme";

    /// <summary>The name of the form field with the chosen theme.</summary>
    public const string Field = "theme";

    /// <summary>The name of the form field with the page to return to.</summary>
    public const string ReturnField = "returnUrl";

    /// <summary>The value that follows the system again.</summary>
    public const string System = "system";

    /// <summary>The light theme.</summary>
    public const string Light = "light";

    /// <summary>The dark theme.</summary>
    public const string Dark = "dark";

    private const string CookieName = "nfhotel.theme";

    /// <summary>The theme the user chose, or <c>null</c> when the shell follows the system.</summary>
    public static string? Read(HttpContext? context) =>
        context?.Request.Cookies.TryGetValue(CookieName, out var value) == true && value is Light or Dark
            ? value
            : null;

    /// <summary>Maps the endpoint that stores the choice and sends the user back to the page.</summary>
    public static WebApplication MapAdminTheme(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var requireTls = HttpsRequirement.IsRequired(app.Configuration);

        app.MapPost(TogglePath, async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context).ConfigureAwait(false))
            {
                return Results.BadRequest();
            }

            var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            var theme = form[Field].ToString();
            var options = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = requireTls || context.Request.IsHttps,
                Path = "/admin",
            };

            if (theme is Light or Dark)
            {
                options.MaxAge = TimeSpan.FromDays(365);
                context.Response.Cookies.Append(CookieName, theme, options);
            }
            else
            {
                context.Response.Cookies.Delete(CookieName, options);
            }

            return Results.LocalRedirect(ReturnUrl.Safe(form[ReturnField].ToString(), "/admin"));
        }).RequireAuthorization();

        return app;
    }
}
