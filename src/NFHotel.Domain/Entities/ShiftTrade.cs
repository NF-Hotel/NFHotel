using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A member of staff gives a shift away, or swaps it for a shift of a colleague (MIL-060).
/// </summary>
/// <remarks>
/// A shift given away is open to every colleague with the role of the shift; the first to take it moves the trade to a
/// Manager. A swap is offered to the colleague who works the other shift, who accepts or declines. A Manager approves
/// or rejects every trade that a colleague agreed to; only then do the shifts change hands. Nothing can be offered,
/// taken or accepted within <see cref="Cutoff"/> of the start of a shift of the trade.
/// </remarks>
public sealed class ShiftTrade : Entity<int>
{
    /// <summary>How long before the start of a shift its trades close.</summary>
    public static readonly TimeSpan Cutoff = TimeSpan.FromHours(24);

    private ShiftTrade()
    {
    }

    /// <summary>The shift offered.</summary>
    public int ShiftId { get; private set; }

    /// <summary>The member of staff who offers it, who works it at the time of the offer.</summary>
    public int OfferedById { get; private set; }

    /// <summary>The shift asked for in a swap, or <c>null</c> when the shift is given away.</summary>
    public int? SwapShiftId { get; private set; }

    /// <summary>
    /// The colleague: for a swap the one who works <see cref="SwapShiftId"/>, set at the offer; for a shift given away the
    /// one who took it, <c>null</c> until then.
    /// </summary>
    public int? TakenById { get; private set; }

    /// <summary>Where the trade is.</summary>
    public TradeState State { get; private set; }

    /// <summary>When it was offered.</summary>
    public DateTimeOffset OfferedAt { get; private set; }

    /// <summary>When it was taken, accepted, declined, withdrawn or called off; <c>null</c> before that.</summary>
    public DateTimeOffset? AnsweredAt { get; private set; }

    /// <summary>The Manager who approved or rejected it, or <c>null</c>.</summary>
    public int? DecidedById { get; private set; }

    /// <summary>When the Manager decided, or <c>null</c>.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>True for a swap, false for a shift given away.</summary>
    public bool IsSwap => SwapShiftId is not null;

    /// <summary>True while the trade waits for a colleague or a Manager.</summary>
    public bool IsPending => State is TradeState.Open or TradeState.AwaitingApproval;

    /// <summary>Tells whether a shift that starts at <paramref name="startsAt"/> can no longer be traded at <paramref name="now"/>.</summary>
    /// <param name="startsAt">The start of the shift.</param>
    /// <param name="now">The time.</param>
    /// <returns>True within <see cref="Cutoff"/> of the start, and after it.</returns>
    public static bool IsClosed(DateTimeOffset startsAt, DateTimeOffset now) => now >= startsAt - Cutoff;

    /// <summary>Gives a shift away to any colleague with its role.</summary>
    /// <param name="shiftId">The shift.</param>
    /// <param name="offeredById">The member of staff who works it.</param>
    /// <param name="now">The time.</param>
    /// <returns>The open trade.</returns>
    public static ShiftTrade GiveAway(int shiftId, int offeredById, DateTimeOffset now) =>
        new() { ShiftId = shiftId, OfferedById = offeredById, State = TradeState.Open, OfferedAt = now };

    /// <summary>Offers a colleague to swap shifts.</summary>
    /// <param name="shiftId">The own shift.</param>
    /// <param name="offeredById">The member of staff who works it.</param>
    /// <param name="swapShiftId">The colleague's shift.</param>
    /// <param name="colleagueId">The colleague who works it.</param>
    /// <param name="now">The time.</param>
    /// <returns>The open trade.</returns>
    /// <exception cref="DomainException">The two shifts or the two members are the same.</exception>
    public static ShiftTrade Swap(int shiftId, int offeredById, int swapShiftId, int colleagueId, DateTimeOffset now)
    {
        if (swapShiftId == shiftId || colleagueId == offeredById)
        {
            throw new DomainException(ShiftErrors.TradeOwnShift);
        }

        return new ShiftTrade
        {
            ShiftId = shiftId,
            OfferedById = offeredById,
            SwapShiftId = swapShiftId,
            TakenById = colleagueId,
            State = TradeState.Open,
            OfferedAt = now,
        };
    }

    /// <summary>A colleague takes a shift that is given away; it then waits for a Manager.</summary>
    /// <param name="staffId">The colleague.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">It is a swap, the own offer, or no longer open.</exception>
    public void Take(int staffId, DateTimeOffset now)
    {
        if (IsSwap)
        {
            throw new DomainException(ShiftErrors.TradeNotYours);
        }

        if (staffId == OfferedById)
        {
            throw new DomainException(ShiftErrors.TradeOwnShift);
        }

        ThrowUnlessOpen();
        TakenById = staffId;
        State = TradeState.AwaitingApproval;
        AnsweredAt = now;
    }

    /// <summary>The colleague accepts a swap; it then waits for a Manager.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller is not the colleague of the swap, or it is no longer open.</exception>
    public void Accept(int staffId, DateTimeOffset now)
    {
        ThrowUnlessSwapFor(staffId);
        State = TradeState.AwaitingApproval;
        AnsweredAt = now;
    }

    /// <summary>The colleague declines a swap.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller is not the colleague of the swap, or it is no longer open.</exception>
    public void Decline(int staffId, DateTimeOffset now)
    {
        ThrowUnlessSwapFor(staffId);
        State = TradeState.Declined;
        AnsweredAt = now;
    }

    /// <summary>The member of staff who offered it takes it back, while it waits for a colleague or a Manager.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller did not offer it, or it is no longer pending.</exception>
    public void Withdraw(int staffId, DateTimeOffset now)
    {
        if (staffId != OfferedById)
        {
            throw new DomainException(ShiftErrors.TradeNotYours);
        }

        if (!IsPending)
        {
            throw new DomainException(ShiftErrors.TradeNotOpen);
        }

        State = TradeState.Withdrawn;
        AnsweredAt = now;
    }

    /// <summary>A Manager approves a trade that a colleague agreed to; the service then hands the shifts over.</summary>
    /// <param name="managerId">The Manager.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The trade does not wait for approval.</exception>
    public void Approve(int managerId, DateTimeOffset now) => Decide(managerId, TradeState.Done, now);

    /// <summary>A Manager rejects a trade that a colleague agreed to; the shifts stay.</summary>
    /// <param name="managerId">The Manager.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The trade does not wait for approval.</exception>
    public void Reject(int managerId, DateTimeOffset now) => Decide(managerId, TradeState.Rejected, now);

    /// <summary>Calls the trade off, because a shift of it changed hands in another way meanwhile.</summary>
    /// <param name="now">The time.</param>
    public void Cancel(DateTimeOffset now)
    {
        State = TradeState.Cancelled;
        AnsweredAt ??= now;
    }

    private void ThrowUnlessOpen()
    {
        if (State != TradeState.Open)
        {
            throw new DomainException(ShiftErrors.TradeNotOpen);
        }
    }

    private void ThrowUnlessSwapFor(int staffId)
    {
        if (!IsSwap || staffId != TakenById)
        {
            throw new DomainException(ShiftErrors.TradeNotYours);
        }

        ThrowUnlessOpen();
    }

    private void Decide(int managerId, TradeState outcome, DateTimeOffset now)
    {
        if (State != TradeState.AwaitingApproval)
        {
            throw new DomainException(ShiftErrors.TradeNotOpen);
        }

        State = outcome;
        DecidedById = managerId;
        DecidedAt = now;
    }
}
