using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>The rules of the nightly base rate of a room size (MIL-055).</summary>
public sealed class RoomRateTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(1));

    /// <summary>Positive: a valid rate keeps its fields, trims the user name and stores the time in UTC.</summary>
    [Fact]
    public void Create_ValidFields_KeepsThemTrimmedAndInUtc()
    {
        // Arrange and act
        RoomRate rate = RoomRate.Create(RoomSize.Suite, 250m, " manager ", At);

        // Assert
        Assert.Equal((RoomSize.Suite, 250m, "manager"), (rate.Size, rate.NightlyPrice, rate.SetBy));
        Assert.Equal(TimeSpan.Zero, rate.SetAt.Offset);
        Assert.Equal(At.UtcDateTime, rate.SetAt.UtcDateTime);
    }

    /// <summary>Boundary: the lowest price 0.01 and the highest price 1,000,000 are accepted.</summary>
    [Theory]
    [InlineData("0.01")]
    [InlineData("1000000")]
    public void Create_PriceAtTheLimit_IsAccepted(string price)
    {
        // Arrange and act
        RoomRate rate = RoomRate.Create(RoomSize.Single, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), "manager", At);

        // Assert
        Assert.Equal(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), rate.NightlyPrice);
    }

    /// <summary>Negative: a price of 0, below 0 or above the largest is refused with its code.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.004")]
    [InlineData("1000000.01")]
    public void Create_PriceOutsideTheLimits_ThrowsThePriceCode(string price)
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(
            () => RoomRate.Create(RoomSize.Double, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), "manager", At));

        // Assert
        Assert.Contains(RoomRateErrors.PriceInvalid, exception.Codes);
    }

    /// <summary>Equivalence: a price with more than two decimals is rounded to two.</summary>
    [Theory]
    [InlineData("89.994", "89.99")]
    [InlineData("89.995", "90.00")]
    public void Create_MoreThanTwoDecimals_RoundsToTwo(string price, string expected)
    {
        // Arrange and act
        RoomRate rate = RoomRate.Create(RoomSize.Single, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), "manager", At);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), rate.NightlyPrice);
    }

    /// <summary>Negative: a room size that is not one of the three is refused with its code.</summary>
    [Fact]
    public void Create_UnknownSize_ThrowsTheSizeCode()
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => RoomRate.Create((RoomSize)42, 100m, "manager", At));

        // Assert
        Assert.Contains(RoomRateErrors.SizeInvalid, exception.Codes);
    }

    /// <summary>Negative: a missing or too long user name is refused with its code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSetBy_ThrowsTheSetByCode(string setBy)
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => RoomRate.Create(RoomSize.Single, 100m, setBy, At));

        // Assert
        Assert.Contains(RoomRateErrors.SetByInvalid, exception.Codes);
    }

    /// <summary>Boundary: a user name of 100 characters is kept, one of 101 is refused.</summary>
    [Fact]
    public void Create_SetByLength_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange and act
        RoomRate accepted = RoomRate.Create(RoomSize.Single, 100m, new string('m', RoomRate.MaxTextLength), At);
        DomainException exception = Assert.Throws<DomainException>(() => RoomRate.Create(RoomSize.Single, 100m, new string('m', RoomRate.MaxTextLength + 1), At));

        // Assert
        Assert.Equal(RoomRate.MaxTextLength, accepted.SetBy.Length);
        Assert.Contains(RoomRateErrors.SetByInvalid, exception.Codes);
    }

    /// <summary>Negative: several faults are reported together.</summary>
    [Fact]
    public void Create_PriceAndSetByWrong_ThrowsBothCodes()
    {
        // Arrange and act
        DomainException exception = Assert.Throws<DomainException>(() => RoomRate.Create(RoomSize.Single, 0m, string.Empty, At));

        // Assert
        Assert.Contains(RoomRateErrors.PriceInvalid, exception.Codes);
        Assert.Contains(RoomRateErrors.SetByInvalid, exception.Codes);
    }

    /// <summary>State: a change replaces the price, the user and the time, and keeps the size.</summary>
    [Fact]
    public void Change_ValidFields_ReplacesEverythingButTheSize()
    {
        // Arrange
        RoomRate rate = RoomRate.Create(RoomSize.Double, 100m, "manager", At);

        // Act
        rate.Change(120.5m, "other", At.AddHours(1));

        // Assert
        Assert.Equal((RoomSize.Double, 120.5m, "other"), (rate.Size, rate.NightlyPrice, rate.SetBy));
        Assert.Equal(At.AddHours(1).UtcDateTime, rate.SetAt.UtcDateTime);
    }

    /// <summary>State: a refused change leaves the rate as it was.</summary>
    [Fact]
    public void Change_PriceZero_ThrowsAndKeepsTheOldRate()
    {
        // Arrange
        RoomRate rate = RoomRate.Create(RoomSize.Double, 100m, "manager", At);

        // Act
        DomainException exception = Assert.Throws<DomainException>(() => rate.Change(0m, "other", At.AddHours(1)));

        // Assert
        Assert.Contains(RoomRateErrors.PriceInvalid, exception.Codes);
        Assert.Equal((100m, "manager"), (rate.NightlyPrice, rate.SetBy));
        Assert.Equal(At.UtcDateTime, rate.SetAt.UtcDateTime);
    }
}
