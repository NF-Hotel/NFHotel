using NFHotel.Application.Dto.Shifts;

namespace NFHotel.Application.UseCases.Shifts;

/// <summary>
/// The shifts and their trades (MIL-060). A Manager plans the shifts and approves the trades; every member of staff sees
/// the shifts of the own role, gives the own shifts away or swaps them, and takes or accepts those of colleagues. The
/// permission of each operation is checked where the call arrives; the rules of a trade are on
/// <see cref="Domain.Entities.ShiftTrade"/>.
/// </summary>
public interface IShiftService
{
    /// <summary>Lists the shifts that overlap a period: every shift for a Manager, the shifts of the own role for the others.</summary>
    /// <param name="from">The start of the period.</param>
    /// <param name="to">The end of the period; at most 62 days after the start.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shifts by start, or <c>shift.period_invalid</c>.</returns>
    Task<Result<IReadOnlyList<ShiftDto>>> ListAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>A Manager plans a shift.</summary>
    /// <param name="request">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shift, or the codes of what is wrong.</returns>
    Task<Result<ShiftDto>> CreateAsync(SaveShiftRequest request, CancellationToken cancellationToken = default);

    /// <summary>A Manager changes a shift.</summary>
    /// <param name="id">The shift.</param>
    /// <param name="request">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The shift, or the codes of what is wrong.</returns>
    Task<Result<ShiftDto>> UpdateAsync(int id, SaveShiftRequest request, CancellationToken cancellationToken = default);

    /// <summary>A Manager deletes a shift and its trades.</summary>
    /// <param name="id">The shift.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success or <c>shift.not_found</c>.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the trades the caller can act on or follow: the own offers, the swaps offered to the caller, the shifts of
    /// the own role given away, and for a Manager every trade. Closed trades of the last 14 days are included.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trades, newest first.</returns>
    Task<Result<IReadOnlyList<ShiftTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default);

    /// <summary>The caller gives an own shift away, or offers it in a swap.</summary>
    /// <param name="request">The shift and the colleague's shift, if any.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The open trade, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> OfferTradeAsync(OfferShiftTradeRequest request, CancellationToken cancellationToken = default);

    /// <summary>The caller takes a shift that is given away; it then waits for a Manager.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> TakeTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The colleague accepts a swap; it then waits for a Manager.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The colleague declines a swap.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The member of staff who offered it takes it back.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>A Manager approves a trade that a colleague agreed to; the shifts change hands.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>A Manager rejects a trade that a colleague agreed to.</summary>
    /// <param name="id">The trade.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The trade after the step, or the codes of what is wrong.</returns>
    Task<Result<ShiftTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default);
}
