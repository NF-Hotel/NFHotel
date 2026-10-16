namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// En gæst med alle felter, til redigeringsformularen.
/// </summary>
/// <remarks>
/// Eneste DTO der bærer pasnummeret i klartekst, og kun fordi formularen skal kunne rette
/// det. Brug <see cref="GuestListItemDto"/> alle andre steder.
/// </remarks>
/// <param name="GuestId">Gæstens id.</param>
/// <param name="FirstName">Fornavn.</param>
/// <param name="LastName">Efternavn.</param>
/// <param name="FullName">"Fornavn Efternavn".</param>
/// <param name="Email">E-mail.</param>
/// <param name="PhoneNumber">Telefonnummer.</param>
/// <param name="Country">Land.</param>
/// <param name="PassportNumber">Pasnummer, eller <c>null</c> hvis der ikke er registreret et (BR-57).</param>
public sealed record GuestDetailsDto(
    int GuestId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string Country,
    string? PassportNumber);