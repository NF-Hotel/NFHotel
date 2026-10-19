namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Which transitions are legal on a room right now (A-07).
/// </summary>
/// <remarks>
/// The room's counterpart to <c>BookingActionsDto</c>. The fields come from the domain's
/// <c>Room.Can*</c> properties, which are the same expressions the transition methods use as a guard.
/// Web must therefore not derive the legality from <c>Status</c> and <c>HousekeepingStatus</c> itself —
/// that would copy the state machine out into the presentation layer (A-07).
/// <para>
/// A field that is <c>false</c> means "this action does not move the room". A transition to
/// the state the room already has is still a legal call (A-08), but gets <c>false</c>,
/// because the fields exist to draw buttons — see the comment at <c>Room.Can*</c>.
/// </para>
/// </remarks>
/// <param name="CanTakeOutOfService">Whether the room may be taken out of service.</param>
/// <param name="CanSendToMaintenance">Whether the room may be blocked for maintenance.</param>
/// <param name="CanReturnToService">Whether the room may be put back in service.</param>
/// <param name="CanMarkDailyCleaningDue">Whether daily cleaning may be marked as due.</param>
/// <param name="CanMarkDepartureCleaningDue">Whether departure cleaning may be marked as due.</param>
/// <param name="CanStartCleaning">Whether the cleaning may be started.</param>
/// <param name="CanCompleteCleaning">Whether the cleaning may be completed.</param>
/// <param name="CanReportServiceNeeded">Whether a service need may be reported.</param>
/// <param name="CanStartService">Whether the service may be started.</param>
/// <param name="CanCompleteService">
/// Whether the service may be completed. Covers both outcomes — "clean" and "needs cleaning" — because the guard
/// is the same; the two buttons of the UI therefore share this one field.
/// </param>
public sealed record RoomActionsDto(
    bool CanTakeOutOfService,
    bool CanSendToMaintenance,
    bool CanReturnToService,
    bool CanMarkDailyCleaningDue,
    bool CanMarkDepartureCleaningDue,
    bool CanStartCleaning,
    bool CanCompleteCleaning,
    bool CanReportServiceNeeded,
    bool CanStartService,
    bool CanCompleteService);
