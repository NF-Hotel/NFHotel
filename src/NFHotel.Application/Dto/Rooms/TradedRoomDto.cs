namespace NFHotel.Application.Dto.Rooms;

/// <summary>A room in a trade (MIL-060).</summary>
/// <param name="RoomId">The room.</param>
/// <param name="RoomNumber">The number on the door.</param>
public sealed record TradedRoomDto(int RoomId, string RoomNumber);
