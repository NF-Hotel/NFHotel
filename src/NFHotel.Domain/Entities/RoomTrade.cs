using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A housekeeper gives rooms to a colleague, or trades them for rooms of the colleague (MIL-060).
/// </summary>
/// <remarks>
/// The colleague accepts or declines. An accepted trade is carried out at once, unless it leaves one of the two with
/// fewer rooms than before and fewer than <see cref="RoomAssignment.MinimumRooms"/>: then a Manager approves or rejects
/// it first. Who holds which room is checked by the service when the trade is offered and again when it is carried out,
/// because the rooms can change hands in between.
/// </remarks>
public sealed class RoomTrade : Entity<int>
{
    private RoomTrade()
    {
    }

    /// <summary>The housekeeper who offers the trade.</summary>
    public int FromStaffId { get; private set; }

    /// <summary>The colleague it is offered to.</summary>
    public int ToStaffId { get; private set; }

    /// <summary>The rooms that go from <see cref="FromStaffId"/> to <see cref="ToStaffId"/>.</summary>
    public int[] GiveRoomIds { get; private set; } = [];

    /// <summary>The rooms that go the other way; empty when the rooms are only given.</summary>
    public int[] TakeRoomIds { get; private set; } = [];

    /// <summary>Where the trade is.</summary>
    public TradeState State { get; private set; }

    /// <summary>When it was offered.</summary>
    public DateTimeOffset OfferedAt { get; private set; }

    /// <summary>When the colleague answered, or the offer was withdrawn or called off; <c>null</c> before that.</summary>
    public DateTimeOffset? AnsweredAt { get; private set; }

    /// <summary>The Manager who approved or rejected it, or <c>null</c>.</summary>
    public int? DecidedById { get; private set; }

    /// <summary>When the Manager decided, or <c>null</c>.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>True while the trade waits for the colleague or a Manager.</summary>
    public bool IsPending => State is TradeState.Open or TradeState.AwaitingApproval;

    /// <summary>Offers a trade.</summary>
    /// <param name="fromStaffId">The housekeeper who offers it.</param>
    /// <param name="toStaffId">The colleague.</param>
    /// <param name="giveRoomIds">The rooms given; at least one.</param>
    /// <param name="takeRoomIds">The rooms asked for in return; none for a gift.</param>
    /// <param name="now">The time.</param>
    /// <returns>The open trade.</returns>
    /// <exception cref="DomainException">No room is given, a room is named twice, a key is not valid, or both are the same.</exception>
    public static RoomTrade Offer(
        int fromStaffId,
        int toStaffId,
        IReadOnlyCollection<int> giveRoomIds,
        IReadOnlyCollection<int> takeRoomIds,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(giveRoomIds);
        ArgumentNullException.ThrowIfNull(takeRoomIds);

        int[] all = [.. giveRoomIds, .. takeRoomIds];
        if (fromStaffId <= 0 || toStaffId <= 0 || Array.Exists(all, IsNotAKey))
        {
            throw new DomainException(RoomTradeErrors.KeyInvalid);
        }

        if (fromStaffId == toStaffId)
        {
            throw new DomainException(RoomTradeErrors.SameStaff);
        }

        if (giveRoomIds.Count == 0)
        {
            throw new DomainException(RoomTradeErrors.NoRooms);
        }

        if (all.Distinct().Count() != all.Length)
        {
            throw new DomainException(RoomTradeErrors.DuplicateRoom);
        }

        return new RoomTrade
        {
            FromStaffId = fromStaffId,
            ToStaffId = toStaffId,
            GiveRoomIds = [.. giveRoomIds.Order()],
            TakeRoomIds = [.. takeRoomIds.Order()],
            State = TradeState.Open,
            OfferedAt = now,
        };
    }

    /// <summary>
    /// Tells whether the trade needs a Manager: it leaves one of the two with fewer rooms than before and fewer than
    /// <see cref="RoomAssignment.MinimumRooms"/>.
    /// </summary>
    /// <param name="fromRoomCount">How many rooms the housekeeper who offers has now.</param>
    /// <param name="toRoomCount">How many rooms the colleague has now.</param>
    /// <returns>True when a Manager must approve it.</returns>
    public bool NeedsApproval(int fromRoomCount, int toRoomCount)
    {
        var fromAfter = fromRoomCount - GiveRoomIds.Length + TakeRoomIds.Length;
        var toAfter = toRoomCount + GiveRoomIds.Length - TakeRoomIds.Length;
        return TooFew(fromRoomCount, fromAfter) || TooFew(toRoomCount, toAfter);
    }

    /// <summary>The colleague accepts. The trade is then carried out, or waits for a Manager.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="needsApproval">The result of <see cref="NeedsApproval"/> with the counts of now.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller is not the colleague, or the trade is not open.</exception>
    public void Accept(int staffId, bool needsApproval, DateTimeOffset now)
    {
        ThrowUnlessOpenFor(staffId, ToStaffId);
        State = needsApproval ? TradeState.AwaitingApproval : TradeState.Done;
        AnsweredAt = now;
    }

    /// <summary>The colleague declines.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller is not the colleague, or the trade is not open.</exception>
    public void Decline(int staffId, DateTimeOffset now)
    {
        ThrowUnlessOpenFor(staffId, ToStaffId);
        State = TradeState.Declined;
        AnsweredAt = now;
    }

    /// <summary>The housekeeper who offered it takes it back, while it waits for the colleague or a Manager.</summary>
    /// <param name="staffId">The caller.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The caller did not offer it, or it is no longer pending.</exception>
    public void Withdraw(int staffId, DateTimeOffset now)
    {
        if (staffId != FromStaffId)
        {
            throw new DomainException(RoomTradeErrors.NotYours);
        }

        if (!IsPending)
        {
            throw new DomainException(RoomTradeErrors.NotOpen);
        }

        State = TradeState.Withdrawn;
        AnsweredAt = now;
    }

    /// <summary>A Manager approves a trade that waits for approval; it is then carried out.</summary>
    /// <param name="managerId">The Manager.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The trade does not wait for approval.</exception>
    public void Approve(int managerId, DateTimeOffset now) => Decide(managerId, TradeState.Done, now);

    /// <summary>A Manager rejects a trade that waits for approval; nothing changes hands.</summary>
    /// <param name="managerId">The Manager.</param>
    /// <param name="now">The time.</param>
    /// <exception cref="DomainException">The trade does not wait for approval.</exception>
    public void Reject(int managerId, DateTimeOffset now) => Decide(managerId, TradeState.Rejected, now);

    /// <summary>Calls the trade off, because a room of it changed hands in another way meanwhile.</summary>
    /// <param name="now">The time.</param>
    public void Cancel(DateTimeOffset now)
    {
        State = TradeState.Cancelled;
        AnsweredAt ??= now;
    }

    private static bool IsNotAKey(int id) => id <= 0;

    private static bool TooFew(int before, int after) => after < before && after < RoomAssignment.MinimumRooms;

    private void ThrowUnlessOpenFor(int staffId, int allowed)
    {
        if (staffId != allowed)
        {
            throw new DomainException(RoomTradeErrors.NotYours);
        }

        if (State != TradeState.Open)
        {
            throw new DomainException(RoomTradeErrors.NotOpen);
        }
    }

    private void Decide(int managerId, TradeState outcome, DateTimeOffset now)
    {
        if (State != TradeState.AwaitingApproval)
        {
            throw new DomainException(RoomTradeErrors.NotOpen);
        }

        State = outcome;
        DecidedById = managerId;
        DecidedAt = now;
    }
}
