using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// A bookable room in the availability lookup (BR-34, BR-36, BR-37).
/// </summary>
/// <remarks>
/// Deliberately without status: the list contains only rooms that <i>are</i> free, so a status column
/// could only have one value. The type lives in <c>Rooms</c> and not in <c>Bookings</c>, because
/// it is a room — even though it is the booking use case that asks for it.
/// </remarks>
/// <param name="RoomId">Rummets id.</param>
/// <param name="RoomNumber">Room number.</param>
/// <param name="Floor">Etage.</param>
/// <param name="Size">Room type.</param>
/// <param name="Capacity">The number of people the room can hold.</param>
public sealed record AvailableRoomDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);