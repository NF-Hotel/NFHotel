using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Et bookbart rum i tilgængelighedsopslaget (BR-34, BR-36, BR-37).
/// </summary>
/// <remarks>
/// Bevidst uden status: listen indeholder kun rum der <i>er</i> ledige, så en statuskolonne
/// ville kun kunne have én værdi. Typen bor i <c>Rooms</c> og ikke i <c>Bookings</c>, fordi
/// det er et rum — selv om det er bookingens use case der efterspørger det.
/// </remarks>
/// <param name="RoomId">Rummets id.</param>
/// <param name="RoomNumber">Værelsesnummer.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Værelsestype.</param>
/// <param name="Capacity">Antal personer rummet kan rumme.</param>
public sealed record AvailableRoomDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);