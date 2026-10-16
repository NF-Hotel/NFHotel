namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Hvilke overgange der er lovlige på et rum lige nu (A-07).
/// </summary>
/// <remarks>
/// Rummets pendant til <c>BookingActionsDto</c>. Felterne kommer fra domænets
/// <c>Room.Can*</c>-properties, som er de samme udtryk overgangsmetoderne bruger som guard.
/// Web må derfor ikke udlede lovligheden af <c>Status</c> og <c>HousekeepingStatus</c> selv —
/// det ville kopiere tilstandsmaskinen ud i præsentationslaget (A-07).
/// <para>
/// Et felt der er <c>false</c> betyder "denne handling flytter ikke rummet". En overgang til
/// den tilstand rummet allerede har er stadig et lovligt kald (A-08), men får <c>false</c>,
/// fordi felterne findes for at tegne knapper — se kommentaren ved <c>Room.Can*</c>.
/// </para>
/// </remarks>
/// <param name="CanTakeOutOfService">Om rummet må tages ud af drift.</param>
/// <param name="CanSendToMaintenance">Om rummet må spærres for vedligehold.</param>
/// <param name="CanReturnToService">Om rummet må sættes i drift igen.</param>
/// <param name="CanMarkDailyCleaningDue">Om daglig rengøring må markeres som manglende.</param>
/// <param name="CanMarkDepartureCleaningDue">Om slutrengøring må markeres som manglende.</param>
/// <param name="CanStartCleaning">Om rengøringen må startes.</param>
/// <param name="CanCompleteCleaning">Om rengøringen må afsluttes.</param>
/// <param name="CanReportServiceNeeded">Om et servicebehov må meldes.</param>
/// <param name="CanStartService">Om servicen må startes.</param>
/// <param name="CanCompleteService">
/// Om servicen må afsluttes. Dækker begge udfald — "rent" og "skal rengøres" — fordi guarden
/// er den samme; UI'ets to knapper deler derfor dette ene felt.
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