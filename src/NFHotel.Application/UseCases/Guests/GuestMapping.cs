using NFHotel.Application.Dto.Guests;
using NFHotel.Domain.Entities;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Eksplicit mapping fra <see cref="NFHotel.Domain.Entities.Guest"/> til gæstens DTO'er (A-14).
/// </summary>
/// <remarks>
/// Håndskrevet og bevidst uden mapping-bibliotek. Konventionsbaseret mapping ville kopiere
/// <c>PassportNumber</c> over i <see cref="GuestListItemDto"/> af sig selv, fordi navnene
/// matcher — og dermed dekryptere feltet i enhver liste, stille (B-09, D-04). Feltvalget er
/// en beslutning, ikke boilerplate.
/// </remarks>
public static class GuestMapping
{
    /// <summary>
    /// Mapper en gæst til en listerække uden pasnummer.
    /// </summary>
    /// <param name="guest">Gæsten.</param>
    /// <returns>Listerækken. Pasnummeret reduceres til et ja/nej.</returns>
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
    /// Mapper en gæst til detaljevisningen med pasnummer i klartekst.
    /// </summary>
    /// <param name="guest">Gæsten.</param>
    /// <returns>Detaljevisningen. Bruges kun af redigeringsformularen.</returns>
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