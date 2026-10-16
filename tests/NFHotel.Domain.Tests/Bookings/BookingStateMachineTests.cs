using NFHotel.Domain.Errors;
using NFHotel.Domain.Rules;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// Bookingens tilstandsmaskine: Confirm, CheckIn, CheckOut, Cancel og Reschedule.
/// </summary>
/// <remarks>
/// Genskaber de 22 tests fra det gamle projekts <c>BookingOverviewViewModelTests</c> mod
/// domænet i stedet for mod en ViewModel (B-10). De gamle tests
/// <c>CanExecuteConfirm_NoSelectedBooking_ReturnsFalse</c>,
/// <c>FilterBookings_ExcludesCancelledBookings</c> (BR-23) og
/// <c>SearchText_FiltersBookingsByGuestName</c> (BR-25) er ikke gengivet her: de handler om
/// markering, filtrering og søgning i en liste, som ligger i Application- og Web-laget.
/// <para>
/// Dækker BR-01..BR-14, BR-17, BR-20, BR-107, BR-108, BR-121, BR-122, BR-123
/// samt præmis B-01 (walk-in) og A-03 (udtjekningsdato som parameter).
/// </para>
/// </remarks>
public class BookingStateMachineTests
{
    // --------------------------------------------------------
    // BEKRÆFTELSE (BR-02, BR-05, BR-121 — gamle T-20, T-27)
    // --------------------------------------------------------

    /// <summary>BR-01, BR-02: en ny booking er <c>Pending</c> og må derfor bekræftes.</summary>
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
    /// BR-02, BR-121: bekræftelse er kun mulig fra præcis <c>Pending</c>.
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
    /// BR-03: reglen gentjekkes i selve handlingen — <c>CanConfirm</c> er ikke bare et
    /// UI-hint. Kaldes den alligevel, afvises den og status står uændret.
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
    /// T-21, B-01: walk-in. En <c>Pending</c> booking må tjekke ind på ankomstdagen uden
    /// først at blive bekræftet — <c>Confirmed</c> betyder "betaling garanteret", ikke
    /// "må tjekke ind". Denne test er selve begrundelsen for B-01.
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
    /// T-22, BR-06: check-in er lovligt fra både <c>Pending</c> og <c>Confirmed</c>, og
    /// forbudt fra alle andre statusser.
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
    /// T-23, BR-06: check-in kræver <c>StartDate &lt;= i dag</c>. Ankomstdagen selv er
    /// tilladt, dagen før ikke, og en forsinket ankomst må stadig tjekke ind.
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

    /// <summary>BR-06: check-in før ankomstdagen afvises også når man kalder metoden direkte.</summary>
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
    /// BR-06: <c>CheckInTime</c> må ikke overskrives af et gentaget check-in — i modsætning
    /// til rummets statusmetoder er check-in ikke idempotent (A-08 gælder kun rummet).
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

    /// <summary>BR-06: en annulleret booking kan ikke tjekke ind (gammel T-test for Cancelled).</summary>
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
    // CHECK-UD (BR-09, BR-10, BR-107 — gammel T-24)
    // --------------------------------------------------------

    /// <summary>
    /// T-24, BR-09, BR-10, A-03: udtjekning sætter tidspunkt, hotel-lokal dato og status.
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
    /// BR-09: udtjekning kræver præcis <c>CheckedIn</c> — hverken en booking der aldrig er
    /// tjekket ind eller en der allerede er tjekket ud må tjekkes ud.
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

    /// <summary>BR-09: reglen gentjekkes i handlingen.</summary>
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
    /// BR-107: udtjekningstidspunktet skal være strengt efter indtjekningstidspunktet.
    /// Samme tidspunkt er også for tidligt.
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
        Assert.Equal(BookingRules.CheckOutBeforeCheckIn, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
    }

    /// <summary>
    /// A-01, A-03: udtjekningsdatoen må ikke ligge før ankomstdagen — ellers ville
    /// <c>EffectiveEndDate</c> beskrive en periode gæsten aldrig kan have haft.
    /// </summary>
    [Theory]
    [InlineData("2025-12-30")]
    [InlineData("2025-12-31")] // dagen før ankomsten
    public void CheckOut_CheckOutDateBeforeStartDate_ThrowsAndLeavesBookingUntouched(string checkOutDate)
    {
        // Arrange
        var booking = BookingFactory.InStatus(BookingStatus.CheckedIn);

        // Act
        var exception = Assert.Throws<DomainException>(
            () => booking.CheckOut(BookingFactory.CheckOutMoment, TestDate.Of(checkOutDate), BookingFactory.Today));

        // Assert
        Assert.Equal(BookingRules.CheckOutDateBeforeStartDate, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Null(booking.CheckOutDate);
        Assert.Null(booking.CheckOutTime);
    }

    /// <summary>
    /// Udtjekningsdatoen må ikke ligge efter "i dag". En dato i fremtiden kan ikke rettes
    /// bagefter — en <c>CheckedOut</c> booking kan hverken omlægges eller annulleres — og
    /// ville spærre rummet frem til den via <c>EffectiveEndDate</c> (A-01).
    /// </summary>
    [Theory]
    [InlineData("2026-01-04", "2026-01-03")] // dagen efter i dag
    [InlineData("2026-01-10", "2026-01-03")] // den bookede afrejsedag, men den er ikke kommet endnu
    [InlineData("2030-01-01", "2026-01-03")] // tastefejl i årstallet
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
        Assert.Equal(BookingRules.CheckOutDateInFuture, exception.Code);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Null(booking.CheckOutDate);
        Assert.Null(booking.CheckOutTime);
    }

    /// <summary>
    /// Grænsen er inklusiv: en udtjekning der registreres samme dag den sker, går igennem.
    /// Det er normaltilfældet, så en for stram guard ville spærre for al drift.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01")] // udtjekning samme dag som ankomsten
    [InlineData("2026-01-03")] // tidlig udtjekning
    [InlineData("2026-01-10")] // den bookede afrejsedag
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
    /// Rækkefølgen af de to datoguards er fastlagt: en dato der bryder begge — før
    /// ankomsten OG efter i dag — rapporteres som "før ankomstdatoen". Uden testen kan
    /// koden bytte dem om, og brugeren får en besked der peger på det forkerte problem.
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
        Assert.Equal(BookingRules.CheckOutDateBeforeStartDate, exception.Code);
    }

    // --------------------------------------------------------
    // ANNULLERING (BR-11, BR-13, BR-122 — gammel T-25)
    // --------------------------------------------------------

    /// <summary>
    /// T-25, BR-11: kun <c>Pending</c> og <c>Confirmed</c> må annulleres. En gæst der er
    /// tjekket ind eller ud kan ikke fjernes med et museklik.
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

    /// <summary>BR-13: annullering er soft-delete — status skifter, bookingen består.</summary>
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

    /// <summary>BR-11: annullering af en booking der er tjekket ind, ud eller allerede annulleret afvises.</summary>
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
    // OMLÆGNING (BR-14, BR-17, BR-20, BR-108, BR-123 — gammel T-26)
    // --------------------------------------------------------

    /// <summary>
    /// T-26, BR-14, BR-123: kun <c>Pending</c> og <c>Confirmed</c> må omlægges.
    /// Hedder <c>CanReschedule</c> overalt, ikke <c>CanEdit</c> (A-06).
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

    /// <summary>BR-20: en godkendt omlægning skriver ny periode og nyt rum.</summary>
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

    /// <summary>BR-17, BR-123: omlægning af en booking der er tjekket ind, ud eller annulleret afvises.</summary>
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
    /// BR-108: den nye periode må ikke starte i fortiden. Bookingen skal stå helt uændret
    /// bagefter — en afvist omlægning må ikke efterlade halvt skrevne datoer.
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
        Assert.Equal(BookingRules.StartDateInPast, exception.Code);
        Assert.Equal(BookingFactory.StandardPeriod, booking.Period);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
    }

    /// <summary>BR-45, BR-105: omlægning kræver stadig et gyldigt rum.</summary>
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
        Assert.Equal(BookingRules.RoomRequired, exception.Code);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
    }

    // --------------------------------------------------------
    // HELE FORLØBET
    // --------------------------------------------------------

    /// <summary>
    /// BR-01, BR-06, BR-09: den fulde lykkelige vej Pending → Confirmed → CheckedIn →
    /// CheckedOut, som er den eneste sti der rører alle fire ikke-annullerede statusser.
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