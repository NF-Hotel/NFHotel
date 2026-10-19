using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>
/// The cleaning and service flow: the whole transition table for <see cref="HousekeepingStatus"/>.
/// </summary>
/// <remarks>
/// Three theories cover the three outcomes a transition can have: legal (the state changes),
/// idempotent (the same target state is a no-op, A-08) and illegal (it throws). Each row is
/// one field of the transition table, so a forgotten transition is visible as a missing row.
/// <para>
/// A-07: cleaning has explicit methods instead of one generic target status, and A-08
/// makes retry harmless for the cleaning staff.
/// </para>
/// </remarks>
public class RoomHousekeepingTests
{
    #region Legal transitions

    /// <summary>
    /// The 15 legal transitions that actually change state.
    /// Note that departure cleaning ranks above daily cleaning: a <c>DepartureCleaningDue</c>
    /// cannot be downgraded to <c>DailyCleaningDue</c>, but the reverse is allowed.
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

    #endregion

    #region Idempotence (A-08)

    /// <summary>
    /// A-08: setting the state the room already has is a no-op — not an exception.
    /// The mobile app has no offline support and retries over an unstable network;
    /// a retry must not give an error to the cleaning staff.
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

    #endregion

    #region Illegal transitions

    /// <summary>
    /// A-08: illegal transitions still throw. Idempotence applies only to the same target state —
    /// it must not be used as an excuse to accept anything.
    /// </summary>
    [Theory]
    // Departure cleaning must not be downgraded to daily cleaning, and work in progress must not be overwritten.
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.MarkDailyCleaningDue, HousekeepingStatus.ServiceInProgress)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.CleaningInProgress)]
    [InlineData(HousekeepingTransition.MarkDepartureCleaningDue, HousekeepingStatus.ServiceInProgress)]
    // One can only start a cleaning that has been ordered.
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.StartCleaning, HousekeepingStatus.ServiceInProgress)]
    // One can only finish a cleaning that is in progress.
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.ServiceRequired)]
    [InlineData(HousekeepingTransition.CompleteCleaning, HousekeepingStatus.ServiceInProgress)]
    // A new service need cannot be reported while the technician is in the room.
    [InlineData(HousekeepingTransition.ReportServiceNeeded, HousekeepingStatus.ServiceInProgress)]
    // One can only start a service that has been ordered.
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.Clean)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.DailyCleaningDue)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.DepartureCleaningDue)]
    [InlineData(HousekeepingTransition.StartService, HousekeepingStatus.CleaningInProgress)]
    // One can only finish a service that is in progress.
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
    /// A-09: the error code has the form <c>&lt;entity&gt;.&lt;transition&gt;.invalid_state</c> with
    /// the method name in snake_case, so Application can map code to a Danish text without
    /// reading an English message.
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

    #endregion

    #region The two normal flows

    /// <summary>
    /// The normal cleaning flow after a departure: departure cleaning is ordered, the staff
    /// acknowledge, and the room is clean again.
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
    /// The service flow: after a messy piece of work the room ends in departure cleaning,
    /// not as clean — and from there can run the normal cleaning flow.
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
    /// B-04: a service need does not block the room for booking by itself — that requires
    /// <c>SendToMaintenance</c>. The two states are independent.
    /// </summary>
    [Fact]
    public void ReportServiceNeeded_RoomAvailable_DoesNotBlockBookingOnItsOwn()
    {
        // Arrange
        var room = RoomFactory.Create();

        // Act
        room.ReportServiceNeeded();

        // Assert
        Assert.Equal(HousekeepingStatus.ServiceRequired, room.HousekeepingStatus);
        Assert.True(room.IsBookable);
    }

    #endregion
}
