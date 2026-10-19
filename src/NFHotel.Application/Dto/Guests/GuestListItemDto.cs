namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// A guest as shown in a list or a search result.
/// </summary>
/// <remarks>
/// Carries <see cref="HasPassportNumber"/> and <b>not</b> the number itself. The passport number is
/// encrypted at rest (B-09), so every display of the field is a decryption — and none of the
/// 126 ported rules shows the passport number in a list. Privacy by design without costing
/// funktionalitet (D-04).
/// </remarks>
/// <param name="GuestId">The id of the guest.</param>
/// <param name="FirstName">Fornavn.</param>
/// <param name="LastName">Efternavn.</param>
/// <param name="FullName">"First name Last name". Comes from the domain, not from display formatting.</param>
/// <param name="Email">E-mail.</param>
/// <param name="PhoneNumber">Telefonnummer.</param>
/// <param name="Country">Land.</param>
/// <param name="HasPassportNumber">Whether a passport number is registered. The value itself is deliberately left out.</param>
public sealed record GuestListItemDto(
    int GuestId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string Country,
    bool HasPassportNumber);
