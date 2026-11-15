using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Ports;

/// <summary>
/// Makes the signed token that proves a guest sign-in to the API (MIL-018, ADR-0047): the role Guest and the key of one
/// booking, signed with the key of the staff tokens. Implemented in Infrastructure.
/// </summary>
public interface IGuestTokenIssuer
{
    #region Public methods

    /// <summary>Issues a token for the guest of one booking.</summary>
    /// <param name="bookingId">The key of the booking that the guest has proved, above zero.</param>
    /// <returns>The token, the instant it stops being valid and the booking it is for.</returns>
    GuestSessionDto Issue(int bookingId);

    #endregion Public methods
}
