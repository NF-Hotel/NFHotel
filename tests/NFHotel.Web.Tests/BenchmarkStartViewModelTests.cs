using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>Starting a benchmark run from the back office and following it until its result is stored (MIL-020).</summary>
public sealed class BenchmarkStartViewModelTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Queued = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region Choosing

    /// <summary>Positive: the installed models are loaded and none is chosen yet.</summary>
    [Fact]
    public async Task InitializeAsync_ServiceUp_LoadsTheModelsAndChoosesNone()
    {
        // Arrange
        var viewModel = Build(out _);

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["m1", "m2", "m3", "m4", "m5"], viewModel.Models);
        Assert.Empty(viewModel.SelectedModels);
        Assert.False(viewModel.CanStart);
        Assert.True(viewModel.IsLoaded);
    }

    /// <summary>Negative: a service that cannot be reached shows the text and offers no model.</summary>
    [Fact]
    public async Task InitializeAsync_ServiceDown_ShowsTheTextAndNoModels()
    {
        // Arrange
        var viewModel = Build(out var client);
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(viewModel.Models);
        Assert.Equal("We couldn't reach the benchmark service. Please try again in a moment.", Assert.Single(viewModel.Errors));
        Assert.False(viewModel.CanStart);
    }

    /// <summary>Boundary: one to four models can be chosen and a fifth is not taken.</summary>
    [Fact]
    public async Task ToggleModel_FiveModels_TakesFourAndIgnoresTheFifth()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        foreach (var model in new[] { "m1", "m2", "m3", "m4", "m5" })
        {
            viewModel.ToggleModel(model);
        }

        // Assert
        Assert.Equal(["m1", "m2", "m3", "m4"], viewModel.SelectedModels);
        Assert.True(viewModel.CanStart);
    }

    /// <summary>State: choosing a model twice removes it again, and a name that is not offered is ignored.</summary>
    [Fact]
    public async Task ToggleModel_SameModelTwiceAndUnknownModel_RemovesAndIgnores()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        viewModel.ToggleModel("m1");
        viewModel.ToggleModel("m1");
        viewModel.ToggleModel("nope");

        // Assert
        Assert.Empty(viewModel.SelectedModels);
    }

    /// <summary>Equivalence: a start needs at least one analysis and one to five runs.</summary>
    [Theory]
    [InlineData(true, false, 1, true)]
    [InlineData(false, true, 5, true)]
    [InlineData(false, false, 1, false)]
    [InlineData(true, false, 0, false)]
    [InlineData(true, false, 6, false)]
    public async Task CanStart_OptionsAndRuns_FollowsTheLimits(bool hotel, bool tasks, int runs, bool expected)
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        viewModel.HotelAnalysis = hotel;
        viewModel.DeterministicTasks = tasks;
        viewModel.Runs = runs;

        // Act and assert
        Assert.Equal(expected, viewModel.CanStart);
    }

    #endregion

    #region Starting

    /// <summary>Positive: a start sends the chosen models and options and holds the queued run.</summary>
    [Fact]
    public async Task StartAsync_ModelsChosen_SendsTheRequestAndHoldsTheQueuedRun()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m2");
        viewModel.ToggleModel("m1");
        viewModel.DeterministicTasks = true;
        viewModel.Runs = 2;

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.Received(1).StartRunAsync(
            Arg.Is<StartBenchmarkRunRequest>(request =>
                request.Models.SequenceEqual(new[] { "m2", "m1" }) && request.HotelAnalysis && request.DeterministicTasks && request.Runs == 2),
            Arg.Any<CancellationToken>());
        Assert.Equal(BenchmarkRunStates.Queued, viewModel.CurrentRun!.State);
        Assert.True(viewModel.IsFollowing);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a start with nothing chosen calls nothing.</summary>
    [Fact]
    public async Task StartAsync_NothingChosen_CallsNothing()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.DidNotReceive().StartRunAsync(Arg.Any<StartBenchmarkRunRequest>(), Arg.Any<CancellationToken>());
        Assert.Null(viewModel.CurrentRun);
    }

    /// <summary>Negative: a full queue shows its text and starts nothing.</summary>
    [Fact]
    public async Task StartAsync_QueueFull_ShowsTheTextAndHoldsNoRun()
    {
        // Arrange
        var viewModel = Build(out var client);
        client.StartRunAsync(Arg.Any<StartBenchmarkRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.QueueFull));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("A run is active and others are waiting. Try again in a few minutes.", Assert.Single(viewModel.Errors));
        Assert.Null(viewModel.CurrentRun);
        Assert.False(viewModel.IsFollowing);
    }

    /// <summary>State: while a run is being followed a second start is not offered.</summary>
    [Fact]
    public async Task CanStart_RunBeingFollowed_IsFalse()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Act and assert
        Assert.False(viewModel.CanStart);
    }

    #endregion

    #region Following

    /// <summary>State transition: the run is followed until it is done and its result is delivered, then following stops.</summary>
    [Fact]
    public async Task RefreshAsync_RunToTheEnd_StopsFollowingWhenTheResultIsDelivered()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(
            Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady)),
            Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Done, BenchmarkDeliveryStates.Pending)),
            Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Done, BenchmarkDeliveryStates.Delivered)));

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        var runningFollowed = viewModel.IsFollowing;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        var pendingFollowed = viewModel.IsFollowing;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(runningFollowed);
        Assert.True(pendingFollowed);
        Assert.False(viewModel.IsFollowing);
        Assert.True(viewModel.ResultIsStored);
        Assert.True(viewModel.CanStart);
    }

    /// <summary>Negative: a failed run shows its reason and stops following.</summary>
    [Fact]
    public async Task RefreshAsync_FailedRun_KeepsTheReasonAndStopsFollowing()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(
            Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Failed, BenchmarkDeliveryStates.NotReady) with { Error = "dataset missing" }));

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("dataset missing", viewModel.CurrentRun!.Error);
        Assert.False(viewModel.IsFollowing);
        Assert.False(viewModel.ResultIsStored);
    }

    /// <summary>
    /// Negative: a service that forgot the run (it restarted) shows the text, records the run as interrupted and stops
    /// following; the choice stays, so the same run can be started again (ADR-0031).
    /// </summary>
    [Fact]
    public async Task RefreshAsync_RunForgotten_RecordsInterruptedAndKeepsTheChoice()
    {
        // Arrange
        var viewModel = Build(out var client, out var requests);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("was restarted and the run was stopped", Assert.Single(viewModel.Errors), StringComparison.Ordinal);
        Assert.False(viewModel.IsFollowing);
        await requests.Received(1).SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request => request.State == BenchmarkRunStates.Interrupted),
            Arg.Any<CancellationToken>());
        Assert.Equal(["m1"], viewModel.SelectedModels);
        Assert.True(viewModel.CanStart);
    }

    /// <summary>State: a short failure to reach the service keeps following, so the next look can succeed.</summary>
    [Fact]
    public async Task RefreshAsync_ServiceBlip_ShowsTheTextAndKeepsFollowing()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(viewModel.Errors);
        Assert.True(viewModel.IsFollowing);
    }

    /// <summary>Boundary: refreshing with no run does nothing and calls nothing.</summary>
    [Fact]
    public async Task RefreshAsync_NoRun_CallsNothing()
    {
        // Arrange
        var viewModel = Build(out var client);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.DidNotReceive().GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Recorded runs (ADR-0031)

    /// <summary>Positive: a started run is recorded through the API with its choice.</summary>
    [Fact]
    public async Task StartAsync_Accepted_RecordsTheRun()
    {
        // Arrange
        var viewModel = Build(out _, out var requests);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m2");
        viewModel.Runs = 3;

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await requests.Received(1).RecordAsync(
            Arg.Is<RecordBenchmarkRequest>(request => request.RunId == RunId && request.Models.SequenceEqual(new[] { "m2" }) && request.Runs == 3),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Error handling: a run that cannot be recorded is still followed, and the page says why.</summary>
    [Fact]
    public async Task StartAsync_RecordFails_FollowsTheRunAndShowsTheText()
    {
        // Arrange
        var viewModel = Build(out _, out var requests);
        requests.RecordAsync(Arg.Any<RecordBenchmarkRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRequestDto>.Failure(ErrorCodes.Api.Unavailable));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsFollowing);
        Assert.Equal("We couldn't reach the hotel system. Please try again in a moment.", Assert.Single(viewModel.Errors));
    }

    /// <summary>State: a run that is still going when the page opens is followed again, with its choice shown.</summary>
    [Fact]
    public async Task OpenAsync_RunStillGoing_FollowsIt()
    {
        // Arrange
        var viewModel = Build(out var client, out _, BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Running));
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady)));

        // Act
        await viewModel.OpenAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsFollowing);
        Assert.Equal(["m1", "m2"], viewModel.SelectedModels);
        Assert.Null(viewModel.StoppedRun);
    }

    /// <summary>
    /// State: a run that a restart stopped while nobody looked is shown as interrupted when the page opens, and its choice
    /// can be taken to start it again.
    /// </summary>
    [Fact]
    public async Task OpenAsync_RunForgottenWhileAway_ShowsItInterrupted()
    {
        // Arrange
        var viewModel = Build(out var client, out _, BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Queued));
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        await viewModel.OpenAsync(null, TestContext.Current.CancellationToken);
        var stopped = viewModel.StoppedRun;
        viewModel.UseChoiceOf(stopped!);

        // Assert
        Assert.Equal(BenchmarkRunStates.Interrupted, stopped?.State);
        Assert.False(viewModel.IsFollowing);
        Assert.Equal(["m1", "m2"], viewModel.SelectedModels);
        Assert.Equal(2, viewModel.Runs);
        Assert.True(viewModel.CanStart);
        Assert.Null(viewModel.StoppedRun);
    }

    /// <summary>Edge: the choice of a run named by the list is taken; a model that is no longer installed is left out.</summary>
    [Fact]
    public async Task OpenAsync_AgainRunWithAModelGone_TakesTheRestOfTheChoice()
    {
        // Arrange
        var record = BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Interrupted) with { Models = ["m2", "gone"] };
        var viewModel = Build(out _, out _, record);

        // Act
        await viewModel.OpenAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["m2"], viewModel.SelectedModels);
        Assert.True(viewModel.CanStart);
        Assert.Null(viewModel.StoppedRun);
    }

    /// <summary>Error handling: when the recorded runs cannot be read, the page still offers the models.</summary>
    [Fact]
    public async Task OpenAsync_RecordsUnavailable_StillOffersTheModels()
    {
        // Arrange
        var viewModel = Build(out _, out var requests);
        requests.GetRecentAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRequestDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.OpenAsync(null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, viewModel.Models.Count);
        Assert.Empty(viewModel.Errors);
        Assert.Null(viewModel.StoppedRun);
    }

    #endregion

    #region Helpers

    private static BenchmarkRunStateDto State(string state, string delivery) =>
        new(RunId, state, delivery, ["m1"], Queued, null, null, null);

    private static BenchmarkStartViewModel Build(out IBenchmarkServiceClient client) => Build(out client, out _);

    private static BenchmarkStartViewModel Build(
        out IBenchmarkServiceClient client,
        out IBenchmarkRequestService requests,
        params BenchmarkRequestDto[] recent)
    {
        var fake = Substitute.For<IBenchmarkServiceClient>();
        fake.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["m1", "m2", "m3", "m4", "m5"]));
        fake.StartRunAsync(Arg.Any<StartBenchmarkRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Queued, BenchmarkDeliveryStates.NotReady)));
        client = fake;

        var runner = Substitute.For<IUseCaseRunner<IBenchmarkServiceClient>>();
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<BenchmarkRunStateDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<BenchmarkRunStateDto>>>>()(fake, call.Arg<CancellationToken>()));
        requests = BenchmarkRequestDoubles.Requests(recent);
        var tracker = new BenchmarkRunTracker(runner, new PassThroughRunner<IBenchmarkRequestService>(requests));
        return new BenchmarkStartViewModel(runner, tracker);
    }

    #endregion
}
