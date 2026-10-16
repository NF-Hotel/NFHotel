using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Ret et rums stamdata (BR-82, BR-84, BR-97 til BR-100, BR-113).
/// </summary>
/// <remarks>
/// <b>Indeholder bevidst hverken <c>Status</c> eller <c>HousekeepingStatus</c> (A-08).</b>
/// Et statusskift har sin egen use case på <see cref="IRoomService"/>, så en retry fra
/// mobilappen ikke kan komme til at rulle en rengøringsstand tilbage ved at sende en
/// forældet værdi med. Samtidig dør BR-82's "RoomId == 0 betyder opret": oprettelse og
/// opdatering er to metoder.
/// </remarks>
/// <param name="RoomId">Rummets id. Skal være større end 0.</param>
/// <param name="RoomNumber">Værelsesnummer.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Værelsestype.</param>
/// <param name="Capacity">Antal personer rummet kan rumme.</param>
public sealed record UpdateRoomRequest(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);