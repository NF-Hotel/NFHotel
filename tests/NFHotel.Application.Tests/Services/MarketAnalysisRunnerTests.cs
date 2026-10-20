using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The work of a market analysis run: read the bookings, send them to the markets API, store the result (MIL-026).</summary>
public sealed class MarketAnalysisRunnerTests
{
    /// <summary>Positive: the bookings of the hotel are sent as they are and the result is stored once with the clock times.</summary>
    [Fact]
    public async Task ExecuteAsync_BookingsAndResult_SendsTheBookingsAndStoresTheResult()
    {
        // Arrange
        var world = new ServiceWorld();
        var runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.MarketAnalysisRuns.Items);
        Assert.Equal(world.MarketAnalysisBookings.Bookings, world.MarketAnalysisClient.LastBookings);
        Assert.Equal((runId, 1, "ollama", "phi3.5:latest"), (stored.RunId, stored.SchemaVersion, stored.Provider, stored.Model));
        Assert.Equal((world.Clock.UtcNow, world.Clock.UtcNow), (stored.StartedAt, stored.FinishedAt));
        Assert.Equal(MarketAnalysisRunStates.Completed, world.MarketAnalysisTracker.Get(runId)!.State);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: with no booking nothing is sent and the run fails with its code.</summary>
    [Fact]
    public async Task ExecuteAsync_NoBookings_FailsWithoutCallingTheService()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisBookings.Bookings.Clear();
        var runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        AssertFailed(world, runId, ErrorCodes.MarketAnalysis.NoBookings);
        Assert.Equal(0, world.MarketAnalysisClient.Calls);
    }

    /// <summary>Negative: a refusal or a failure of the markets API ends the run with that code and stores nothing.</summary>
    [Theory]
    [InlineData("market_analysis_runner.input_rejected")]
    [InlineData("market_analysis_runner.unavailable")]
    [InlineData("market_analysis_runner.not_configured")]
    [InlineData("market_analysis_runner.unexpected_response")]
    public async Task ExecuteAsync_ServiceFails_FailsWithTheCodeAndStoresNothing(string code)
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.FailWith = code;
        var runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        AssertFailed(world, runId, code);
    }

    /// <summary>Negative: a result with an unknown schema version is refused and not stored.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ExecuteAsync_UnknownSchemaVersion_FailsAndStoresNothing(int version)
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult(version);
        var runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        AssertFailed(world, runId, ErrorCodes.MarketAnalysis.SchemaVersionUnsupported);
    }

    /// <summary>Negative: a result without content is an invalid body.</summary>
    [Fact]
    public async Task ExecuteAsync_NoContent_FailsAsInvalidBody()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult() with { Content = null };
        var runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        AssertFailed(world, runId, ErrorCodes.MarketAnalysis.BodyInvalid);
    }

    /// <summary>State: a failed run keeps the report that was stored before it.</summary>
    [Fact]
    public async Task ExecuteAsync_FailureAfterASuccess_KeepsTheEarlierReport()
    {
        // Arrange
        var world = new ServiceWorld();
        await world.MarketAnalysisRunner().ExecuteAsync(Begin(world), TestContext.Current.CancellationToken);
        world.MarketAnalysisClient.FailWith = ErrorCodes.MarketAnalysisRunner.Unavailable;
        var second = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(second, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(world.MarketAnalysisRuns.Items);
        Assert.Equal(MarketAnalysisRunStates.Failed, world.MarketAnalysisTracker.Get(second)!.State);
    }

    private static Guid Begin(ServiceWorld world) => world.MarketAnalysisTracker.TryBegin().Value.RunId;

    private static void AssertFailed(ServiceWorld world, Guid runId, string code)
    {
        var state = world.MarketAnalysisTracker.Get(runId)!;
        Assert.Equal((MarketAnalysisRunStates.Failed, code), (state.State, state.Error));
        Assert.Empty(world.MarketAnalysisRuns.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }
}
