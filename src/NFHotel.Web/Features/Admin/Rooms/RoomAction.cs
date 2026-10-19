namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// The transitions the room overview can perform on one room.
/// </summary>
/// <remarks>
/// One value per method on <c>IRoomService</c> (A-07). An enum and not a string, so a button
/// that points to a transition that does not exist becomes a compiler error.
/// <para>
/// The last two values are the same use case — <c>CompleteServiceAsync</c> — each with its
/// own answer to whether the room must be cleaned afterwards. They are split here because they are two
/// different buttons for the user, not two different rules.
/// </para>
/// </remarks>
public enum RoomAction
{
    /// <summary>Take the room out of service.</summary>
    TakeOutOfService = 0,

    /// <summary>Send the room to maintenance.</summary>
    SendToMaintenance = 1,

    /// <summary>Put the room back in service.</summary>
    ReturnToService = 2,

    /// <summary>Mark that daily cleaning is due.</summary>
    MarkDailyCleaningDue = 3,

    /// <summary>Mark that departure cleaning is due.</summary>
    MarkDepartureCleaningDue = 4,

    /// <summary>Start the cleaning.</summary>
    StartCleaning = 5,

    /// <summary>Finish the cleaning.</summary>
    CompleteCleaning = 6,

    /// <summary>Meld servicebehov.</summary>
    ReportServiceNeeded = 7,

    /// <summary>Start the service.</summary>
    StartService = 8,

    /// <summary>Finish the service — the room must be cleaned afterwards.</summary>
    CompleteServiceStillDirty = 9,

    /// <summary>Finish the service — the room is clean.</summary>
    CompleteServiceClean = 10
}
