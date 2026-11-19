using System.Security.Claims;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

using NFHotel.Application.Dto.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Security;

/// <summary>
/// Registers the guest sign-in of the web application (MIL-018, ADR-0047 decision 6): a second cookie scheme beside the
/// staff cookie, the holder of the guest token for the use-case runner, the tickets of the booking flow, the two
/// endpoints that start and end a guest session, and the redirect of an old address that carried a booking number.
/// </summary>
public static class GuestSignInRegistration
{
    #region Fields

    /// <summary>The page where a guest signs in with the reference and the email of the booking (ADR-0049).</summary>
    public const string SignInPath = "/booking";

    /// <summary>The page of the signed-in guest's own booking.</summary>
    public const string MyBookingPath = "/my-booking";

    /// <summary>The endpoint that turns a ticket of the booking flow into the guest cookie.</summary>
    public const string SessionPath = "/my-booking/start";

    /// <summary>The endpoint that signs the guest out.</summary>
    public const string SignOutPath = "/my-booking/sign-out";

    /// <summary>The name of the form field that carries the ticket of the booking flow.</summary>
    public const string TicketField = "ticket";

    #endregion Fields

    #region Public methods

    /// <summary>
    /// Adds the guest cookie scheme, the holder of the guest token and the tickets of the booking flow. The cookie is
    /// <c>HttpOnly</c>, <c>SameSite=Lax</c>, sent only to the pages of the guest's own booking, and <c>Secure</c> behind
    /// the TLS proxy; it expires with the guest token.
    /// </summary>
    /// <param name="services">The service collection; the staff sign-in is the default scheme and stays it.</param>
    /// <param name="requireTls">True when TLS is in front of the application: the cookie is then always <c>Secure</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddGuestSignIn(this IServiceCollection services, bool requireTls = false)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication()
            .AddCookie(GuestClaims.CookieScheme, options =>
            {
                options.Cookie.Name = GuestClaims.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;

                // The browser sends the guest cookie only to the guest's own booking and its two endpoints, never to the
                // booking flow, the back office or the circuit.
                options.Cookie.Path = MyBookingPath;

                // Always Secure behind the TLS proxy (ADR-0006); Secure when the request is HTTPS otherwise.
                options.Cookie.SecurePolicy = requireTls ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.LoginPath = SignInPath;
                options.SlidingExpiration = false;
            });

        services.AddScoped<GuestTokenHolder>();
        services.AddScoped<IGuestTokenSource>(provider => provider.GetRequiredService<GuestTokenHolder>());
        services.AddSingleton<GuestSessionTickets>();
        return services;
    }

    /// <summary>
    /// Maps the endpoints that start a guest session from a ticket and end a guest session, and the redirect of an old
    /// address of the sign-in.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static WebApplication MapGuestSession(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Before MIL-085 the sign-in took a booking number from the address (/booking/FLZ-000123). An old link now goes
        // to the empty sign-in: the number is neither filled in nor looked up, and a reference never stands in an address,
        // where access logs and the history of the browser would keep it (ADR-0049 decisions 3 and 5).
        app.MapGet(SignInPath + "/{address}", () => Results.Redirect(SignInPath));

        app.MapPost(SessionPath, async (HttpContext context, IAntiforgery antiforgery, GuestSessionTickets tickets) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context).ConfigureAwait(false))
            {
                return Results.BadRequest();
            }

            IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
            GuestSessionDto? session = tickets.Redeem(form[TicketField].ToString());
            if (session is null)
            {
                // A used or expired ticket: the guest signs in with the reference and the email instead.
                return Results.Redirect(SignInPath);
            }

            await SignInAsync(context, session).ConfigureAwait(false);
            return Results.Redirect(MyBookingPath);
        });

        app.MapPost(SignOutPath, async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(context).ConfigureAwait(false))
            {
                return Results.BadRequest();
            }

            await context.SignOutAsync(GuestClaims.CookieScheme).ConfigureAwait(false);
            return Results.Redirect(SignInPath);
        });
        return app;
    }

    /// <summary>Sets the guest cookie for a session: it expires when the guest token does, and is not renewed.</summary>
    /// <param name="context">The request whose answer carries the cookie.</param>
    /// <param name="session">The guest session from the API.</param>
    /// <returns>A task that completes when the cookie is set.</returns>
    public static Task SignInAsync(HttpContext context, GuestSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(session);

        return context.SignInAsync(
            GuestClaims.CookieScheme,
            GuestClaims.CreatePrincipal(session),
            new AuthenticationProperties { ExpiresUtc = session.ExpiresAt, IsPersistent = true, AllowRefresh = false });
    }

    /// <summary>Reads the signed-in guest of a request from the guest cookie.</summary>
    /// <param name="context">The request, or <c>null</c> in an interactive circuit, which has none.</param>
    /// <returns>The principal of the guest, or <c>null</c> when no valid guest cookie came with the request.</returns>
    public static async Task<ClaimsPrincipal?> ReadGuestAsync(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        AuthenticateResult result = await context.AuthenticateAsync(GuestClaims.CookieScheme).ConfigureAwait(false);
        return result.Succeeded ? result.Principal : null;
    }

    #endregion Public methods
}
