namespace NFHotel.Domain.Errors;

/// <summary>Error codes of the room assignments and the trades of rooms between housekeepers (MIL-060).</summary>
public static class RoomTradeErrors
{
    /// <summary>The trade does not exist.</summary>
    public const string NotFound = "room_trade.not_found";

    /// <summary>The caller is not the member of staff who may answer or withdraw the trade.</summary>
    public const string NotYours = "room_trade.not_yours";

    /// <summary>The trade is not in a state that allows the step.</summary>
    public const string NotOpen = "room_trade.not_open";

    /// <summary>A room of the trade changed hands meanwhile, so the trade was called off.</summary>
    public const string RoomsChanged = "room_trade.rooms_changed";

    /// <summary>The trade gives no room.</summary>
    public const string NoRooms = "room_trade.no_rooms";

    /// <summary>A room is named twice in the trade.</summary>
    public const string DuplicateRoom = "room_trade.duplicate_room";

    /// <summary>The trade is with oneself.</summary>
    public const string SameStaff = "room_trade.same_staff";

    /// <summary>The colleague is not an active housekeeper.</summary>
    public const string NotHousekeeper = "room_trade.not_housekeeper";

    /// <summary>A room offered is not assigned to the caller.</summary>
    public const string RoomNotYours = "room_trade.room_not_yours";

    /// <summary>A room asked for is not assigned to the colleague.</summary>
    public const string RoomNotTheirs = "room_trade.room_not_theirs";

    /// <summary>The key of a room or a member of staff is not valid.</summary>
    public const string KeyInvalid = "room_trade.key_invalid";

    /// <summary>A room can only be assigned to an active housekeeper.</summary>
    public const string AssigneeInvalid = "room_assignment.assignee_invalid";
}
