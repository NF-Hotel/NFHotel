using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>
/// Validation of the guest token that arrives in <see cref="ApiAuthentication.GuestTokenHeader"/> (MIL-018, ADR-0047).
/// The staff token stays the default scheme and the only one that fills the user of the request, so nothing that reads
/// the staff user can take a guest for a member of staff. The guest token is read only for a call without a valid staff
/// token, and the outcome is kept on the request for <see cref="RequiresPermissionAttribute"/> and
/// <see cref="HttpGuestCallContext"/>.
/// </summary>
public static class GuestAuthentication
{
    #region Fields

    /// <summary>The name of the authentication scheme of the guest token.</summary>
    public const string Scheme = "GuestToken";

    private const string BookingItem = "NFHotel.Guest.BookingId";
    private const string RefusedItem = "NFHotel.Guest.Refused";

    #endregion Fields

    #region Public methods

    /// <summary>
    /// Registers the validation of the guest token: the issuer and the key of the staff tokens, the audience of a guest
    /// token, the role Guest and a booking key. A token that is tampered, expired, signed with another key or meant for
    /// staff is refused.
    /// </summary>
    /// <param name="services">The service collection; <see cref="StaffSecurityOptions"/> must be registered.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddGuestAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication().AddJwtBearer(Scheme);
        services.AddOptions<JwtBearerOptions>(Scheme)
            .Configure<StaffSecurityOptions>((bearer, staff) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = ApiTokenValidation.For(staff, JwtGuestTokenIssuer.Audience);
                bearer.Events = new JwtBearerEvents
                {
                    // The Authorization header belongs to the token of the web application (ADR-0004), the staff header
                    // to the staff token (ADR-0005).
                    OnMessageReceived = context =>
                    {
                        ApiTokenValidation.ReadToken(context, ApiAuthentication.GuestTokenHeader);
                        return Task.CompletedTask;
                    },

                    // A valid signature is not enough: the token must name the role Guest and one booking.
                    OnTokenValidated = context =>
                    {
                        if (BookingIdIn(context.Principal) is null)
                        {
                            context.Fail("The token is not the token of a guest.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });
        return services;
    }

    /// <summary>
    /// Reads the guest token of a call that has no valid staff token, after the staff authentication: a valid one gives
    /// the booking of the guest, any other one marks the call as refused.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseGuestAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            await AuthenticateGuestAsync(context).ConfigureAwait(false);
            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>Reads the booking of the signed-in guest of a request.</summary>
    /// <param name="context">The request, or <c>null</c> outside a request.</param>
    /// <returns>The key of the booking in a valid guest token, or <c>null</c>.</returns>
    public static int? BookingIdOf(HttpContext? context) =>
        context is not null && context.Items.TryGetValue(BookingItem, out object? value) && value is int bookingId
            ? bookingId
            : null;

    /// <summary>
    /// The one refusal of a guest (ADR-0047 decision 3): 401 with the one code and nothing else, so a wrong email, an
    /// unknown number, a cancelled booking and an expired, tampered or foreign guest token all get the same text.
    /// </summary>
    /// <returns>The answer.</returns>
    public static ObjectResult Refusal() =>
        new(new ApiResponse([ErrorCodes.Api.GuestUnauthorized])) { StatusCode = StatusCodes.Status401Unauthorized };

    /// <summary>Tells whether the request carried a guest token that was refused.</summary>
    /// <param name="context">The request.</param>
    /// <returns>True for a guest token that is expired, tampered, signed with another key or not a guest token.</returns>
    public static bool IsRefused(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.ContainsKey(RefusedItem);
    }

    #endregion Public methods

    #region Private methods

    private static async Task AuthenticateGuestAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            || string.IsNullOrWhiteSpace(context.Request.Headers[ApiAuthentication.GuestTokenHeader].ToString()))
        {
            return;
        }

        AuthenticateResult result = await context.AuthenticateAsync(Scheme).ConfigureAwait(false);
        if (result.Succeeded && BookingIdIn(result.Principal) is int bookingId)
        {
            context.Items[BookingItem] = bookingId;
            return;
        }

        context.Items[RefusedItem] = true;
    }

    private static int? BookingIdIn(ClaimsPrincipal? principal) =>
        principal?.FindFirst(JwtStaffTokenIssuer.RoleClaim)?.Value == Roles.Guest
        && int.TryParse(principal.FindFirst(JwtGuestTokenIssuer.BookingClaim)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int bookingId)
        && bookingId > 0
            ? bookingId
            : null;

    #endregion Private methods
}
