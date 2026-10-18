using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.Entities;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Explicit mapping from <see cref="NFHotel.Domain.Entities.Guest"/> to the DTOs of the guest (A-14).
/// </summary>
/// <remarks>
/// Hand-written and deliberately without a mapping library. Convention-based mapping would copy
/// <c>PassportNumber</c> into <see cref="GuestListItemDto"/> by itself, because the names
/// match — and thereby decrypt the field in every list, silently (B-09, D-04). The choice of fields is
/// a decision, not boilerplate.
/// </remarks>
public static class GuestMapping
{
    /// <summary>
    /// Maps a guest to a list row without passport number.
    /// </summary>
    /// <param name="guest">The guest.</param>
    /// <returns>The list row. The passport number is reduced to a yes/no.</returns>
    public static GuestListItemDto ToListItem(this Guest guest)
    {
        ArgumentNullException.ThrowIfNull(guest);

        return new GuestListItemDto(
            guest.Id,
            guest.FirstName,
            guest.LastName,
            guest.FullName,
            guest.Email,
            guest.PhoneNumber,
            guest.Country,
            !string.IsNullOrWhiteSpace(guest.PassportNumber));
    }

    /// <summary>
    /// Maps a guest to the detail view with passport number in plain text.
    /// </summary>
    /// <param name="guest">The guest.</param>
    /// <returns>The detail view. Used only by the edit form.</returns>
    public static GuestDetailsDto ToDetails(this Guest guest)
    {
        ArgumentNullException.ThrowIfNull(guest);

        return new GuestDetailsDto(
            guest.Id,
            guest.FirstName,
            guest.LastName,
            guest.FullName,
            guest.Email,
            guest.PhoneNumber,
            guest.Country,
            guest.PassportNumber);
    }
}