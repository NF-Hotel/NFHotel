using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
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

    /// <summary>Negative: a service that forgot the run shows the text and stops following.</summary>
    [Fact]
    public async Task RefreshAsync_RunForgotten_ShowsTheTextAndStopsFollowing()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("no longer knows this run", Assert.Single(viewModel.Errors), StringComparison.Ordinal);
        Assert.False(viewModel.IsFollowing);
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

    #region Live state

    /// <summary>Positive: the runs of the service are loaded with the models, so a refresh of the page keeps what runs.</summary>
    [Fact]
    public async Task InitializeAsync_RunsInTheService_ShowsThemNewestFirst()
    {
        // Arrange
        var viewModel = Build(out var client);
        var queued = State(BenchmarkRunStates.Queued, BenchmarkDeliveryStates.NotReady) with { RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000002") };
        var running = State(BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady);
        client.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Success([queued, running]));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([BenchmarkRunStates.Queued, BenchmarkRunStates.Running], viewModel.KnownRuns.Select(run => run.State));
        Assert.True(viewModel.IsLive);
    }

    /// <summary>State: a change of state that the service made shows in the next read, also for the run this page started.</summary>
    [Fact]
    public async Task RefreshLiveAsync_RunChangesState_ShowsItAndUpdatesTheRunOfThisPage()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        client.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Success([State(BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady)]));

        // Act
        await viewModel.RefreshLiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRunStates.Running, Assert.Single(viewModel.KnownRuns).State);
        Assert.Equal(BenchmarkRunStates.Running, viewModel.CurrentRun!.State);
    }

    /// <summary>State: a model that was deleted elsewhere leaves the list and the choice; a model that was added joins the list.</summary>
    [Fact]
    public async Task RefreshLiveAsync_ModelsChangedElsewhere_UpdatesTheListAndDropsAGoneChoice()
    {
        // Arrange
        var viewModel = Build(out var client);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m1");
        viewModel.ToggleModel("m2");
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["m2", "m6"]));

        // Act
        await viewModel.RefreshLiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["m2", "m6"], viewModel.Models);
        Assert.Equal(["m2"], viewModel.SelectedModels);
    }

    /// <summary>Negative: a read that fails keeps the last state and says that the live view is paused.</summary>
    [Fact]
    public async Task RefreshLiveAsync_ServiceDown_KeepsTheLastStateAndIsNotLive()
    {
        // Arrange
        var viewModel = Build(out var client);
        client.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Success([State(BenchmarkRunStates.Running, BenchmarkDeliveryStates.NotReady)]));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        client.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        await viewModel.RefreshLiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(viewModel.IsLive);
        Assert.Single(viewModel.KnownRuns);
        Assert.Equal(5, viewModel.Models.Count);
    }

    /// <summary>Boundary: nothing is read while a call of the user is running.</summary>
    [Fact]
    public async Task RefreshLiveAsync_NothingChanged_KeepsTheModelsAndTheChoice()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.ToggleModel("m3");

        // Act
        await viewModel.RefreshLiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["m3"], viewModel.SelectedModels);
        Assert.Equal(5, viewModel.Models.Count);
    }

    #endregion

    #region Helpers

    private static BenchmarkRunStateDto State(string state, string delivery) =>
        new(RunId, state, delivery, ["m1"], Queued, null, null, null);

    private static BenchmarkStartViewModel Build(out IBenchmarkServiceClient client)
    {
        var fake = Substitute.For<IBenchmarkServiceClient>();
        fake.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["m1", "m2", "m3", "m4", "m5"]));
        fake.StartRunAsync(Arg.Any<StartBenchmarkRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Queued, BenchmarkDeliveryStates.NotReady)));
        fake.ListRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunStateDto>>.Success([]));
        client = fake;

        var runner = Substitute.For<IUseCaseRunner<IBenchmarkServiceClient>>();
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<BenchmarkRunStateDto>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<BenchmarkRunStateDto>>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<BenchmarkRunStateDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<BenchmarkRunStateDto>>>>()(fake, call.Arg<CancellationToken>()));
        return new BenchmarkStartViewModel(runner);
    }

    #endregion
}
