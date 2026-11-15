namespace NFHotel.Application.Ports;

/// <summary>
/// The guest who is calling, when the call carries a valid guest token (MIL-018, ADR-0047). The API fills it from the
/// validated token; every other caller, a member of staff or a command on the server, has none. The use cases read it
/// for the ownership rule: a guest reaches only the booking in the token and the guest of that booking.
/// </summary>
public interface IGuestCallContext
{
    #region Properties

    /// <summary>The key of the booking that the guest signed in to, or <c>null</c> when the caller is no signed-in guest.</summary>
    int? BookingId { get; }

    #endregion Properties
}
