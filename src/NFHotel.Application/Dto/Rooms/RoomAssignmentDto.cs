namespace NFHotel.Application.Dto.Rooms;

/// <summary>A room and the housekeeper who looks after it (MIL-060).</summary>
/// <param name="RoomId">The room.</param>
/// <param name="RoomNumber">The number on the door.</param>
/// <param name="Floor">The floor.</param>
/// <param name="StaffId">The housekeeper, or <c>null</c> for a room without one.</param>
/// <param name="StaffName">The name of the housekeeper, or <c>null</c>.</param>
public sealed record RoomAssignmentDto(int RoomId, string RoomNumber, int Floor, int? StaffId, string? StaffName);
