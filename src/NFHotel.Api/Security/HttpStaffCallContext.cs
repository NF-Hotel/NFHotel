using NFHotel.Api.Contracts;
using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>
/// Reads who is calling from the validated staff token and the request (ADR-0015). The address is the one that the
/// web application forwards in <see cref="ApiAuthentication.ClientAddressHeader"/> (it is authenticated by its service token), or the
/// address of the connection when there is none.
/// </summary>
public sealed class HttpStaffCallContext : IStaffCallContext
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Creates the context.</summary>
    /// <param name="accessor">Gives the current request.</param>
    public HttpStaffCallContext(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    /// <inheritdoc />
    public int? StaffId =>
        int.TryParse(_accessor.HttpContext?.User.FindFirst("sub")?.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var id)
            && _accessor.HttpContext?.User.Identity?.IsAuthenticated == true
                ? id
                : null;

    /// <inheritdoc />
    public string? StaffName =>
        _accessor.HttpContext?.User.Identity?.IsAuthenticated == true
            ? _accessor.HttpContext.User.FindFirst(JwtStaffTokenIssuer.NameClaim)?.Value
            : null;

    /// <inheritdoc />
    public string? RemoteAddress
    {
        get
        {
            var context = _accessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            var forwarded = context.Request.Headers[ApiAuthentication.ClientAddressHeader].ToString().Split(',')[0].Trim();
            return forwarded.Length > 0 ? forwarded : context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
