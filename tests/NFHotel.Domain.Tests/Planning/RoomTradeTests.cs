using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Planning;

/// <summary>The trades of rooms between housekeepers and the minimum of rooms (MIL-060).</summary>
public sealed class RoomTradeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);

    #region Offer

    /// <summary>Positive: an offer is open and keeps the rooms in order.</summary>
    [Fact]
    public void Offer_GiveAndTake_IsOpenWithSortedRooms()
    {
        // Act
        var trade = RoomTrade.Offer(1, 2, [103, 101], [201], Now);

        // Assert
        Assert.Equal(TradeState.Open, trade.State);
        Assert.Equal([101, 103], trade.GiveRoomIds);
        Assert.Equal([201], trade.TakeRoomIds);
        Assert.True(trade.IsPending);
    }

    /// <summary>Negative: an offer without rooms, with a room twice, or to oneself is refused with its code.</summary>
    [Theory]
    [InlineData(new int[0], new int[0], 2, RoomTradeErrors.NoRooms)]
    [InlineData(new[] { 101, 101 }, new int[0], 2, RoomTradeErrors.DuplicateRoom)]
    [InlineData(new[] { 101 }, new[] { 101 }, 2, RoomTradeErrors.DuplicateRoom)]
    [InlineData(new[] { 101 }, new int[0], 1, RoomTradeErrors.SameStaff)]
    [InlineData(new[] { 0 }, new int[0], 2, RoomTradeErrors.KeyInvalid)]
    public void Offer_Invalid_ThrowsItsCode(int[] give, int[] take, int to, string code)
    {
        // Act
        var error = Assert.Throws<DomainException>(() => RoomTrade.Offer(1, to, give, take, Now));

        // Assert
        Assert.Equal([code], error.Codes);
    }

    #endregion

    #region The minimum

    /// <summary>
    /// Boundary: a trade needs a Manager when it leaves one of the two with fewer rooms than before and fewer than three;
    /// keeping three, gaining rooms, or a swap of equal size does not.
    /// </summary>
    [Theory]
    [InlineData(4, 1, 0, 3, false)] // the giver keeps 3
    [InlineData(3, 1, 0, 3, true)]  // the giver drops to 2
    [InlineData(2, 1, 1, 5, false)] // an even swap below the minimum loses nothing
    [InlineData(5, 1, 3, 4, true)]  // the colleague drops from 4 to 2
    [InlineData(1, 1, 0, 0, true)]  // the giver drops to 0
    [InlineData(6, 3, 0, 0, false)] // the giver keeps 3 and the colleague gains
    public void NeedsApproval_Counts_FollowTheMinimum(int fromRooms, int give, int take, int toRooms, bool expected)
    {
        // Arrange
        var trade = RoomTrade.Offer(1, 2, [.. Enumerable.Range(100, give)], [.. Enumerable.Range(200, take)], Now);

        // Act and assert
        Assert.Equal(expected, trade.NeedsApproval(fromRooms, toRooms));
    }

    #endregion

    #region Steps

    /// <summary>State transition: the colleague accepts; the trade is done, or waits for a Manager who approves it.</summary>
    [Fact]
    public void Accept_ThenApprove_WaitsThenIsDone()
    {
        // Arrange
        var direct = RoomTrade.Offer(1, 2, [101], [], Now);
        var approved = RoomTrade.Offer(1, 2, [101], [], Now);

        // Act
        direct.Accept(2, needsApproval: false, Now);
        approved.Accept(2, needsApproval: true, Now);
        var waiting = approved.State;
        approved.Approve(9, Now);

        // Assert
        Assert.Equal(TradeState.Done, direct.State);
        Assert.Equal(TradeState.AwaitingApproval, waiting);
        Assert.Equal((TradeState.Done, 9), (approved.State, approved.DecidedById));
    }

    /// <summary>Negative: only the colleague answers, and only an open trade.</summary>
    [Fact]
    public void Accept_ByAnotherOrTwice_IsRefused()
    {
        // Arrange
        var trade = RoomTrade.Offer(1, 2, [101], [], Now);

        // Act
        var notYours = Assert.Throws<DomainException>(() => trade.Accept(1, false, Now));
        trade.Decline(2, Now);
        var notOpen = Assert.Throws<DomainException>(() => trade.Accept(2, false, Now));

        // Assert
        Assert.Equal([RoomTradeErrors.NotYours], notYours.Codes);
        Assert.Equal([RoomTradeErrors.NotOpen], notOpen.Codes);
        Assert.Equal(TradeState.Declined, trade.State);
    }

    /// <summary>State transition: the giver withdraws while it waits, but not after it is done; a Manager only decides a waiting one.</summary>
    [Fact]
    public void Withdraw_AndDecide_FollowTheState()
    {
        // Arrange
        var waiting = RoomTrade.Offer(1, 2, [101], [], Now);
        waiting.Accept(2, needsApproval: true, Now);
        var open = RoomTrade.Offer(1, 2, [102], [], Now);

        // Act
        waiting.Withdraw(1, Now);
        var notWaiting = Assert.Throws<DomainException>(() => open.Reject(9, Now));
        var notTheGiver = Assert.Throws<DomainException>(() => open.Withdraw(2, Now));

        // Assert
        Assert.Equal(TradeState.Withdrawn, waiting.State);
        Assert.Equal([RoomTradeErrors.NotOpen], notWaiting.Codes);
        Assert.Equal([RoomTradeErrors.NotYours], notTheGiver.Codes);
    }

    #endregion
}
