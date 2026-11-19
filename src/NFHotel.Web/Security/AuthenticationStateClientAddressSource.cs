using Microsoft.AspNetCore.Components.Authorization;

using NFHotel.Web.Common;

namespace NFHotel.Web.Security;

/// <summary>
/// Reads the address of the browser from the authentication state of the page or the circuit, and from the request only
/// where no authentication state is set up (MIL-087, ADR-0051 decision 4).
/// </summary>
public sealed class AuthenticationStateClientAddressSource : IClientAddressSource
{
    #region Fields

    private readonly AuthenticationStateProvider _provider;
    private readonly IHttpContextAccessor _accessor;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the source.</summary>
    /// <param name="provider">The authentication state of the circuit or the request.</param>
    /// <param name="accessor">The request, where no authentication state is set up.</param>
    public AuthenticationStateClientAddressSource(AuthenticationStateProvider provider, IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(accessor);

        _provider = provider;
        _accessor = accessor;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public async ValueTask<string?> GetAddressAsync()
    {
        AuthenticationState state;
        try
        {
            state = await _provider.GetAuthenticationStateAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // The provider has no state outside the scope of a component, and no way to ask for one other than this
            // refusal; the request of such a call still has the user that ClientAddressMiddleware gave the address.
            return ClientAddressClaims.Of(_accessor.HttpContext?.User);
        }

        return ClientAddressClaims.Of(state.User);
    }

    #endregion Public methods
}
