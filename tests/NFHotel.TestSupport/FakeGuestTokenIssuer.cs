using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>A guest token issuer that hands out readable tokens and counts them, so a test can see what was issued (MIL-018).</summary>
public sealed class FakeGuestTokenIssuer : IGuestTokenIssuer
{
    #region Properties

    /// <summary>When the tokens expire.</summary>
    public DateTimeOffset ExpiresAt { get; set; } = new(2030, 1, 1, 14, 0, 0, TimeSpan.Zero);

    /// <summary>The bookings that a token was issued for, in the order issued.</summary>
    public List<int> Issued { get; } = [];

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public GuestSessionDto Issue(int bookingId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        Issued.Add(bookingId);
        return new GuestSessionDto($"guest-token-{Issued.Count}-booking-{bookingId}", ExpiresAt, bookingId);
    }

    #endregion Public methods
}
