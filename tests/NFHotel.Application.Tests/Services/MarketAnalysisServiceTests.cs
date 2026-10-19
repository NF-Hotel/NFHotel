using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Starting the market analysis in the background, following its run and reading the latest report (MIL-026).</summary>
public sealed class MarketAnalysisServiceTests
{
    #region Start and state

    /// <summary>Positive: a start returns at once with a queued run and hands it to the launcher; nothing has run yet.</summary>
    [Fact]
    public async Task StartRunAsync_NoRunActive_ReturnsTheQueuedRunAndLaunchesIt()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.MarketAnalysisService().StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MarketAnalysisRunStates.Queued, result.Value.State);
        Assert.Equal([result.Value.RunId], world.MarketAnalysisLauncher.Launched);
        Assert.Equal(0, world.MarketAnalysisClient.Calls);
        Assert.Empty(world.MarketAnalysisRuns.Items);
    }

    /// <summary>Negative: a second start while a run is active is refused and launches nothing.</summary>
    [Fact]
    public async Task StartRunAsync_RunActive_IsRefusedAndLaunchesNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();
        await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken);

        // Act
        var second = await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, Assert.Single(second.Errors).Code);
        Assert.Single(world.MarketAnalysisLauncher.Launched);
    }

    /// <summary>State: when the run has ended a new start is allowed, with a new run id.</summary>
    [Fact]
    public async Task StartRunAsync_AfterTheRunEnded_StartsANewRun()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();
        var first = (await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken)).Value;
        await world.MarketAnalysisRunner().ExecuteAsync(first.RunId, TestContext.Current.CancellationToken);

        // Act
        var second = await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.RunId, second.Value.RunId);
        Assert.Equal(2, world.MarketAnalysisLauncher.Launched.Count);
    }

    /// <summary>Positive: the state follows the run from queued to running to completed.</summary>
    [Fact]
    public async Task GetRunStateAsync_RunInProgress_FollowsTheRun()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();
        var runId = (await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken)).Value.RunId;
        world.MarketAnalysisClient.Gate = new TaskCompletionSource();

        // Act
        var queued = await service.GetRunStateAsync(runId, TestContext.Current.CancellationToken);
        var work = world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);
        var running = await service.GetRunStateAsync(runId, TestContext.Current.CancellationToken);
        world.MarketAnalysisClient.Gate.SetResult();
        await work;
        var completed = await service.GetRunStateAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [MarketAnalysisRunStates.Queued, MarketAnalysisRunStates.Running, MarketAnalysisRunStates.Completed],
            [queued.Value.State, running.Value.State, completed.Value.State]);
        Assert.NotNull(completed.Value.FinishedAt);
    }

    /// <summary>Negative: an unknown run and the empty id are not found.</summary>
    [Fact]
    public async Task GetRunStateAsync_UnknownRun_IsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();
        await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken);

        // Act
        var unknown = await service.GetRunStateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var empty = await service.GetRunStateAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(unknown.Errors).Code);
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(empty.Errors).Code);
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

    /// <summary>Positive: after a run the report carries the figures, the sections, the insight and the model.</summary>
    [Fact]
    public async Task GetLatestReportAsync_AfterARun_ReturnsTheDeliveredContent()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.MarketAnalysisService();
        var runId = (await service.StartRunAsync(new StartMarketAnalysisRequest(), TestContext.Current.CancellationToken)).Value.RunId;
        await world.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);

        // Act
        var result = await service.GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        var report = result.Value;
        var cancellations = report.Content.Sections[0];
        Assert.Equal((runId, "ollama", "phi3.5:latest"), (report.RunId, report.Provider, report.Model));
        Assert.Equal("Bookings analysed", Assert.Single(report.Content.Figures).Label);
        Assert.Equal(["cancellations", "room_value"], report.Content.Sections.Select(section => section.Id));
        Assert.Equal("Non-refundable bookings are cancelled most often.", cancellations.Insight!.ExecutiveSummary);
        Assert.Equal(1200, Assert.Single(cancellations.Insight.Suggestions).SampleSize);
        Assert.Equal(["No Deposit", "Non Refund"], Assert.Single(cancellations.Series).Points.Select(point => point.Label));
        Assert.Equal("Missing field: price_per_night", report.Content.Sections[1].Reason);
    }

    /// <summary>State: of two stored runs the one that finished last is the report.</summary>
    [Fact]
    public async Task GetLatestReportAsync_TwoRuns_ReturnsTheOneThatFinishedLast()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisRuns.Add(Stored("old", new DateTimeOffset(2026, 10, 1, 0, 5, 0, TimeSpan.Zero)));
        world.MarketAnalysisRuns.Add(Stored("new", new DateTimeOffset(2026, 10, 9, 0, 5, 0, TimeSpan.Zero)));

        // Act
        var result = await world.MarketAnalysisService().GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("new", result.Value.Model);
    }

    /// <summary>Negative: stored content that cannot be read is an invalid body and never an exception.</summary>
    [Fact]
    public async Task GetLatestReportAsync_UnreadableContent_ReturnsInvalidBody()
    {
        // Arrange
        var world = new ServiceWorld();
        world.MarketAnalysisRuns.Add(MarketAnalysisRun.Create(Guid.NewGuid(), 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null, null, "not json"));

        // Act
        var result = await world.MarketAnalysisService().GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.BodyInvalid, Assert.Single(result.Errors).Code);
    }

    #endregion

    private static MarketAnalysisRun Stored(string model, DateTimeOffset finishedAt) =>
        MarketAnalysisRun.Create(Guid.NewGuid(), 1, finishedAt.AddMinutes(-5), finishedAt, null, model, "{\"figures\":[],\"sections\":[]}");
}
