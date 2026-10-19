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
/// <param name="FirstName">Fornavn.</param>
/// <param name="LastName">Efternavn.</param>
/// <param name="Email">Email. Must contain both '@' and '.' (BR-55, BR-93).</param>
/// <param name="PhoneNumber">Phone number. No format requirement (BR-94).</param>
/// <param name="Country">Land.</param>
/// <param name="PassportNumber">Passport number. Optional and not validated beyond its length (BR-57).</param>
public sealed record GuestFields(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Country,
    string? PassportNumber);
