using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Create a new room (BR-81, BR-97 to BR-100, BR-113).
/// </summary>
/// <remarks>
/// No status: a newly created room is always bookable and clean (BR-81). The caller does not choose it.
/// </remarks>
/// <param name="RoomNumber">Room number, for example "101" or "12B".</param>
/// <param name="Floor">Floor. Must be greater than 0.</param>
/// <param name="Size">Room type.</param>
/// <param name="Capacity">The number of people the room can hold. Must be greater than 0.</param>
public sealed record CreateRoomRequest(
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);
