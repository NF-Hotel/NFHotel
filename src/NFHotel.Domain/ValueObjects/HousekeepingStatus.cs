namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The cleaning and service state of the room. Updated by cleaning staff and service technicians.
/// </summary>
/// <remarks>
/// Does NOT affect bookability — a room can be <see cref="RoomStatus.Available"/> and
/// at the same time wait for cleaning (B-04). <c>Clean = 0</c> is chosen so that
/// <c>default(HousekeepingStatus)</c> and the database <c>DEFAULT 0</c> mean
/// "nothing in the way", parallel to <see cref="RoomStatus.Available"/>.
/// New steps (for example an inspection step) are added at the end — renumbering is expensive.
/// </remarks>
public enum HousekeepingStatus
{
    /// <summary>Clean and ready for a guest. The start value of a newly created room.</summary>
    Clean = 0,

    /// <summary>An occupied room waiting for daily cleaning.</summary>
    DailyCleaningDue = 1,

    /// <summary>The guest has left; the room is waiting for departure cleaning.</summary>
    DepartureCleaningDue = 2,

    /// <summary>The cleaning staff have acknowledged and are at work.</summary>
    CleaningInProgress = 3,

    /// <summary>A problem has been reported; the room is waiting for a service technician.</summary>
    ServiceRequired = 4,

    /// <summary>The service technician has acknowledged and is at work.</summary>
    ServiceInProgress = 5
}
