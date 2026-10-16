using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Errors;

/// <summary>
/// Fejlkoderne for brud på room-invarianterne. Koden er kontrakten (A-09); Application oversætter den til tekst.
/// </summary>
public static class RoomErrors
{
    /// <summary>Fejlkode: værelsesnummer mangler (BR-97).</summary>
    public const string RoomNumberRequired = "room.room_number_required";

    /// <summary>Fejlkode: værelsesnummeret er længere end <see cref="Room.MaxRoomNumberLength"/> (A-10).</summary>
    public const string RoomNumberTooLong = "room.room_number_too_long";

    /// <summary>Fejlkode: etagen er ikke større end 0 (BR-98).</summary>
    public const string FloorInvalid = "room.floor_invalid";

    /// <summary>Fejlkode: værelsestypen er ikke en defineret <see cref="RoomSize"/> (BR-99).</summary>
    public const string SizeInvalid = "room.size_invalid";

    /// <summary>Fejlkode: kapaciteten er ikke større end 0 (BR-100).</summary>
    public const string CapacityInvalid = "room.capacity_invalid";
}