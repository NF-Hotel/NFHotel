using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Rooms;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the room list and the room form of the back office (MIL-054 task 4).</summary>
public sealed class RoomViewModelTests
{
    private static readonly RoomFilterOptionsDto Options = new([1, 2], [RoomSize.Double], [RoomStatus.Available], [HousekeepingStatus.Clean]);

    private readonly IRoomService _rooms = Substitute.For<IRoomService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private RoomListViewModel CreateList() => new(new PassThroughRunner<IRoomService>(_rooms));

    private RoomFormViewModel CreateForm() => new(new PassThroughRunner<IRoomService>(_rooms));

    private void SearchFinds(params RoomListItemDto[] rooms) =>
        _rooms.SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>()).Returns(List(rooms));

    private void EveryTransitionSucceeds()
    {
        var done = Ok(RoomDetails());
        _rooms.TakeOutOfServiceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.SendToMaintenanceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.ReturnToServiceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.MarkDailyCleaningDueAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.MarkDepartureCleaningDueAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.StartCleaningAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.CompleteCleaningAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.ReportServiceNeededAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.StartServiceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(done);
        _rooms.CompleteServiceAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(done);
    }

    #region Room list

    /// <summary>Negative: the runner is required, and a missing room is refused.</summary>
    [Fact]
    public void RoomList_MissingArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new RoomListViewModel(null!));
        var viewModel = CreateList();
        Assert.Throws<ArgumentNullException>(() => viewModel.ToggleSelection(null!));
        Assert.Throws<ArgumentNullException>(() => viewModel.IsSelected(null!));
        Assert.Throws<ArgumentNullException>(() => viewModel.RequestDelete(null!));
    }

    /// <summary>Positive: the first load fetches the filter options, then the rooms with no filter.</summary>
    [Fact]
    public async Task InitializeAsync_Rooms_LoadsOptionsThenRooms()
    {
        _rooms.GetFilterOptionsAsync(Arg.Any<CancellationToken>()).Returns(Ok(Options));
        SearchFinds(Room(1), Room(2));
        var viewModel = CreateList();

        await viewModel.InitializeAsync(Token);

        Assert.Same(Options, viewModel.FilterOptions);
        Assert.Equal(2, viewModel.Rooms.Count);
        Assert.False(viewModel.IsEmpty);
        Assert.False(viewModel.HasFilter);
        await _rooms.Received(1).SearchAsync(new RoomFilter(), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: without the filter options the rooms are not asked for.</summary>
    [Fact]
    public async Task InitializeAsync_OptionsRefused_StopsWithTheError()
    {
        _rooms.GetFilterOptionsAsync(Arg.Any<CancellationToken>()).Returns(Fail<RoomFilterOptionsDto>(ErrorCodes.Room.NotFound));
        var viewModel = CreateList();

        await viewModel.InitializeAsync(Token);

        Assert.True(viewModel.IsLoaded);
        Assert.Equal([Text(ErrorCodes.Room.NotFound)], viewModel.Errors);
        await _rooms.DidNotReceiveWithAnyArgs().SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Equivalence: each filter alone counts as a filter, and clearing resets them all.</summary>
    [Fact]
    public async Task ClearFilterAsync_FiltersSet_ResetsAndSearches()
    {
        SearchFinds();
        var viewModel = CreateList();
        viewModel.FilterFloor = 2;
        Assert.True(viewModel.HasFilter);
        viewModel.FilterFloor = null;
        viewModel.FilterSize = RoomSize.Suite;
        Assert.True(viewModel.HasFilter);
        viewModel.FilterSize = null;
        viewModel.FilterStatus = RoomStatus.Maintenance;
        Assert.True(viewModel.HasFilter);
        viewModel.FilterStatus = null;
        viewModel.FilterHousekeepingStatus = HousekeepingStatus.DailyCleaningDue;
        Assert.True(viewModel.HasFilter);

        await viewModel.ClearFilterAsync(Token);

        Assert.False(viewModel.HasFilter);
        Assert.True(viewModel.IsEmpty);
        await _rooms.Received(1).SearchAsync(new RoomFilter(), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused search shows the text and empties the list.</summary>
    [Fact]
    public async Task SearchAsync_Refused_ShowsTheError()
    {
        _rooms.SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<RoomListItemDto>>(ErrorCodes.Room.NotFound));
        var viewModel = CreateList();

        await viewModel.SearchAsync(Token);

        Assert.Empty(viewModel.Rooms);
        Assert.False(viewModel.IsEmpty);
    }

    /// <summary>State transition: a click selects a room, a second click on it deselects; a reload finds the selection again or drops it.</summary>
    [Fact]
    public async Task ToggleSelection_AndReload_FollowsTheRoom()
    {
        var viewModel = CreateList();
        Assert.Empty(viewModel.AvailableActions);

        viewModel.ToggleSelection(Room(1));
        Assert.True(viewModel.IsSelected(Room(1)));
        Assert.NotEmpty(viewModel.AvailableActions);

        SearchFinds(Room(1, RoomStatus.OutOfService));
        await viewModel.SearchAsync(Token);
        Assert.Equal(RoomStatus.OutOfService, viewModel.SelectedRoom!.Status);

        SearchFinds(Room(2));
        await viewModel.SearchAsync(Token);
        Assert.Null(viewModel.SelectedRoom);

        viewModel.ToggleSelection(Room(2));
        viewModel.ToggleSelection(Room(2));
        Assert.False(viewModel.IsSelected(Room(2)));
    }

    /// <summary>Equivalence: over every status, each of the eleven actions is offered with its label.</summary>
    [Fact]
    public void AvailableActions_EveryStatus_OffersEveryActionSomewhere()
    {
        var viewModel = CreateList();
        var offered = new HashSet<RoomAction>();

        foreach (var status in Enum.GetValues<RoomStatus>())
        {
            foreach (var housekeeping in Enum.GetValues<HousekeepingStatus>())
            {
                viewModel.ToggleSelection(Room(1, status, housekeeping));
                offered.UnionWith(viewModel.AvailableActions.Select(option => option.Action));
                Assert.All(viewModel.AvailableActions, option => Assert.False(string.IsNullOrWhiteSpace(option.Label)));
                viewModel.ToggleSelection(Room(1, status, housekeeping));
            }
        }

        Assert.Equal(Enum.GetValues<RoomAction>().ToHashSet(), offered);
    }

    /// <summary>Equivalence: each action calls its own use case on the selected room and reloads.</summary>
    [Theory]
    [InlineData(RoomAction.TakeOutOfService, nameof(IRoomService.TakeOutOfServiceAsync))]
    [InlineData(RoomAction.SendToMaintenance, nameof(IRoomService.SendToMaintenanceAsync))]
    [InlineData(RoomAction.ReturnToService, nameof(IRoomService.ReturnToServiceAsync))]
    [InlineData(RoomAction.MarkDailyCleaningDue, nameof(IRoomService.MarkDailyCleaningDueAsync))]
    [InlineData(RoomAction.MarkDepartureCleaningDue, nameof(IRoomService.MarkDepartureCleaningDueAsync))]
    [InlineData(RoomAction.StartCleaning, nameof(IRoomService.StartCleaningAsync))]
    [InlineData(RoomAction.CompleteCleaning, nameof(IRoomService.CompleteCleaningAsync))]
    [InlineData(RoomAction.ReportServiceNeeded, nameof(IRoomService.ReportServiceNeededAsync))]
    [InlineData(RoomAction.StartService, nameof(IRoomService.StartServiceAsync))]
    [InlineData(RoomAction.CompleteServiceStillDirty, nameof(IRoomService.CompleteServiceAsync))]
    [InlineData(RoomAction.CompleteServiceClean, nameof(IRoomService.CompleteServiceAsync))]
    public async Task ExecuteAsync_Action_CallsItsUseCase(RoomAction action, string useCase)
    {
        EveryTransitionSucceeds();
        SearchFinds(Room(4));
        var viewModel = CreateList();
        viewModel.ToggleSelection(Room(4));

        await viewModel.ExecuteAsync(action, Token);

        Assert.Empty(viewModel.Errors);
        var call = _rooms.ReceivedCalls().First(received => received.GetMethodInfo().Name == useCase);
        Assert.Equal(4, call.GetArguments()[0]);
        if (useCase == nameof(IRoomService.CompleteServiceAsync))
        {
            Assert.Equal(action == RoomAction.CompleteServiceStillDirty, call.GetArguments()[1]);
        }
    }

    /// <summary>Negative: an unknown action is refused; without a selected room nothing happens.</summary>
    [Fact]
    public async Task ExecuteAsync_UnknownActionOrNoRoom_IsRefusedOrIgnored()
    {
        var viewModel = CreateList();
        await viewModel.ExecuteAsync(RoomAction.StartCleaning, Token);
        Assert.Empty(_rooms.ReceivedCalls());

        viewModel.ToggleSelection(Room(4));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => viewModel.ExecuteAsync((RoomAction)99, Token));
    }

    /// <summary>Error handling: a refused transition shows the text and does not reload.</summary>
    [Fact]
    public async Task ExecuteAsync_Refused_ShowsTheError()
    {
        _rooms.StartCleaningAsync(4, Arg.Any<CancellationToken>()).Returns(Fail<RoomDetailsDto>(ErrorCodes.Room.ConcurrencyConflict));
        var viewModel = CreateList();
        viewModel.ToggleSelection(Room(4));

        await viewModel.ExecuteAsync(RoomAction.StartCleaning, Token);

        Assert.Equal([Text(ErrorCodes.Room.ConcurrencyConflict)], viewModel.Errors);
        await _rooms.DidNotReceiveWithAnyArgs().SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Concurrency: an action or a deletion while a load runs is ignored.</summary>
    [Fact]
    public async Task ExecuteAndDelete_WhileLoading_AreIgnored()
    {
        var pending = new TaskCompletionSource<Result<IReadOnlyList<RoomListItemDto>>>();
        _rooms.SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = CreateList();
        viewModel.ToggleSelection(Room(4));

        var load = viewModel.SearchAsync(Token);
        await viewModel.ExecuteAsync(RoomAction.StartCleaning, Token);
        viewModel.RequestDelete(Room(4));
        await viewModel.ConfirmDeleteAsync(Token);
        pending.SetResult(Result<IReadOnlyList<RoomListItemDto>>.Success([]));
        await load;

        await _rooms.DidNotReceiveWithAnyArgs().StartCleaningAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _rooms.DidNotReceiveWithAnyArgs().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: deleting the selected room clears the selection, reloads and keeps the receipt.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_SelectedRoom_ClearsTheSelection()
    {
        SearchFinds();
        _rooms.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var viewModel = CreateList();
        viewModel.ToggleSelection(Room(4));
        viewModel.RequestDelete(Room(4));

        await viewModel.ConfirmDeleteAsync(Token);

        Assert.Null(viewModel.SelectedRoom);
        Assert.Equal("Room 104 was deleted.", viewModel.SuccessMessage);
        viewModel.DismissSuccess();
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>Positive: deleting another room keeps the selection.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_OtherRoom_KeepsTheSelection()
    {
        SearchFinds(Room(1));
        _rooms.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        var viewModel = CreateList();
        viewModel.ToggleSelection(Room(1));
        viewModel.RequestDelete(Room(4));

        await viewModel.ConfirmDeleteAsync(Token);

        Assert.Equal(1, viewModel.SelectedRoom!.RoomId);
    }

    /// <summary>Error handling: a refused deletion shows the text and clears the question; an aborted one deletes nothing.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_RefusedThenAborted_DeletesNothing()
    {
        _rooms.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure(ErrorCodes.Room.NotFound)));
        var viewModel = CreateList();
        viewModel.RequestDelete(Room(4));

        await viewModel.ConfirmDeleteAsync(Token);
        Assert.Equal([Text(ErrorCodes.Room.NotFound)], viewModel.Errors);
        Assert.Null(viewModel.RoomPendingDeletion);

        viewModel.RequestDelete(Room(4));
        viewModel.AbortDelete();
        await viewModel.ConfirmDeleteAsync(Token);
        await _rooms.Received(1).DeleteAsync(4, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Room form

    /// <summary>Negative: the runner is required.</summary>
    [Fact]
    public void RoomForm_MissingRunner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RoomFormViewModel(null!));
    }

    /// <summary>Positive: a new form starts at floor 1, a single room for one person.</summary>
    [Fact]
    public void BeginCreate_NewForm_HasTheDefaults()
    {
        var viewModel = CreateForm();
        viewModel.RoomNumber = "old";

        viewModel.BeginCreate();

        Assert.True(viewModel.IsOpen);
        Assert.Equal("New room", viewModel.Title);
        Assert.Equal(string.Empty, viewModel.RoomNumber);
        Assert.Equal(1, viewModel.Floor);
        Assert.Equal(RoomSize.Single, viewModel.Size);
        Assert.Equal(1, viewModel.Capacity);
        Assert.Equal(Enum.GetValues<RoomSize>(), viewModel.Sizes);
        Assert.True(viewModel.CanSave);
    }

    /// <summary>Positive and error handling: editing loads the room; a room that cannot be read leaves the form closed.</summary>
    [Fact]
    public async Task BeginEditAsync_FoundThenRefused_FillsThenShowsTheError()
    {
        _rooms.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(1)), Fail<RoomDetailsDto>(ErrorCodes.Room.NotFound));
        var viewModel = CreateForm();

        await viewModel.BeginEditAsync(1, Token);
        Assert.True(viewModel.IsOpen);
        Assert.Equal("Edit room 101", viewModel.Title);
        Assert.Equal(RoomSize.Double, viewModel.Size);
        Assert.Equal(2, viewModel.Capacity);

        viewModel.Close();
        Assert.False(viewModel.IsOpen);
        Assert.Null(viewModel.EditingRoomId);

        await viewModel.BeginEditAsync(1, Token);
        Assert.False(viewModel.IsOpen);
        Assert.Equal([Text(ErrorCodes.Room.NotFound)], viewModel.Errors);
    }

    /// <summary>Positive: a new room is created with the entered values, and the form closes with a receipt.</summary>
    [Fact]
    public async Task SaveAsync_NewRoom_Creates()
    {
        _rooms.CreateAsync(Arg.Any<CreateRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(5)));
        var viewModel = CreateForm();
        viewModel.BeginCreate();
        viewModel.RoomNumber = "105";
        viewModel.Floor = 1;
        viewModel.Size = RoomSize.Double;
        viewModel.Capacity = 2;

        await viewModel.SaveAsync(Token);

        Assert.True(viewModel.SavedSuccessfully);
        Assert.False(viewModel.IsOpen);
        Assert.Equal("Room 105 was created.", viewModel.SuccessMessage);
        await _rooms.Received(1).CreateAsync(new CreateRoomRequest("105", 1, RoomSize.Double, 2), Arg.Any<CancellationToken>());
        viewModel.DismissSuccess();
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>Positive: an edited room is updated under its own id.</summary>
    [Fact]
    public async Task SaveAsync_EditedRoom_Updates()
    {
        _rooms.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(1)));
        _rooms.UpdateAsync(Arg.Any<UpdateRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(RoomDetails(1)));
        var viewModel = CreateForm();
        await viewModel.BeginEditAsync(1, Token);

        await viewModel.SaveAsync(Token);

        Assert.Equal("Room 101 was updated.", viewModel.SuccessMessage);
        await _rooms.Received(1).UpdateAsync(new UpdateRoomRequest(1, "101", 1, RoomSize.Double, 2), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused save keeps the form open with the text.</summary>
    [Fact]
    public async Task SaveAsync_Refused_KeepsTheFormOpen()
    {
        _rooms.CreateAsync(Arg.Any<CreateRoomRequest>(), Arg.Any<CancellationToken>()).Returns(Fail<RoomDetailsDto>(ErrorCodes.Room.ConcurrencyConflict));
        var viewModel = CreateForm();
        viewModel.BeginCreate();

        await viewModel.SaveAsync(Token);

        Assert.False(viewModel.SavedSuccessfully);
        Assert.True(viewModel.IsOpen);
        Assert.Equal([Text(ErrorCodes.Room.ConcurrencyConflict)], viewModel.Errors);
    }

    /// <summary>Concurrency: a save or an edit while a load runs is ignored.</summary>
    [Fact]
    public async Task SaveAndEdit_WhileLoading_AreIgnored()
    {
        var pending = new TaskCompletionSource<Result<RoomDetailsDto>>();
        _rooms.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = CreateForm();

        var load = viewModel.BeginEditAsync(1, Token);
        Assert.False(viewModel.CanSave);
        await viewModel.BeginEditAsync(1, Token);
        await viewModel.SaveAsync(Token);
        pending.SetResult(Result<RoomDetailsDto>.Success(RoomDetails(1)));
        await load;

        await _rooms.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
        await _rooms.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<UpdateRoomRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
