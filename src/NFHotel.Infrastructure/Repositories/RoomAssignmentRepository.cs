using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the room assignments and the trades of rooms (MIL-060).</summary>
public sealed class RoomAssignmentRepository : Repository<RoomAssignment, int>, IRoomAssignmentRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public RoomAssignmentRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomAssignment>> ListAsync(CancellationToken cancellationToken = default) =>
        await Context.RoomAssignments.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RoomAssignment?> FindByRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await Context.RoomAssignments
            .FirstOrDefaultAsync(assignment => assignment.RoomId == roomId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Remove(RoomAssignment assignment) => MarkForRemoval(assignment);

    /// <inheritdoc />
    public async Task<RoomTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default) =>
        await Context.RoomTrades.FirstOrDefaultAsync(trade => trade.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomTrade>> ListTradesAsync(
        int? staffId,
        bool includeAwaitingApproval,
        CancellationToken cancellationToken = default) =>
        await Context.RoomTrades
            .AsNoTracking()
            .Where(trade => (staffId != null && (trade.FromStaffId == staffId || trade.ToStaffId == staffId))
                || (includeAwaitingApproval && trade.State == TradeState.AwaitingApproval))
            .OrderByDescending(trade => trade.OfferedAt)
            .ThenByDescending(trade => trade.Id)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddTrade(RoomTrade trade) => Context.RoomTrades.Add(trade);

    /// <inheritdoc />
    public void UpdateTrade(RoomTrade trade) => Context.RoomTrades.Update(trade);
}
