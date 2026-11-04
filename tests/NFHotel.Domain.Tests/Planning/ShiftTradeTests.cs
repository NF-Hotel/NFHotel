using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Planning;

/// <summary>The shifts, and the trades of shifts with their cut-off (MIL-060).</summary>
public sealed class ShiftTradeTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 10, 7, 0, 0, TimeSpan.FromHours(1));

    #region Shift

    /// <summary>Negative: a shift must end after it starts, last at most 16 hours, and need a known role.</summary>
    [Theory]
    [InlineData(0, ShiftErrors.EndBeforeStart)]
    [InlineData(-1, ShiftErrors.EndBeforeStart)]
    [InlineData(17, ShiftErrors.TooLong)]
    public void Plan_InvalidLength_ThrowsItsCode(int hours, string code)
    {
        // Act
        var error = Assert.Throws<DomainException>(() => Shift.Plan(Start, Start.AddHours(hours), StaffRole.Housekeeping, null));

        // Assert
        Assert.Equal([code], error.Codes);
    }

    /// <summary>Boundary: a shift that ends when the other starts does not overlap it; one minute more does.</summary>
    [Fact]
    public void Overlaps_TouchingAndOneMinuteMore_OnlyTheSecond()
    {
        // Arrange
        var shift = Shift.Plan(Start, Start.AddHours(8), StaffRole.Housekeeping, 3);

        // Act and assert
        Assert.False(shift.Overlaps(Start.AddHours(8), Start.AddHours(16)));
        Assert.True(shift.Overlaps(Start.AddHours(8).AddMinutes(-1), Start.AddHours(16)));
        Assert.False(shift.Overlaps(Start.AddHours(-8), Start));
    }

    #endregion

    #region Cut-off

    /// <summary>Boundary: a shift can be traded until 24 hours before its start, not from then on.</summary>
    [Fact]
    public void IsClosed_AroundTheCutoff_ClosesAtTwentyFourHours()
    {
        // Act and assert
        Assert.False(ShiftTrade.IsClosed(Start, Start.AddHours(-24).AddMinutes(-1)));
        Assert.True(ShiftTrade.IsClosed(Start, Start.AddHours(-24)));
        Assert.True(ShiftTrade.IsClosed(Start, Start.AddHours(1)));
    }

    #endregion

    #region Steps

    /// <summary>State transition: a shift given away is taken by a colleague, then approved.</summary>
    [Fact]
    public void GiveAway_TakeThenApprove_IsDoneWithTheTaker()
    {
        // Arrange
        var trade = ShiftTrade.GiveAway(5, 1, Start.AddDays(-3));

        // Act
        trade.Take(2, Start.AddDays(-2));
        var waiting = trade.State;
        trade.Approve(9, Start.AddDays(-2));

        // Assert
        Assert.Equal(TradeState.AwaitingApproval, waiting);
        Assert.Equal((TradeState.Done, 2, 9), (trade.State, trade.TakenById, trade.DecidedById));
    }

    /// <summary>Negative: the giver cannot take the own shift, a swap cannot be taken, and only the colleague of a swap answers.</summary>
    [Fact]
    public void Steps_ByTheWrongMember_AreRefused()
    {
        // Arrange
        var gift = ShiftTrade.GiveAway(5, 1, Start);
        var swap = ShiftTrade.Swap(5, 1, 6, 2, Start);

        // Act
        var own = Assert.Throws<DomainException>(() => gift.Take(1, Start));
        var takeSwap = Assert.Throws<DomainException>(() => swap.Take(3, Start));
        var acceptByOther = Assert.Throws<DomainException>(() => swap.Accept(3, Start));
        var acceptGift = Assert.Throws<DomainException>(() => gift.Accept(2, Start));

        // Assert
        Assert.Equal([ShiftErrors.TradeOwnShift], own.Codes);
        Assert.Equal([ShiftErrors.TradeNotYours], takeSwap.Codes);
        Assert.Equal([ShiftErrors.TradeNotYours], acceptByOther.Codes);
        Assert.Equal([ShiftErrors.TradeNotYours], acceptGift.Codes);
    }

    /// <summary>State transition: a swap is declined by the colleague, or accepted and rejected by a Manager.</summary>
    [Fact]
    public void Swap_DeclineOrAcceptThenReject_EndsWithoutAChange()
    {
        // Arrange
        var declined = ShiftTrade.Swap(5, 1, 6, 2, Start);
        var rejected = ShiftTrade.Swap(7, 1, 8, 2, Start);

        // Act
        declined.Decline(2, Start);
        rejected.Accept(2, Start);
        rejected.Reject(9, Start);

        // Assert
        Assert.Equal(TradeState.Declined, declined.State);
        Assert.Equal(TradeState.Rejected, rejected.State);
        Assert.True(declined.IsSwap);
    }

    /// <summary>Negative: a swap of a shift with itself, or with oneself, is refused.</summary>
    [Fact]
    public void Swap_SameShiftOrMember_IsRefused()
    {
        // Act and assert
        Assert.Throws<DomainException>(() => ShiftTrade.Swap(5, 1, 5, 2, Start));
        Assert.Throws<DomainException>(() => ShiftTrade.Swap(5, 1, 6, 1, Start));
    }

    #endregion
}
