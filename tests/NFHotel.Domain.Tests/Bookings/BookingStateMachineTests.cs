using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The state machine of the booking: Confirm, CheckIn, CheckOut, Cancel and Reschedule.
/// </summary>
/// <remarks>
/// Recreates the 22 tests from the old project's <c>BookingOverviewViewModelTests</c> against
/// the domain instead of against a ViewModel (B-10). The old tests
/// <c>CanExecuteConfirm_NoSelectedBooking_ReturnsFalse</c>,
/// <c>FilterBookings_ExcludesCancelledBookings</c> (BR-23) and
/// <c>SearchText_FiltersBookingsByGuestName</c> (BR-25) are not reproduced here: they are about
/// selection, filtering and searching in a list, which belongs in the Application and Web layers.
/// <para>
/// Covers BR-01..BR-14, BR-17, BR-20, BR-107, BR-108, BR-121, BR-122, BR-123
/// as well as premise B-01 (walk-in) and A-03 (check-out date as parameter).
/// </para>
/// </remarks>
public class BookingStateMachineTests
{
    // --------------------------------------------------------
    // CONFIRMATION (BR-02, BR-05, BR-121 — old T-20, T-27)
    // --------------------------------------------------------

    /// <summary>BR-01, BR-02: a new booking is <c>Pending</c> and may therefore be confirmed.</summary>
    [Fact]
    public void Confirm_PendingBooking_SetsStatusToConfirmed()
    {
        // Arrange
        var booking = BookingFactory.Pending();

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    /// <summary>
    /// BR-02, BR-121: confirmation is possible only from exactly <c>Pending</c>.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.CheckedIn, false)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CanConfirm_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var canConfirm = booking.CanConfirm;

        // Assert
        Assert.Equal(expected, canConfirm);
    }

    /// <summary>
    /// BR-03: the rule is rechecked in the action itself — <c>CanConfirm</c> is not just a
    /// UI hint. If it is called anyway, it is rejected and the status is unchanged.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void Confirm_FromIllegalStatus_ThrowsAndLeavesStatusUnchanged(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        Assert.Throws<InvalidStateTransitionException>(booking.Confirm);

        // Assert
        Assert.Equal(status, booking.Status);
    }

    // --------------------------------------------------------
    // CHECK-IN (BR-06, BR-07 — gamle T-21, T-22, T-23)
    // --------------------------------------------------------

    /// <summary>
    /// T-21, B-01: walk-in. A <c>Pending</c> booking may check in on the arrival day without
    /// first being confirmed — <c>Confirmed</c> means "payment guaranteed", not
    /// "may check in". This test is the very justification for B-01.
    /// </summary>
    [Fact]
    public void CheckIn_PendingBookingOnStartDate_SetsCheckedInWithoutConfirming()
    {
        // Arrange
        var booking = BookingFactory.Pending();

        // Act
        booking.CheckIn(BookingFactory.CheckInMoment, BookingFactory.Today);

        // Assert
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Equal(BookingFactory.CheckInMoment, booking.CheckInTime);
    }

    /// <summary>
    /// T-22, BR-06: check-in is legal from both <c>Pending</c> and <c>Confirmed</c>, and
    /// forbidden from all other statuses.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, false)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CanCheckIn_OnStartDate_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var canCheckIn = booking.CanCheckIn(BookingFactory.Today);

        // Assert
        Assert.Equal(expected, canCheckIn);
    }

    /// <summary>
    /// T-23, BR-06: check-in requires <c>StartDate &lt;= today</c>. The arrival day itself is
    /// allowed, the day before is not, and a late arrival may still check in.
    /// </summary>
    [Theory]
    [InlineData("2025-12-31", false)]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-01-05", true)]
    public void CanCheckIn_ComparesStartDateToToday(string today, bool expected)
    {
        // Arrange
        var booking = BookingFactory.Pending();

        // Act
        var canCheckIn = booking.CanCheckIn(TestDate.Of(today));

        // Assert
        Assert.Equal(expected, canCheckIn);
    }

    /// <summary>BR-06: check-in before the arrival day is rejected even when the method is called directly.</summary>
    [Fact]
    public void CheckIn_BeforeStartDate_ThrowsAndLeavesBookingUntouched()
    {
        // Arrange
        var booking = BookingFactory.Pending();
        var dayBeforeArrival = TestDate.Of("2025-12-31");

        // Act
        Assert.Throws<InvalidStateTransitionException>(
            () => booking.CheckIn(BookingFactory.CheckInMoment, dayBeforeArrival));

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.CheckInTime);
    }

    /// <summary>
    /// BR-06: <c>CheckInTime</c> must not be overwritten by a repeated check-in — unlike
    /// the status methods of the room, check-in is not idempotent (A-08 applies only to the room).
    /// </summary>
    [Fact]
    public void CheckIn_WhenAlreadyCheckedIn_ThrowsAndKeepsOriginalCheckInTime()
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);
        var laterMoment = BookingFactory.CheckInMoment.AddHours(4);

        // Act
        Assert.Throws<InvalidStateTransitionException>(
            () => booking.CheckIn(laterMoment, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingFactory.CheckInMoment, booking.CheckInTime);
    }

    /// <summary>BR-06: a cancelled booking cannot check in (old T test for Cancelled).</summary>
    [Fact]
    public void CheckIn_CancelledBooking_ThrowsWithTraceableErrorCode()
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.Cancelled);

        // Act
        var exception = Assert.Throws<InvalidStateTransitionException>(
            () => booking.CheckIn(BookingFactory.CheckInMoment, BookingFactory.Today));

        // Assert
        Assert.Equal("booking.check_in.invalid_state", exception.Code);
        Assert.Equal(nameof(BookingStatus.Cancelled), exception.CurrentState);
    }

    // --------------------------------------------------------
    // CHECK-OUT (BR-09, BR-10, BR-107 — old T-24)
    // --------------------------------------------------------

    /// <summary>
    /// T-24, BR-09, BR-10, A-03: check-out sets time, hotel-local date and status.
    /// </summary>
    [Fact]
    public void CheckOut_CheckedInBooking_SetsCheckedOutWithTimeAndDate()
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);

        // Act
        booking.CheckOut(BookingFactory.CheckOutMoment, BookingFactory.Departure, BookingFactory.Departure);

        // Assert
        Assert.Equal(BookingStatus.CheckedOut, booking.Status);
        Assert.Equal(BookingFactory.CheckOutMoment, booking.CheckOutTime);
        Assert.Equal(BookingFactory.Departure, booking.CheckOutDate);
    }

    /// <summary>
    /// BR-09: check-out requires exactly <c>CheckedIn</c> — neither a booking that has never been
    /// checked in nor one that has already been checked out may be checked out.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, false)]
    [InlineData(BookingStatus.Confirmed, false)]
    [InlineData(BookingStatus.CheckedIn, true)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CanCheckOut_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var canCheckOut = booking.CanCheckOut;

        // Assert
        Assert.Equal(expected, canCheckOut);
    }

    /// <summary>BR-09: the rule is rechecked in the action.</summary>
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void CheckOut_FromIllegalStatus_ThrowsAndLeavesStatusUnchanged(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        Assert.Throws<InvalidStateTransitionException>(
            () => booking.CheckOut(BookingFactory.CheckOutMoment, BookingFactory.Departure, BookingFactory.Departure));

        // Assert
        Assert.Equal(status, booking.Status);
    }

    /// <summary>
    /// BR-107: the check-out time must be strictly after the check-in time.
    /// The same time is also too early.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-48)]
    public void CheckOut_NotStrictlyAfterCheckIn_ThrowsCheckOutBeforeCheckIn(int hoursFromCheckIn)
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);
        var occurredAt = BookingFactory.CheckInMoment.AddHours(hoursFromCheckIn);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.CheckOut(occurredAt, BookingFactory.Departure, BookingFactory.Departure));

        // Assert
        Assert.Equal(BookingErrors.CheckOutBeforeCheckIn, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
    }

    /// <summary>
    /// A-01, A-03: the check-out date must not lie before the arrival day — otherwise
    /// <c>EffectiveEndDate</c> would describe a period the guest can never have had.
    /// </summary>
    [Theory]
    [InlineData("2025-12-30")]
    [InlineData("2025-12-31")] // the day before arrival
    public void CheckOut_CheckOutDateBeforeStartDate_ThrowsAndLeavesBookingUntouched(string checkOutDate)
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.CheckOut(BookingFactory.CheckOutMoment, TestDate.Of(checkOutDate), BookingFactory.Today));

        // Assert
        Assert.Equal(BookingErrors.CheckOutDateBeforeStartDate, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Null(booking.CheckOutDate);
        Assert.Null(booking.CheckOutTime);
    }

    /// <summary>
    /// The check-out date must not lie after "today". A date in the future cannot be corrected
    /// afterwards — a <c>CheckedOut</c> booking can be neither rescheduled nor cancelled — and
    /// would block the room until then via <c>EffectiveEndDate</c> (A-01).
    /// </summary>
    [Theory]
    [InlineData("2026-01-04", "2026-01-03")] // the day after today
    [InlineData("2026-01-10", "2026-01-03")] // the booked departure day, but it has not come yet
    [InlineData("2030-01-01", "2026-01-03")] // typing mistake in the year
    public void CheckOut_CheckOutDateIsInTheFuture_ThrowsAndLeavesBookingUntouched(
        string checkOutDate,
        string today)
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);
        var occurredAt = BookingFactory.CheckInMoment.AddDays(2);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.CheckOut(occurredAt, TestDate.Of(checkOutDate), TestDate.Of(today)));

        // Assert
        Assert.Equal(BookingErrors.CheckOutDateInFuture, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Null(booking.CheckOutDate);
        Assert.Null(booking.CheckOutTime);
    }

    /// <summary>
    /// The boundary is inclusive: a check-out that is registered the same day it happens goes through.
    /// That is the normal case, so a too tight guard would block all operation.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01")] // check-out on the same day as arrival
    [InlineData("2026-01-03")] // early check-out
    [InlineData("2026-01-10")] // the booked departure day
    public void CheckOut_CheckOutDateIsToday_IsAccepted(string today)
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);
        var checkOutDate = TestDate.Of(today);
        var occurredAt = new DateTimeOffset(checkOutDate, new TimeOnly(18, 0), TimeSpan.Zero);

        // Act
        booking.CheckOut(occurredAt, checkOutDate, checkOutDate);

        // Assert
        Assert.Equal(BookingStatus.CheckedOut, booking.Status);
        Assert.Equal(checkOutDate, booking.CheckOutDate);
    }

    /// <summary>
    /// The order of the two date guards is fixed: a date that breaks both — before
    /// the arrival AND after today — is reported as "before the arrival date". Without the test the
    /// code could swap them, and the user gets a message that points to the wrong problem.
    /// </summary>
    [Fact]
    public void CheckOut_CheckOutDateViolatesBothDateGuards_ReportsBeforeStartDate()
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);
        var beforeArrivalAndAfterToday = TestDate.Of("2025-12-30");
        var today = TestDate.Of("2025-12-29");

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.CheckOut(BookingFactory.CheckOutMoment, beforeArrivalAndAfterToday, today));

        // Assert
        Assert.Equal(BookingErrors.CheckOutDateBeforeStartDate, exception.Code);
    }

    // --------------------------------------------------------
    // CANCELLATION (BR-11, BR-13, BR-122 — old T-25)
    // --------------------------------------------------------

    /// <summary>
    /// T-25, BR-11: only <c>Pending</c> and <c>Confirmed</c> may be cancelled. A guest who has
    /// checked in or out cannot be removed with a mouse click.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, false)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CanCancel_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var canCancel = booking.CanCancel;

        // Assert
        Assert.Equal(expected, canCancel);
    }

    /// <summary>BR-13: cancellation is a soft delete — the status changes, the booking remains.</summary>
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Cancel_FromLegalStatus_SetsStatusToCancelled(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        booking.Cancel();

        // Assert
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(BookingFactory.StandardPeriod, booking.Period);
    }

    /// <summary>BR-11: cancelling a booking that is checked in, checked out or already cancelled is rejected.</summary>
    [Theory]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void Cancel_FromIllegalStatus_ThrowsAndLeavesStatusUnchanged(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        Assert.Throws<InvalidStateTransitionException>(booking.Cancel);

        // Assert
        Assert.Equal(status, booking.Status);
    }

    // --------------------------------------------------------
    // RESCHEDULING (BR-14, BR-17, BR-20, BR-108, BR-123 — old T-26)
    // --------------------------------------------------------

    /// <summary>
    /// T-26, BR-14, BR-123: only <c>Pending</c> and <c>Confirmed</c> may be rescheduled.
    /// Called <c>CanReschedule</c> everywhere, not <c>CanEdit</c> (A-06).
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, false)]
    [InlineData(BookingStatus.CheckedOut, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void CanReschedule_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var canReschedule = booking.CanReschedule;

        // Assert
        Assert.Equal(expected, canReschedule);
    }

    /// <summary>BR-20: an approved reschedule writes the new period and the new room.</summary>
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Reschedule_FromLegalStatus_UpdatesPeriodAndRoom(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);
        var newPeriod = new DateRange(TestDate.Of("2026-02-01"), TestDate.Of("2026-02-04"));
        const int newRoomId = 42;

        // Act
        booking.Reschedule(newPeriod, newRoomId, BookingFactory.Today);

        // Assert
        Assert.Equal(newPeriod, booking.Period);
        Assert.Equal(newRoomId, booking.RoomId);
        Assert.Equal(3, booking.NumberOfNights);
        Assert.Equal(status, booking.Status);
    }

    /// <summary>BR-17, BR-123: rescheduling a booking that is checked in, checked out or cancelled is rejected.</summary>
    [Theory]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void Reschedule_FromIllegalStatus_ThrowsAndLeavesPeriodUnchanged(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);
        var newPeriod = new DateRange(TestDate.Of("2026-02-01"), TestDate.Of("2026-02-04"));

        // Act
        Assert.Throws<InvalidStateTransitionException>(
            () => booking.Reschedule(newPeriod, 42, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingFactory.StandardPeriod, booking.Period);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
    }

    /// <summary>
    /// BR-108: the new period must not start in the past. The booking must stand completely unchanged
    /// afterwards — a rejected reschedule must not leave half-written dates.
    /// </summary>
    [Fact]
    public void Reschedule_NewPeriodStartsInThePast_ThrowsAndLeavesBookingUnchanged()
    {
        // Arrange
        var booking = BookingFactory.Pending();
        var pastPeriod = new DateRange(TestDate.Of("2025-12-20"), TestDate.Of("2025-12-24"));

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.Reschedule(pastPeriod, 42, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingErrors.StartDateInPast, exception.Code);
        Assert.Equal(BookingFactory.StandardPeriod, booking.Period);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
    }

    /// <summary>BR-45, BR-105: rescheduling still requires a valid room.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reschedule_WithoutRoom_ThrowsRoomRequired(int newRoomId)
    {
        // Arrange
        var booking = BookingFactory.Pending();
        var newPeriod = new DateRange(TestDate.Of("2026-02-01"), TestDate.Of("2026-02-04"));

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.Reschedule(newPeriod, newRoomId, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingErrors.RoomRequired, exception.Code);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
    }

    // --------------------------------------------------------
    // THE WHOLE FLOW
    // --------------------------------------------------------

    /// <summary>
    /// BR-01, BR-06, BR-09: the full happy path Pending → Confirmed → CheckedIn →
    /// CheckedOut, which is the only path that touches all four non-cancelled statuses.
    /// </summary>
    [Fact]
    public void BookingLifecycle_ConfirmCheckInCheckOut_EndsCheckedOut()
    {
        // Arrange
        var booking = BookingFactory.Pending();

        // Act
        booking.Confirm();
        booking.CheckIn(BookingFactory.CheckInMoment, BookingFactory.Today);
        booking.CheckOut(BookingFactory.CheckOutMoment, BookingFactory.Departure, BookingFactory.Departure);

        // Assert
        Assert.Equal(BookingStatus.CheckedOut, booking.Status);
        Assert.False(booking.CanConfirm);
        Assert.False(booking.CanCheckIn(BookingFactory.Departure));
        Assert.False(booking.CanCheckOut);
        Assert.False(booking.CanCancel);
        Assert.False(booking.CanReschedule);
    }
}