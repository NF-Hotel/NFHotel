using NFHotel.Domain.Errors;
using NFHotel.Domain.Rules;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// Et fysisk værelse. Aggregatrod.
/// </summary>
/// <remarks>
/// Rummet bærer to uafhængige tilstande: bookbarhed (<see cref="Status"/>) og
/// rengøringsstand (<see cref="HousekeepingStatus"/>) — B-04. Den gamle enum blandede
/// de to og var uenig med sin egen XAML-dropdown (BR-126).
/// <para>
/// Alle status-metoder er idempotente: sætter man en tilstand rummet allerede har, sker
/// der intet, og der kastes ikke (A-08). Mobilappen har ingen offline-understøttelse og
/// retry'er over ustabilt netværk — en retry må ikke give en fejl til rengøringspersonalet.
/// Ulovlige overgange kaster fortsat <see cref="InvalidStateTransitionException"/>.
/// </para>
/// </remarks>
public sealed class Room : Entity<int>
{
    /// <summary>Længste tilladte værelsesnummer (A-10). Spejles af EF-konfigurationen.</summary>
    public const int MaxRoomNumberLength = 20;

    private const string EntityName = "room";

    /// <summary>Til EF Core. Brug <see cref="Create"/> i kode.</summary>
    private Room()
    {
    }

    /// <summary>Værelsesnummer som tekst, fx "101" eller "12B" (BR-97).</summary>
    public string RoomNumber { get; private set; } = string.Empty;

    /// <summary>Etage. Altid større end 0 (BR-98).</summary>
    public int Floor { get; private set; }

    /// <summary>Værelsestype. Var fri tekst i det gamle system; er nu enum.</summary>
    public RoomSize Size { get; private set; }

    /// <summary>Antal personer rummet kan rumme. Altid større end 0 (BR-100).</summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Bookbarhed. Ændres kun via <see cref="TakeOutOfService"/>,
    /// <see cref="SendToMaintenance"/> og <see cref="ReturnToService"/>.
    /// </summary>
    public RoomStatus Status { get; private set; }

    /// <summary>Rengørings- og servicestand. Ændres kun via housekeeping-metoderne (A-06).</summary>
    public HousekeepingStatus HousekeepingStatus { get; private set; }

    /// <summary>
    /// Sand hvis rummet overhovedet må udlejes (BR-110). Afhænger UDELUKKENDE af
    /// <see cref="Status"/> — rengøringsstand er bevidst ikke med (B-04).
    /// </summary>
    public bool IsBookable => Status == RoomStatus.Available;

    /// <summary>
    /// Opretter et nyt rum, bookbart og rent.
    /// </summary>
    /// <param name="roomNumber">Værelsesnummer, fx "101" eller "12B".</param>
    /// <param name="floor">Etage. Skal være større end 0.</param>
    /// <param name="size">Værelsestype.</param>
    /// <param name="capacity">Antal personer rummet kan rumme. Skal være større end 0.</param>
    /// <returns>Et nyt, gyldigt rum der endnu ikke er persisteret.</returns>
    /// <exception cref="DomainException">
    /// Ved tomt eller for langt værelsesnummer (BR-97, A-10), etage ≤ 0 (BR-98),
    /// ukendt <paramref name="size"/> (BR-99) eller kapacitet ≤ 0 (BR-100).
    /// Fejlkoden er den første kode fra <see cref="RoomRules.Validate"/>.
    /// </exception>
    public static Room Create(string roomNumber, int floor, RoomSize size, int capacity)
    {
        var room = new Room();
        room.Apply(roomNumber, floor, size, capacity);

        // BR-81: et nyoprettet rum er bookbart og rent. HousekeepingStatus.Clean er
        // tilmed default(HousekeepingStatus), så databasens DEFAULT 0 siger det samme.
        room.Status = RoomStatus.Available;
        room.HousekeepingStatus = HousekeepingStatus.Clean;

        return room;
    }

    /// <summary>
    /// Opdaterer rummets stamdata. Rører hverken <see cref="Status"/> eller
    /// <see cref="HousekeepingStatus"/> — statusskift har deres egne metoder (A-08).
    /// </summary>
    /// <param name="roomNumber">Værelsesnummer.</param>
    /// <param name="floor">Etage.</param>
    /// <param name="size">Værelsestype.</param>
    /// <param name="capacity">Antal personer rummet kan rumme.</param>
    /// <exception cref="DomainException">Samme invarianter som <see cref="Create"/>.</exception>
    public void UpdateDetails(string roomNumber, int floor, RoomSize size, int capacity) =>
        Apply(roomNumber, floor, size, capacity);

    // --------------------------------------------------------
    // LOVLIGE OVERGANGE (Can*)
    // --------------------------------------------------------
    // Én property pr. overgangsmetode. Metoden nedenfor bruger den samme property som sin
    // guard, så domænet og det UI der spørger på forhånd ikke kan drive fra hinanden — samme
    // princip som Booking.CanConfirm / CanCancel (BR-22).
    //
    // A-08-valget: en overgang til den tilstand rummet allerede har er stadig et lovligt kald
    // (metoden kaster ikke, den gør ingenting), men Can* er alligevel FALSE i den situation.
    // Properties her svarer på "ændrer denne handling noget?" og ikke "må kaldet foretages?",
    // fordi de findes for at tegne knapper: en knap der beviseligt ikke gør noget er støj og
    // får personalet til at tro at systemet ignorerer dem. Idempotensen er der stadig for
    // mobilappens retries — den beskytter et kald der allerede er sendt, ikke en knap.
    // Derfor står no-op-checket fortsat som sit eget tidlige return i hver metode, FØR
    // guarden; Can* dækker kun de tilstande hvor overgangen faktisk flytter rummet.

    /// <summary>Om <see cref="TakeOutOfService"/> vil ændre noget (A-08).</summary>
    public bool CanTakeOutOfService => Status != RoomStatus.OutOfService;

    /// <summary>Om <see cref="SendToMaintenance"/> vil ændre noget (A-08).</summary>
    public bool CanSendToMaintenance => Status != RoomStatus.Maintenance;

    /// <summary>Om <see cref="ReturnToService"/> vil ændre noget (A-08).</summary>
    public bool CanReturnToService => Status != RoomStatus.Available;

    /// <summary>
    /// Om <see cref="MarkDailyCleaningDue"/> er lovlig og ændrer noget. Slutrengøring og
    /// igangværende arbejde må ikke nedgraderes til daglig rengøring.
    /// </summary>
    public bool CanMarkDailyCleaningDue =>
        HousekeepingStatus is HousekeepingStatus.Clean or HousekeepingStatus.ServiceRequired;

    /// <summary>
    /// Om <see cref="MarkDepartureCleaningDue"/> er lovlig og ændrer noget. Igangværende
    /// rengøring eller service må ikke afbrydes.
    /// </summary>
    public bool CanMarkDepartureCleaningDue =>
        HousekeepingStatus is HousekeepingStatus.Clean
            or HousekeepingStatus.DailyCleaningDue
            or HousekeepingStatus.ServiceRequired;

    /// <summary>Om <see cref="StartCleaning"/> er lovlig og ændrer noget.</summary>
    public bool CanStartCleaning =>
        HousekeepingStatus is HousekeepingStatus.DailyCleaningDue or HousekeepingStatus.DepartureCleaningDue;

    /// <summary>Om <see cref="CompleteCleaning"/> er lovlig og ændrer noget.</summary>
    public bool CanCompleteCleaning => HousekeepingStatus == HousekeepingStatus.CleaningInProgress;

    /// <summary>Om <see cref="ReportServiceNeeded"/> er lovlig og ændrer noget.</summary>
    public bool CanReportServiceNeeded =>
        HousekeepingStatus is not (HousekeepingStatus.ServiceRequired or HousekeepingStatus.ServiceInProgress);

    /// <summary>Om <see cref="StartService"/> er lovlig og ændrer noget.</summary>
    public bool CanStartService => HousekeepingStatus == HousekeepingStatus.ServiceRequired;

    /// <summary>
    /// Om <see cref="CompleteService"/> er lovlig og ændrer noget. Gælder begge udfald:
    /// guarden er den samme, og ingen af sluttilstandene er <c>ServiceInProgress</c>, så
    /// no-op-tilfældet er allerede udelukket.
    /// </summary>
    public bool CanCompleteService => HousekeepingStatus == HousekeepingStatus.ServiceInProgress;

    // --------------------------------------------------------
    // BOOKBARHED (RoomStatus)
    // --------------------------------------------------------
    // Der findes ingen forretningsregel der forbyder en bestemt overgang mellem de tre
    // statusser, og "ingen ændring" er en no-op (A-08). Metoderne kaster derfor aldrig.

    /// <summary>
    /// Tager rummet ud af drift på ubestemt tid. Rummet kan ikke bookes.
    /// Er en no-op hvis rummet allerede er ude af drift (A-08).
    /// </summary>
    public void TakeOutOfService()
    {
        if (!CanTakeOutOfService)
        {
            return;
        }

        Status = RoomStatus.OutOfService;
    }

    /// <summary>
    /// Spærrer rummet midlertidigt på grund af en fejl der gør det ubeboeligt.
    /// Er en no-op hvis rummet allerede er spærret (A-08).
    /// </summary>
    public void SendToMaintenance()
    {
        if (!CanSendToMaintenance)
        {
            return;
        }

        Status = RoomStatus.Maintenance;
    }

    /// <summary>
    /// Gør rummet bookbart igen. Rører ikke rengøringsstanden — et rum kan være
    /// bookbart og samtidig afvente rengøring (B-04).
    /// Er en no-op hvis rummet allerede er bookbart (A-08).
    /// </summary>
    public void ReturnToService()
    {
        if (!CanReturnToService)
        {
            return;
        }

        Status = RoomStatus.Available;
    }

    // --------------------------------------------------------
    // RENGØRING OG SERVICE (HousekeepingStatus)
    // --------------------------------------------------------

    /// <summary>
    /// Markerer at det beboede rum afventer daglig rengøring.
    /// Er en no-op hvis rummet allerede afventer daglig rengøring (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis rummet afventer eller er under slutrengøring, eller er under service.
    /// Slutrengøring rangerer over daglig rengøring og må ikke nedgraderes.
    /// </exception>
    public void MarkDailyCleaningDue()
    {
        if (HousekeepingStatus == HousekeepingStatus.DailyCleaningDue)
        {
            return;
        }

        if (!CanMarkDailyCleaningDue)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(MarkDailyCleaningDue), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.DailyCleaningDue;
    }

    /// <summary>
    /// Markerer at rummet afventer slutrengøring efter afrejse. Kaldes typisk af
    /// Application-laget umiddelbart efter en udtjekning.
    /// Er en no-op hvis rummet allerede afventer slutrengøring (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes hvis rummet er under rengøring eller service.
    /// </exception>
    public void MarkDepartureCleaningDue()
    {
        if (HousekeepingStatus == HousekeepingStatus.DepartureCleaningDue)
        {
            return;
        }

        if (!CanMarkDepartureCleaningDue)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(MarkDepartureCleaningDue), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.DepartureCleaningDue;
    }

    /// <summary>
    /// Rengøringspersonalet kvitterer og går i gang.
    /// Er en no-op hvis rengøringen allerede er i gang (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes medmindre rummet afventer daglig rengøring eller slutrengøring.
    /// </exception>
    public void StartCleaning()
    {
        if (HousekeepingStatus == HousekeepingStatus.CleaningInProgress)
        {
            return;
        }

        if (!CanStartCleaning)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(StartCleaning), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.CleaningInProgress;
    }

    /// <summary>
    /// Rengøringen er færdig; rummet er klar.
    /// Er en no-op hvis rummet allerede er rent (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Kastes medmindre rengøringen er i gang.</exception>
    public void CompleteCleaning()
    {
        if (HousekeepingStatus == HousekeepingStatus.Clean)
        {
            return;
        }

        if (!CanCompleteCleaning)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CompleteCleaning), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.Clean;
    }

    /// <summary>
    /// Rapporterer at rummet skal ses af en servicetekniker. Spærrer IKKE rummet for
    /// booking — det kræver <see cref="SendToMaintenance"/> (B-04).
    /// Er en no-op hvis rummet allerede afventer service (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Kastes hvis service allerede er i gang.</exception>
    public void ReportServiceNeeded()
    {
        if (HousekeepingStatus == HousekeepingStatus.ServiceRequired)
        {
            return;
        }

        if (!CanReportServiceNeeded)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(ReportServiceNeeded), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.ServiceRequired;
    }

    /// <summary>
    /// Serviceteknikeren kvitterer og går i gang.
    /// Er en no-op hvis servicen allerede er i gang (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Kastes medmindre rummet afventer service.</exception>
    public void StartService()
    {
        if (HousekeepingStatus == HousekeepingStatus.ServiceInProgress)
        {
            return;
        }

        if (!CanStartService)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(StartService), HousekeepingStatus);
        }

        HousekeepingStatus = HousekeepingStatus.ServiceInProgress;
    }

    /// <summary>
    /// Servicen er afsluttet.
    /// </summary>
    /// <param name="requiresCleaning">
    /// Sand hvis arbejdet har efterladt rummet snavset; rummet sættes da til
    /// slutrengøring i stedet for rent.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Kastes medmindre servicen er i gang — eller rummet allerede står i den ønskede
    /// sluttilstand, hvilket er en no-op (A-08).
    /// </exception>
    public void CompleteService(bool requiresCleaning)
    {
        var target = requiresCleaning
            ? HousekeepingStatus.DepartureCleaningDue
            : HousekeepingStatus.Clean;

        if (HousekeepingStatus == target)
        {
            return;
        }

        if (!CanCompleteService)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CompleteService), HousekeepingStatus);
        }

        HousekeepingStatus = target;
    }

    /// <summary>
    /// Håndhæver stamdata-invarianterne og skriver værdierne. Delt af <see cref="Create"/>
    /// og <see cref="UpdateDetails"/>, så reglen kun findes ét sted (BR-84, BR-113).
    /// </summary>
    private void Apply(string roomNumber, int floor, RoomSize size, int capacity)
    {
        var errors = RoomRules.Validate(roomNumber, floor, size, capacity);

        if (errors.Count > 0)
        {
            throw new DomainException(errors[0]);
        }

        RoomNumber = roomNumber;
        Floor = floor;
        Size = size;
        Capacity = capacity;
    }
}