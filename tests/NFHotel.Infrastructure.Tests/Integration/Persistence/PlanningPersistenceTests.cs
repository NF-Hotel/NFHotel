using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The room assignments, the shifts and their trades against a real PostgreSQL (MIL-060).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PlanningPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2030, 3, 4, 7, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public PlanningPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Rooms

    /// <summary>Integration: a trade keeps its rooms as arrays and its state, and the list finds it for both housekeepers.</summary>
    [Fact]
    public async Task RoomTrade_RoundTrip_KeepsTheRoomsAndTheState()
    {
        // Arrange
        var (hilda, otto, rooms) = await SeedAsync();
        await using (var context = _fixture.CreateContext())
        {
            var offered = RoomTrade.Offer(hilda, otto, [rooms[1], rooms[0]], [rooms[2]], Start);
            offered.Accept(otto, needsApproval: true, Start);
            new RoomAssignmentRepository(context).AddTrade(offered);
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();
        var repository = new RoomAssignmentRepository(reader);

        // Act
        var forOtto = await repository.ListTradesAsync(otto, includeAwaitingApproval: false, TestContext.Current.CancellationToken);
        var forManager = await repository.ListTradesAsync(null, includeAwaitingApproval: true, TestContext.Current.CancellationToken);

        // Assert
        var trade = Assert.Single(forOtto);
        Assert.Equal([rooms[0], rooms[1]], trade.GiveRoomIds);
        Assert.Equal([rooms[2]], trade.TakeRoomIds);
        Assert.Equal(TradeState.AwaitingApproval, trade.State);
        Assert.Single(forManager);
    }

    /// <summary>Integration: a room has one housekeeper; a second assignment of the same room breaks the unique index.</summary>
    [Fact]
    public async Task RoomAssignment_SameRoomTwice_IsRefusedByTheIndex()
    {
        // Arrange
        var (hilda, otto, rooms) = await SeedAsync();
        await using var context = _fixture.CreateContext();
        var repository = new RoomAssignmentRepository(context);
        repository.Add(RoomAssignment.Create(rooms[0], hilda));
        repository.Add(RoomAssignment.Create(rooms[0], otto));

        // Act and assert
        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region Deletion and shifts

    /// <summary>
    /// Integration: deleting an account removes its room assignments and trades and leaves its shifts without a member.
    /// </summary>
    [Fact]
    public async Task DeleteStaff_WithAssignmentsShiftsAndTrades_CascadesAndClearsTheShift()
    {
        // Arrange
        var (hilda, otto, rooms) = await SeedAsync();
        int shiftId;
        await using (var context = _fixture.CreateContext())
        {
            var assignments = new RoomAssignmentRepository(context);
            assignments.Add(RoomAssignment.Create(rooms[0], hilda));
            assignments.AddTrade(RoomTrade.Offer(hilda, otto, [rooms[0]], [], Start));
            var shifts = new ShiftRepository(context);
            var planned = Shift.Plan(Start, Start.AddHours(8), StaffRole.Housekeeping, hilda);
            shifts.Add(planned);
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
            shifts.AddTrade(ShiftTrade.GiveAway(planned.Id, hilda, Start.AddDays(-3)));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
            shiftId = planned.Id;
        }

        await using (var context = _fixture.CreateContext())
        {
            var staff = new StaffRepository(context);
            staff.Remove((await staff.GetByIdAsync(hilda, TestContext.Current.CancellationToken))!);
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();

        // Act
        var assignmentsLeft = await reader.RoomAssignments.CountAsync(TestContext.Current.CancellationToken);
        var tradesLeft = await reader.RoomTrades.CountAsync(TestContext.Current.CancellationToken) + await reader.ShiftTrades.CountAsync(TestContext.Current.CancellationToken);
        var shift = await new ShiftRepository(reader).GetByIdAsync(shiftId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, assignmentsLeft);
        Assert.Equal(0, tradesLeft);
        Assert.NotNull(shift);
        Assert.Null(shift.AssigneeId);
    }

    /// <summary>Integration: the shifts of a period and of a member are found by overlap, not only by start.</summary>
    [Fact]
    public async Task ShiftLists_ByPeriodAndMember_FindOverlappingShifts()
    {
        // Arrange
        var (hilda, _, _) = await SeedAsync();
        await using (var context = _fixture.CreateContext())
        {
            var shifts = new ShiftRepository(context);
            shifts.Add(Shift.Plan(Start.AddHours(-4), Start.AddHours(4), StaffRole.Housekeeping, hilda));
            shifts.Add(Shift.Plan(Start.AddDays(2), Start.AddDays(2).AddHours(8), StaffRole.Receptionist, null));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();
        var repository = new ShiftRepository(reader);

        // Act
        var housekeeping = await repository.ListAsync(Start, Start.AddDays(3), StaffRole.Housekeeping, TestContext.Current.CancellationToken);
        var everyRole = await repository.ListAsync(Start, Start.AddDays(3), null, TestContext.Current.CancellationToken);
        var hildas = await repository.ListForStaffAsync(hilda, Start, Start.AddHours(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(housekeeping);
        Assert.Equal(2, everyRole.Count);
        Assert.Single(hildas);
    }

    #endregion

    #region Helpers

    private async Task<(int Hilda, int Otto, int[] Rooms)> SeedAsync()
    {
        await using var context = _fixture.CreateContext();
        var hilda = StaffMember.Create("hilda", "hash-1", StaffRole.Housekeeping);
        var otto = StaffMember.Create("otto", "hash-1", StaffRole.Housekeeping);
        var staff = new StaffRepository(context);
        staff.Add(hilda);
        staff.Add(otto);
        var rooms = new[] { Room.Create("501", 5, RoomSize.Single, 1), Room.Create("502", 5, RoomSize.Single, 1), Room.Create("503", 5, RoomSize.Single, 1) };
        foreach (var room in rooms)
        {
            context.Rooms.Add(room);
        }

        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return (hilda.Id, otto.Id, [.. rooms.Select(room => room.Id)]);
    }

    #endregion
}
