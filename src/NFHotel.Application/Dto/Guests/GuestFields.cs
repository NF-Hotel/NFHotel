using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// The six editable guest fields (BR-52 to BR-57, BR-91 to BR-96).
/// </summary>
/// <remarks>
/// A type of its own, because the fields are entered in two places: in the guest form and in "create booking for
/// a new guest" (BR-47). It is the only allowed cross-reference between feature folders, and
/// it goes towards a request — never towards another feature's service.
/// <para>
/// The fields are <c>string</c> and not <c>string?</c>, because they are mandatory. That they
/// can nevertheless be empty is caught by the validation attributes of <c>Guest</c>, not by the type system —
/// an empty string from a form is a user outcome, not a programmer error.
/// </para>
/// </remarks>
/// <param name="FirstName">First name; its characters are the rule of <c>PersonNameAttribute</c> (BR-52, BR-91, MIL-086).</param>
/// <param name="LastName">Last name; its characters are the rule of <c>PersonNameAttribute</c> (BR-53, BR-92, MIL-086).</param>
/// <param name="Email">Email; one plain address, the rule of <c>EmailFormatAttribute</c> (BR-55, BR-93, MIL-086).</param>
/// <param name="PhoneNumber">Phone number; 7 to 15 digits, the rule of <c>PhoneNumberAttribute</c> (BR-54, BR-94, MIL-086).</param>
/// <param name="Country">Country; a name of the list of ISO 3166-1, the rule of <c>CountryNameAttribute</c> (BR-56, BR-95, MIL-086).</param>
/// <param name="PassportNumber">Passport number; optional, ASCII letters and digits, the rule of <c>PassportNumberAttribute</c> (BR-57, MIL-086).</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GuestFields(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Country,
    string? PassportNumber);
