using System.Globalization;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>The rules of a price change of a room size: its fields, the days it counts on, and the revert (MIL-055).</summary>
public sealed class PriceChangeTests
{
    private static readonly DateTimeOffset At = new(2027, 5, 1, 10, 0, 0, TimeSpan.FromHours(1));
    private static readonly DateOnly First = new(2027, 6, 1);
    private static readonly DateOnly Last = new(2027, 8, 31);

    private static PriceChange Summer() => PriceChange.Approve(RoomSize.Double, 20m, First, Last, "manager", At);

    #region Approving

    /// <summary>Positive: a valid change keeps its fields, trims the user name, stores the time in UTC and starts approved.</summary>
    [Fact]
    public void Approve_ValidFields_KeepsThemAndIsApproved()
    {
        // Arrange and act
        PriceChange change = PriceChange.Approve(RoomSize.Suite, 12.345m, First, Last, " manager ", At);

        // Assert
        Assert.Equal((RoomSize.Suite, 12.35m, First, Last), (change.Size, change.Percentage, change.FirstDay, change.LastDay));
        Assert.Equal((PriceChangeState.Approved, "manager"), (change.State, change.ApprovedBy));
        Assert.Equal(TimeSpan.Zero, change.ApprovedAt.Offset);
        Assert.Equal(At.UtcDateTime, change.ApprovedAt.UtcDateTime);
        Assert.Null(change.RevertedBy);
        Assert.Null(change.RevertedAt);
        Assert.Null(change.EndedOn);
    }

    /// <summary>Boundary: a discount of 99.99 and a surcharge of 1000 are accepted.</summary>
    [Theory]
    [InlineData("-99.99")]
    [InlineData("-15")]
    [InlineData("0.01")]
    [InlineData("1000")]
    public void Approve_PercentageAtTheLimit_IsAccepted(string percentage)
    {
        // Arrange and act
        PriceChange change = PriceChange.Approve(RoomSize.Single, Parse(percentage), First, Last, "manager", At);

        // Assert
        Assert.Equal(Parse(percentage), change.Percentage);
    }

    /// <summary>Negative: 0, a percentage that rounds to 0, -100 and below, and above 1000 are refused with the percentage code.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("0.004")]
    [InlineData("-100")]
    [InlineData("-99.996")]
    [InlineData("-250")]
    [InlineData("1000.01")]
    public void Approve_PercentageOutsideTheLimits_ThrowsThePercentageCode(string percentage)
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => PriceChange.Approve(RoomSize.Single, Parse(percentage), First, Last, "manager", At));

        // Assert
        Assert.Equal([PriceChangeErrors.PercentageInvalid], exception.Codes);
    }

    /// <summary>Negative: a room size that is not defined is refused with the size code.</summary>
    [Fact]
    public void Approve_UnknownSize_ThrowsTheSizeCode()
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => PriceChange.Approve((RoomSize)7, 10m, First, Last, "manager", At));

        // Assert
        Assert.Equal([PriceChangeErrors.SizeInvalid], exception.Codes);
    }

    /// <summary>Boundary: a period of one day and one of 366 days are accepted.</summary>
    [Fact]
    public void Approve_PeriodAtTheLimits_IsAccepted()
    {
        // Arrange and act
        PriceChange oneDay = PriceChange.Approve(RoomSize.Single, 10m, First, First, "manager", At);
        PriceChange longest = PriceChange.Approve(RoomSize.Single, 10m, First, First.AddDays(365), "manager", At);

        // Assert
        Assert.Equal(First, oneDay.LastDay);
        Assert.Equal(First.AddDays(365), longest.LastDay);
    }

    /// <summary>Negative: a last day before the first day, and a period of 367 days, are refused with the period code.</summary>
    [Fact]
    public void Approve_PeriodOutsideTheLimits_ThrowsThePeriodCode()
    {
        // Arrange and act
        DomainException backwards = Assert.Throws<DomainException>(() => PriceChange.Approve(RoomSize.Single, 10m, First, First.AddDays(-1), "manager", At));
        DomainException tooLong = Assert.Throws<DomainException>(() => PriceChange.Approve(RoomSize.Single, 10m, First, First.AddDays(366), "manager", At));

        // Assert
        Assert.Equal([PriceChangeErrors.PeriodInvalid], backwards.Codes);
        Assert.Equal([PriceChangeErrors.PeriodInvalid], tooLong.Codes);
    }

    /// <summary>Positive: a first day in the past is allowed; the domain only needs the last day on or after the first.</summary>
    [Fact]
    public void Approve_PastPeriod_IsAccepted()
    {
        // Arrange and act
        PriceChange change = PriceChange.Approve(RoomSize.Single, 10m, new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), "manager", At);

        // Assert
        Assert.Equal(new DateOnly(2020, 1, 1), change.FirstDay);
    }

    /// <summary>Negative: a blank, missing or 101 character user name is refused with the approved-by code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Approve_BlankApprovedBy_ThrowsTheApprovedByCode(string? approvedBy)
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => PriceChange.Approve(RoomSize.Single, 10m, First, Last, approvedBy!, At));

        // Assert
        Assert.Equal([PriceChangeErrors.ApprovedByInvalid], exception.Codes);
    }

    /// <summary>Boundary: a user name of 100 characters is kept, one of 101 is refused.</summary>
    [Fact]
    public void Approve_ApprovedByLength_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange and act
        PriceChange accepted = PriceChange.Approve(RoomSize.Single, 10m, First, Last, new string('m', PriceChange.MaxTextLength), At);
        DomainException exception = Assert.Throws<DomainException>(
            () => PriceChange.Approve(RoomSize.Single, 10m, First, Last, new string('m', PriceChange.MaxTextLength + 1), At));

        // Assert
        Assert.Equal(PriceChange.MaxTextLength, accepted.ApprovedBy.Length);
        Assert.Equal([PriceChangeErrors.ApprovedByInvalid], exception.Codes);
    }

    /// <summary>Negative: several faults are reported together, each code once.</summary>
    [Fact]
    public void Approve_SeveralFieldsWrong_ThrowsEveryCode()
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => PriceChange.Approve(RoomSize.Single, 0m, Last, First, string.Empty, At));

        // Assert
        Assert.Equal(3, exception.Codes.Count);
        Assert.Contains(PriceChangeErrors.PercentageInvalid, exception.Codes);
        Assert.Contains(PriceChangeErrors.PeriodInvalid, exception.Codes);
        Assert.Contains(PriceChangeErrors.ApprovedByInvalid, exception.Codes);
    }

    #endregion

    #region The days a change counts on

    /// <summary>Boundary: an approved change counts on its first day, its last day and every day between, and on no other day.</summary>
    [Theory]
    [InlineData("2027-05-31", false)]
    [InlineData("2027-06-01", true)]
    [InlineData("2027-07-15", true)]
    [InlineData("2027-08-31", true)]
    [InlineData("2027-09-01", false)]
    public void CountsOn_ApprovedChange_CountsOnlyInsideThePeriod(string day, bool expected)
    {
        // Arrange
        PriceChange change = Summer();

        // Act and assert
        Assert.Equal(expected, change.CountsOn(DateOnly.Parse(day, CultureInfo.InvariantCulture)));
    }

    /// <summary>State: a withdrawn change counts on no day, not even inside its period.</summary>
    [Fact]
    public void CountsOn_WithdrawnChange_CountsOnNoDay()
    {
        // Arrange
        PriceChange change = Summer();
        change.Revert("manager", At, new DateOnly(2027, 5, 20));

        // Act and assert
        Assert.False(change.CountsOn(First));
        Assert.False(change.CountsOn(new DateOnly(2027, 7, 15)));
        Assert.False(change.CountsOn(Last));
        Assert.Null(change.CountedLastDay);
    }

    /// <summary>State: a change ended on 2027-07-09 counts up to and including that day and not after.</summary>
    [Theory]
    [InlineData("2027-06-01", true)]
    [InlineData("2027-07-09", true)]
    [InlineData("2027-07-10", false)]
    [InlineData("2027-08-31", false)]
    public void CountsOn_EndedChange_CountsUpToTheEndDay(string day, bool expected)
    {
        // Arrange
        PriceChange change = Summer();
        change.Revert("manager", At, new DateOnly(2027, 7, 10));

        // Act and assert
        Assert.Equal(expected, change.CountsOn(DateOnly.Parse(day, CultureInfo.InvariantCulture)));
    }

    /// <summary>Positive: periods that share a day count within each other; adjacent ones and a withdrawn change do not.</summary>
    [Fact]
    public void CountsWithin_Periods_SharesADayOrNot()
    {
        // Arrange
        PriceChange change = Summer();
        PriceChange withdrawn = Summer();
        withdrawn.Revert("manager", At, new DateOnly(2027, 5, 1));

        // Act and assert
        Assert.True(change.CountsWithin(new DateOnly(2027, 8, 31), new DateOnly(2027, 9, 30)));
        Assert.True(change.CountsWithin(new DateOnly(2027, 1, 1), new DateOnly(2027, 6, 1)));
        Assert.False(change.CountsWithin(new DateOnly(2027, 9, 1), new DateOnly(2027, 9, 30)));
        Assert.False(change.CountsWithin(new DateOnly(2027, 5, 1), new DateOnly(2027, 5, 31)));
        Assert.False(withdrawn.CountsWithin(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)));
    }

    #endregion

    #region Reverting

    /// <summary>State: a change that has not begun is withdrawn; it records who and when and has no end day.</summary>
    [Fact]
    public void Revert_BeforeTheFirstDay_WithdrawsTheChange()
    {
        // Arrange
        PriceChange change = Summer();
        DateTimeOffset revertedAt = At.AddDays(3);

        // Act
        change.Revert(" other ", revertedAt, new DateOnly(2027, 5, 31));

        // Assert
        Assert.Equal((PriceChangeState.Reverted, "other"), (change.State, change.RevertedBy));
        Assert.Equal(revertedAt.UtcDateTime, change.RevertedAt!.Value.UtcDateTime);
        Assert.Null(change.EndedOn);
    }

    /// <summary>State: a change in force ends yesterday, so it counted up to the day before the revert.</summary>
    [Fact]
    public void Revert_InForce_EndsYesterday()
    {
        // Arrange
        PriceChange change = Summer();

        // Act
        change.Revert("other", At, new DateOnly(2027, 7, 10));

        // Assert
        Assert.Equal(PriceChangeState.Reverted, change.State);
        Assert.Equal(new DateOnly(2027, 7, 9), change.EndedOn);
    }

    /// <summary>Boundary: reverted on its first day, the end day is the day before the first day, so it counts for no day.</summary>
    [Fact]
    public void Revert_OnTheFirstDay_CountsForNoDay()
    {
        // Arrange
        PriceChange change = Summer();

        // Act
        change.Revert("other", At, First);

        // Assert
        Assert.Equal(First.AddDays(-1), change.EndedOn);
        Assert.False(change.CountsOn(First));
        Assert.False(change.CountsOn(First.AddDays(-1)));
    }

    /// <summary>Boundary: reverted on its last day, it counted up to the day before.</summary>
    [Fact]
    public void Revert_OnTheLastDay_EndsTheDayBefore()
    {
        // Arrange
        PriceChange change = Summer();

        // Act
        change.Revert("other", At, Last);

        // Assert
        Assert.Equal(Last.AddDays(-1), change.EndedOn);
    }

    /// <summary>Negative: a change reverted twice is refused with its code and keeps the first revert.</summary>
    [Fact]
    public void Revert_AlreadyReverted_ThrowsAndKeepsTheFirstRevert()
    {
        // Arrange
        PriceChange change = Summer();
        change.Revert("first", At, new DateOnly(2027, 7, 10));

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => change.Revert("second", At.AddDays(1), new DateOnly(2027, 7, 11)));

        // Assert
        Assert.Equal([PriceChangeErrors.AlreadyReverted], exception.Codes);
        Assert.Equal(("first", new DateOnly(2027, 7, 9)), (change.RevertedBy, change.EndedOn));
    }

    /// <summary>Negative: a change whose last day is past is refused with the period-ended code and stays approved.</summary>
    [Fact]
    public void Revert_AfterTheLastDay_ThrowsPeriodEnded()
    {
        // Arrange
        PriceChange change = Summer();

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => change.Revert("other", At, Last.AddDays(1)));

        // Assert
        Assert.Equal([PriceChangeErrors.PeriodEnded], exception.Codes);
        Assert.Equal(PriceChangeState.Approved, change.State);
    }

    /// <summary>Negative: a blank user name is refused with the reverted-by code and the change stays approved.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Revert_BlankName_ThrowsAndStaysApproved(string name)
    {
        // Arrange
        PriceChange change = Summer();

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => change.Revert(name, At, new DateOnly(2027, 7, 10)));

        // Assert
        Assert.Equal([PriceChangeErrors.RevertedByInvalid], exception.Codes);
        Assert.Equal((PriceChangeState.Approved, (DateOnly?)null), (change.State, change.EndedOn));
    }

    #endregion

    private static decimal Parse(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);
}
