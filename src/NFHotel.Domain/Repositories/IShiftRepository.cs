using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the shifts and of the trades of shifts (MIL-060).</summary>
public interface IShiftRepository : IRepository<Shift, int>
{
    /// <summary>Lists the shifts that overlap a period, of one role when asked, by start.</summary>
    /// <param name="from">The start of the period.</param>
    /// <param name="to">The end of the period.</param>
    /// <param name="role">The role, or <c>null</c> for every role.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shifts.</returns>
    Task<IReadOnlyList<Shift>> ListAsync(DateTimeOffset from, DateTimeOffset to, StaffRole? role, CancellationToken cancellationToken = default);

    /// <summary>Lists the shifts of a member of staff that overlap a period.</summary>
    /// <param name="staffId">The member of staff.</param>
    /// <param name="from">The start of the period.</param>
    /// <param name="to">The end of the period.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shifts.</returns>
    Task<IReadOnlyList<Shift>> ListForStaffAsync(int staffId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>Finds the shifts with the keys, tracked so that a trade can hand them over.</summary>
    /// <param name="ids">The keys.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shifts that exist.</returns>
    Task<IReadOnlyList<Shift>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Marks a shift for removal; its trades go with it.</summary>
    /// <param name="shift">The shift.</param>
    void Remove(Shift shift);

    /// <summary>Finds a trade.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, or <c>null</c>.</returns>
    Task<ShiftTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists the trades that are open or wait for a Manager, and the closed trades since a moment.</summary>
    /// <param name="closedSince">The oldest closed trade to list.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    Task<IReadOnlyList<ShiftTrade>> ListTradesAsync(DateTimeOffset closedSince, CancellationToken cancellationToken = default);

    /// <summary>Tells whether a shift has a trade that is open or waits for a Manager.</summary>
    /// <param name="shiftId">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>True when it has one.</returns>
    Task<bool> HasPendingTradeAsync(int shiftId, CancellationToken cancellationToken = default);

    /// <summary>Marks a new trade for insertion.</summary>
    /// <param name="trade">The trade.</param>
    void AddTrade(ShiftTrade trade);

    /// <summary>Marks a changed trade for saving.</summary>
    /// <param name="trade">The trade.</param>
    void UpdateTrade(ShiftTrade trade);
}
