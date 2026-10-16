namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// De seks redigérbare gæstefelter (BR-52 til BR-57, BR-91 til BR-96).
/// </summary>
/// <remarks>
/// Egen type, fordi felterne indtastes to steder: i gæsteformularen og i "opret booking for
/// en ny gæst" (BR-47). Det er den eneste tilladte krydsreference mellem feature-mapper, og
/// den går mod en request — aldrig mod en anden features service.
/// <para>
/// Felterne er <c>string</c> og ikke <c>string?</c>, fordi de er obligatoriske. At de
/// alligevel kan være tomme fanges af <c>GuestRules.Validate</c>, ikke af typesystemet —
/// en tom streng fra en formular er et brugerudfald, ikke en programmørfejl.
/// </para>
/// </remarks>
/// <param name="FirstName">Fornavn.</param>
/// <param name="LastName">Efternavn.</param>
/// <param name="Email">E-mail. Skal indeholde både '@' og '.' (BR-55, BR-93).</param>
/// <param name="PhoneNumber">Telefonnummer. Intet formatkrav (BR-94).</param>
/// <param name="Country">Land.</param>
/// <param name="PassportNumber">Pasnummer. Valgfrit og uvalideret ud over længden (BR-57).</param>
public sealed record GuestFields(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Country,
    string? PassportNumber);