using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// Den reviderede overlapsregel A-01: en booking blokerer rummet i
/// <c>[StartDate, EffectiveEndDate)</c> medmindre den er annulleret.
/// </summary>
/// <remarks>
/// Dækker BR-19 og BR-39 samt afgørelsen A-01. Den oprindelige B-02 spærrede for
/// genudlejning efter tidlig udtjekning; testene her er den konkrete driftsfejl skrevet
/// som asserts, så reglen ikke kan drive tilbage.
/// <para>
/// <c>EffectiveEndDate = GREATEST(StartDate + 1, COALESCE(CheckOutDate, EndDate))</c>.
/// </para>
/// </remarks>
public class BookingOverlapTests
{
    private const int OtherRoomId = 99;

    // --------------------------------------------------------
    // EFFEKTIV SLUTDATO (A-01)
    // --------------------------------------------------------

    /// <summary>
    /// A-01: uden udtjekning er den effektive slutdato den bookede slutdato — reglen må
    /// ikke ændre adfærd for bookinger der endnu ikke er tjekket ud.
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
    /// A-01: tidlig udtjekning flytter den effektive slutdato frem og frigiver dermed de
    /// resterende nætter. Gulvet <c>StartDate + 1</c> sikrer mindst én nats belægning,
    /// også ved udtjekning samme dag som ankomsten.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-02")] // udtjekning samme dag som ankomst → GREATEST-gulvet
    [InlineData("2026-01-02", "2026-01-02")] // udtjekning dagen efter ankomst → gulvet rammes præcis
    [InlineData("2026-01-03", "2026-01-03")] // tidlig udtjekning → belægningen slutter den 3.
    [InlineData("2026-01-09", "2026-01-09")]
    [InlineData("2026-01-10", "2026-01-10")] // udtjekning på den bookede afrejsedag
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
    /// A-01: udtjekning må ALDRIG røre <c>EndDate</c>. Den bookede periode er historik og
    /// er det eneste sted der stadig kan svare på hvad gæsten faktisk bestilte.
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
    /// A-01: udtjekning samme dag som ankomsten giver stadig én nats belægning — ellers
    /// ville <c>EffectivePeriod</c> være tom og rummet kunne dobbeltbookes samme nat.
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

    // --------------------------------------------------------
    // HVILKE STATUSSER BLOKERER (A-01, BR-19)
    // --------------------------------------------------------

    /// <summary>
    /// A-01: kun annullering frigiver rummet helt. Databasens exclusion constraint bruger
    /// samme prædikat (<c>WHERE status &lt;&gt; 4</c>), så listen må ikke afvige.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, true)]
    [InlineData(BookingStatus.CheckedOut, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void BlocksRoom_OnlyCancelledReleasesTheRoom(BookingStatus status, bool expected)
    {
        // Arrange & Act
        var blocks = status.BlocksRoom();

        // Assert
        Assert.Equal(expected, blocks);
    }

    /// <summary>
    /// BR-19: en annulleret booking spærrer ikke, alle andre statusser gør — også på en
    /// periode der ligger midt i den bookede.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.CheckedIn, true)]
    [InlineData(BookingStatus.CheckedOut, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void Conflicts_ReflectsStatus(BookingStatus status, bool expected)
    {
        // Arrange
        var existing = BookingFactory.InStatus(status);
        var desiredPeriod = new DateRange(TestDate.Of("2026-01-04"), TestDate.Of("2026-01-06"));

        // Act
        var conflicts = existing.ConflictsWith(BookingFactory.RoomId, desiredPeriod);

        // Assert
        Assert.Equal(expected, conflicts);
    }

    /// <summary>BR-19: en booking på et andet rum er aldrig i konflikt.</summary>
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

    // --------------------------------------------------------
    // GENUDLEJNING EFTER TIDLIG UDTJEKNING (A-01, hovedscenariet)
    // --------------------------------------------------------

    /// <summary>
    /// A-01, hovedscenariet: booking 1.–10. januar, gæsten rejser den 3. Rummet står tomt
    /// fra den 3., så en ny booking 4.–8. januar må IKKE afvises. Med den oprindelige
    /// B-02 ville den have været i konflikt.
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
    /// A-01: efter udtjekning den 3. januar blokerer bookingen præcis nætterne 1.–2.
    /// Grænserne testes fra begge sider, så hverken en for løs eller en for stram
    /// afgrænsning slipper igennem.
    /// </summary>
    [Theory]
    // Frigivne nætter: ny booking må starte på udtjekningsdagen (halvåbent interval).
    [InlineData("2026-01-03", "2026-01-08", false)]
    [InlineData("2026-01-04", "2026-01-08", false)]
    [InlineData("2026-01-09", "2026-01-11", false)]
    // Perioden før ankomsten deler ingen nat, fordi ankomstdagen er ekskluderet i den anden ende.
    [InlineData("2025-12-28", "2026-01-01", false)]
    // Stadig belagte nætter: 1. og 2. januar.
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
    /// BR-39: afrejse- og ankomstdag samme dag er ikke overlap — hverken når den
    /// eksisterende booking rejser først eller når den ankommer sidst.
    /// </summary>
    [Theory]
    [InlineData("2026-01-10", "2026-01-14", false)] // ny ankomst = eksisterende afrejse
    [InlineData("2025-12-28", "2026-01-01", false)] // ny afrejse = eksisterende ankomst
    [InlineData("2026-01-09", "2026-01-14", true)]  // én delt nat
    [InlineData("2025-12-28", "2026-01-02", true)]  // én delt nat i den anden ende
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
    /// A-01: konflikttjekket skal måle mod <c>EffectivePeriod</c>, ikke <c>Period</c>.
    /// Testen fanger den fejl at nogen bytter de to om: den ønskede periode overlapper
    /// den bookede periode, men ikke den faktiske belægning.
    /// </summary>
    [Fact]
    public void Conflicts_ComparesAgainstEffectivePeriodNotBookedPeriod()
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
}