namespace NFHotel.Application.Dto.Rooms;

/// <summary>Every room with its housekeeper, and every active housekeeper (MIL-060).</summary>
/// <param name="Rooms">The rooms, by floor and number.</param>
/// <param name="Housekeepers">The active housekeepers, by name.</param>
/// <param name="MinimumRooms">The fewest rooms a housekeeper keeps without a Manager's approval.</param>
public sealed record RoomAssignmentOverviewDto(
    IReadOnlyList<RoomAssignmentDto> Rooms,
    IReadOnlyList<HousekeeperDto> Housekeepers,
    int MinimumRooms);
