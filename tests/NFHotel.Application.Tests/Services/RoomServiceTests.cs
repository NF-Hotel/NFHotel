using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// Unit tests of <c>RoomService</c> with in-memory repositories and a fake unit of work: no database,
/// no sleeping, no network.
/// </summary>
public sealed class RoomServiceTests
{
    #region Reading

    /// <summary>Positive: an existing room is returned as the query port describes it.</summary>
    [Fact]
    public async Task GetByIdAsync_ExistingRoom_ReturnsTheDetails()
    {
        // Arrange
        var world = new ServiceWorld();
        var details = new RoomDetailsDto(1, "101", 1, RoomSize.Double, 2, RoomStatus.Available, HousekeepingStatus.Clean, true, false);
        world.RoomQueries.GetDetailsAsync(1, TestContext.Current.CancellationToken).Returns(details);

        // Act
        var result = await world.RoomService().GetByIdAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(details, result.Value);
    }

    /// <summary>Negative: an unknown room is reported with the not-found code.</summary>
    [Fact]
    public async Task GetByIdAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().GetByIdAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>Boundary: a key below one is a programming error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_KeyBelowOne_ThrowsArgumentOutOfRange(int roomId)
    {
        // Arrange
        var service = new ServiceWorld().RoomService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetByIdAsync(roomId, TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: every room of the query port is returned.</summary>
    [Fact]
    public async Task GetAllAsync_RoomsExist_ReturnsThemAll()
    {
        // Arrange
        var world = new ServiceWorld();
        world.RoomQueries.GetAllAsync(TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<RoomListItemDto>>([ListItem(1), ListItem(2)]));

        // Act
        var result = await world.RoomService().GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Value.Count);
    }

    /// <summary>Error handling: a failing query is not swallowed.</summary>
    [Fact]
    public async Task GetAllAsync_QueryFails_PropagatesTheException()
    {
        // Arrange
        var world = new ServiceWorld();
        world.RoomQueries.GetAllAsync(TestContext.Current.CancellationToken)
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => world.RoomService().GetAllAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: the filter is passed on to the query port unchanged.</summary>
    [Fact]
    public async Task SearchAsync_Filter_IsPassedToTheQueryPort()
    {
        // Arrange
        var world = new ServiceWorld();
        var filter = new RoomFilter(Floor: 2, Size: RoomSize.Suite);
        world.RoomQueries.GetByFilterAsync(filter, TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<RoomListItemDto>>([ListItem(5)]));

        // Act
        var result = await world.RoomService().SearchAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, result.Value.Single().RoomId);
    }

    /// <summary>Negative: a missing filter is a programming error.</summary>
    [Fact]
    public async Task SearchAsync_NullFilter_ThrowsArgumentNull()
    {
        // Arrange
        var service = new ServiceWorld().RoomService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SearchAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: the options hold the floors of the query port and every enum value.</summary>
    [Fact]
    public async Task GetFilterOptionsAsync_FloorsExist_ReturnsFloorsAndEveryEnumValue()
    {
        // Arrange
        var world = new ServiceWorld();
        world.RoomQueries.GetDistinctFloorsAsync(TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<IReadOnlyList<int>>([1, 2, 3]));

        // Act
        var result = await world.RoomService().GetFilterOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([1, 2, 3], result.Value.Floors);
        Assert.Equal(Enum.GetValues<RoomStatus>(), result.Value.Statuses);
        Assert.Equal(Enum.GetValues<HousekeepingStatus>(), result.Value.HousekeepingStatuses);
    }

    /// <summary>Error handling: a failing floor query is not swallowed.</summary>
    [Fact]
    public async Task GetFilterOptionsAsync_QueryFails_PropagatesTheException()
    {
        // Arrange
        var world = new ServiceWorld();
        world.RoomQueries.GetDistinctFloorsAsync(TestContext.Current.CancellationToken)
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => world.RoomService().GetFilterOptionsAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region Creating and updating

    /// <summary>Positive: valid fields give a saved, bookable and clean room (BR-81).</summary>
    [Fact]
    public async Task CreateAsync_ValidFields_SavesAnAvailableCleanRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new CreateRoomRequest("201", 2, RoomSize.Suite, 4);

        // Act
        var result = await world.RoomService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Available, result.Value.Status);
        Assert.Equal(HousekeepingStatus.Clean, result.Value.HousekeepingStatus);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a room number that is already used is refused (BR-N-01).</summary>
    [Fact]
    public async Task CreateAsync_RoomNumberAlreadyUsed_ReturnsRoomNumberAlreadyExists()
    {
        // Arrange
        var world = new ServiceWorld();
        world.AddRoom("201");

        // Act
        var result = await world.RoomService().CreateAsync(
            new CreateRoomRequest("201", 2, RoomSize.Double, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.RoomNumberAlreadyExists, result.FirstError?.Code);
        Assert.Single(world.Rooms.Items);
    }

    /// <summary>Negative: every field error is reported at once.</summary>
    [Fact]
    public async Task CreateAsync_SeveralInvalidFields_ReturnsEveryCode()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = new CreateRoomRequest(" ", 0, RoomSize.Single, 0);

        // Act
        var result = await world.RoomService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var codes = result.Errors.Select(error => error.Code).ToArray();
        Assert.Contains(ErrorCodes.Room.RoomNumberRequired, codes);
        Assert.Contains(ErrorCodes.Room.FloorInvalid, codes);
        Assert.Contains(ErrorCodes.Room.CapacityInvalid, codes);
    }

    /// <summary>Boundary: a room number of the maximum length is accepted, one more character is not.</summary>
    [Fact]
    public async Task CreateAsync_RoomNumberAtAndAboveTheMaximum_AcceptsTheMaximumOnly()
    {
        // Arrange
        var world = new ServiceWorld();
        var atLimit = new CreateRoomRequest(new string('1', Room.MaxRoomNumberLength), 1, RoomSize.Single, 1);
        var aboveLimit = new CreateRoomRequest(new string('2', Room.MaxRoomNumberLength + 1), 1, RoomSize.Single, 1);

        // Act
        var accepted = await world.RoomService().CreateAsync(atLimit, TestContext.Current.CancellationToken);
        var rejected = await world.RoomService().CreateAsync(aboveLimit, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(accepted.IsSuccess);
        Assert.Equal(ErrorCodes.Room.RoomNumberTooLong, rejected.FirstError?.Code);
    }

    /// <summary>Error handling: the unique index wins a race and gives the same error code as the check.</summary>
    [Fact]
    public async Task CreateAsync_UniqueIndexViolatedWhileSaving_ReturnsRoomNumberAlreadyExists()
    {
        // Arrange
        var world = new ServiceWorld();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate", "ux_room_number");

        // Act
        var result = await world.RoomService().CreateAsync(
            new CreateRoomRequest("301", 3, RoomSize.Single, 1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.RoomNumberAlreadyExists, result.FirstError?.Code);
    }

    /// <summary>Positive: the room changes and the change is saved.</summary>
    [Fact]
    public async Task UpdateAsync_ValidFields_ChangesTheRoomAndSaves()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom("101");

        // Act
        var result = await world.RoomService().UpdateAsync(
            new UpdateRoomRequest(room.Id, "102", 1, RoomSize.Suite, 3), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("102", room.RoomNumber);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Edge: a room keeps its own number, it does not clash with itself.</summary>
    [Fact]
    public async Task UpdateAsync_KeepsItsOwnRoomNumber_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom("101");

        // Act
        var result = await world.RoomService().UpdateAsync(
            new UpdateRoomRequest(room.Id, "101", 2, RoomSize.Double, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative: the number of another room is refused.</summary>
    [Fact]
    public async Task UpdateAsync_RoomNumberOfAnotherRoom_ReturnsRoomNumberAlreadyExists()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom("101");
        world.AddRoom("102");

        // Act
        var result = await world.RoomService().UpdateAsync(
            new UpdateRoomRequest(room.Id, "102", 1, RoomSize.Double, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.RoomNumberAlreadyExists, result.FirstError?.Code);
        Assert.Equal("101", room.RoomNumber);
    }

    /// <summary>Negative: an unknown room cannot be updated.</summary>
    [Fact]
    public async Task UpdateAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().UpdateAsync(
            new UpdateRoomRequest(99, "101", 1, RoomSize.Double, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency conflict while saving is reported.</summary>
    [Fact]
    public async Task UpdateAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        // Act
        var result = await world.RoomService().UpdateAsync(
            new UpdateRoomRequest(room.Id, "101", 1, RoomSize.Double, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.ConcurrencyConflict, result.FirstError?.Code);
    }

    #endregion

    #region Deleting

    /// <summary>Positive: a room without bookings is removed and the removal is saved.</summary>
    [Fact]
    public async Task DeleteAsync_RoomWithoutBookings_RemovesTheRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().DeleteAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(world.Rooms.Items);
    }

    /// <summary>Negative: a room with bookings is kept.</summary>
    [Fact]
    public async Task DeleteAsync_RoomWithBookings_ReturnsHasBookings()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        world.Rooms.RoomIdsWithBookings.Add(room.Id);

        // Act
        var result = await world.RoomService().DeleteAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.HasBookings, result.FirstError?.Code);
        Assert.Single(world.Rooms.Items);
    }

    /// <summary>Negative: an unknown room cannot be deleted.</summary>
    [Fact]
    public async Task DeleteAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().DeleteAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency conflict while deleting is reported.</summary>
    [Fact]
    public async Task DeleteAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        // Act
        var result = await world.RoomService().DeleteAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.ConcurrencyConflict, result.FirstError?.Code);
    }

    #endregion

    #region Bookability transitions

    /// <summary>State transition: an available room goes out of service.</summary>
    [Fact]
    public async Task TakeOutOfServiceAsync_AvailableRoom_BecomesOutOfService()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().TakeOutOfServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.OutOfService, room.Status);
    }

    /// <summary>Edge: taking a room that is already out of service out of service again changes nothing (A-08).</summary>
    [Fact]
    public async Task TakeOutOfServiceAsync_AlreadyOutOfService_IsAnIdempotentNoOp()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        room.TakeOutOfService();

        // Act
        var result = await world.RoomService().TakeOutOfServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.OutOfService, room.Status);
    }

    /// <summary>Negative: an unknown room cannot be taken out of service.</summary>
    [Fact]
    public async Task TakeOutOfServiceAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().TakeOutOfServiceAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: an available room goes to maintenance.</summary>
    [Fact]
    public async Task SendToMaintenanceAsync_AvailableRoom_BecomesMaintenance()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().SendToMaintenanceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Maintenance, room.Status);
    }

    /// <summary>Edge: sending a room that is already in maintenance changes nothing (A-08).</summary>
    [Fact]
    public async Task SendToMaintenanceAsync_AlreadyInMaintenance_IsAnIdempotentNoOp()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        room.SendToMaintenance();

        // Act
        var result = await world.RoomService().SendToMaintenanceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Maintenance, room.Status);
    }

    /// <summary>Negative: an unknown room cannot be sent to maintenance.</summary>
    [Fact]
    public async Task SendToMaintenanceAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().SendToMaintenanceAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: a blocked room returns to service.</summary>
    [Fact]
    public async Task ReturnToServiceAsync_RoomOutOfService_BecomesAvailable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        room.TakeOutOfService();

        // Act
        var result = await world.RoomService().ReturnToServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Available, room.Status);
    }

    /// <summary>Edge: returning a room that is already available changes nothing (A-08).</summary>
    [Fact]
    public async Task ReturnToServiceAsync_AvailableRoom_IsAnIdempotentNoOp()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().ReturnToServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Available, room.Status);
    }

    /// <summary>Negative: an unknown room cannot return to service.</summary>
    [Fact]
    public async Task ReturnToServiceAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().ReturnToServiceAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    #endregion

    #region Housekeeping transitions

    /// <summary>State transition: a clean room is marked for daily cleaning.</summary>
    [Fact]
    public async Task MarkDailyCleaningDueAsync_CleanRoom_BecomesDailyCleaningDue()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().MarkDailyCleaningDueAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.DailyCleaningDue, room.HousekeepingStatus);
    }

    /// <summary>State transition: a room being cleaned cannot be marked for daily cleaning.</summary>
    [Fact]
    public async Task MarkDailyCleaningDueAsync_CleaningInProgress_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.CleaningInProgress);

        // Act
        var result = await world.RoomService().MarkDailyCleaningDueAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.MarkDailyCleaningDueNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: a clean room is marked for departure cleaning.</summary>
    [Fact]
    public async Task MarkDepartureCleaningDueAsync_CleanRoom_BecomesDepartureCleaningDue()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().MarkDepartureCleaningDueAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.DepartureCleaningDue, room.HousekeepingStatus);
    }

    /// <summary>State transition: a room being cleaned cannot be marked for departure cleaning.</summary>
    [Fact]
    public async Task MarkDepartureCleaningDueAsync_CleaningInProgress_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.CleaningInProgress);

        // Act
        var result = await world.RoomService().MarkDepartureCleaningDueAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.MarkDepartureCleaningDueNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: cleaning starts on a room that is due for cleaning.</summary>
    [Fact]
    public async Task StartCleaningAsync_DailyCleaningDue_BecomesCleaningInProgress()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.DailyCleaningDue);

        // Act
        var result = await world.RoomService().StartCleaningAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.CleaningInProgress, room.HousekeepingStatus);
    }

    /// <summary>State transition: a clean room cannot start cleaning.</summary>
    [Fact]
    public async Task StartCleaningAsync_CleanRoom_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().StartCleaningAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.StartCleaningNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: cleaning completes and the room is clean again.</summary>
    [Fact]
    public async Task CompleteCleaningAsync_CleaningInProgress_BecomesClean()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.CleaningInProgress);

        // Act
        var result = await world.RoomService().CompleteCleaningAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.Clean, room.HousekeepingStatus);
    }

    /// <summary>State transition: cleaning cannot complete on a room that needs service.</summary>
    [Fact]
    public async Task CompleteCleaningAsync_ServiceRequired_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.ServiceRequired);

        // Act
        var result = await world.RoomService().CompleteCleaningAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.CompleteCleaningNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: a clean room can report that it needs service.</summary>
    [Fact]
    public async Task ReportServiceNeededAsync_CleanRoom_BecomesServiceRequired()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().ReportServiceNeededAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.ServiceRequired, room.HousekeepingStatus);
    }

    /// <summary>State transition: a room in the middle of service cannot report service needed.</summary>
    [Fact]
    public async Task ReportServiceNeededAsync_ServiceInProgress_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.ServiceInProgress);

        // Act
        var result = await world.RoomService().ReportServiceNeededAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.ReportServiceNeededNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: service starts on a room that needs it.</summary>
    [Fact]
    public async Task StartServiceAsync_ServiceRequired_BecomesServiceInProgress()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.ServiceRequired);

        // Act
        var result = await world.RoomService().StartServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HousekeepingStatus.ServiceInProgress, room.HousekeepingStatus);
    }

    /// <summary>State transition: service cannot start on a clean room.</summary>
    [Fact]
    public async Task StartServiceAsync_CleanRoom_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().StartServiceAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.StartServiceNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Equivalence: after service the room is clean, or due for cleaning if the service asks for it.</summary>
    [Theory]
    [InlineData(false, HousekeepingStatus.Clean)]
    [InlineData(true, HousekeepingStatus.DepartureCleaningDue)]
    public async Task CompleteServiceAsync_ServiceInProgress_LeavesTheRoomAsAsked(bool requiresCleaning, HousekeepingStatus expected)
    {
        // Arrange
        var world = new ServiceWorld();
        var room = RoomInStatus(world, HousekeepingStatus.ServiceInProgress);

        // Act
        var result = await world.RoomService().CompleteServiceAsync(room.Id, requiresCleaning, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, room.HousekeepingStatus);
    }

    /// <summary>State transition: service that asks for cleaning cannot complete on a clean room.</summary>
    [Fact]
    public async Task CompleteServiceAsync_CleanRoomNeedingCleaning_ReturnsNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();

        // Act
        var result = await world.RoomService().CompleteServiceAsync(room.Id, true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.CompleteServiceNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Negative: a transition on an unknown room reports the not-found code.</summary>
    [Fact]
    public async Task StartCleaningAsync_UnknownRoom_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomService().StartCleaningAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency conflict while saving a transition is reported.</summary>
    [Fact]
    public async Task MarkDailyCleaningDueAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        // Act
        var result = await world.RoomService().MarkDailyCleaningDueAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.ConcurrencyConflict, result.FirstError?.Code);
    }

    #endregion

    #region Helpers

    private static RoomListItemDto ListItem(int id) =>
        new(id, $"10{id}", 1, RoomSize.Double, 2, RoomStatus.Available, HousekeepingStatus.Clean);

    private static Room RoomInStatus(ServiceWorld world, HousekeepingStatus status)
    {
        var room = world.AddRoom();
        switch (status)
        {
            case HousekeepingStatus.Clean:
                break;
            case HousekeepingStatus.DailyCleaningDue:
                room.MarkDailyCleaningDue();
                break;
            case HousekeepingStatus.DepartureCleaningDue:
                room.MarkDepartureCleaningDue();
                break;
            case HousekeepingStatus.CleaningInProgress:
                room.MarkDailyCleaningDue();
                room.StartCleaning();
                break;
            case HousekeepingStatus.ServiceRequired:
                room.ReportServiceNeeded();
                break;
            case HousekeepingStatus.ServiceInProgress:
                room.ReportServiceNeeded();
                room.StartService();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return room;
    }

    #endregion
}