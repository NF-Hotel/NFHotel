using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Approving and reverting price changes, and the effective price of a room size on a day (MIL-055).</summary>
public sealed class PriceChangeServiceTests
{
    private static readonly DateOnly First = new(2027, 6, 1);
    private static readonly DateOnly Last = new(2027, 8, 31);

    private static ServiceWorld WorldWithBaseRate(decimal singleRate = 100m)
    {
        var world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.Clock.Today = new DateOnly(2027, 5, 1);
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, singleRate, "manager", world.Clock.UtcNow));
        world.RoomRates.Add(RoomRate.Create(RoomSize.Double, 150m, "manager", world.Clock.UtcNow));
        return world;
    }

    private static async Task<decimal?> PriceAsync(ServiceWorld world, string size, string day)
    {
        var result = await world.PriceChangeService().GetEffectivePriceAsync(size, DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        return result.Value.Price;
    }

    #region Approving

    /// <summary>Positive: an approval stores one approved change with the caller and the clock time, saves once and says it was created.</summary>
    [Fact]
    public async Task ApproveAsync_ValidRequest_StoresOneApprovedChange()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.PriceChanges.Items);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.Equal((RoomSize.Single, 20m, First, Last), (stored.Size, stored.Percentage, stored.FirstDay, stored.LastDay));
        Assert.Equal((PriceChangeState.Approved, "manager", world.Clock.UtcNow), (stored.State, stored.ApprovedBy, stored.ApprovedAt));
        Assert.Equal(("Single", 20m, "Approved", "manager"), (result.Value.Change.Size, result.Value.Change.Percentage, result.Value.Change.State, result.Value.Change.ApprovedBy));
        Assert.Equal(stored.Id, result.Value.Change.Id);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: the size is accepted in any letter case and with surrounding spaces.</summary>
    [Theory]
    [InlineData("single", RoomSize.Single)]
    [InlineData("DOUBLE", RoomSize.Double)]
    [InlineData(" Suite ", RoomSize.Suite)]
    public async Task ApproveAsync_SizeInAnyCase_StoresThatSize(string size, RoomSize expected)
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync(size, 10m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, Assert.Single(world.PriceChanges.Items).Size);
    }

    /// <summary>Negative: an unknown size, a number, or nothing is refused with the size code and nothing is saved.</summary>
    [Theory]
    [InlineData("Penthouse")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ApproveAsync_UnknownSize_ReturnsTheSizeCodeAndSavesNothing(string? size)
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync(size!, 10m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.SizeInvalid, result.FirstError!.Code);
        Assert.Empty(world.PriceChanges.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a percentage of 0 and a backwards period are refused with their codes and nothing is saved.</summary>
    [Fact]
    public async Task ApproveAsync_InvalidFields_ReturnsTheCodesAndSavesNothing()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 0m, Last, First, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            [PriceChangeErrors.PercentageInvalid, PriceChangeErrors.PeriodInvalid],
            result.Errors.Select(error => error.Code).Order(StringComparer.Ordinal));
        Assert.Empty(world.PriceChanges.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a call without a staff name is refused with the approved-by code.</summary>
    [Fact]
    public async Task ApproveAsync_NoActor_ReturnsTheApprovedByCode()
    {
        // Arrange
        var world = WorldWithBaseRate();
        world.CallContext.StaffName = null;

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 10m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.ApprovedByInvalid, result.FirstError!.Code);
        Assert.Empty(world.PriceChanges.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative (RC-265 F12): a period wholly before today is refused with the period code and nothing is saved.</summary>
    [Fact]
    public async Task ApproveAsync_PeriodWhollyInThePast_ReturnsThePeriodCodeAndSavesNothing()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 10m, new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.PeriodInvalid, result.FirstError!.Code);
        Assert.Empty(world.PriceChanges.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Boundary: a period whose last day is today is still allowed, and so is one that began in the past.</summary>
    [Fact]
    public async Task ApproveAsync_LastDayIsToday_IsAllowed()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 10m, new DateOnly(2027, 4, 1), new DateOnly(2027, 5, 1), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.PriceChanges.Items);
    }

    #endregion

    #region Overlap and repeated approval

    /// <summary>Negative: a second change of the same size on a shared day is refused with the overlap code and nothing is saved.</summary>
    [Fact]
    public async Task ApproveAsync_OverlapsASameSizeChange_ReturnsTheOverlapCode()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await service.ApproveAsync("Single", -15m, new DateOnly(2027, 8, 31), new DateOnly(2027, 9, 30), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.Overlap, result.FirstError!.Code);
        Assert.Single(world.PriceChanges.Items);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: another percentage on exactly the same period is an overlap, not a repeat.</summary>
    [Fact]
    public async Task ApproveAsync_OtherPercentageOnTheSamePeriod_ReturnsTheOverlapCode()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await service.ApproveAsync("Single", 25m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.Overlap, result.FirstError!.Code);
    }

    /// <summary>Positive: another size, the day after the last day, and the day before the first day never overlap.</summary>
    [Fact]
    public async Task ApproveAsync_OtherSizeOrAdjacentDays_IsAllowed()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var otherSize = await service.ApproveAsync("Double", 10m, First, Last, TestContext.Current.CancellationToken);
        var after = await service.ApproveAsync("Single", -15m, Last.AddDays(1), new DateOnly(2027, 9, 30), TestContext.Current.CancellationToken);
        var before = await service.ApproveAsync("Single", 5m, new DateOnly(2027, 5, 1), First.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(otherSize.IsSuccess);
        Assert.True(after.IsSuccess);
        Assert.True(before.IsSuccess);
        Assert.Equal(4, world.PriceChanges.Items.Count);
    }

    /// <summary>Positive: the same period is free again after a withdrawn change, which never blocks.</summary>
    [Fact]
    public async Task ApproveAsync_AfterAWithdrawnChange_IsAllowed()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var first = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        await service.RevertAsync(first.Value.Change.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await service.ApproveAsync("Single", -15m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.Equal(2, world.PriceChanges.Items.Count);
    }

    /// <summary>State: after an ended change the days from the revert day on are free, the days it still counted for are not.</summary>
    [Fact]
    public async Task ApproveAsync_AfterAnEndedChange_OnlyTheEndedDaysBlock()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var first = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 10);
        await service.RevertAsync(first.Value.Change.Id, TestContext.Current.CancellationToken);

        // Act
        var overlapping = await service.ApproveAsync("Single", -15m, new DateOnly(2027, 7, 9), Last, TestContext.Current.CancellationToken);
        var free = await service.ApproveAsync("Single", -15m, new DateOnly(2027, 7, 10), Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceChangeErrors.Overlap, overlapping.FirstError!.Code);
        Assert.True(free.IsSuccess);
    }

    /// <summary>Positive: the same approval twice returns the existing change with Created false and saves nothing more.</summary>
    [Fact]
    public async Task ApproveAsync_SameChangeTwice_ReturnsTheExistingOne()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var first = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var second = await service.ApproveAsync("single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.False(second.Value.Created);
        Assert.Equal(first.Value.Change.Id, second.Value.Change.Id);
        Assert.Single(world.PriceChanges.Items);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Reverting

    /// <summary>State: reverting a change that has not begun withdraws it, saves once, and the base rate is the price on all its days at once.</summary>
    [Fact]
    public async Task RevertAsync_FutureChange_WithdrawsItAndTheBaseRateAppliesAtOnce()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var approved = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.PriceChanges.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal(("Reverted", (DateOnly?)null, "manager"), (result.Value.State, result.Value.EndedOn, result.Value.RevertedBy));
        Assert.Equal(PriceChangeState.Reverted, stored.State);
        Assert.Equal(world.Clock.UtcNow, stored.RevertedAt);
        Assert.Equal(2, world.UnitOfWork.SaveCount);
        Assert.Equal(100m, await PriceAsync(world, "Single", "2027-06-01"));
        Assert.Equal(100m, await PriceAsync(world, "Single", "2027-07-15"));
        Assert.Equal(100m, await PriceAsync(world, "Single", "2027-08-31"));
    }

    /// <summary>State: reverting a change in force on 2027-07-10 ends it; 07-09 is still 120, 07-10 and later are the base rate.</summary>
    [Fact]
    public async Task RevertAsync_ChangeInForce_EndsItTodayAndKeepsYesterday()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var approved = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 10);

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("Reverted", new DateOnly(2027, 7, 9)), (result.Value.State, result.Value.EndedOn));
        Assert.Equal(new DateOnly(2027, 7, 9), Assert.Single(world.PriceChanges.Items).EndedOn);
        Assert.Equal(120m, await PriceAsync(world, "Single", "2027-07-09"));
        Assert.Equal(100m, await PriceAsync(world, "Single", "2027-07-10"));
        Assert.Equal(100m, await PriceAsync(world, "Single", "2027-08-01"));
        Assert.Equal(120m, await PriceAsync(world, "Single", "2027-06-01"));
    }

    /// <summary>Negative: reverting an unknown key is refused with the not-found code and nothing is saved.</summary>
    [Fact]
    public async Task RevertAsync_UnknownId_ReturnsNotFound()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().RevertAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.NotFound, result.FirstError!.Code);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a second revert is refused with the already-reverted code, saves nothing and keeps the first revert.</summary>
    [Fact]
    public async Task RevertAsync_AlreadyReverted_ReturnsTheCodeAndKeepsTheState()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var approved = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 10);
        await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 12);
        world.CallContext.StaffName = "other";

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.PriceChanges.Items);
        Assert.Equal(PriceChangeErrors.AlreadyReverted, result.FirstError!.Code);
        Assert.Equal(("manager", new DateOnly(2027, 7, 9)), (stored.RevertedBy, stored.EndedOn));
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a change whose period is over is refused with the period-ended code.</summary>
    [Fact]
    public async Task RevertAsync_PeriodOver_ReturnsPeriodEnded()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var approved = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        world.Clock.Today = Last.AddDays(1);

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceChangeErrors.PeriodEnded, result.FirstError!.Code);
        Assert.Equal(PriceChangeState.Approved, Assert.Single(world.PriceChanges.Items).State);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a revert without a staff name is refused with the reverted-by code and the change stays approved.</summary>
    [Fact]
    public async Task RevertAsync_NoActor_ReturnsTheRevertedByCode()
    {
        // Arrange
        var world = WorldWithBaseRate();
        var service = world.PriceChangeService();
        var approved = await service.ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);
        world.CallContext.StaffName = null;

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceChangeErrors.RevertedByInvalid, result.FirstError!.Code);
        Assert.Equal(PriceChangeState.Approved, Assert.Single(world.PriceChanges.Items).State);
    }

    #endregion

    #region Effective price

    /// <summary>Boundary: base 100 and +20 percent for 2027-06-01 to 2027-08-31 give 120.00 on both ends and 100.00 the day before and after.</summary>
    [Theory]
    [InlineData("2027-05-31", "100")]
    [InlineData("2027-06-01", "120")]
    [InlineData("2027-08-31", "120")]
    [InlineData("2027-09-01", "100")]
    public async Task GetEffectivePriceAsync_AroundThePeriod_AppliesTheChangeOnlyInside(string day, string expected)
    {
        // Arrange
        var world = WorldWithBaseRate();
        await world.PriceChangeService().ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Single", DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result.Value.Price);
        Assert.Equal(100m, result.Value.BaseRate);
    }

    /// <summary>Positive: a discount of 15 percent on a single room of 100 gives 85.00, and the percentage is reported.</summary>
    [Fact]
    public async Task GetEffectivePriceAsync_Discount_LowersThePrice()
    {
        // Arrange
        var world = WorldWithBaseRate();
        await world.PriceChangeService().ApproveAsync("Single", -15m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Single", First, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((85m, -15m), (result.Value.Price, result.Value.Percentage));
    }

    /// <summary>Equivalence: a price is rounded to two decimals, halves away from zero (99.99 plus 10 percent is 109.989 and gives 109.99).</summary>
    [Fact]
    public async Task GetEffectivePriceAsync_ManyDecimals_RoundsToTwo()
    {
        // Arrange
        var world = WorldWithBaseRate(99.99m);
        await world.PriceChangeService().ApproveAsync("Single", 10m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Single", First, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(109.99m, result.Value.Price);
    }

    /// <summary>Negative: without a base rate the base rate and the price are null, also on a day a change counts.</summary>
    [Fact]
    public async Task GetEffectivePriceAsync_NoBaseRate_ReturnsNullPrice()
    {
        // Arrange
        var world = WorldWithBaseRate();
        await world.PriceChangeService().ApproveAsync("Suite", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Suite", First, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((null, null, 20m), (result.Value.BaseRate, result.Value.Price, result.Value.Percentage));
    }

    /// <summary>Negative: an unknown size is refused with the size code.</summary>
    [Fact]
    public async Task GetEffectivePriceAsync_UnknownSize_ReturnsTheSizeCode()
    {
        // Arrange
        var world = WorldWithBaseRate();

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Penthouse", First, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.SizeInvalid, result.FirstError!.Code);
    }

    /// <summary>Equivalence: should the data hold two changes on a day, the one approved last wins and nothing throws.</summary>
    [Fact]
    public async Task GetEffectivePriceAsync_TwoChangesCountOnADay_TakesTheLastApproved()
    {
        // Arrange
        var world = WorldWithBaseRate();
        world.PriceChanges.Add(PriceChange.Approve(RoomSize.Single, 20m, First, Last, "manager", world.Clock.UtcNow));
        world.PriceChanges.Add(PriceChange.Approve(RoomSize.Single, 50m, First, Last, "manager", world.Clock.UtcNow.AddHours(1)));

        // Act
        var result = await world.PriceChangeService().GetEffectivePriceAsync("Single", First, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(150m, result.Value.Price);
    }

    /// <summary>Positive: the prices of all three sizes come in the order of the sizes, each with its own rate and change.</summary>
    [Fact]
    public async Task GetEffectivePricesAsync_AllSizes_ReturnsOnePricePerSize()
    {
        // Arrange
        var world = WorldWithBaseRate();
        await world.PriceChangeService().ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Act
        var result = await world.PriceChangeService().GetEffectivePricesAsync(First, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["Single", "Double", "Suite"], result.Value.Select(item => item.Size));
        Assert.Equal([120m, 150m, null], result.Value.Select(item => item.Price));
        Assert.Equal([20m, null, null], result.Value.Select(item => item.Percentage));
        Assert.All(result.Value, item => Assert.Equal(First, item.Day));
    }

    #endregion
}
