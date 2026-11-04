using NFHotel.Application.Dto.Rooms;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Which housekeeper looks after which room, and the trades of rooms between housekeepers (MIL-060). The permission of
/// each operation is checked where the call arrives; the rules of a trade are on <see cref="Domain.Entities.RoomTrade"/>.
/// </summary>
public interface IRoomAssignmentService
{
    /// <summary>Reads every room with its housekeeper, and every active housekeeper with the number of rooms.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The overview.</returns>
    Task<Result<RoomAssignmentOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>A Manager gives a room to a housekeeper, or leaves it without one.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="request">The housekeeper, or none.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, <c>room.not_found</c> or <c>room_assignment.assignee_invalid</c>.</returns>
    Task<Result> AssignAsync(int roomId, AssignRoomRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists the trades of the caller, and for a Manager every trade that waits for approval.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    Task<Result<IReadOnlyList<RoomTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default);

    /// <summary>The caller gives rooms to a colleague, or trades them for rooms of the colleague.</summary>
    /// <param name="request">The colleague and the rooms.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open trade, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> OfferTradeAsync(OfferRoomTradeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// The colleague accepts. The rooms change hands at once, unless the trade leaves one of the two below the minimum:
    /// then it waits for a Manager.
    /// </summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The colleague declines.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The housekeeper who offered it takes it back.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>A Manager approves a trade that waits for approval; the rooms change hands.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>A Manager rejects a trade that waits for approval.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<RoomTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default);
}
