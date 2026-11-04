using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the room assignments and of the trades of rooms (MIL-060).</summary>
public interface IRoomAssignmentRepository : IRepository<RoomAssignment, int>
{
    /// <summary>Lists every assignment, tracked so that a trade can move them.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The assignments.</returns>
    Task<IReadOnlyList<RoomAssignment>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds the assignment of a room.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The assignment, or <c>null</c> for a room without a housekeeper.</returns>
    Task<RoomAssignment?> FindByRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Marks an assignment for removal.</summary>
    /// <param name="assignment">The assignment.</param>
    void Remove(RoomAssignment assignment);

    /// <summary>Finds a trade.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade, or <c>null</c>.</returns>
    Task<RoomTrade?> GetTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists the trades of a housekeeper, offered or received, and every trade that waits for a Manager when asked.</summary>
    /// <param name="staffId">The housekeeper, or <c>null</c> for none.</param>
    /// <param name="includeAwaitingApproval">True to add every trade that waits for a Manager.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    Task<IReadOnlyList<RoomTrade>> ListTradesAsync(int? staffId, bool includeAwaitingApproval, CancellationToken cancellationToken = default);

    /// <summary>Marks a new trade for insertion.</summary>
    /// <param name="trade">The trade.</param>
    void AddTrade(RoomTrade trade);

    /// <summary>Marks a changed trade for saving.</summary>
    /// <param name="trade">The trade.</param>
    void UpdateTrade(RoomTrade trade);
}
