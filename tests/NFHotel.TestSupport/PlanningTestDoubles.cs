using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.TestSupport;

/// <summary>Keeps entities of a second kind in memory with keys, such as the trades next to their assignments (MIL-060).</summary>
/// <typeparam name="TEntity">The entity.</typeparam>
public sealed class InMemoryList<TEntity> : InMemoryRepository<TEntity>
    where TEntity : Entity<int>;

/// <summary>The room assignments and their trades in memory (MIL-060).</summary>
public sealed class InMemoryRoomAssignmentRepository : InMemoryRepository<RoomAssignment>, IRoomAssignmentRepository
{
    /// <summary>The trades.</summary>
    public InMemoryList<RoomTrade> Trades { get; } = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<RoomAssignment>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomAssignment>>([.. Items]);

    /// <inheritdoc />
    public Task<RoomAssignment?> FindByRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(assignment => assignment.RoomId == roomId));

    /// <inheritdoc />
    public Task<RoomTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default) => Trades.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RoomTrade>> ListTradesAsync(int? staffId, bool includeAwaitingApproval, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomTrade>>(
        [
            .. Trades.Items
                .Where(trade => (staffId is not null && (trade.FromStaffId == staffId || trade.ToStaffId == staffId))
                    || (includeAwaitingApproval && trade.State == TradeState.AwaitingApproval))
                .OrderByDescending(trade => trade.Id),
        ]);

    /// <inheritdoc />
    public void AddTrade(RoomTrade trade) => Trades.Add(trade);

    /// <inheritdoc />
    public void UpdateTrade(RoomTrade trade) => Trades.Update(trade);
}

/// <summary>The shifts and their trades in memory (MIL-060).</summary>
public sealed class InMemoryShiftRepository : InMemoryRepository<Shift>, IShiftRepository
{
    /// <summary>The trades.</summary>
    public InMemoryList<ShiftTrade> Trades { get; } = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<Shift>> ListAsync(DateTimeOffset from, DateTimeOffset to, StaffRole? role, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Shift>>(
            [.. Items.Where(shift => shift.Overlaps(from, to) && (role is null || shift.Role == role)).OrderBy(shift => shift.StartsAt)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Shift>> ListForStaffAsync(int staffId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Shift>>([.. Items.Where(shift => shift.AssigneeId == staffId && shift.Overlaps(from, to))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Shift>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Shift>>([.. Items.Where(shift => ids.Contains(shift.Id))]);

    /// <inheritdoc />
    public Task<ShiftTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default) => Trades.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ShiftTrade>> ListTradesAsync(DateTimeOffset closedSince, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShiftTrade>>(
            [.. Trades.Items.Where(trade => trade.IsPending || trade.OfferedAt >= closedSince).OrderByDescending(trade => trade.Id)]);

    /// <inheritdoc />
    public Task<bool> HasPendingTradeAsync(int shiftId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Trades.Items.Any(trade => (trade.ShiftId == shiftId || trade.SwapShiftId == shiftId) && trade.IsPending));

    /// <inheritdoc />
    public void AddTrade(ShiftTrade trade) => Trades.Add(trade);

    /// <inheritdoc />
    public void UpdateTrade(ShiftTrade trade) => Trades.Update(trade);
}
