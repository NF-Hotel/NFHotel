using System.Security.Claims;

using NFHotel.Application.Dto.Guests;

namespace NFHotel.Web.Security;

/// <summary>
/// The claims of a signed-in guest in the web application (MIL-018, ADR-0047 decision 6, ADR-0049 decision 3): the guest
/// token for the API and nothing else, so the cookie names no key of a booking; the API reads the booking from the token.
/// The guest cookie holds it, in a cookie scheme of its own, so a guest is never taken for a member of staff: the staff
/// cookie is the default scheme and the only one the staff pages and policies read.
/// </summary>
public static class GuestClaims
{
    #region Fields

    /// <summary>The name of the cookie scheme of the guest.</summary>
    public const string CookieScheme = "NFHotelGuest";

    /// <summary>The name of the guest cookie.</summary>
    public const string CookieName = "nfhotel.guest";

    /// <summary>The claim that carries the guest token for the API.</summary>
    public const string TokenClaim = "guest_token";

    #endregion Fields

    #region Public methods

    /// <summary>Builds the principal that goes into the guest cookie.</summary>
    /// <param name="session">The session the API gave at the sign-in or the booking.</param>
    /// <returns>The principal; it has no role, so no staff policy can ever accept it.</returns>
    public static ClaimsPrincipal CreatePrincipal(GuestSessionDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ClaimsIdentity identity = new([new Claim(TokenClaim, session.Token)], CookieScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Reads the guest token of a signed-in guest.</summary>
    /// <param name="guest">The principal of the guest cookie, or <c>null</c> when there is none.</param>
    /// <returns>The token, or <c>null</c> when no guest is signed in.</returns>
    public static string? TokenOf(ClaimsPrincipal? guest) =>
        guest?.Identity?.IsAuthenticated == true && guest.FindFirst(TokenClaim)?.Value is { Length: > 0 } token
            ? token
            : null;

    #endregion Public methods
}
