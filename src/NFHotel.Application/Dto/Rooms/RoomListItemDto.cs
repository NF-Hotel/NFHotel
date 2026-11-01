using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// A room as shown in a list or a table row.
/// </summary>
/// <remarks>
/// No formatting: dates are dates, status is an enum, not a colour or text. Formatting
/// belongs in Web — otherwise the old system's problem just moves one layer down.
/// <para>
/// <see cref="Actions"/> is a computed property and not a constructor parameter: the row
/// is built as a SQL projection in the repository (A-11), and the database cannot fill a
/// <see cref="RoomActionsDto"/> in it. It is also the list that needs it — the action
/// buttons of the room sit on the selected list item.
/// </para>
/// </remarks>
/// <param name="RoomId">The room id.</param>
/// <param name="RoomNumber">Room number, for example "101" or "12B".</param>
/// <param name="Floor">The floor.</param>
/// <param name="Size">Room type.</param>
/// <param name="Capacity">The number of people the room can hold.</param>
/// <param name="Status">Whether the room can be booked (B-04).</param>
/// <param name="HousekeepingStatus">Cleaning and service state (B-04, A-06).</param>
public sealed record RoomListItemDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity,
    RoomStatus Status,
    HousekeepingStatus HousekeepingStatus)
{
    /// <summary>
    /// Which transitions are legal on the room right now (A-07). Same source as
    /// <see cref="RoomDetailsDto.Actions"/>.
    /// </summary>
    public RoomActionsDto Actions => RoomMapping.ToActions(Status, HousekeepingStatus);
}
