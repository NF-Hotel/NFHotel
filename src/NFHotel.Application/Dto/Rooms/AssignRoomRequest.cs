namespace NFHotel.Application.Dto.Rooms;

/// <summary>The housekeeper that a Manager gives a room (MIL-060).</summary>
/// <param name="StaffId">The housekeeper, or <c>null</c> to leave the room without one.</param>
public sealed record AssignRoomRequest(int? StaffId);
