using NFHotel.Application.Ports;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The guest context of a caller that is not an HTTP request, for example a command on the server (MIL-018). It knows no
/// guest, so the ownership rule lets nobody through as a guest.
/// </summary>
public sealed class AnonymousGuestCallContext : IGuestCallContext
{
    #region Properties

    /// <inheritdoc />
    public int? BookingId => null;

    #endregion Properties
}
