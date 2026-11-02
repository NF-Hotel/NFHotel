using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The audit events that an approval and a revert of a price change write in the same save (MIL-055).</summary>
public sealed class PriceChangeAuditTests
{
    private static readonly DateOnly First = new(2027, 6, 1);
    private static readonly DateOnly Last = new(2027, 8, 31);

    private static ServiceWorld World(bool withRate)
    {
        var world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.Clock.Today = new DateOnly(2027, 5, 1);
        if (withRate)
        {
            world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 99.99m, "manager", world.Clock.UtcNow));
        }

        return world;
    }

    #region Approving

    /// <summary>Positive: the detail shows the rounded new price of a rate with decimals, in the invariant culture.</summary>
    [Fact]
    public async Task ApproveAsync_RateWithDecimals_AuditDetailShowsOldAndNewPrice()
    {
        // Arrange
        var world = World(withRate: true);
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
        try
        {
            // Act
            var result = await world.PriceChangeService().ApproveAsync("single", -12.5m, First, Last, TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Single -12.50% 2027-06-01..2027-08-31 (99.99 -> 87.49)", Assert.Single(world.StaffAudit.Events).Detail);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    /// <summary>Positive: without a base rate the detail leaves the price part out.</summary>
    [Fact]
    public async Task ApproveAsync_NoRate_AuditDetailHasNoPricePart()
    {
        // Arrange
        var world = World(withRate: false);

        // Act
        var result = await world.PriceChangeService().ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Single +20.00% 2027-06-01..2027-08-31", Assert.Single(world.StaffAudit.Events).Detail);
    }

    /// <summary>Negative: an invalid request and an unknown size write no audit event.</summary>
    [Fact]
    public async Task ApproveAsync_Invalid_WritesNoAuditEvent()
    {
        // Arrange
        var world = World(withRate: true);
        var service = world.PriceChangeService();

        // Act
        await service.ApproveAsync("Penthouse", 20m, First, Last, TestContext.Current.CancellationToken);
        await service.ApproveAsync("Single", 0m, First, Last, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Positive: the change and its audit event are added before the one save, so a failed save leaves both unsaved together.</summary>
    [Fact]
    public async Task ApproveAsync_SaveFails_TheExceptionPassesAndBothAreOnlyPending()
    {
        // Arrange
        var world = World(withRate: true);
        world.UnitOfWork.FailWith = new InvalidOperationException("db down");

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => world.PriceChangeService().ApproveAsync("Single", 20m, First, Last, TestContext.Current.CancellationToken));
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion
}
