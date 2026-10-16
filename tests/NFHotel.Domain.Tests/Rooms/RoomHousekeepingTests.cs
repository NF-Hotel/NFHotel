using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>
/// Rengørings- og serviceforløbet: hele overgangstabellen for <see cref="HousekeepingStatus"/>.
/// </summary>
/// <remarks>
/// Tre theories dækker de tre udfald en overgang kan have: lovlig (tilstanden skifter),
/// idempotent (samme måltilstand er en no-op, A-08) og ulovlig (der kastes). Hver række er
/// ét felt i overgangstabellen, så en glemt overgang er synlig som en manglende række.
/// <para>
/// A-07: rengøring har eksplicitte metoder i stedet for én generisk mål-status, og A-08
/// gør retry ufarligt for rengøringspersonalet.
/// </para>
/// </remarks>
public class RoomHousekeepingTests
{
    // --------------------------------------------------------
    // LOVLIGE OVERGANGE
    // --------------------------------------------------------

    /// <summary>
    /// De 15 lovlige overgange der faktisk ændrer tilstand.
    /// Bemærk at slutrengøring rangerer over daglig rengøring: en <c>DepartureCleaningDue</c>
    /// kan ikke nedgraderes til <c>DailyCleaningDue</c>, men det omvendte er tilladt.
    /// </summary>
    [Theory]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.Clean, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.ServiceRequired, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.Clean, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.DailyCleaningDue, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.ServiceRequired, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.DailyCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.DepartureCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.CleaningInProgress, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.Clean, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.DailyCleaningDue, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.DepartureCleaningDue, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.CleaningInProgress, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.ServiceRequired, HousekeepingStatus.ServiceInProgress)]
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.ServiceInProgress, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.ServiceInProgress, HousekeepingStatus.DepartureCleaningDue)]
    public void HousekeepingTransition_FromLegalState_MovesToExpectedState(
        HousekeepingTransition transition,
        HousekeepingStatus from,
        HousekeepingStatus expected)
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(from);

        // Act
        HousekeepingTransitionInvoker.Invoke(room, transition);

        // Assert
        Assert.Equal(expected, room.HousekeepingStatus);
    }

    // --------------------------------------------------------
    // IDEMPOTENS (A-08)
    // --------------------------------------------------------

    /// <summary>
    /// A-08: at sætte den tilstand rummet allerede har er en no-op — ikke en exception.
    /// Mobilappen har ingen offline-understøttelse og retry'er over ustabilt netværk;
    /// en retry må ikke give en fejl til rengøringspersonalet.
    /// </summary>
    [Theory]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.ServiceInProgress)]
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.DepartureCleaningDue)]
    public void HousekeepingTransition_WhenAlreadyInTargetState_IsNoOpAndDoesNotThrow(
        HousekeepingTransition transition,
        HousekeepingStatus target)
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(target);

        // Act
        HousekeepingTransitionInvoker.Invoke(room, transition);
        HousekeepingTransitionInvoker.Invoke(room, transition);

        // Assert
        Assert.Equal(target, room.HousekeepingStatus);
    }

    // --------------------------------------------------------
    // ULOVLIGE OVERGANGE
    // --------------------------------------------------------

    /// <summary>
    /// A-08: ulovlige overgange kaster fortsat. Idempotens gælder kun samme måltilstand —
    /// den må ikke bruges som undskyldning for at acceptere hvad som helst.
    /// </summary>
    [Theory]
    // Slutrengøring må ikke nedgraderes til daglig rengøring, og igangværende arbejde må ikke overskrives.
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.ServiceInProgress)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.ServiceInProgress)]
    // Man kan kun gå i gang med en rengøring der er bestilt.
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.ServiceInProgress)]
    // Man kan kun afslutte en rengøring der er i gang.
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.ServiceInProgress)]
    // Et nyt servicebehov kan ikke rapporteres mens teknikeren står i rummet.
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.ServiceInProgress)]
    // Man kan kun gå i gang med en service der er bestilt.
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.CleaningInProgress)]
    // Man kan kun afslutte en service der er i gang.
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.CompleteServiceWithoutCleaning, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.CompleteServiceRequiringCleaning, HousekeepingStatus.ServiceRequired)]
    public void HousekeepingTransition_FromIllegalState_ThrowsAndLeavesStateUnchanged(
        HousekeepingTransition transition,
        HousekeepingStatus from)
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(from);

        // Act
        Assert.Throws<InvalidStateTransitionException>(
            () => HousekeepingTransitionInvoker.Invoke(room, transition));

        // Assert
        Assert.Equal(from, room.HousekeepingStatus);
    }

    /// <summary>
    /// A-09: fejlkoden har formen <c>&lt;entitet&gt;.&lt;overgang&gt;.invalid_state</c> med
    /// metodenavnet i snake_case, så Application kan mappe kode til en dansk tekst uden at
    /// læse en engelsk besked.
    /// </summary>
    [Fact]
    public void HousekeepingTransition_FromIllegalState_ThrowsWithTraceableErrorCode()
    {
        // Arrange
        var room = RoomFactory.WithHousekeeping(HousekeepingStatus.Clean);

        // Act
        var exception = Assert.Throws<InvalidStateTransitionException>(room.StartCleaning);

        // Assert
        Assert.Equal("room.start_cleaning.invalid_state", exception.Code);
        Assert.Equal(nameof(HousekeepingStatus.Clean), exception.CurrentState);
        Assert.Equal(nameof(Room.StartCleaning), exception.Transition);
    }

    // --------------------------------------------------------
    // DE TO NORMALFORLØB
    // --------------------------------------------------------

    /// <summary>
    /// Det normale rengøringsforløb efter en afrejse: slutrengøring bestilles, personalet
    /// kvitterer, og rummet er rent igen.
    /// </summary>
    [Fact]
    public void DepartureCleaningFlow_FromCleanAndBack_EndsClean()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        room.MarkDepartureCleaningDue();
        room.StartCleaning();
        room.CompleteCleaning();

        // Assert
        Assert.Equal(HousekeepingStatus.Clean, room.HousekeepingStatus);
    }

    /// <summary>
    /// Serviceforløbet: efter et snavsende stykke arbejde ender rummet i slutrengøring,
    /// ikke som rent — og kan derfra køre det normale rengøringsforløb.
    /// </summary>
    [Fact]
    public void ServiceFlow_WhenWorkLeftTheRoomDirty_EndsInDepartureCleaningDue()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        room.ReportServiceNeeded();
        room.StartService();
        room.CompleteService(requiresCleaning: true);

        // Assert
        Assert.Equal(HousekeepingStatus.DepartureCleaningDue, room.HousekeepingStatus);
        room.StartCleaning();
        room.CompleteCleaning();
        Assert.Equal(HousekeepingStatus.Clean, room.HousekeepingStatus);
    }

    /// <summary>
    /// B-04: et servicebehov spærrer ikke rummet for booking af sig selv — det kræver
    /// <c>SendToMaintenance</c>. De to tilstande er uafhængige.
    /// </summary>
    [Fact]
    public void ReportServiceNeeded_DoesNotBlockBookingOnItsOwn()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        room.ReportServiceNeeded();

        // Assert
        Assert.Equal(HousekeepingStatus.ServiceRequired, room.HousekeepingStatus);
        Assert.True(room.IsBookable);
    }
}