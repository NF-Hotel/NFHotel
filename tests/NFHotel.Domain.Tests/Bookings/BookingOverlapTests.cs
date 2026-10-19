using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The revised overlap rule A-01: a booking blocks the room in
/// <c>[StartDate, EffectiveEndDate)</c> unless it is cancelled.
/// </summary>
/// <remarks>
/// Covers BR-19 and BR-39 as well as the decision A-01. The original B-02 blocked
/// re-letting after early check-out; the tests here are that concrete operational fault written
/// as asserts, so the rule cannot drift back.
/// <para>
/// <c>EffectiveEndDate = GREATEST(StartDate + 1, COALESCE(CheckOutDate, EndDate))</c>.
/// </para>
/// </remarks>
public class BookingOverlapTests
{
    private const int OtherRoomId = 99;

    #region Effective end date (A-01)

    /// <summary>
    /// A-01: without a check-out the effective end date is the booked end date — the rule must
    /// not change behaviour for bookings that have not yet been checked out.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.Cancelled)]
    public void EffectiveEndDate_WithoutCheckOut_EqualsBookedEndDate(BookingStatus status)
    {
        // Arrange
        var booking = BookingFactory.InStatus(status);

        // Act
        var effectiveEndDate = booking.EffectiveEndDate;

        // Assert
        Assert.Equal(BookingFactory.Departure, effectiveEndDate);
    }

    /// <summary>
    /// A-01: early check-out moves the effective end date forward and thereby releases the
    /// remaining nights. The floor <c>StartDate + 1</c> ensures at least one night of occupancy,
    /// even on check-out on the same day as arrival.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-02")] // check-out on the same day as arrival → the GREATEST floor
    [InlineData("2026-01-02", "2026-01-02")] // check-out the day after arrival → the floor is hit exactly
    [InlineData("2026-01-03", "2026-01-03")] // early check-out → the occupancy ends on the 3rd
    [InlineData("2026-01-09", "2026-01-09")]
    [InlineData("2026-01-10", "2026-01-10")] // check-out on the booked departure day
    public void EffectiveEndDate_AfterCheckOut_IsGreatestOfOneNightAndCheckOutDate(
        string checkOutDate,
        string expectedEffectiveEndDate)
    {
        // Arrange
        var booking = BookingFactory.CheckedOutOn(TestDate.Of(checkOutDate));

        // Act
        var effectiveEndDate = booking.EffectiveEndDate;

        // Assert
        Assert.Equal(TestDate.Of(expectedEffectiveEndDate), effectiveEndDate);
    }

    /// <summary>
    /// A-01: check-out must NEVER touch <c>EndDate</c>. The booked period is history and
    /// is the only place that can still answer what the guest actually ordered.
    /// </summary>
    [Fact]
    public void CheckOut_EarlyDeparture_LeavesBookedEndDateAndNumberOfNightsUntouched()
    {
        // Arrange
        var earlyDeparture = TestDate.Of("2026-01-03");

        // Act
        var booking = BookingFactory.CheckedOutOn(earlyDeparture);

        // Assert
        Assert.Equal(BookingFactory.Departure, booking.EndDate);
        Assert.Equal(BookingFactory.StandardPeriod, booking.Period);
        Assert.Equal(9, booking.NumberOfNights);
        Assert.Equal(earlyDeparture, booking.CheckOutDate);
        Assert.Equal(earlyDeparture, booking.EffectiveEndDate);
    }

    /// <summary>
    /// A-01: check-out on the same day as arrival still gives one night of occupancy — otherwise
    /// <c>EffectivePeriod</c> would be empty and the room could be double-booked the same night.
    /// </summary>
    [Fact]
    public void EffectivePeriod_SameDayCheckOut_StillBlocksOneNight()
    {
        // Arrange
        var booking = BookingFactory.CheckedOutOn(BookingFactory.Arrival);

        // Act
        var effectivePeriod = booking.EffectivePeriod;

        // Assert
        Assert.Equal(1, effectivePeriod.Nights);
        Assert.Equal(BookingFactory.Arrival, effectivePeriod.Start);
        Assert.Equal(BookingFactory.Arrival.AddDays(1), booking.EffectiveEndDate);
        Assert.Equal(BookingFactory.Arrival, booking.CheckOutDate);
    }

    #endregion

    #region Which statuses block (A-01, BR-19)

    /// <summary>
    /// A-01: only cancellation releases the room completely. The database exclusion constraint uses the
    /// same predicate (<c>WHERE status &lt;&gt; 4</c>), so the list must not deviate.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, true)]
    [InlineData(BookingStatus.CheckedOut, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void BlocksRoom_AnyStatus_OnlyCancelledReleasesTheRoom(BookingStatus status, bool expected)
    {
        // Arrange & Act
        var blocks = status.BlocksRoom();

        // Assert
        Assert.Equal(expected, blocks);
    }

    /// <summary>
    /// BR-19: a cancelled booking does not block, all other statuses do — also on a
    /// period that lies in the middle of the booked one.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, true)]
    [InlineData(BookingStatus.CheckedOut, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void Conflicts_AnyStatus_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var existing = BookingFactory.InStatus(status);
        var desiredPeriod = new DateRange(TestDate.Of("2026-01-04"), TestDate.Of("2026-01-06"));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.Equal(expected, conflicts);
    }

    /// <summary>BR-19: a booking on another room is never in conflict.</summary>
    [Fact]
    public void Conflicts_DifferentRoom_ReturnsFalse()
    {
        // Arrange
        var existing = BookingFactory.InStatus(BookingStatus.Confirmed);
        var desiredPeriod = BookingFactory.StandardPeriod;

        // Act
        var conflicts = existing.ConflictsWith(OtherRoomId, desiredPeriod);

        // Assert
        Assert.False(conflicts);
    }

    #endregion

    #region Re-letting after early check-out (A-01, the main scenario)

    /// <summary>
    /// A-01, the main scenario: booking 1–10 January, the guest leaves on the 3rd. The room is empty
    /// from the 3rd, so a new booking 4–8 January must NOT be rejected. With the original
    /// B-02 it would have been in conflict.
    /// </summary>
    [Fact]
    public void Conflicts_NewBookingInNightsReleasedByEarlyCheckOut_ReturnsFalse()
    {
        // Arrange
        var existing = BookingFactory.CheckedOutOn(TestDate.Of("2026-01-03"));
        var desiredPeriod = new DateRange(TestDate.Of("2026-01-04"), TestDate.Of("2026-01-08"));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.False(conflicts);
    }

    /// <summary>
    /// A-01: after check-out on 3 January the booking blocks exactly the nights 1–2.
    /// The boundaries are tested from both sides, so neither a too loose nor a too tight
    /// boundary gets through.
    /// </summary>
    [Theory]
    // Released nights: a new booking may start on the check-out day (half-open interval).
    [InlineData("2026-01-03", "2026-01-08", false)]
    [InlineData("2026-01-04", "2026-01-08", false)]
    [InlineData("2026-01-09", "2026-01-11", false)]
    // The period before the arrival shares no night, because the arrival day is excluded at the other end.
    [InlineData("2025-12-28", "2026-01-01", false)]
    // Still occupied nights: 1 and 2 January.
    [InlineData("2026-01-01", "2026-01-02", true)]
    [InlineData("2026-01-02", "2026-01-05", true)]
    [InlineData("2025-12-28", "2026-01-02", true)]
    [InlineData("2026-01-01", "2026-01-31", true)]
    public void Conflicts_AfterEarlyCheckOut_BlocksOnlyTheNightsActuallyStayed(
        string desiredStart,
        string desiredEnd,
        bool expected)
    {
        // Arrange
        var existing = BookingFactory.CheckedOutOn(TestDate.Of("2026-01-03"));
        var desiredPeriod = new DateRange(TestDate.Of(desiredStart), TestDate.Of(desiredEnd));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.Equal(expected, conflicts);
    }

    /// <summary>
    /// BR-39: departure and arrival on the same day is not an overlap — neither when the
    /// existing booking leaves first nor when it arrives last.
    /// </summary>
    [Theory]
    [InlineData("2026-01-10", "2026-01-14", false)] // new arrival = existing departure
    [InlineData("2025-12-28", "2026-01-01", false)] // new departure = existing arrival
    [InlineData("2026-01-09", "2026-01-14", true)]  // one shared night
    [InlineData("2025-12-28", "2026-01-02", true)]  // one shared night at the other end
    public void Conflicts_DepartureDayEqualsArrivalDay_IsNotAnOverlap(
        string desiredStart,
        string desiredEnd,
        bool expected)
    {
        // Arrange
        var existing = BookingFactory.InStatus(BookingStatus.Confirmed);
        var desiredPeriod = new DateRange(TestDate.Of(desiredStart), TestDate.Of(desiredEnd));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.Equal(expected, conflicts);
    }

    /// <summary>
    /// A-01: the conflict check must measure against <c>EffectivePeriod</c>, not <c>Period</c>.
    /// The test catches the fault of someone swapping the two: the desired period overlaps
    /// the booked period, but not the actual occupancy.
    /// </summary>
    [Fact]
    public void Conflicts_AfterEarlyCheckOut_ComparesAgainstEffectivePeriodNotBookedPeriod()
    {
        // Arrange
        var existing = BookingFactory.CheckedOutOn(TestDate.Of("2026-01-03"));
        var desiredPeriod = new DateRange(TestDate.Of("2026-01-05"), TestDate.Of("2026-01-07"));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.True(existing.Period.Overlaps(desiredPeriod));
        Assert.False(existing.EffectivePeriod.Overlaps(desiredPeriod));
        Assert.False(conflicts);
    }

    #endregion
}
