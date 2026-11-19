using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// A guest's sign-in with the reference and the email of a booking (MIL-018, ADR-0049 decision 2). Both travel in the
/// body of the request, never in an address, so they stay out of access logs.
/// </summary>
/// <param name="Reference">
/// The reference as the guest typed it. Text on purpose: anything that is not a reference gets the one refusal of the
/// sign-in, not an answer of its own from the model validation of the API.
/// </param>
/// <param name="Email">
/// The email of the booking, in any letter case. Nullable on purpose: a missing email reaches the use case, which
/// refuses it with its own code on every path, instead of the model validation of the API answering it alone.
/// </param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GuestSignInRequest(string? Reference, string? Email);
