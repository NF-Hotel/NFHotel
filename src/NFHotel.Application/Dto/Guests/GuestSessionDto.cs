namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// What a successful guest sign-in gives the web application (MIL-018, ADR-0047): the token for the guest header of
/// the API, its expiry and the booking it opens. The web application keeps it in the guest cookie, which ends when the
/// token does.
/// </summary>
/// <param name="Token">The signed token that the API wants in the guest header.</param>
/// <param name="ExpiresAt">When the token stops being valid; the guest cookie expires then too.</param>
/// <param name="BookingId">The key of the one booking that the token opens.</param>
public sealed record GuestSessionDto(string Token, DateTimeOffset ExpiresAt, int BookingId);
