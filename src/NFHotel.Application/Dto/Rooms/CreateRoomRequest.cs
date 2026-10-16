using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Opret et nyt rum (BR-81, BR-97 til BR-100, BR-113).
/// </summary>
/// <remarks>
/// Ingen status: et nyoprettet rum er altid bookbart og rent (BR-81). Kaldet vælger det ikke.
/// </remarks>
/// <param name="RoomNumber">Værelsesnummer, fx "101" eller "12B".</param>
/// <param name="Floor">Etage. Skal være større end 0.</param>
/// <param name="Size">Værelsestype.</param>
/// <param name="Capacity">Antal personer rummet kan rumme. Skal være større end 0.</param>
public sealed record CreateRoomRequest(
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);