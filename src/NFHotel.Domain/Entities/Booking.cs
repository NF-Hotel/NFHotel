using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A reservation of one room for one guest for one period. Aggregate root.
/// </summary>
/// <remarks>
/// The booking knows only its own fields. Rules that need knowledge of <i>other</i>
/// bookings — for example overlap on creation (BR-19) — belong in the Application layer, which calls
/// <see cref="Booking.ConflictsWith"/>.
/// <para>
/// The domain does not know the clock: "today" (<c>today</c>) and actual timestamps
/// (<c>occurredAt</c>) are always passed in as parameters. That makes the state machine deterministically
/// testable and keeps the time zone policy in one place in Application (A-05).
/// </para>
/// </remarks>
public sealed class Booking : Entity<int>
{
    private const string EntityName = "booking";
    private const string BookingNumberPrefix = "FLZ-";
    private const string BookingNumberFormat = "D6";

    /// <summary>For EF Core. Use <see cref="Create"/> in code.</summary>
    private Booking()
    {
    }

    /// <summary>Arrival date without a time of day (B-08).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Departure date without a time of day, exclusive (B-08).
    /// </summary>
    /// <remarks>
    /// The <i>booked</i> end date. NEVER changed by a check-out (A-01) — it is
    /// history. It is <see cref="EffectiveEndDate"/> that governs occupancy.
    /// </remarks>
    public DateOnly EndDate { get; private set; }

    /// <summary>Actual check-in time. Null until check-in (B-08).</summary>
    public DateTimeOffset? CheckInTime { get; private set; }

    /// <summary>Actual check-out time. Null until check-out (B-08).</summary>
    public DateTimeOffset? CheckOutTime { get; private set; }

    /// <summary>
    /// The hotel-local date on which the guest actually checked out. Null until check-out.
    /// </summary>
    /// <remarks>
    /// A field of its own, not derived from <see cref="CheckOutTime"/>: converting a
    /// timestamp to a hotel-local date is a time zone policy that neither the domain nor
    /// the database can perform deterministically (A-02, A-03). The date is therefore supplied from outside.
    /// </remarks>
    public DateOnly? CheckOutDate { get; private set; }

    /// <summary>The life cycle state of the booking. Changed only through the methods of the class.</summary>
    public BookingStatus Status { get; private set; }

    /// <summary>Foreign key to the room. Always greater than 0 (BR-45, BR-105).</summary>
    public int RoomId { get; private set; }

    /// <summary>Foreign key to the guest. Always greater than 0 (BR-106).</summary>
    public int GuestId { get; private set; }

    /// <summary>
    /// Navigation to the room. Filled only when the call asked for it; never use it as a guard.
    /// </summary>
    public Room? Room { get; private set; }

    /// <summary>
    /// Navigation to the guest. Filled only when the call asked for it; never use it as a guard.
    /// </summary>
    public Guest? Guest { get; private set; }

    /// <summary>The booked period as a value object. History — not used for overlap checks.</summary>
    public DateRange Period => new(StartDate, EndDate);

    /// <summary>
    /// The end date up to which the booking actually occupies the room (A-01):
    /// <c>GREATEST(StartDate + 1 day, COALESCE(CheckOutDate, EndDate))</c>.
    /// </summary>
    /// <remarks>
    /// Early check-out thus releases the remaining nights automatically, without touching
    /// <see cref="EndDate"/>. <c>StartDate + 1</c> is the floor, so a check-out on the same
    /// day as arrival does not give an empty period — the generated database column
    /// <c>effective_end_date</c> computes exactly the same expression.
    /// </remarks>
    public DateOnly EffectiveEndDate
    {
        get
        {
            var actualEnd = CheckOutDate ?? EndDate;
            var minimumEnd = StartDate.AddDays(1);

            return actualEnd > minimumEnd ? actualEnd : minimumEnd;
        }
    }

    /// <summary>
    /// The period during which the booking actually blocks the room: <c>[StartDate, EffectiveEndDate)</c>.
    /// This is the period type overlap checks must use — not <see cref="Period"/> (A-01).
    /// </summary>
    public DateRange EffectivePeriod => new(StartDate, EffectiveEndDate);

    /// <summary>Number of booked nights (BR-109, BR-117).</summary>
    public int NumberOfNights => Period.Nights;

    /// <summary>
    /// Displayed booking number, for example <c>FLZ-000123</c> (BR-109). Derived, never persisted.
    /// </summary>
    public string BookingNumber => $"{BookingNumberPrefix}{Id.ToString(BookingNumberFormat)}";

    /// <summary>True if the booking may be confirmed (BR-02, BR-121).</summary>
    public bool CanConfirm => Status == BookingStatus.Pending;

    /// <summary>True if the booking may be checked out (BR-09).</summary>
    public bool CanCheckOut =>
        Status == BookingStatus.CheckedIn && CheckInTime is not null && CheckOutTime is null;

    /// <summary>True if the booking may be cancelled (BR-11, BR-122).</summary>
    public bool CanCancel => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>True if the dates and the room of the booking may be changed (BR-14, BR-123, A-06).</summary>
    public bool CanReschedule => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>
    /// Decides whether the booking may be checked in on the given date (BR-06).
    /// </summary>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <returns>True if check-in is allowed. Note B-01: <c>Confirmed</c> is not required.</returns>
    public bool CanCheckIn(DateOnly today) =>
        (Status is BookingStatus.Pending or BookingStatus.Confirmed)
        && CheckInTime is null
        && StartDate <= today;

    /// <summary>
    /// Creates a new booking with status <see cref="BookingStatus.Pending"/> (BR-01, BR-48).
    /// </summary>
    /// <param name="period">The booking period. Must start on or after <paramref name="today"/>.</param>
    /// <param name="roomId">The id of the room. Must be greater than 0.</param>
    /// <param name="guestId">The id of the guest. Must be greater than 0.</param>
    /// <param name="today">Today's date in the hotel's time zone. Supplied by the caller — the domain does not know the clock.</param>
    /// <returns>A new, valid booking that is not yet persisted.</returns>
    /// <exception cref="DomainException">
    /// On a start date in the past (BR-44, BR-104), a missing room (BR-45, BR-105) or
    /// a missing guest (BR-106).
    /// </exception>
    public static Booking Create(DateRange period, int roomId, int guestId, DateOnly today)
    {
        GuardPeriodIsNotInThePast(period, today);
        GuardRoomId(roomId);
        GuardGuestId(guestId);

        return new Booking
        {
            StartDate = period.Start,
            EndDate = period.End,
            RoomId = roomId,
            GuestId = guestId,
            Status = BookingStatus.Pending
        };
    }

    /// <summary>
    /// Confirms the booking — payment is guaranteed (BR-02, BR-03, BR-05).
    /// Confirmation is not a prerequisite for check-in (B-01).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is not exactly <see cref="BookingStatus.Pending"/>.
    /// </exception>
    public void Confirm()
    {
        if (!CanConfirm)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Confirm), Status);
        }

        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Checks the guest in (BR-06, BR-07). Allowed from both <see cref="BookingStatus.Pending"/>
    /// and <see cref="BookingStatus.Confirmed"/> (B-01).
    /// </summary>
    /// <param name="occurredAt">The actual check-in time.</param>
    /// <param name="today">
    /// Today's date in the hotel's time zone. Supplied separately, because converting a timestamp
    /// to a local calendar date is a time zone policy that belongs in the Application layer.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is neither Pending nor Confirmed, if <see cref="CheckInTime"/>
    /// is already set, or if <see cref="StartDate"/> is after <paramref name="today"/>.
    /// </exception>
    public void CheckIn(DateTimeOffset occurredAt, DateOnly today)
    {
        if (!CanCheckIn(today))
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CheckIn), Status);
        }

        CheckInTime = occurredAt;
        Status = BookingStatus.CheckedIn;
    }

    /// <summary>
    /// Checks the guest out (BR-09, BR-10).
    /// </summary>
    /// <param name="occurredAt">
    /// The actual check-out time. Must be strictly after <see cref="CheckInTime"/> (BR-107).
    /// </param>
    /// <param name="checkOutDate">
    /// The hotel-local date on which the check-out happened (A-03). Part of
    /// <see cref="EffectiveEndDate"/> and thus releases the remaining nights on early
    /// check-out, without <see cref="EndDate"/> changing (A-01).
    /// </param>
    /// <param name="today">
    /// Today's date in the hotel's time zone — parallel to <see cref="CheckIn"/>. Supplied by
    /// the caller, because the domain must never know "today" itself.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is not <see cref="BookingStatus.CheckedIn"/>, or if
    /// <see cref="CheckOutTime"/> is already set.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown if <paramref name="occurredAt"/> is not after <see cref="CheckInTime"/>
    /// (BR-107), or if <paramref name="checkOutDate"/> is before
    /// <see cref="StartDate"/> or after <paramref name="today"/>.
    /// </exception>
    public void CheckOut(DateTimeOffset occurredAt, DateOnly checkOutDate, DateOnly today)
    {
        if (!CanCheckOut)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(CheckOut), Status);
        }

        if (occurredAt <= CheckInTime)
        {
            throw new DomainException(BookingErrors.CheckOutBeforeCheckIn);
        }

        if (checkOutDate < StartDate)
        {
            throw new DomainException(BookingErrors.CheckOutDateBeforeStartDate);
        }

        // A check-out date in the future cannot be corrected afterwards: a CheckedOut booking
        // can be neither rescheduled nor cancelled, and the date would block the room until
        // it via EffectiveEndDate (A-01). Hence an upper bound, not only a lower one.
        if (checkOutDate > today)
        {
            throw new DomainException(BookingErrors.CheckOutDateInFuture);
        }

        CheckOutTime = occurredAt;
        CheckOutDate = checkOutDate;
        Status = BookingStatus.CheckedOut;
    }

    /// <summary>
    /// Cancels the booking (BR-11, BR-13). Soft delete — the row is never deleted.
    /// The only transition that releases the room completely (A-01).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is neither Pending nor Confirmed. A booking that is checked
    /// in or out cannot be cancelled.
    /// </exception>
    public void Cancel()
    {
        if (!CanCancel)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Cancel), Status);
        }

        Status = BookingStatus.Cancelled;
    }

    /// <summary>
    /// Moves the booking to a new period and/or a new room (BR-14, BR-17, BR-18, BR-20).
    /// </summary>
    /// <param name="newPeriod">The new period. Must start on or after <paramref name="today"/> (BR-108).</param>
    /// <param name="newRoomId">The id of the new room. Must be greater than 0.</param>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is neither Pending nor Confirmed.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown if the new period starts in the past (BR-108) or the id of the room is not valid.
    /// </exception>
    /// <remarks>
    /// Does NOT check overlap against other bookings — that requires knowledge of other
    /// bookings and therefore belongs in the Application layer (BR-19).
    /// </remarks>
    public void Reschedule(DateRange newPeriod, int newRoomId, DateOnly today)
    {
        if (!CanReschedule)
        {
            throw InvalidStateTransitionException.For(EntityName, nameof(Reschedule), Status);
        }

        GuardPeriodIsNotInThePast(newPeriod, today);
        GuardRoomId(newRoomId);

        StartDate = newPeriod.Start;
        EndDate = newPeriod.End;
        RoomId = newRoomId;
    }

    /// <summary>Rejects a period that starts before today (BR-44, BR-104, BR-108).</summary>
    private static void GuardPeriodIsNotInThePast(DateRange period, DateOnly today)
    {
        if (!period.StartsOnOrAfter(today))
        {
            throw new DomainException(BookingErrors.StartDateInPast);
        }
    }

    /// <summary>Rejects a missing room reference (BR-45, BR-105).</summary>
    private static void GuardRoomId(int roomId)
    {
        if (roomId <= 0)
        {
            throw new DomainException(BookingErrors.RoomRequired);
        }
    }

    /// <summary>Rejects a missing guest reference (BR-106).</summary>
    private static void GuardGuestId(int guestId)
    {
        if (guestId <= 0)
        {
            throw new DomainException(BookingErrors.GuestRequired);
        }
    }

    /// <summary>
    /// Decides whether this booking conflicts with a desired period on a room (BR-19).
    /// </summary>
    /// <param name="roomId">The room the desired period applies to.</param>
    /// <param name="desiredPeriod">The desired period.</param>
    /// <returns>True if this booking blocks the desired period.</returns>
    /// <remarks>
    /// The comparison is made against <see cref="EffectivePeriod"/>, not <see cref="Period"/>
    /// (A-01): a guest who leaves early must release the remaining nights. The caller is
    /// responsible for excluding the booking itself when editing. The period is half-open
    /// (B-03, BR-39): departure and arrival on the same day are not an overlap.
    /// </remarks>
    public bool ConflictsWith(int roomId, DateRange desiredPeriod) =>
        RoomId == roomId
        && Status.BlocksRoom()
        && EffectivePeriod.Overlaps(desiredPeriod);
}
