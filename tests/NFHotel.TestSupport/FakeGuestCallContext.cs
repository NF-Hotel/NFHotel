using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>A guest call context that a test fills in: the booking in the guest token of the call, or none (MIL-018).</summary>
public sealed class FakeGuestCallContext : IGuestCallContext
{
    #region Properties

    /// <inheritdoc />
    public int? BookingId { get; set; }

    #endregion Properties
}
