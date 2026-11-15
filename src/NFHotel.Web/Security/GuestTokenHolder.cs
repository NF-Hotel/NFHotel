using System.Security.Claims;

using NFHotel.Web.Common;

namespace NFHotel.Web.Security;

/// <summary>
/// Holds the guest token of one request (MIL-018). A guest page reads the guest cookie and hands the principal here; the
/// runner of the same request then gives the token to each use-case call. Scoped, so it never outlives the request, and
/// empty in an interactive circuit, where no page fills it.
/// </summary>
public sealed class GuestTokenHolder : IGuestTokenSource
{
    #region Properties

    /// <inheritdoc />
    public string? Token { get; private set; }

    #endregion Properties

    #region Public methods

    /// <summary>Keeps the token of the signed-in guest, or forgets it when nobody is signed in.</summary>
    /// <param name="guest">The principal of the guest cookie, or <c>null</c>.</param>
    public void Hold(ClaimsPrincipal? guest) => Token = GuestClaims.TokenOf(guest);

    #endregion Public methods
}
