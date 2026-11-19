using NFHotel.Api.Contracts;
using NFHotel.Application.Ports;

namespace NFHotel.Api.Security;

/// <summary>
/// The address of the caller of a request (ADR-0049 decision 6, ADR-0051 decision 4): on a call with the service token
/// of the web application the one address that it names in <see cref="ApiAuthentication.ClientAddressHeader"/>, and
/// otherwise the address of the connection, which the forwarded headers of nginx set when the peer is in the
/// <see cref="ClientAddresses.StackNetworks"/>. A call without the service token, such as one of the mobile app, cannot
/// name an address, and a value that is not one address is ignored. It is the address of the audit trail (ADR-0017),
/// so the limits and the audit trail name the same caller, always in one form.
/// </summary>
public sealed class HttpCallerAddress : ICallerAddress
{
    #region Fields

    private readonly IHttpContextAccessor _accessor;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the address.</summary>
    /// <param name="accessor">Gives the current request.</param>
    public HttpCallerAddress(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        _accessor = accessor;
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    public string? Address => Of(_accessor.HttpContext);

    #endregion Properties

    #region Public methods

    /// <summary>Reads the address of the caller of a request.</summary>
    /// <param name="context">The request, or <see langword="null"/> outside a request.</param>
    /// <returns>The address, or <see langword="null"/> when the request has none.</returns>
    public static string? Of(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        if (ServiceCall.IsMarked(context)
            && context.Request.Headers[ApiAuthentication.ClientAddressHeader] is { Count: 1 } named
            && ClientAddresses.TryNormalize(named.ToString(), out string? address))
        {
            return address;
        }

        return context.Connection.RemoteIpAddress is { } connection ? ClientAddresses.Normalize(connection) : null;
    }

    #endregion Public methods
}
