using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the shifts and their trades (MIL-060).</summary>
public sealed class ShiftRepository : Repository<Shift, int>, IShiftRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public ShiftRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shift>> ListAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        StaffRole? role,
        CancellationToken cancellationToken = default) =>
        await Context.Shifts
            .AsNoTracking()
            .Where(shift => shift.StartsAt < to && from < shift.EndsAt && (role == null || shift.Role == role))
            .OrderBy(shift => shift.StartsAt)
            .ThenBy(shift => shift.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shift>> ListForStaffAsync(
        int staffId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await Context.Shifts
            .AsNoTracking()
            .Where(shift => shift.AssigneeId == staffId && shift.StartsAt < to && from < shift.EndsAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shift>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        await Context.Shifts.Where(shift => ids.Contains(shift.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Remove(Shift shift) => MarkForRemoval(shift);

    /// <inheritdoc />
    public async Task<ShiftTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default) =>
        await Context.ShiftTrades.FirstOrDefaultAsync(trade => trade.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShiftTrade>> ListTradesAsync(DateTimeOffset closedSince, CancellationToken cancellationToken = default) =>
        await Context.ShiftTrades
            .AsNoTracking()
            .Where(trade => trade.State == TradeState.Open
                || trade.State == TradeState.AwaitingApproval
                || trade.OfferedAt >= closedSince)
            .OrderByDescending(trade => trade.OfferedAt)
            .ThenByDescending(trade => trade.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> HasPendingTradeAsync(int shiftId, CancellationToken cancellationToken = default) =>
        await Context.ShiftTrades
            .AnyAsync(
                trade => (trade.ShiftId == shiftId || trade.SwapShiftId == shiftId)
                    && (trade.State == TradeState.Open || trade.State == TradeState.AwaitingApproval),
                cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddTrade(ShiftTrade trade) => Context.ShiftTrades.Add(trade);

    /// <inheritdoc />
    public void UpdateTrade(ShiftTrade trade) => Context.ShiftTrades.Update(trade);
}
