using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Domain.Entities;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The work of a market analysis run: read the bookings, send them to the markets API, store the result (MIL-026).</summary>
public sealed class MarketAnalysisRunnerTests
{
    #region Sending and storing

    /// <summary>Positive: the bookings of the hotel are sent as they are and the result is stored once with the clock times.</summary>
    [Fact]
    public async Task ExecuteAsync_BookingsAndResult_SendsTheBookingsAndStoresTheResult()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        Guid runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        MarketAnalysisRun stored = Assert.Single(world.MarketAnalysisRuns.Items);
        Assert.Equal(world.MarketAnalysisBookings.Bookings, world.MarketAnalysisClient.LastBookings);
        Assert.Equal((runId, 1, "ollama", "phi3.5:latest"), (stored.RunId, stored.SchemaVersion, stored.Provider, stored.Model));
        Assert.Equal((world.Clock.UtcNow, world.Clock.UtcNow), (stored.StartedAt, stored.FinishedAt));
        // The tracker keeps every run it began, so Get finds this one.
        Assert.Equal(MarketAnalysisRunStates.Completed, world.MarketAnalysisTracker.Get(runId)!.State);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: the rooms of the hotel and the period of the Manager go to the service with the bookings.</summary>
    [Fact]
    public async Task ExecuteAsync_RoomsAndPeriod_AreSentWithTheBookings()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisBookings.RoomCount = 187;
        Guid runId = world.MarketAnalysisTracker.TryBegin(new DateOnly(2019, 1, 1), new DateOnly(2019, 12, 31)).Value.RunId;

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(world.MarketAnalysisClient.LastRequest);
        MarketAnalysisRunRequest sent = world.MarketAnalysisClient.LastRequest;
        Assert.Equal((187, new DateOnly(2019, 1, 1), new DateOnly(2019, 12, 31)), (sent.RoomCount, sent.From, sent.To));
        Assert.Equal(world.MarketAnalysisBookings.Bookings, sent.Bookings);
    }

    /// <summary>Positive: the default pricing settings go to the service as shares and lower-case room sizes, so it makes the suggestions (MIL-071).</summary>
    [Fact]
    public async Task ExecuteAsync_DefaultSettings_SendsThePricingOfTheDefaults()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        Guid runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(world.MarketAnalysisClient.LastRequest?.Pricing);
        MarketAnalysisPricingRequest pricing = world.MarketAnalysisClient.LastRequest.Pricing;
        Assert.Equal((0.8m, 0.4m), (pricing.HighOccupancyThreshold, pricing.LowOccupancyThreshold));
        Assert.All(pricing.DiscountRoomSizes, size => Assert.Equal(size.ToLowerInvariant(), size));
        Assert.NotEmpty(pricing.DiscountRoomSizes);
    }

    /// <summary>Positive: the thresholds and percentages the Manager saved are the ones sent.</summary>
    [Fact]
    public async Task ExecuteAsync_SavedSettings_AreSentToTheService()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.CallContext.RemoteAddress = "10.0.0.5";
        // GetAsync always succeeds with a value: the saved settings, or the defaults when nothing is saved.
        PricingSettingsDto saved = (await world.PricingSettingsService().GetAsync(TestContext.Current.CancellationToken)).Value!;
        Result<PricingSettingsDto> set = await world.PricingSettingsService().SetAsync(new(25, 10, 85, 35, 50, saved.MinimumNightlyPrice, saved.DiscountSizes), TestContext.Current.CancellationToken);
        Assert.True(set.IsSuccess);
        Guid runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(world.MarketAnalysisClient.LastRequest?.Pricing);
        MarketAnalysisPricingRequest pricing = world.MarketAnalysisClient.LastRequest.Pricing;
        Assert.Equal((0.85m, 0.35m, 25m, 10m), (pricing.HighOccupancyThreshold, pricing.LowOccupancyThreshold, pricing.SurchargePercent, pricing.DiscountPercent));
    }

    #endregion

    #region Refusals

    /// <summary>Negative: a hotel with no room cannot have its occupancy read, so nothing is sent and the run is refused.</summary>
    [Fact]
    public async Task ExecuteAsync_NoRooms_FailsAsInputRejectedWithoutCallingTheService()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisBookings.RoomCount = 0;
        Guid runId = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        AssertFailed(world, runId, ErrorCodes.MarketAnalysisRunner.InputRejected);
        Assert.Equal(0, world.MarketAnalysisClient.Calls);
    }

    /// <summary>Negative: with no booking nothing is sent and the run fails with its code.</summary>
    [Fact]
    public async Task ExecuteAsync_NoBookings_FailsWithoutCallingTheService()
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisBookings.Bookings.Clear();
        Guid runId = Begin(world);

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
    [InlineData("market_analysis_runner.unauthorized")]
    [InlineData("market_analysis_runner.already_running")]
    [InlineData("market_analysis_runner.run_failed:NO_DATA")]
    public async Task ExecuteAsync_ServiceFails_FailsWithTheCodeAndStoresNothing(string code)
    {
        // Arrange
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisClient.FailWith = code;
        Guid runId = Begin(world);

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
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult(version);
        Guid runId = Begin(world);

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
        ServiceWorld world = new ServiceWorld();
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult() with { Content = null };
        Guid runId = Begin(world);

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
        ServiceWorld world = new ServiceWorld();
        await world.MarketAnalysisRunner().ExecuteAsync(Begin(world), TestContext.Current.CancellationToken);
        world.MarketAnalysisClient.FailWith = ErrorCodes.MarketAnalysisRunner.Unavailable;
        Guid second = Begin(world);

        // Act
        await world.MarketAnalysisRunner().ExecuteAsync(second, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(world.MarketAnalysisRuns.Items);
        // The tracker keeps every run it began, so Get finds this one.
        Assert.Equal(MarketAnalysisRunStates.Failed, world.MarketAnalysisTracker.Get(second)!.State);
    }

    #endregion

    #region Helpers

    private static Guid Begin(ServiceWorld world) => world.MarketAnalysisTracker.TryBegin().Value.RunId;

    private static void AssertFailed(ServiceWorld world, Guid runId, string code)
    {
        // The tracker keeps every run it began, so Get finds this one.
        MarketAnalysisRunStateDto state = world.MarketAnalysisTracker.Get(runId)!;
        Assert.Equal((MarketAnalysisRunStates.Failed, code), (state.State, state.Error));
        Assert.Empty(world.MarketAnalysisRuns.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion
}
