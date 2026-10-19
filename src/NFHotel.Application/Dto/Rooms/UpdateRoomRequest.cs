using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Change the master data of a room (BR-82, BR-84, BR-97 to BR-100, BR-113).
/// </summary>
/// <remarks>
/// <b>Deliberately contains neither <c>Status</c> nor <c>HousekeepingStatus</c> (A-08).</b>
/// A status change has its own use case on <see cref="IRoomService"/>, so a retry from
/// the mobile app cannot roll a cleaning state back by sending an
/// outdated value along. At the same time BR-82's "RoomId == 0 means create" dies: creation and
/// update are two methods.
/// </remarks>
/// <param name="RoomId">The id of the room. Must be greater than 0.</param>
/// <param name="RoomNumber">Room number.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Room type.</param>
/// <param name="Capacity">The number of people the room can hold.</param>
public sealed record UpdateRoomRequest(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);
