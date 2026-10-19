namespace NFHotel.Application.Dto.Guests;

/// <summary>
/// A guest with all fields, for the edit form.
/// </summary>
/// <remarks>
/// The only DTO that carries the passport number in plain text, and only because the form must be able to
/// correct it. Use <see cref="GuestListItemDto"/> everywhere else.
/// </remarks>
/// <param name="GuestId">The id of the guest.</param>
/// <param name="FirstName">Fornavn.</param>
/// <param name="LastName">Efternavn.</param>
/// <param name="FullName">"Fornavn Efternavn".</param>
/// <param name="Email">E-mail.</param>
/// <param name="PhoneNumber">Telefonnummer.</param>
/// <param name="Country">Land.</param>
/// <param name="PassportNumber">Passport number, or <c>null</c> if none is registered (BR-57).</param>
public sealed record GuestDetailsDto(
    int GuestId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string PhoneNumber,
    string Country,
    string? PassportNumber);
