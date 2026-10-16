using Microsoft.AspNetCore.Components.Authorization;

using NFHotel.Web.Common;

namespace NFHotel.Web.Security;

/// <summary>Reads the staff token from the claims of the circuit's authentication state.</summary>
public sealed class AuthenticationStateStaffTokenSource : IStaffTokenSource
{
    private readonly AuthenticationStateProvider _provider;

    /// <summary>Creates the source.</summary>
    /// <param name="provider">The authentication state of the circuit or the request.</param>
    public AuthenticationStateStaffTokenSource(AuthenticationStateProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetTokenAsync()
    {
        try
        {
            var state = await _provider.GetAuthenticationStateAsync().ConfigureAwait(false);
            return state.User.Identity?.IsAuthenticated == true ? state.User.FindFirst(StaffClaims.TokenClaim)?.Value : null;
        }
        catch (InvalidOperationException)
        {
            // No authentication state is set up yet (for example before the first render): the call is anonymous.
            return null;
        }
    }
}