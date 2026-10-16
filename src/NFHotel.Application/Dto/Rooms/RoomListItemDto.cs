using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Et rum som det vises i en liste eller en tabelrække.
/// </summary>
/// <remarks>
/// Ingen formatering: datoer er datoer, status er enum, ikke farve eller tekst. Formatering
/// hører i Web — ellers flytter det gamle systems problem bare ét lag ned.
/// <para>
/// <see cref="Actions"/> er en beregnet property og ikke en konstruktørparameter: rækken
/// bygges som SQL-projektion i repositoryet (A-11), og databasen kan ikke fylde et
/// <see cref="RoomActionsDto"/> i den. Det er også listen der har brug for den — rummets
/// handlingsknapper sidder på det valgte listeelement.
/// </para>
/// </remarks>
/// <param name="RoomId">Rummets id.</param>
/// <param name="RoomNumber">Værelsesnummer, fx "101" eller "12B".</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Værelsestype.</param>
/// <param name="Capacity">Antal personer rummet kan rumme.</param>
/// <param name="Status">Bookbarhed (B-04).</param>
/// <param name="HousekeepingStatus">Rengørings- og servicestand (B-04, A-06).</param>
public sealed record RoomListItemDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity,
    RoomStatus Status,
    HousekeepingStatus HousekeepingStatus)
{
    /// <summary>
    /// Hvilke overgange der er lovlige på rummet lige nu (A-07). Samme kilde som
    /// <see cref="RoomDetailsDto.Actions"/>.
    /// </summary>
    public RoomActionsDto Actions => RoomMapping.ToActions(Status, HousekeepingStatus);
}