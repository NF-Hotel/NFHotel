using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Filter til rumsøgningen (BR-74, BR-76).
/// </summary>
/// <remarks>
/// Alle felter er valgfrie: <c>null</c> betyder "filtrér ikke på dette". "Ryd filter" er
/// derfor et tomt <see cref="RoomFilter"/> — der er ingen særskilt use case for det.
/// Filteret er et <i>objekt</i> og ikke løse parametre, fordi antallet af kriterier
/// allerede var på vej fra tre til fem.
/// </remarks>
/// <param name="Floor">Etage, eller <c>null</c> for alle.</param>
/// <param name="Size">Værelsestype, eller <c>null</c> for alle.</param>
/// <param name="Status">Bookbarhed, eller <c>null</c> for alle.</param>
/// <param name="HousekeepingStatus">Rengørings- og servicestand, eller <c>null</c> for alle.</param>
public sealed record RoomFilter(
    int? Floor = null,
    RoomSize? Size = null,
    RoomStatus? Status = null,
    HousekeepingStatus? HousekeepingStatus = null);