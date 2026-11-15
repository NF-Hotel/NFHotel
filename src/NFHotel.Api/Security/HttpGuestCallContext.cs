using NFHotel.Application.Ports;

namespace NFHotel.Api.Security;

/// <summary>
/// Reads the signed-in guest of a request from the guest token that <see cref="GuestAuthentication"/> validated
/// (MIL-018, ADR-0047). A request with a valid staff token, without a guest token or with a refused one has no guest.
/// </summary>
public sealed class HttpGuestCallContext : IGuestCallContext
{
    #region Fields

    private readonly IHttpContextAccessor _accessor;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the context.</summary>
    /// <param name="accessor">Gives the current request.</param>
    public HttpGuestCallContext(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        _accessor = accessor;
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    public int? BookingId => GuestAuthentication.BookingIdOf(_accessor.HttpContext);

    #endregion Properties
}
