namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// What a successful guest sign-in gives the web application (MIL-018, ADR-0047): the token for the guest header of
/// the API and its expiry. The web application keeps the token in the guest cookie, which ends when the token does.
/// </summary>
/// <param name="Token">The signed token that the API wants in the guest header.</param>
/// <param name="ExpiresAt">When the token stops being valid; the guest cookie expires then too.</param>
/// <remarks>
/// The session names no booking: the key of the booking stays inside the API, in the claim of the token that only the
/// API reads (ADR-0049 decision 3).
/// </remarks>
public sealed record GuestSessionDto(string Token, DateTimeOffset ExpiresAt);
