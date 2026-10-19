using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Starting the market analysis, storing the result of a completed run once, and reading the latest report (MIL-026).</summary>
public sealed class MarketAnalysisServiceTests
{
    private static readonly Guid RunId = FakeMarketAnalysisServiceClient.RunId;

    #region Start

    /// <summary>Positive: a start queues a run on the service and stores nothing.</summary>
    [Fact]
    public async Task StartRunAsync_ServiceUp_ReturnsTheQueuedRunAndStoresNothing()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.MarketAnalysisService().StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((RunId, MarketAnalysisRunStates.Queued), (result.Value.RunId, result.Value.State));
        Assert.Equal(1, world.MarketAnalysisClient.StartCount);
        Assert.Empty(world.MarketAnalysisRuns.Items);
    }

    /// <summary>Negative: a run that is already active is passed on with its own code.</summary>
    [Fact]
    public async Task StartRunAsync_AlreadyRunning_ReturnsTheCode()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.FailWith = ErrorCodes.MarketAnalysisRunner.AlreadyRunning;

        // Act
        var result = await world.MarketAnalysisService().StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region State and storing

    /// <summary>Positive: a run that is still running stores nothing and does not read the result.</summary>
    [Theory]
    [InlineData(MarketAnalysisRunStates.Queued)]
    [InlineData(MarketAnalysisRunStates.Running)]
    [InlineData(MarketAnalysisRunStates.Failed)]
    public async Task GetRunStateAsync_NotCompleted_ReturnsTheStateAndStoresNothing(string state)
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = state;

        // Act
        var result = await world.MarketAnalysisService().GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(state, result.Value.State);
        Assert.Empty(world.MarketAnalysisRuns.Items);
        Assert.Equal(0, world.MarketAnalysisClient.ResultReads);
    }

    /// <summary>Positive: the first read of a completed run reads the result and stores it with its header.</summary>
    [Fact]
    public async Task GetRunStateAsync_Completed_StoresTheResultOnce()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;

        // Act
        var result = await world.MarketAnalysisService().GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.MarketAnalysisRuns.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal((RunId, 1, "ollama", "phi3.5:latest"), (stored.RunId, stored.SchemaVersion, stored.Provider, stored.Model));
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>State: the page polls, so a second read of a stored run neither reads the result nor stores it again.</summary>
    [Fact]
    public async Task GetRunStateAsync_CompletedTwice_StoresOnceAndReadsTheResultOnce()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;
        var service = world.MarketAnalysisService();

        // Act
        await service.GetRunStateAsync(RunId, TestContext.Current.CancellationToken);
        var second = await service.GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.Single(world.MarketAnalysisRuns.Items);
        Assert.Equal(1, world.MarketAnalysisClient.ResultReads);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a result with an unknown schema version is refused and not stored.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task GetRunStateAsync_UnknownSchemaVersion_RefusesAndStoresNothing(int version)
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult(version);

        // Act
        var result = await world.MarketAnalysisService().GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.SchemaVersionUnsupported, Assert.Single(result.Errors).Code);
        Assert.Empty(world.MarketAnalysisRuns.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a result without content is refused as an invalid body.</summary>
    [Fact]
    public async Task GetRunStateAsync_NoContent_RefusesAsInvalidBody()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;
        world.MarketAnalysisClient.Result = FakeMarketAnalysisServiceClient.SampleResult() with { Content = null };

        // Act
        var result = await world.MarketAnalysisService().GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.BodyInvalid, Assert.Single(result.Errors).Code);
        Assert.Empty(world.MarketAnalysisRuns.Items);
    }

    /// <summary>Negative: the empty id and an unknown run are not found, and the service is not asked for the empty id.</summary>
    [Fact]
    public async Task GetRunStateAsync_EmptyAndUnknownId_AreNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();

        // Act
        var empty = await service.GetRunStateAsync(Guid.Empty, TestContext.Current.CancellationToken);
        var unknown = await service.GetRunStateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(empty.Errors).Code);
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(unknown.Errors).Code);
    }

    /// <summary>Negative: a service that is down is passed on and nothing is stored.</summary>
    [Fact]
    public async Task GetRunStateAsync_ServiceDown_ReturnsTheCode()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.FailWith = ErrorCodes.MarketAnalysisRunner.Unavailable;

        // Act
        var result = await world.MarketAnalysisService().GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
        Assert.Empty(world.MarketAnalysisRuns.Items);
    }

    #endregion

    #region Latest report

    /// <summary>Negative: with no stored run the report is not found, so the page can say so.</summary>
    [Fact]
    public async Task GetLatestReportAsync_NothingStored_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.MarketAnalysisService().GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.NotFound, Assert.Single(result.Errors).Code);
    }

    /// <summary>Positive: the stored content comes back as the summary, the figures and the series that were delivered.</summary>
    [Fact]
    public async Task GetLatestReportAsync_StoredRun_ReturnsTheDeliveredContent()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;
        var service = world.MarketAnalysisService();
        await service.GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Act
        var result = await service.GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        var report = result.Value;
        var series = Assert.Single(report.Content.Series);
        Assert.Equal((RunId, "phi3.5:latest"), (report.RunId, report.Model));
        Assert.StartsWith("Portugal is the largest market.", report.Content.Summary, StringComparison.Ordinal);
        Assert.Equal("Cancellation rate", Assert.Single(report.Content.Figures).Label);
        Assert.Equal("bookings_by_country", series.Id);
        Assert.Equal(["Portugal", "United Kingdom"], series.Points.Select(point => point.Label));
    }

    /// <summary>State: of two stored runs the one that finished last is the report.</summary>
    [Fact]
    public async Task GetLatestReportAsync_TwoRuns_ReturnsTheOneThatFinishedLast()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisRuns.Add(NFHotel.Domain.Entities.MarketAnalysisRun.Create(
            Guid.NewGuid(), 1, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 1, 0, 5, 0, TimeSpan.Zero), null, "old", "{\"summary\":\"old\",\"figures\":[],\"series\":[]}"));
        world.MarketAnalysisRuns.Add(NFHotel.Domain.Entities.MarketAnalysisRun.Create(
            Guid.NewGuid(), 1, new DateTimeOffset(2026, 10, 9, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 9, 0, 5, 0, TimeSpan.Zero), null, "new", "{\"summary\":\"new\",\"figures\":[],\"series\":[]}"));

        // Act
        var result = await world.MarketAnalysisService().GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(("new", "new"), (result.Value.Model, result.Value.Content.Summary));
    }

    /// <summary>Negative: stored content that cannot be read is an invalid body and never an exception.</summary>
    [Fact]
    public async Task GetLatestReportAsync_UnreadableContent_ReturnsInvalidBody()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisRuns.Add(NFHotel.Domain.Entities.MarketAnalysisRun.Create(
            Guid.NewGuid(), 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null, null, "not json"));

        // Act
        var result = await world.MarketAnalysisService().GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.BodyInvalid, Assert.Single(result.Errors).Code);
    }

    #endregion
}
