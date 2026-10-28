using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.TestSupport;

/// <summary>
/// Builds rooms in a desired cleaning state.
/// </summary>
/// <remarks>
/// The states are reached solely through the legal transitions, so the factory cannot
/// produce a state the room could not itself have reached.
/// </remarks>
internal static class RoomFactory
{
    /// <summary>Default room number.</summary>
    internal const string RoomNumber = "101";

    /// <summary>Default floor. Must be > 0 (BR-98).</summary>
    internal const int Floor = 1;

    /// <summary>Default capacity. Must be > 0 (BR-100).</summary>
    internal const int Capacity = 2;

    /// <summary>
    /// Creates a new room: bookable and clean (BR-81).
    /// </summary>
    /// <returns>A valid, non-persisted room.</returns>
    internal static Room Create() => Room.Create(RoomNumber, Floor, RoomSize.Double, Capacity);

    /// <summary>
    /// Creates a room and runs it to the desired cleaning state.
    /// </summary>
    /// <param name="status">The desired cleaning state.</param>
    /// <returns>A room in <paramref name="status"/>.</returns>
    internal static Room WithHousekeeping(HousekeepingStatus status)
    {
        var room = Create();

        switch (status)
        {
            case HousekeepingStatus.Clean:
                break;

            case HousekeepingStatus.DailyCleaningDue:
                room.MarkDailyCleaningDue();
                break;

            case HousekeepingStatus.DepartureCleaningDue:
                room.MarkDepartureCleaningDue();
                break;

            case HousekeepingStatus.CleaningInProgress:
                room.MarkDepartureCleaningDue();
                room.StartCleaning();
                break;

            case HousekeepingStatus.ServiceRequired:
                room.ReportServiceNeeded();
                break;

            case HousekeepingStatus.ServiceInProgress:
                room.ReportServiceNeeded();
                room.StartService();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown cleaning status.");
        }

        return room;
    }
}
