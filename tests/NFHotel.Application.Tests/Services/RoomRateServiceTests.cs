using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.Errors;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Reading and setting the nightly base rate per room size, with its audit event (MIL-051).</summary>
public sealed class RoomRateServiceTests
{
    private static ServiceWorld WorldWithManager()
    {
        var world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.CallContext.RemoteAddress = "10.0.0.5";
        return world;
    }

    #region Setting a rate

    /// <summary>Positive: the first rate of a size is stored once with the clock time and the caller, and one audit event is saved with it.</summary>
    [Fact]
    public async Task SetAsync_NoRateYet_StoresItOnceWithAnAuditEvent()
    {
        // Arrange
        var world = WorldWithManager();

        // Act
        var result = await world.RoomRateService().SetAsync("Single", new SetRoomRateRequest(90m), TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.RoomRates.Items);
        var audit = Assert.Single(world.StaffAudit.Events);
        Assert.True(result.IsSuccess);
        Assert.Equal((RoomSize.Single, 90m, "manager"), (stored.Size, stored.NightlyPrice, stored.SetBy));
        Assert.Equal(world.Clock.UtcNow, stored.SetAt);
        Assert.Equal(("Single", 90m, "manager"), (result.Value.Size, result.Value.NightlyPrice, result.Value.SetBy));
        Assert.Equal(StaffAuditAction.RoomRateChanged, audit.Action);
        Assert.Equal("Single: (none) -> 90.00", audit.Detail);
        Assert.Equal((7, "manager", "Success"), (audit.ActorId, audit.ActorName, audit.Outcome));
        Assert.Null(audit.TargetId);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>State: setting a size again replaces its rate, adds no second row and records the old and the new price.</summary>
    [Fact]
    public async Task SetAsync_RateExists_ReplacesItAndRecordsOldAndNew()
    {
        // Arrange
        var world = WorldWithManager();
        var service = world.RoomRateService();
        await service.SetAsync("single", new SetRoomRateRequest(80m), TestContext.Current.CancellationToken);
        world.CallContext.StaffName = "other";

        // Act
        var result = await service.SetAsync("single", new SetRoomRateRequest(90.5m), TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.RoomRates.Items);
        Assert.Equal((90.5m, "other"), (stored.NightlyPrice, stored.SetBy));
        Assert.Equal(90.5m, result.Value.NightlyPrice);
        Assert.Equal(["Single: (none) -> 80.00", "Single: 80.00 -> 90.50"], world.StaffAudit.Events.Select(item => item.Detail));
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: the size is accepted in any letter case and with surrounding spaces.</summary>
    [Theory]
    [InlineData("single", RoomSize.Single)]
    [InlineData("DOUBLE", RoomSize.Double)]
    [InlineData(" Suite ", RoomSize.Suite)]
    public async Task SetAsync_SizeInAnyCase_StoresThatSize(string size, RoomSize expected)
    {
        // Arrange
        var world = WorldWithManager();

        // Act
        var result = await world.RoomRateService().SetAsync(size, new SetRoomRateRequest(100m), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, Assert.Single(world.RoomRates.Items).Size);
    }

    /// <summary>Negative: a size that is not a name is refused with its code, and nothing is stored, audited or saved.</summary>
    [Theory]
    [InlineData("penthouse")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SetAsync_UnknownSize_ReturnsTheSizeCodeAndStoresNothing(string? size)
    {
        // Arrange
        var world = WorldWithManager();

        // Act
        var result = await world.RoomRateService().SetAsync(size!, new SetRoomRateRequest(100m), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(RoomRateErrors.SizeInvalid, result.Errors.Select(error => error.Code));
        Assert.Empty(world.RoomRates.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a price of 0 is refused, the old rate stays and no audit event is written.</summary>
    [Fact]
    public async Task SetAsync_PriceZero_ReturnsThePriceCodeAndKeepsTheOldRate()
    {
        // Arrange
        var world = WorldWithManager();
        var service = world.RoomRateService();
        await service.SetAsync("Double", new SetRoomRateRequest(120m), TestContext.Current.CancellationToken);
        var saves = world.UnitOfWork.SaveCount;

        // Act
        var result = await service.SetAsync("Double", new SetRoomRateRequest(0m), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(RoomRateErrors.PriceInvalid, result.Errors.Select(error => error.Code));
        Assert.Equal(120m, Assert.Single(world.RoomRates.Items).NightlyPrice);
        Assert.Single(world.StaffAudit.Events);
        Assert.Equal(saves, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a call with nobody signed in is refused with the set-by code, and nothing is written.</summary>
    [Fact]
    public async Task SetAsync_NobodySignedIn_ReturnsTheSetByCodeAndWritesNothing()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.RoomRateService().SetAsync("Single", new SetRoomRateRequest(100m), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(RoomRateErrors.SetByInvalid, result.Errors.Select(error => error.Code));
        Assert.Empty(world.RoomRates.Items);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Boundary: a price with more than two decimals is stored and written to the audit trail with two.</summary>
    [Fact]
    public async Task SetAsync_ThreeDecimals_StoresAndAuditsTwo()
    {
        // Arrange
        var world = WorldWithManager();

        // Act
        await world.RoomRateService().SetAsync("Suite", new SetRoomRateRequest(199.999m), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(200m, Assert.Single(world.RoomRates.Items).NightlyPrice);
        Assert.Equal("Suite: (none) -> 200.00", Assert.Single(world.StaffAudit.Events).Detail);
    }

    /// <summary>Negative (RC-260 F13): a simultaneous first save hits the unique index; the call returns the conflict code, not an exception.</summary>
    [Fact]
    public async Task SetAsync_UniqueIndexBrokenBySimultaneousSave_ReturnsTheConflictCode()
    {
        // Arrange
        var world = WorldWithManager();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate key", "ux_room_rate_size");

        // Act
        var result = await world.RoomRateService().SetAsync("Single", new SetRoomRateRequest(90m), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(RoomRateErrors.ConcurrencyConflict, result.FirstError!.Code);
    }

    #endregion

    #region Reading the rates

    /// <summary>Positive: the list holds the rates that are set, ordered by size.</summary>
    [Fact]
    public async Task GetAllAsync_TwoRates_ReturnsThemOrderedBySize()
    {
        // Arrange
        var world = WorldWithManager();
        var service = world.RoomRateService();
        await service.SetAsync("Suite", new SetRoomRateRequest(300m), TestContext.Current.CancellationToken);
        await service.SetAsync("Single", new SetRoomRateRequest(80m), TestContext.Current.CancellationToken);

        // Act
        var result = await service.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Single", 80m), ("Suite", 300m)], result.Value.Select(value => (value.Size, value.NightlyPrice)));
    }

    /// <summary>Boundary: with nothing set the list is empty and not an error.</summary>
    [Fact]
    public async Task GetAllAsync_NothingSet_ReturnsAnEmptyList()
    {
        // Arrange
        var world = WorldWithManager();

        // Act
        var result = await world.RoomRateService().GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    #endregion
}
