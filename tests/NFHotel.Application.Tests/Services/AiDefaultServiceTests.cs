using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Reading and setting the default AI model per purpose (ADR-0014).</summary>
public sealed class AiDefaultServiceTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    /// <summary>Positive: the first default of a purpose is stored once with the clock time and the user.</summary>
    [Fact]
    public async Task SetAsync_NoDefaultYet_StoresItOnce()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.AiDefaultService().SetAsync(AiDefault.Website, new SetAiDefaultRequest("llama3.2:3b", RunId), "manager", TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.AiDefaults.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal(("website", "llama3.2:3b", "manager", RunId), (stored.Purpose, stored.Model, stored.SetBy, stored.SourceRunId));
        Assert.Equal(world.Clock.UtcNow, stored.SetAt);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>State: setting a purpose again replaces its default and does not add a second one.</summary>
    [Fact]
    public async Task SetAsync_DefaultExists_ReplacesItWithoutAddingOne()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.AiDefaultService();
        await service.SetAsync(AiDefault.Website, new SetAiDefaultRequest("m1"), "manager", TestContext.Current.CancellationToken);

        // Act
        var result = await service.SetAsync(AiDefault.Website, new SetAiDefaultRequest("m2"), "other", TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.AiDefaults.Items);
        Assert.Equal("m2", result.Value.Model);
        Assert.Equal(("m2", "other"), (stored.Model, stored.SetBy));
    }

    /// <summary>State: the two purposes are independent of each other.</summary>
    [Fact]
    public async Task SetAsync_TwoPurposes_KeepsOneDefaultForEach()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.AiDefaultService();

        // Act
        await service.SetAsync(AiDefault.Website, new SetAiDefaultRequest("m1"), "manager", TestContext.Current.CancellationToken);
        await service.SetAsync(AiDefault.MarketAnalysis, new SetAiDefaultRequest("m2"), "manager", TestContext.Current.CancellationToken);
        var all = await service.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("market_analysis", "m2"), ("website", "m1")], all.Value.Select(value => (value.Purpose, value.Model)));
    }

    /// <summary>Negative: an unknown purpose is refused with its code and nothing is stored or saved.</summary>
    [Fact]
    public async Task SetAsync_UnknownPurpose_ReturnsThePurposeCodeAndStoresNothing()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.AiDefaultService().SetAsync("billing", new SetAiDefaultRequest("m1"), "manager", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(AiDefaultErrors.PurposeInvalid, result.Errors.Select(error => error.Code));
        Assert.Empty(world.AiDefaults.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a blank model is refused and the old default stays.</summary>
    [Fact]
    public async Task SetAsync_BlankModel_ReturnsTheModelCodeAndKeepsTheOldDefault()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.AiDefaultService();
        await service.SetAsync(AiDefault.Website, new SetAiDefaultRequest("m1"), "manager", TestContext.Current.CancellationToken);
        var saves = world.UnitOfWork.SaveCount;

        // Act
        var result = await service.SetAsync(AiDefault.Website, new SetAiDefaultRequest(" "), "manager", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(AiDefaultErrors.ModelInvalid, result.Errors.Select(error => error.Code));
        Assert.Equal("m1", Assert.Single(world.AiDefaults.Items).Model);
        Assert.Equal(saves, world.UnitOfWork.SaveCount);
    }

    /// <summary>Boundary: with nothing set the list is empty and not an error.</summary>
    [Fact]
    public async Task GetAllAsync_NothingSet_ReturnsAnEmptyList()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.AiDefaultService().GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
