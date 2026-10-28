using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>Keeping the hotel's record of the benchmark runs in step with the benchmark service (ADR-0031).</summary>
public sealed class BenchmarkRunTrackerTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly Guid OtherRunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000002");

    #region RecordStartedAsync

    /// <summary>Positive: a started run is recorded with its id and the whole choice.</summary>
    [Fact]
    public async Task RecordStartedAsync_StartedRun_RecordsTheChoice()
    {
        // Arrange
        var tracker = Build(out _, out var requests);

        // Act
        var result = await tracker.RecordStartedAsync(State(BenchmarkRunStates.Queued), new StartBenchmarkRunRequest(["m1", "m2"], true, true, 3), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await requests.Received(1).RecordAsync(
            Arg.Is<RecordBenchmarkRequest>(request =>
                request.RunId == RunId && request.Models.SequenceEqual(new[] { "m1", "m2" }) && request.DeterministicTasks && request.Runs == 3),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a record the API refuses is returned as a failure, with its code.</summary>
    [Fact]
    public async Task RecordStartedAsync_ApiDown_ReturnsTheFailure()
    {
        // Arrange
        var tracker = Build(out _, out var requests);
        requests.RecordAsync(Arg.Any<RecordBenchmarkRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<BenchmarkRequestDto>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        var result = await tracker.RecordStartedAsync(State(BenchmarkRunStates.Queued), new StartBenchmarkRunRequest(["m1"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region RefreshAsync

    /// <summary>State: a new state is recorded with its error.</summary>
    [Fact]
    public async Task RefreshAsync_StateChanged_RecordsIt()
    {
        // Arrange
        var tracker = Build(out var client, out var requests);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Failed, "out of memory")));

        // Act
        var result = await tracker.RefreshAsync(RunId, BenchmarkRunStates.Running, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRunStates.Failed, result.Value.State);
        await requests.Received(1).SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request => request.State == BenchmarkRunStates.Failed && request.Error == "out of memory"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Equivalence: the same state as known records nothing, so following a run does not write every few seconds.</summary>
    [Fact]
    public async Task RefreshAsync_SameState_RecordsNothing()
    {
        // Arrange
        var tracker = Build(out var client, out var requests);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Success(State(BenchmarkRunStates.Running)));

        // Act
        await tracker.RefreshAsync(RunId, BenchmarkRunStates.Running, TestContext.Current.CancellationToken);

        // Assert
        await requests.DidNotReceive().SetStateAsync(Arg.Any<Guid>(), Arg.Any<SetBenchmarkRequestStateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a run the service forgot is recorded as interrupted, and the failure is returned.</summary>
    [Fact]
    public async Task RefreshAsync_RunForgotten_RecordsInterrupted()
    {
        // Arrange
        var tracker = Build(out var client, out var requests);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        var result = await tracker.RefreshAsync(RunId, BenchmarkRunStates.Queued, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        await requests.Received(1).SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request =>
                request.State == BenchmarkRunStates.Interrupted && request.Error == BenchmarkRunTracker.InterruptedReason),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a service that cannot be reached records nothing; the next look tries again.</summary>
    [Fact]
    public async Task RefreshAsync_ServiceDown_RecordsNothing()
    {
        // Arrange
        var tracker = Build(out var client, out var requests);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        await tracker.RefreshAsync(RunId, BenchmarkRunStates.Queued, TestContext.Current.CancellationToken);

        // Assert
        await requests.DidNotReceive().SetStateAsync(Arg.Any<Guid>(), Arg.Any<SetBenchmarkRequestStateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// State (MIL-046): a run the service forgot whose result is stored is recorded done, and the answer is the stored
    /// run, delivered.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_RunForgottenButStored_RecordsDone()
    {
        // Arrange
        var tracker = BuildWithStored([RunId], out var client, out var requests);
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        var result = await tracker.RefreshAsync(RunId, BenchmarkRunStates.Running, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((BenchmarkRunStates.Done, BenchmarkDeliveryStates.Delivered), (result.Value.State, result.Value.Delivery));
        await requests.Received(1).SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request => request.State == BenchmarkRunStates.Done && request.Error == null),
            Arg.Any<CancellationToken>());
        await requests.DidNotReceive().SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request => request.State == BenchmarkRunStates.Interrupted),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region LoadRecentAsync

    /// <summary>
    /// Integration of the two: an open run the service forgot is shown and recorded as interrupted, a finished run is not
    /// asked about, and an open run the service cannot answer for is shown as it was.
    /// </summary>
    [Fact]
    public async Task LoadRecentAsync_OpenAndFinishedRuns_BringsTheOpenOnesUpToDate()
    {
        // Arrange
        var tracker = Build(
            out var client,
            out var requests,
            BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Queued),
            BenchmarkRequestDoubles.Record(OtherRunId, BenchmarkRunStates.Failed, "out of memory"));
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound));

        // Act
        var result = await tracker.LoadRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [(RunId, BenchmarkRunStates.Interrupted), (OtherRunId, BenchmarkRunStates.Failed)],
            result.Value.Select(record => (record.RunId, record.State)));
        await client.DidNotReceive().GetRunAsync(OtherRunId, Arg.Any<CancellationToken>());
        await requests.Received(1).SetStateAsync(RunId, Arg.Any<SetBenchmarkRequestStateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// State (MIL-046): a run recorded interrupted whose result turns out to be stored becomes done and leaves the list;
    /// one without a stored result stays interrupted.
    /// </summary>
    [Fact]
    public async Task LoadRecentAsync_InterruptedButStored_BecomesDoneAndLeavesTheList()
    {
        // Arrange
        var tracker = BuildWithStored(
            [RunId],
            out var client,
            out var requests,
            BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Interrupted, BenchmarkRunTracker.InterruptedReason),
            BenchmarkRequestDoubles.Record(OtherRunId, BenchmarkRunStates.Interrupted, BenchmarkRunTracker.InterruptedReason));

        // Act
        var result = await tracker.LoadRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OtherRunId, Assert.Single(result.Value).RunId);
        await requests.Received(1).SetStateAsync(
            RunId,
            Arg.Is<SetBenchmarkRequestStateRequest>(request => request.State == BenchmarkRunStates.Done),
            Arg.Any<CancellationToken>());
        await requests.DidNotReceive().SetStateAsync(OtherRunId, Arg.Any<SetBenchmarkRequestStateRequest>(), Arg.Any<CancellationToken>());
        await client.DidNotReceive().GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: an open run whose state cannot be read stays as recorded.</summary>
    [Fact]
    public async Task LoadRecentAsync_ServiceDown_KeepsTheRecord()
    {
        // Arrange
        var tracker = Build(out var client, out _, BenchmarkRequestDoubles.Record(RunId, BenchmarkRunStates.Running));
        client.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        var result = await tracker.LoadRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRunStates.Running, Assert.Single(result.Value).State);
    }

    /// <summary>Negative: a list the API cannot give is returned as the failure.</summary>
    [Fact]
    public async Task LoadRecentAsync_ApiDown_ReturnsTheFailure()
    {
        // Arrange
        var tracker = Build(out _, out var requests);
        requests.GetRecentAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRequestDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        var result = await tracker.LoadRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    private static BenchmarkRunStateDto State(string state, string? error = null) =>
        new(RunId, state, BenchmarkDeliveryStates.NotReady, ["m1"], new DateTimeOffset(2026, 10, 26, 0, 9, 0, TimeSpan.Zero), null, null, error);

    private static BenchmarkRunTracker Build(
        out IBenchmarkServiceClient client,
        out IBenchmarkRequestService requests,
        params BenchmarkRequestDto[] recent) =>
        BuildWithStored([], out client, out requests, recent);

    private static BenchmarkRunTracker BuildWithStored(
        Guid[] storedRuns,
        out IBenchmarkServiceClient client,
        out IBenchmarkRequestService requests,
        params BenchmarkRequestDto[] recent)
    {
        client = Substitute.For<IBenchmarkServiceClient>();
        requests = BenchmarkRequestDoubles.Requests(recent);
        var stored = BenchmarkRequestDoubles.Stored(storedRuns);
        return new BenchmarkRunTracker(
            new PassThroughRunner<IBenchmarkServiceClient>(client),
            new PassThroughRunner<IBenchmarkRequestService>(requests),
            new PassThroughRunner<IBenchmarkService>(stored));
    }
}
