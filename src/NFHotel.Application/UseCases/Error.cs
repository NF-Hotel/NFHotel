using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases;

/// <summary>
/// One rule violation, identified by its error code.
/// </summary>
/// <remarks>
/// The code is the contract (A-09) — not a user-facing text. The UI chooses text from the code and
/// can thereby translate; a later API can map the code to an HTTP status. That is why
/// the type deliberately does not carry an English <c>Message</c> as the old system did.
/// </remarks>
/// <param name="Code">The error code, for example <c>"booking.room_not_available"</c>. See <see cref="ErrorCodes"/>.</param>
public sealed record Error(string Code);