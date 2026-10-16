using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Errors;

/// <summary>
/// The error codes for breaches of the room invariants. The code is the contract (A-09); Application translates it to text.
/// </summary>
public static class RoomErrors
{
    /// <summary>Error code: room number is missing (BR-97).</summary>
    public const string RoomNumberRequired = "room.room_number_required";

    /// <summary>Error code: the room number is longer than <see cref="Room.MaxRoomNumberLength"/> (A-10).</summary>
    public const string RoomNumberTooLong = "room.room_number_too_long";

    /// <summary>Error code: the floor is not greater than 0 (BR-98).</summary>
    public const string FloorInvalid = "room.floor_invalid";

    /// <summary>Error code: the room type is not a defined <see cref="RoomSize"/> (BR-99).</summary>
    public const string SizeInvalid = "room.size_invalid";

    /// <summary>Error code: the capacity is not greater than 0 (BR-100).</summary>
    public const string CapacityInvalid = "room.capacity_invalid";
}