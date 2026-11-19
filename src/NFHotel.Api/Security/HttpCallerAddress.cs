using NFHotel.Api.Contracts;
using NFHotel.Application.Ports;

namespace NFHotel.Api.Security;

/// <summary>
/// The address of the caller of a request, as the API reads it today (ADR-0049 decision 6): the first address that the
/// web application forwards in <see cref="ApiAuthentication.ClientAddressHeader"/>, or the address of the connection
/// when there is none. It is the address of the audit trail (ADR-0017), so the limits of the guest sign-in and the
/// audit trail name the same caller; MIL-087 makes it the address that the proxy saw.
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

        string forwarded = context.Request.Headers[ApiAuthentication.ClientAddressHeader].ToString().Split(',')[0].Trim();
        return forwarded.Length > 0 ? forwarded : context.Connection.RemoteIpAddress?.ToString();
    }

    #endregion Public methods
}
