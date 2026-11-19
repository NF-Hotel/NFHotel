using NFHotel.Api.Contracts;

namespace NFHotel.Web.Security;

/// <summary>
/// Adds the address of the browser to the user of every request (MIL-087, ADR-0051 decision 4): the address that nginx
/// wrote into <see cref="ApiAuthentication.ClientAddressHeader"/>, read only from a peer in
/// <see cref="ClientAddresses.StackNetworks"/> and only when it is one address; otherwise the address of the connection,
/// as in development without nginx.
/// </summary>
public static class ClientAddressMiddleware
{
    #region Public methods

    /// <summary>Adds the step to the pipeline; it must come after the authentication, which sets the user.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseClientAddress(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            if (AddressOf(context) is { } address)
            {
                context.User.AddIdentity(ClientAddressClaims.IdentityFor(address));
            }

            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>The address of the browser of a request.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The address in its one form, or <see langword="null"/> when the host gives none.</returns>
    public static string? AddressOf(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Connection.RemoteIpAddress is not { } peer)
        {
            return null;
        }

        // Only nginx, a peer of the stack, can write the header: the web application publishes no port. A list, a second
        // header or a value that is not one address is not trusted, and the connection stands in for it.
        if (ClientAddresses.IsInStack(peer)
            && context.Request.Headers[ApiAuthentication.ClientAddressHeader] is { Count: 1 } header
            && ClientAddresses.TryNormalize(header.ToString(), out string? address))
        {
            return address;
        }

        return ClientAddresses.Normalize(peer);
    }

    #endregion Public methods
}
