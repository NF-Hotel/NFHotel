using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Explicit mapping from <see cref="Room"/> to the DTOs of the room (A-14).
/// </summary>
/// <remarks>
/// Hand-written and without a mapping library. The choices of fields are decisions, not boilerplate —
/// convention-based mapping would copy everything that happens to have the same name, and
/// thereby make them for us.
/// <para>
/// Mapping is used only on <b>command paths</b>, where the entity has been fetched anyway.
/// Read paths project directly in the repository (A-11), so a missing <c>Include</c> cannot
/// give silently empty fields.
/// </para>
/// </remarks>
public static class RoomMapping
{
    /// <summary>
    /// Maps a room to a list row.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>The list row.</returns>
    public static RoomListItemDto ToListItem(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomListItemDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity,
            room.Status,
            room.HousekeepingStatus);
    }

    /// <summary>
    /// Maps a room to the detail view.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <param name="hasBookings">
    /// Whether there are bookings for the room. Supplied from outside, because it is a lookup in another
    /// table and not a property of the room.
    /// </param>
    /// <returns>The detail view. <c>IsBookable</c> comes from the domain (BR-110).</returns>
    public static RoomDetailsDto ToDetails(this Room room, bool hasBookings)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new RoomDetailsDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity,
            room.Status,
            room.HousekeepingStatus,
            room.IsBookable,
            hasBookings);
    }

    /// <summary>
    /// Computes which transitions are legal on a room right now (A-07).
    /// </summary>
    /// <param name="status">Rummets bookbarhed.</param>
    /// <param name="housekeepingStatus">The cleaning and service state of the room.</param>
    /// <returns>Handlingerne.</returns>
    /// <remarks>
    /// The predicates mirror <see cref="Room.CanTakeOutOfService"/> and the other
    /// <c>Room.Can*</c> properties, which the transition methods themselves use as a guard. They cannot be
    /// called directly, because both the list and the detail view are projected in the repository and
    /// never materialise the entity (A-11).
    /// <para>
    /// <b>This is the only copy in Application</b> — the <c>Actions</c> of both DTOs and
    /// <see cref="ToActions(Room)"/> call the same function, so the read path and the command path cannot
    /// disagree. The same arrangement as <c>BookingMapping.ToActions</c>, and a
    /// parity test against the domain must pin it.
    /// </para>
    /// </remarks>
    public static RoomActionsDto ToActions(RoomStatus status, HousekeepingStatus housekeepingStatus) =>
        new(
            CanTakeOutOfService: status != RoomStatus.OutOfService,
            CanSendToMaintenance: status != RoomStatus.Maintenance,
            CanReturnToService: status != RoomStatus.Available,
            CanMarkDailyCleaningDue:
                housekeepingStatus is HousekeepingStatus.Clean or HousekeepingStatus.ServiceRequired,
            CanMarkDepartureCleaningDue:
                housekeepingStatus is HousekeepingStatus.Clean
                    or HousekeepingStatus.DailyCleaningDue
                    or HousekeepingStatus.ServiceRequired,
            CanStartCleaning:
                housekeepingStatus is HousekeepingStatus.DailyCleaningDue
                    or HousekeepingStatus.DepartureCleaningDue,
            CanCompleteCleaning: housekeepingStatus == HousekeepingStatus.CleaningInProgress,
            CanReportServiceNeeded:
                housekeepingStatus is not (HousekeepingStatus.ServiceRequired
                    or HousekeepingStatus.ServiceInProgress),
            CanStartService: housekeepingStatus == HousekeepingStatus.ServiceRequired,
            CanCompleteService: housekeepingStatus == HousekeepingStatus.ServiceInProgress);

    /// <summary>
    /// Computes the legal transitions for a room entity (A-07).
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>Handlingerne.</returns>
    public static RoomActionsDto ToActions(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return ToActions(room.Status, room.HousekeepingStatus);
    }

    /// <summary>
    /// Maps a room to a row of the availability list.
    /// </summary>
    /// <param name="room">The room.</param>
    /// <returns>The row without status — the list contains only free rooms.</returns>
    public static AvailableRoomDto ToAvailableRoom(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return new AvailableRoomDto(
            room.Id,
            room.RoomNumber,
            room.Floor,
            room.Size,
            room.Capacity);
    }
}