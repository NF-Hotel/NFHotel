using System.Security.Claims;

namespace NFHotel.Web.Security;

/// <summary>
/// The address of the browser as a claim of the user (MIL-087, ADR-0051 decision 4): the one claim of an identity without
/// authentication, beside the identities of the sign-ins, so the user stays who they were. Blazor fills the
/// authentication state of a circuit from the user of its connection, so a circuit reads the address as a static page
/// does.
/// </summary>
public static class ClientAddressClaims
{
    #region Fields

    /// <summary>The type of the claim that holds the address.</summary>
    public const string AddressClaim = "nfhotel:client_address";

    #endregion Fields

    #region Public methods

    /// <summary>Makes the identity without authentication that holds an address.</summary>
    /// <param name="address">The address in its one form.</param>
    /// <returns>The identity.</returns>
    public static ClaimsIdentity IdentityFor(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // No authentication type, so the identity is not authenticated, and authorization and antiforgery pass it by.
        return new ClaimsIdentity([new Claim(AddressClaim, address)]);
    }

    /// <summary>Reads the address of a user.</summary>
    /// <param name="user">The user, or <see langword="null"/>.</param>
    /// <returns>The address, or <see langword="null"/> when the user has none.</returns>
    public static string? Of(ClaimsPrincipal? user) => user?.FindFirst(AddressClaim)?.Value;

    #endregion Public methods
}
