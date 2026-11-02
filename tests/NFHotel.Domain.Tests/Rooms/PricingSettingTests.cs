using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Rooms;

/// <summary>The rules of the pricing settings of the Manager (MIL-051).</summary>
public sealed class PricingSettingTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(1));

    private static PricingSetting Make(
        decimal surcharge = 20m,
        decimal discount = 15m,
        decimal high = 80m,
        decimal low = 40m,
        decimal maxChange = 30m,
        decimal minimum = 0m,
        RoomSize[]? sizes = null,
        string setBy = "manager") =>
        PricingSetting.Create(surcharge, discount, high, low, maxChange, minimum, sizes ?? [RoomSize.Single], setBy, At);

    private static string[] CodesOf(Action action) => Assert.Throws<DomainException>(action).Codes.ToArray();

    #region Creating and changing

    /// <summary>Positive: valid settings keep their fields, trim the user name and store the time in UTC.</summary>
    [Fact]
    public void Create_ValidFields_KeepsThemTrimmedAndInUtc()
    {
        // Arrange and act
        var setting = Make(25m, 10m, 90m, 30m, 20m, 55.5m, [RoomSize.Double, RoomSize.Single], " manager ");

        // Assert
        Assert.Equal((25m, 10m, 90m, 30m, 20m, 55.5m), (setting.SurchargePercent, setting.DiscountPercent, setting.HighThresholdPercent, setting.LowThresholdPercent, setting.MaxChangePercent, setting.MinimumNightlyPrice));
        Assert.Equal([RoomSize.Single, RoomSize.Double], setting.DiscountSizes);
        Assert.Equal("manager", setting.SetBy);
        Assert.Equal(TimeSpan.Zero, setting.SetAt.Offset);
        Assert.Equal(PricingSetting.SingletonSlot, setting.Slot);
    }

    /// <summary>State: a size that is listed twice is kept once, and the sizes are kept in the order of the sizes.</summary>
    [Fact]
    public void Create_RepeatedAndUnorderedSizes_KeepsEachOnceInOrder()
    {
        // Arrange and act
        var setting = Make(sizes: [RoomSize.Suite, RoomSize.Single, RoomSize.Suite]);

        // Assert
        Assert.Equal([RoomSize.Single, RoomSize.Suite], setting.DiscountSizes);
    }

    /// <summary>State: a change replaces every field.</summary>
    [Fact]
    public void Change_ValidFields_ReplacesEverything()
    {
        // Arrange
        var setting = Make();

        // Act
        setting.Change(5m, 6m, 70m, 10m, 50m, 99m, [RoomSize.Suite], "other", At.AddHours(1));

        // Assert
        Assert.Equal((5m, 6m, 70m, 10m, 50m, 99m), (setting.SurchargePercent, setting.DiscountPercent, setting.HighThresholdPercent, setting.LowThresholdPercent, setting.MaxChangePercent, setting.MinimumNightlyPrice));
        Assert.Equal([RoomSize.Suite], setting.DiscountSizes);
        Assert.Equal("other", setting.SetBy);
        Assert.Equal(At.AddHours(1).UtcDateTime, setting.SetAt.UtcDateTime);
    }

    /// <summary>State: a refused change leaves the settings as they were.</summary>
    [Fact]
    public void Change_InvalidSurcharge_ThrowsAndKeepsTheOldSettings()
    {
        // Arrange
        var setting = Make();

        // Act
        var codes = CodesOf(() => setting.Change(101m, 15m, 80m, 40m, 30m, 0m, [RoomSize.Double], "other", At.AddHours(1)));

        // Assert
        Assert.Contains(PricingSettingErrors.SurchargeInvalid, codes);
        Assert.Equal((20m, "manager"), (setting.SurchargePercent, setting.SetBy));
        Assert.Equal([RoomSize.Single], setting.DiscountSizes);
        Assert.Equal(At.UtcDateTime, setting.SetAt.UtcDateTime);
    }

    #endregion

    #region Bounds

    /// <summary>Boundary: 0 and 100 percent are accepted for the surcharge, the discount and the thresholds.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("100")]
    public void Create_PercentAtTheLimit_IsAccepted(string percent)
    {
        // Arrange
        var value = decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var setting = Make(surcharge: value, discount: value);

        // Assert
        Assert.Equal((value, value), (setting.SurchargePercent, setting.DiscountPercent));
    }

    /// <summary>Negative: a surcharge outside 0 to 100 is refused with its code.</summary>
    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    public void Create_SurchargeOutsideTheLimits_ThrowsTheSurchargeCode(string percent)
    {
        // Arrange
        var value = decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var codes = CodesOf(() => Make(surcharge: value));

        // Assert
        Assert.Equal([PricingSettingErrors.SurchargeInvalid], codes);
    }

    /// <summary>Negative: a discount outside 0 to 100 is refused with its code.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public void Create_DiscountOutsideTheLimits_ThrowsTheDiscountCode(string percent)
    {
        // Arrange
        var value = decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var codes = CodesOf(() => Make(discount: value));

        // Assert
        Assert.Equal([PricingSettingErrors.DiscountInvalid], codes);
    }

    /// <summary>Negative: a high threshold above 100 is refused with its own code and no order code.</summary>
    [Fact]
    public void Create_HighThresholdAbove100_ThrowsOnlyTheHighThresholdCode()
    {
        // Arrange and act
        var codes = CodesOf(() => Make(high: 100.01m));

        // Assert
        Assert.Equal([PricingSettingErrors.HighThresholdInvalid], codes);
    }

    /// <summary>Negative: a negative low threshold is refused with its own code and no order code.</summary>
    [Fact]
    public void Create_LowThresholdBelowZero_ThrowsOnlyTheLowThresholdCode()
    {
        // Arrange and act
        var codes = CodesOf(() => Make(low: -1m));

        // Assert
        Assert.Equal([PricingSettingErrors.LowThresholdInvalid], codes);
    }

    /// <summary>Boundary: the largest change 0.01 and 100 are accepted.</summary>
    [Theory]
    [InlineData("0.01")]
    [InlineData("100")]
    public void Create_MaxChangeAtTheLimit_IsAccepted(string percent)
    {
        // Arrange
        var value = decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var setting = Make(maxChange: value);

        // Assert
        Assert.Equal(value, setting.MaxChangePercent);
    }

    /// <summary>Negative: a largest change of 0, below 0 or above 100 is refused with its code.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("0.004")]
    [InlineData("-5")]
    [InlineData("100.01")]
    public void Create_MaxChangeOutsideTheLimits_ThrowsTheMaxChangeCode(string percent)
    {
        // Arrange
        var value = decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var codes = CodesOf(() => Make(maxChange: value));

        // Assert
        Assert.Equal([PricingSettingErrors.MaxChangeInvalid], codes);
    }

    /// <summary>Boundary: a minimum price of 0 and of 1,000,000 are accepted.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("1000000")]
    public void Create_MinimumPriceAtTheLimit_IsAccepted(string price)
    {
        // Arrange
        var value = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var setting = Make(minimum: value);

        // Assert
        Assert.Equal(value, setting.MinimumNightlyPrice);
    }

    /// <summary>Negative: a minimum price below 0 or above 1,000,000 is refused with its code.</summary>
    [Theory]
    [InlineData("-0.01")]
    [InlineData("1000000.01")]
    public void Create_MinimumPriceOutsideTheLimits_ThrowsTheMinimumPriceCode(string price)
    {
        // Arrange
        var value = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var codes = CodesOf(() => Make(minimum: value));

        // Assert
        Assert.Equal([PricingSettingErrors.MinimumPriceInvalid], codes);
    }

    #endregion

    #region Thresholds and sizes

    /// <summary>Boundary: a low threshold one cent below the high threshold is accepted.</summary>
    [Fact]
    public void Create_LowThresholdJustBelowHigh_IsAccepted()
    {
        // Arrange and act
        var setting = Make(high: 50m, low: 49.99m);

        // Assert
        Assert.Equal((50m, 49.99m), (setting.HighThresholdPercent, setting.LowThresholdPercent));
    }

    /// <summary>Negative: a low threshold equal to or above the high threshold is refused with the order code.</summary>
    [Theory]
    [InlineData("50", "50")]
    [InlineData("50", "60")]
    [InlineData("0", "0")]
    public void Create_LowThresholdNotBelowHigh_ThrowsTheOrderCode(string high, string low)
    {
        // Arrange
        var highValue = decimal.Parse(high, System.Globalization.CultureInfo.InvariantCulture);
        var lowValue = decimal.Parse(low, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var codes = CodesOf(() => Make(high: highValue, low: lowValue));

        // Assert
        Assert.Equal([PricingSettingErrors.ThresholdsOrderInvalid], codes);
    }

    /// <summary>Negative: without any room size the discount has nobody to apply to, so the sizes code is thrown.</summary>
    [Fact]
    public void Create_NoSizes_ThrowsTheSizesCode()
    {
        // Arrange and act
        var codes = CodesOf(() => Make(sizes: []));

        // Assert
        Assert.Equal([PricingSettingErrors.DiscountSizesInvalid], codes);
    }

    /// <summary>Negative: a size that is not one of the three is refused with the sizes code.</summary>
    [Fact]
    public void Create_UnknownSize_ThrowsTheSizesCode()
    {
        // Arrange and act
        var codes = CodesOf(() => Make(sizes: [RoomSize.Single, (RoomSize)42]));

        // Assert
        Assert.Equal([PricingSettingErrors.DiscountSizesInvalid], codes);
    }

    #endregion

    #region Rounding, names and several faults

    /// <summary>Equivalence: a number with more than two decimals is rounded to two, away from zero at the middle.</summary>
    [Theory]
    [InlineData("12.344", "12.34")]
    [InlineData("12.345", "12.35")]
    public void Create_MoreThanTwoDecimals_RoundsToTwo(string typed, string expected)
    {
        // Arrange
        var value = decimal.Parse(typed, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var setting = Make(surcharge: value, discount: value, high: 90m, low: value, maxChange: value, minimum: value);

        // Assert
        var rounded = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(
            (rounded, rounded, rounded, rounded, rounded),
            (setting.SurchargePercent, setting.DiscountPercent, setting.LowThresholdPercent, setting.MaxChangePercent, setting.MinimumNightlyPrice));
    }

    /// <summary>Boundary: a value that rounds to 100.00 is accepted, and one that rounds to 100.01 is refused.</summary>
    [Fact]
    public void Create_RoundingAtTheUpperLimit_AcceptsTheRoundedValue()
    {
        // Arrange and act
        var accepted = Make(surcharge: 100.004m);
        var codes = CodesOf(() => Make(surcharge: 100.005m));

        // Assert
        Assert.Equal(100m, accepted.SurchargePercent);
        Assert.Contains(PricingSettingErrors.SurchargeInvalid, codes);
    }

    /// <summary>Negative: a missing user name is refused with its code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSetBy_ThrowsTheSetByCode(string setBy)
    {
        // Arrange and act
        var codes = CodesOf(() => Make(setBy: setBy));

        // Assert
        Assert.Equal([PricingSettingErrors.SetByInvalid], codes);
    }

    /// <summary>Boundary: a user name of 100 characters is kept, one of 101 is refused.</summary>
    [Fact]
    public void Create_SetByLength_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange and act
        var accepted = Make(setBy: new string('m', PricingSetting.MaxTextLength));
        var codes = CodesOf(() => Make(setBy: new string('m', PricingSetting.MaxTextLength + 1)));

        // Assert
        Assert.Equal(PricingSetting.MaxTextLength, accepted.SetBy.Length);
        Assert.Equal([PricingSettingErrors.SetByInvalid], codes);
    }

    /// <summary>Negative: several faults are reported together.</summary>
    [Fact]
    public void Create_SeveralFaults_ThrowsEveryCode()
    {
        // Arrange and act
        var codes = CodesOf(() => Make(surcharge: -1m, maxChange: 0m, high: 10m, low: 20m, sizes: [], setBy: string.Empty));

        // Assert
        Assert.Equivalent(
            new[]
            {
                PricingSettingErrors.SurchargeInvalid,
                PricingSettingErrors.MaxChangeInvalid,
                PricingSettingErrors.ThresholdsOrderInvalid,
                PricingSettingErrors.DiscountSizesInvalid,
                PricingSettingErrors.SetByInvalid,
            },
            codes);
    }

    #endregion
}
