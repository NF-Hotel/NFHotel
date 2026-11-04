using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Dto.Shifts;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The room assignments, the shifts and their trades through HTTP with the staff token alone, as the mobile app calls
/// (MIL-060, ADR-0015): who may take each step, and the status of each refusal.
/// </summary>
public sealed class PlanningEndpointTests
{
    private const string Password = "correct horse battery staple";

    #region Rooms

    /// <summary>Integration: a housekeeper gives a room, the colleague accepts, and the overview shows the new holder.</summary>
    [Fact]
    public async Task RoomTrade_OfferAndAccept_MovesTheRoom()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var hilda = factory.AddStaff("trade-hilda", Password, StaffRole.Housekeeping);
        var otto = factory.AddStaff("trade-otto", Password, StaffRole.Housekeeping);
        var rooms = AddRooms(factory, 4);
        foreach (var room in rooms)
        {
            factory.World.RoomAssignments.Add(RoomAssignment.Create(room.Id, hilda.Id));
        }

        using var hildaClient = Client(factory, hilda);
        using var ottoClient = Client(factory, otto);

        // Act
        using var offered = await hildaClient.PostAsJsonAsync(
            ApiRoutes.RoomTrades, new OfferRoomTradeRequest(otto.Id, [rooms[3].Id], []), ApiJson.Options, TestContext.Current.CancellationToken);
        var trade = (await offered.Content.ReadFromJsonAsync<ApiResponse<RoomTradeDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
        using var byGiver = await hildaClient.PostAsync($"{ApiRoutes.RoomTrades}/{trade.Id}/accept", null, TestContext.Current.CancellationToken);
        using var accepted = await ottoClient.PostAsync($"{ApiRoutes.RoomTrades}/{trade.Id}/accept", null, TestContext.Current.CancellationToken);
        using var again = await ottoClient.PostAsync($"{ApiRoutes.RoomTrades}/{trade.Id}/accept", null, TestContext.Current.CancellationToken);
        var overview = await ottoClient.GetFromJsonAsync<ApiResponse<RoomAssignmentOverviewDto>>(ApiRoutes.RoomAssignments, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, offered.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byGiver.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(otto.Id, overview!.Value!.Rooms.Single(room => room.RoomId == rooms[3].Id).StaffId);
    }

    /// <summary>Equivalence: only a Manager assigns rooms and approves; a receptionist cannot see the assignments.</summary>
    [Theory]
    [InlineData(StaffRole.Manager, HttpStatusCode.OK, HttpStatusCode.OK)]
    [InlineData(StaffRole.Housekeeping, HttpStatusCode.OK, HttpStatusCode.Forbidden)]
    [InlineData(StaffRole.Receptionist, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden)]
    public async Task RoomAssignments_ByRole_FollowThePermissions(StaffRole role, HttpStatusCode read, HttpStatusCode assign)
    {
        // Arrange
        await using var factory = new ApiFactory();
        var keeper = factory.AddStaff("assign-keeper", Password, StaffRole.Housekeeping);
        var room = AddRooms(factory, 1)[0];
        using var client = Client(factory, factory.AddStaff("assign-caller", Password, role));

        // Act
        using var readResponse = await client.GetAsync(ApiRoutes.RoomAssignments, TestContext.Current.CancellationToken);
        using var assignResponse = await client.PutAsJsonAsync(
            $"{ApiRoutes.RoomAssignments}/{room.Id}", new AssignRoomRequest(keeper.Id), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(read, readResponse.StatusCode);
        Assert.Equal(assign, assignResponse.StatusCode);
    }

    #endregion

    #region Shifts

    /// <summary>Integration: a Manager plans a shift, the member gives it away, a colleague takes it, a Manager approves.</summary>
    [Fact]
    public async Task ShiftTrade_GiveTakeApprove_ChangesHands()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var manager = factory.AddStaff("shift-boss", Password, StaffRole.Manager);
        var hilda = factory.AddStaff("shift-hilda", Password, StaffRole.Housekeeping);
        var otto = factory.AddStaff("shift-otto", Password, StaffRole.Housekeeping);
        using var managerClient = Client(factory, manager);
        using var hildaClient = Client(factory, hilda);
        using var ottoClient = Client(factory, otto);
        var start = factory.World.Clock.UtcNow.AddDays(3);

        // Act
        using var planned = await managerClient.PostAsJsonAsync(
            ApiRoutes.Shifts, new SaveShiftRequest(start, start.AddHours(8), StaffRole.Housekeeping, hilda.Id), ApiJson.Options, TestContext.Current.CancellationToken);
        var shift = (await planned.Content.ReadFromJsonAsync<ApiResponse<ShiftDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
        using var byHousekeeper = await hildaClient.PostAsJsonAsync(
            ApiRoutes.Shifts, new SaveShiftRequest(start, start.AddHours(8), StaffRole.Housekeeping, hilda.Id), ApiJson.Options, TestContext.Current.CancellationToken);
        using var offered = await hildaClient.PostAsJsonAsync(
            ApiRoutes.ShiftTrades, new OfferShiftTradeRequest(shift.Id, null), ApiJson.Options, TestContext.Current.CancellationToken);
        var trade = (await offered.Content.ReadFromJsonAsync<ApiResponse<ShiftTradeDto>>(ApiJson.Options, TestContext.Current.CancellationToken))!.Value!;
        using var taken = await ottoClient.PostAsync($"{ApiRoutes.ShiftTrades}/{trade.Id}/take", null, TestContext.Current.CancellationToken);
        using var approveByOtto = await ottoClient.PostAsync($"{ApiRoutes.ShiftTrades}/{trade.Id}/approve", null, TestContext.Current.CancellationToken);
        using var approved = await managerClient.PostAsync($"{ApiRoutes.ShiftTrades}/{trade.Id}/approve", null, TestContext.Current.CancellationToken);
        var ottoShifts = await ottoClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ShiftDto>>>(
            $"{ApiRoutes.Shifts}?from={Uri.EscapeDataString(start.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(start.AddDays(1).ToString("O"))}",
            ApiJson.Options,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, planned.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byHousekeeper.StatusCode);
        Assert.Equal(HttpStatusCode.OK, taken.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, approveByOtto.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(otto.Id, ottoShifts!.Value!.Single().AssigneeId);
    }

    /// <summary>Negative: a shift within the cut-off is refused with 409 and its code.</summary>
    [Fact]
    public async Task ShiftTrade_WithinTheCutoff_Returns409()
    {
        // Arrange
        await using var factory = new ApiFactory();
        var hilda = factory.AddStaff("cutoff-hilda", Password, StaffRole.Housekeeping);
        var start = factory.World.Clock.UtcNow.AddHours(5);
        var shift = Shift.Plan(start, start.AddHours(8), StaffRole.Housekeeping, hilda.Id);
        factory.World.Shifts.Add(shift);
        using var client = Client(factory, hilda);

        // Act
        using var response = await client.PostAsJsonAsync(
            ApiRoutes.ShiftTrades, new OfferShiftTradeRequest(shift.Id, null), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal([ShiftErrors.TradeExpired], body!.Errors);
    }

    #endregion

    #region Helpers

    private static List<Room> AddRooms(ApiFactory factory, int count)
    {
        var rooms = new List<Room>();
        for (var index = 0; index < count; index++)
        {
            var room = Room.Create($"9{index:00}", 9, RoomSize.Single, 1);
            factory.World.Rooms.Add(room);
            rooms.Add(room);
        }

        return rooms;
    }

    private static HttpClient Client(ApiFactory factory, StaffMember member)
    {
        // The staff token alone, as the mobile app sends it.
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, factory.IssueStaffToken(member));
        return client;
    }

    #endregion
}
