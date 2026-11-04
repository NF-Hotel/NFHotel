using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The room assignments of the housekeepers and the trades of rooms between them (MIL-060): the minimum of three rooms,
/// the answer of the colleague, the approval of a Manager, and the rooms that change hands meanwhile.
/// </summary>
public sealed class RoomAssignmentServiceTests
{
    #region Overview and assignment

    /// <summary>Positive: the overview lists every room with its housekeeper and every active housekeeper with a count.</summary>
    [Fact]
    public async Task GetOverviewAsync_AssignedRooms_ListsRoomsAndCounts()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2, 3);

        // Act
        var result = await world.Service().GetOverviewAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value.Rooms.Count);
        Assert.Equal("Hilda Berg", result.Value.Rooms[0].StaffName);
        Assert.Null(result.Value.Rooms[5].StaffId);
        Assert.Equal(4, result.Value.Housekeepers.Single(keeper => keeper.Id == world.Hilda.Id).RoomCount);
        Assert.DoesNotContain(result.Value.Housekeepers, keeper => keeper.Id == world.Manager.Id);
        Assert.Equal(RoomAssignment.MinimumRooms, result.Value.MinimumRooms);
    }

    /// <summary>State transition: a Manager assigns a free room, moves it, and frees it again.</summary>
    [Fact]
    public async Task AssignAsync_AssignMoveAndFree_FollowsTheRequest()
    {
        // Arrange
        var world = new Plan();
        var service = world.Service(world.Manager);
        var room = world.Rooms[0];

        // Act
        await service.AssignAsync(room.Id, new AssignRoomRequest(world.Hilda.Id), TestContext.Current.CancellationToken);
        var first = world.Data.RoomAssignments.Items.Single().StaffId;
        await service.AssignAsync(room.Id, new AssignRoomRequest(world.Otto.Id), TestContext.Current.CancellationToken);
        var second = world.Data.RoomAssignments.Items.Single().StaffId;
        await service.AssignAsync(room.Id, new AssignRoomRequest(null), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((world.Hilda.Id, world.Otto.Id), (first, second));
        Assert.Empty(world.Data.RoomAssignments.Items);
    }

    /// <summary>Negative: a room cannot go to a Manager, a disabled housekeeper, or a room that does not exist.</summary>
    [Fact]
    public async Task AssignAsync_NotAnActiveHousekeeperOrNoRoom_IsRefused()
    {
        // Arrange
        var world = new Plan();
        var service = world.Service(world.Manager);
        world.Otto.Deactivate();

        // Act
        var manager = await service.AssignAsync(world.Rooms[0].Id, new AssignRoomRequest(world.Manager.Id), TestContext.Current.CancellationToken);
        var disabled = await service.AssignAsync(world.Rooms[0].Id, new AssignRoomRequest(world.Otto.Id), TestContext.Current.CancellationToken);
        var noRoom = await service.AssignAsync(999, new AssignRoomRequest(world.Hilda.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RoomTradeErrors.AssigneeInvalid, manager.FirstError?.Code);
        Assert.Equal(RoomTradeErrors.AssigneeInvalid, disabled.FirstError?.Code);
        Assert.Equal(ErrorCodes.Room.NotFound, noRoom.FirstError?.Code);
    }

    #endregion

    #region Offer

    /// <summary>Negative: a housekeeper can only give own rooms, ask for the colleague's, and trade with an active housekeeper.</summary>
    [Fact]
    public async Task OfferTradeAsync_RoomsOfOthersOrNoHousekeeper_IsRefused()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2, 3);
        world.Give(world.Otto, 4);
        var service = world.Service(world.Hilda);

        // Act
        var notMine = await service.OfferTradeAsync(world.Offer(world.Otto, give: [4]), TestContext.Current.CancellationToken);
        var notTheirs = await service.OfferTradeAsync(world.Offer(world.Otto, give: [0], take: [5]), TestContext.Current.CancellationToken);
        var toManager = await service.OfferTradeAsync(world.Offer(world.Manager, give: [0]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RoomTradeErrors.RoomNotYours, notMine.FirstError?.Code);
        Assert.Equal(RoomTradeErrors.RoomNotTheirs, notTheirs.FirstError?.Code);
        Assert.Equal(RoomTradeErrors.NotHousekeeper, toManager.FirstError?.Code);
        Assert.Empty(world.Data.RoomAssignments.Trades.Items);
    }

    #endregion

    #region Accept

    /// <summary>Positive: a gift that leaves the giver with three rooms is carried out as soon as the colleague accepts.</summary>
    [Fact]
    public async Task AcceptTradeAsync_GiverKeepsTheMinimum_MovesTheRoomsAtOnce()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2, 3);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [3]));

        // Act
        var result = await world.Service(world.Otto).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TradeState.Done, result.Value.State);
        Assert.Equal(world.Otto.Id, world.HolderOf(3));
        Assert.Equal(3, world.CountOf(world.Hilda));
    }

    /// <summary>Boundary: a gift that leaves the giver with two rooms waits for a Manager, and moves nothing until approved.</summary>
    [Fact]
    public async Task AcceptTradeAsync_GiverBelowTheMinimum_WaitsForAManagerWhoApproves()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [2]));

        // Act
        var accepted = await world.Service(world.Otto).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);
        var heldMeanwhile = world.HolderOf(2);
        var approved = await world.Service(world.Manager).ApproveTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TradeState.AwaitingApproval, accepted.Value.State);
        Assert.Equal(world.Hilda.Id, heldMeanwhile);
        Assert.Equal(TradeState.Done, approved.Value.State);
        Assert.Equal(world.Otto.Id, world.HolderOf(2));
    }

    /// <summary>Positive: a swap moves the rooms both ways.</summary>
    [Fact]
    public async Task AcceptTradeAsync_Swap_MovesTheRoomsBothWays()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2);
        world.Give(world.Otto, 3, 4, 5);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [0], take: [3]));

        // Act
        var result = await world.Service(world.Otto).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TradeState.Done, result.Value.State);
        Assert.Equal((world.Otto.Id, world.Hilda.Id), (world.HolderOf(0), world.HolderOf(3)));
    }

    /// <summary>Negative: only the colleague the trade is for accepts it.</summary>
    [Fact]
    public async Task AcceptTradeAsync_ByTheGiver_IsNotYours()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2, 3);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [3]));

        // Act
        var result = await world.Service(world.Hilda).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RoomTradeErrors.NotYours, result.FirstError?.Code);
        Assert.Equal(world.Hilda.Id, world.HolderOf(3));
    }

    /// <summary>State transition: when a Manager moved a room of the trade meanwhile, accepting calls the trade off.</summary>
    [Fact]
    public async Task AcceptTradeAsync_RoomMovedMeanwhile_CancelsTheTrade()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2, 3);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [3]));
        await world.Service(world.Manager).AssignAsync(world.Rooms[3].Id, new AssignRoomRequest(null), TestContext.Current.CancellationToken);

        // Act
        var result = await world.Service(world.Otto).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RoomTradeErrors.RoomsChanged, result.FirstError?.Code);
        Assert.Equal(TradeState.Cancelled, world.Data.RoomAssignments.Trades.Items.Single().State);
    }

    #endregion

    #region Lists

    /// <summary>Equivalence: a housekeeper sees the own trades; a Manager also sees every trade that waits for approval.</summary>
    [Fact]
    public async Task ListTradesAsync_HousekeeperAndManager_SeeTheirTrades()
    {
        // Arrange
        var world = new Plan();
        world.Give(world.Hilda, 0, 1, 2);
        var trade = await world.OfferAsync(world.Hilda, world.Offer(world.Otto, give: [2]));
        await world.Service(world.Otto).AcceptTradeAsync(trade.Id, TestContext.Current.CancellationToken);

        // Act
        var hilda = await world.Service(world.Hilda).ListTradesAsync(TestContext.Current.CancellationToken);
        var nora = await world.Service(world.Nora).ListTradesAsync(TestContext.Current.CancellationToken);
        var manager = await world.Service(world.Manager).ListTradesAsync(TestContext.Current.CancellationToken);

        // Assert
        var listed = Assert.Single(hilda.Value);
        Assert.Equal(("Hilda Berg", "Otto Lund", "203"), (listed.FromName, listed.ToName, listed.Give.Single().RoomNumber));
        Assert.Empty(nora.Value);
        Assert.Single(manager.Value);
    }

    #endregion

    /// <summary>Six rooms, three housekeepers and a Manager.</summary>
    private sealed class Plan
    {
        public Plan()
        {
            Manager = AddStaff("mia", StaffRole.Manager, "Mia", "Holm");
            Hilda = AddStaff("hilda", StaffRole.Housekeeping, "Hilda", "Berg");
            Otto = AddStaff("otto", StaffRole.Housekeeping, "Otto", "Lund");
            Nora = AddStaff("nora", StaffRole.Housekeeping, null, null);
            foreach (var number in new[] { "201", "202", "203", "204", "205", "206" })
            {
                var room = Room.Create(number, 2, RoomSize.Double, 2);
                Data.Rooms.Add(room);
                Rooms.Add(room);
            }
        }

        public ServiceWorld Data { get; } = new();

        public List<Room> Rooms { get; } = [];

        public StaffMember Manager { get; }

        public StaffMember Hilda { get; }

        public StaffMember Otto { get; }

        public StaffMember Nora { get; }

        public NFHotel.Application.UseCases.Rooms.RoomAssignmentService Service(StaffMember? caller = null)
        {
            var context = new FakeStaffCallContext { StaffId = caller?.Id, StaffName = caller?.UserName };
            return Data.RoomAssignmentService(context: context);
        }

        public void Give(StaffMember housekeeper, params int[] roomIndexes)
        {
            foreach (var index in roomIndexes)
            {
                Data.RoomAssignments.Add(RoomAssignment.Create(Rooms[index].Id, housekeeper.Id));
            }
        }

        public OfferRoomTradeRequest Offer(StaffMember to, int[] give, int[]? take = null) =>
            new(to.Id, [.. give.Select(index => Rooms[index].Id)], [.. (take ?? []).Select(index => Rooms[index].Id)]);

        public async Task<RoomTradeDto> OfferAsync(StaffMember from, OfferRoomTradeRequest request)
        {
            var result = await Service(from).OfferTradeAsync(request, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(error => error.Code)));
            return result.Value;
        }

        public int? HolderOf(int roomIndex) =>
            Data.RoomAssignments.Items.SingleOrDefault(assignment => assignment.RoomId == Rooms[roomIndex].Id)?.StaffId;

        public int CountOf(StaffMember housekeeper) => Data.RoomAssignments.Items.Count(assignment => assignment.StaffId == housekeeper.Id);

        private StaffMember AddStaff(string userName, StaffRole role, string? first, string? last)
        {
            var member = StaffMember.Create(userName, "hash", role, firstName: first, lastName: last);
            Data.Staff.Add(member);
            return member;
        }
    }
}
