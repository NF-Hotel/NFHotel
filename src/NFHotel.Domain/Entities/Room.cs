using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A physical room. Aggregate root.
/// </summary>
/// <remarks>
/// The room carries two independent states: bookability (<see cref="Status"/>) and
/// cleaning state (<see cref="HousekeepingStatus"/>) — B-04. The old enum mixed
/// the two and disagreed with its own XAML dropdown (BR-126).
/// <para>
/// All status methods are idempotent: setting a state the room already has does
/// nothing and does not throw (A-08). The mobile app has no offline support and
/// retries over an unstable network — a retry must not give an error to the cleaning staff.
/// Illegal transitions still throw <see cref="InvalidStateTransitionException"/>.
/// </para>
/// </remarks>
public sealed class Room : Entity<int>
{
    /// <summary>Longest allowed room number (A-10). Mirrored by the EF configuration.</summary>
    public const int MaxRoomNumberLength = 20;

    private const string EntityName = "room";

    /// <summary>For EF Core. Use <see cref="Create"/> in code.</summary>
    private Room()
    {
    }

    /// <summary>Room number as text, for example "101" or "12B" (BR-97).</summary>
    [Required(ErrorMessage = RoomErrors.RoomNumberRequired)]
    [StringLength(MaxRoomNumberLength, ErrorMessage = RoomErrors.RoomNumberTooLong)]
    public string RoomNumber { get; private set; } = string.Empty;

    /// <summary>Floor. Always greater than 0 (BR-98).</summary>
    [Range(1, int.MaxValue, ErrorMessage = RoomErrors.FloorInvalid)]
    public int Floor { get; private set; }

    /// <summary>Room type. Was free text in the old system; is now an enum.</summary>
    [EnumDataType(typeof(RoomSize), ErrorMessage = RoomErrors.SizeInvalid)]
    public RoomSize Size { get; private set; }

    /// <summary>The number of people the room can hold. Always greater than 0 (BR-100).</summary>
    [Range(1, int.MaxValue, ErrorMessage = RoomErrors.CapacityInvalid)]
    public int Capacity { get; private set; }

    /// <summary>
    /// Bookability. Changed only through <see cref="TakeOutOfService"/>,
    /// <see cref="SendToMaintenance"/> and <see cref="ReturnToService"/>.
    /// </summary>
    public RoomStatus Status { get; private set; }

    /// <summary>Cleaning and service state. Changed only through the housekeeping methods (A-06).</summary>
    public HousekeepingStatus HousekeepingStatus { get; private set; }

    /// <summary>
    /// True if the room may be let at all (BR-110). Depends EXCLUSIVELY on
    /// <see cref="Status"/> — the cleaning state is deliberately not included (B-04).
    /// </summary>
    public bool IsBookable => Status == RoomStatus.Available;

    /// <summary>
    /// Creates a new room, bookable and clean.
    /// </summary>
    /// <param name="roomNumber">Room number, for example "101" or "12B".</param>
    /// <param name="floor">Floor. Must be greater than 0.</param>
    /// <param name="size">Room type.</param>
    /// <param name="capacity">The number of people the room can hold. Must be greater than 0.</param>
    /// <returns>A new, valid room that is not yet persisted.</returns>
    /// <exception cref="DomainException">
    /// On an empty or too long room number (BR-97, A-10), floor ≤ 0 (BR-98),
    /// unknown <paramref name="size"/> (BR-99) or capacity ≤ 0 (BR-100).
    /// The error code is the first code; all codes are in <see cref="DomainException.Codes"/>.
    /// </exception>
    public static Room Create(string roomNumber, int floor, RoomSize size, int capacity)
    {
        var room = new Room();
        room.Apply(roomNumber, floor, size, capacity);

        // BR-81: a newly created room is bookable and clean. HousekeepingStatus.Clean is
        // also default(HousekeepingStatus), so the database DEFAULT 0 says the same.
        room.Status = RoomStatus.Available;
        room.HousekeepingStatus = HousekeepingStatus.Clean;

        return room;
    }

    /// <summary>
    /// Updates the master data of the room. Touches neither <see cref="Status"/> nor
    /// <see cref="HousekeepingStatus"/> — status changes have their own methods (A-08).
    /// </summary>
    /// <param name="roomNumber">Room number.</param>
    /// <param name="floor">Etage.</param>
    /// <param name="size">Room type.</param>
    /// <param name="capacity">The number of people the room can hold.</param>
    /// <exception cref="DomainException">The same invariants as <see cref="Create"/>.</exception>
    public void UpdateDetails(string roomNumber, int floor, RoomSize size, int capacity) =>
        Apply(roomNumber, floor, size, capacity);

    // --------------------------------------------------------
    // LOVLIGE OVERGANGE (Can*)
    // --------------------------------------------------------
    // One property per transition method. The method below uses the same property as its
    // guard, so the domain and the UI that asks in advance cannot drift apart — the same
    // principle as Booking.CanConfirm / CanCancel (BR-22).
    //
    // The A-08 choice: a transition to the state the room already has is still a legal call
    // (the method does not throw, it does nothing), but Can* is nevertheless FALSE in that situation.
    // The properties here answer "does this action change anything?" and not "may the call be made?",
    // because they exist to draw buttons: a button that demonstrably does nothing is noise and
    // makes the staff think the system ignores them. The idempotence is still there for
    // the retries of the mobile app — it protects a call that has already been sent, not a button.
    // Therefore the no-op check remains its own early return in each method, BEFORE
    // the guard; Can* covers only the states where the transition actually moves the room.

    /// <summary>Whether <see cref="TakeOutOfService"/> will change anything (A-08).</summary>
    public bool CanTakeOutOfService => Status != RoomStatus.OutOfService;

    /// <summary>Whether <see cref="SendToMaintenance"/> will change anything (A-08).</summary>
    public bool CanSendToMaintenance => Status != RoomStatus.Maintenance;

    /// <summary>Whether <see cref="ReturnToService"/> will change anything (A-08).</summary>
    public bool CanReturnToService => Status != RoomStatus.Available;

    /// <summary>
    /// Whether <see cref="MarkDailyCleaningDue"/> is legal and changes anything. Departure cleaning and
    /// work in progress must not be downgraded to daily cleaning.
    /// </summary>
    public bool CanMarkDailyCleaningDue =>
        HousekeepingStatus is HousekeepingStatus.Clean or HousekeepingStatus.ServiceRequired;

    /// <summary>
    /// Whether <see cref="MarkDepartureCleaningDue"/> is legal and changes anything. Cleaning
    /// or service in progress must not be interrupted.
    /// </summary>
    public bool CanMarkDepartureCleaningDue =>
        HousekeepingStatus is HousekeepingStatus.Clean
            or HousekeepingStatus.DailyCleaningDue
            or HousekeepingStatus.ServiceRequired;

    /// <summary>Whether <see cref="StartCleaning"/> is legal and changes anything.</summary>
    public bool CanStartCleaning =>
        HousekeepingStatus is HousekeepingStatus.DailyCleaningDue or HousekeepingStatus.DepartureCleaningDue;

    /// <summary>Whether <see cref="CompleteCleaning"/> is legal and changes anything.</summary>
    public bool CanCompleteCleaning => HousekeepingStatus == HousekeepingStatus.CleaningInProgress;

    /// <summary>Whether <see cref="ReportServiceNeeded"/> is legal and changes anything.</summary>
    public bool CanReportServiceNeeded =>
        HousekeepingStatus is not (HousekeepingStatus.ServiceRequired or HousekeepingStatus.ServiceInProgress);

    /// <summary>Whether <see cref="StartService"/> is legal and changes anything.</summary>
    public bool CanStartService => HousekeepingStatus == HousekeepingStatus.ServiceRequired;

    /// <summary>
    /// Whether <see cref="CompleteService"/> is legal and changes anything. Applies to both outcomes:
    /// the guard is the same, and neither of the end states is <c>ServiceInProgress</c>, so
    /// the no-op case is already excluded.
    /// </summary>
    public bool CanCompleteService => HousekeepingStatus == HousekeepingStatus.ServiceInProgress;

    // --------------------------------------------------------
    // BOOKBARHED (RoomStatus)
    // --------------------------------------------------------
    // There is no business rule that forbids a particular transition between the three
    // statuses, and "no change" is a no-op (A-08). The methods therefore never throw.

    /// <summary>
    /// Takes the room out of service indefinitely. The room cannot be booked.
    /// Is a no-op if the room is already out of service (A-08).
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
    /// Blocks the room temporarily because of a fault that makes it uninhabitable.
    /// Is a no-op if the room is already blocked (A-08).
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
    /// Makes the room bookable again. Does not touch the cleaning state — a room can be
    /// bookable and at the same time wait for cleaning (B-04).
    /// Is a no-op if the room is already bookable (A-08).
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
    // CLEANING AND SERVICE (HousekeepingStatus)
    // --------------------------------------------------------

    /// <summary>
    /// Marks that the occupied room is waiting for daily cleaning.
    /// Is a no-op if the room is already waiting for daily cleaning (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the room is waiting for or undergoing departure cleaning, or is under service.
    /// Departure cleaning ranks above daily cleaning and must not be downgraded.
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
    /// Marks that the room is waiting for departure cleaning after departure. Typically called by
    /// the Application layer immediately after a check-out.
    /// Is a no-op if the room is already waiting for departure cleaning (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the room is under cleaning or service.
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
    /// The cleaning staff acknowledge and start work.
    /// Is a no-op if the cleaning is already in progress (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown unless the room is waiting for daily cleaning or departure cleaning.
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
    /// The cleaning is finished; the room is ready.
    /// Is a no-op if the room is already clean (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown unless the cleaning is in progress.</exception>
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
    /// Reports that the room must be seen by a service technician. Does NOT block the room for
    /// booking — that requires <see cref="SendToMaintenance"/> (B-04).
    /// Is a no-op if the room is already waiting for service (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if service is already in progress.</exception>
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
    /// The service technician acknowledges and starts work.
    /// Is a no-op if the service is already in progress (A-08).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown unless the room is waiting for service.</exception>
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
    /// The service is finished.
    /// </summary>
    /// <param name="requiresCleaning">
    /// True if the work left the room dirty; the room is then set to
    /// departure cleaning instead of clean.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown unless the service is in progress — or the room is already in the desired
    /// end state, which is a no-op (A-08).
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
    /// Enforces the master data invariants and writes the values. Shared by <see cref="Create"/>
    /// and <see cref="UpdateDetails"/>, so the rule exists in one place only (BR-84, BR-113).
    /// </summary>
    private void Apply(string roomNumber, int floor, RoomSize size, int capacity)
    {
        var candidate = new Room
        {
            RoomNumber = roomNumber,
            Floor = floor,
            Size = size,
            Capacity = capacity,
        };

        EntityValidation.ThrowIfInvalid(candidate);

        RoomNumber = roomNumber;
        Floor = floor;
        Size = size;
        Capacity = capacity;
    }
}
