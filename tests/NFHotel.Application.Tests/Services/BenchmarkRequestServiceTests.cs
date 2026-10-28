using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Recording the benchmark runs a Manager starts and the states the service reports (ADR-0031).</summary>
public sealed class BenchmarkRequestServiceTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly RecordBenchmarkRequest Choice = new(RunId, ["m1", "m2"], true, false, 2);

    #region RecordAsync

    /// <summary>Positive: a started run is stored once, queued, with the user and the clock time.</summary>
    [Fact]
    public async Task RecordAsync_NewRun_StoresItQueued()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkRequestService().RecordAsync(Choice, "mona", TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.BenchmarkRequests.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal((RunId, BenchmarkRequest.Queued, "mona", world.Clock.UtcNow), (stored.RunId, stored.State, stored.RequestedBy, stored.RequestedAt));
        Assert.Equal(["m1", "m2"], result.Value.Models);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>State: recording the same run again changes nothing and saves nothing.</summary>
    [Fact]
    public async Task RecordAsync_SameRunTwice_KeepsOneRecord()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkRequestService();
        await service.RecordAsync(Choice, "mona", TestContext.Current.CancellationToken);

        // Act
        var again = await service.RecordAsync(Choice with { Runs = 5 }, "other", TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(world.BenchmarkRequests.Items);
        Assert.Equal((2, "mona"), (again.Value.Runs, again.Value.RequestedBy));
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an invalid choice is refused with the codes and nothing is stored.</summary>
    [Fact]
    public async Task RecordAsync_InvalidChoice_ReturnsTheCodes()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkRequestService().RecordAsync(Choice with { Models = [] }, "mona", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(BenchmarkRequestErrors.ModelsInvalid, result.Errors.Select(error => error.Code));
        Assert.Empty(world.BenchmarkRequests.Items);
    }

    #endregion

    #region SetStateAsync

    /// <summary>State: a reported state is recorded with its error and saved.</summary>
    [Fact]
    public async Task SetStateAsync_AllowedChange_IsSaved()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkRequestService();
        await service.RecordAsync(Choice, "mona", TestContext.Current.CancellationToken);

        // Act
        var result = await service.SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Interrupted, "restarted"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((BenchmarkRunStates.Interrupted, "restarted"), (result.Value.State, result.Value.Error));
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: the same state again succeeds without a save.</summary>
    [Fact]
    public async Task SetStateAsync_SameState_SavesNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkRequestService();
        await service.RecordAsync(Choice, "mona", TestContext.Current.CancellationToken);

        // Act
        var result = await service.SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Queued), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a refused change returns the code and saves nothing.</summary>
    [Fact]
    public async Task SetStateAsync_RefusedChange_ReturnsTheCode()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkRequestService();
        await service.RecordAsync(Choice, "mona", TestContext.Current.CancellationToken);
        await service.SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Interrupted), TestContext.Current.CancellationToken);

        // Act
        var result = await service.SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Running), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRequestErrors.StateChangeRefused, Assert.Single(result.Errors).Code);
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a run that was never recorded is not found.</summary>
    [Fact]
    public async Task SetStateAsync_UnknownRun_IsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkRequestService().SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Running), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region GetRecentAsync

    /// <summary>Boundary: the list holds the runs of the last seven days that are not done, newest first.</summary>
    [Fact]
    public async Task GetRecentAsync_MixedRuns_ListsTheRecentNotDoneNewestFirst()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkRequestService();
        var now = world.Clock.UtcNow;
        Guid Id(int index) => new($"00000000-0000-4000-8000-00000000000{index}");
        world.BenchmarkRequests.Add(BenchmarkRequest.Create(Id(1), ["m1"], true, false, 1, "mona", now.AddDays(-7)));
        world.BenchmarkRequests.Add(BenchmarkRequest.Create(Id(2), ["m1"], true, false, 1, "mona", now.AddDays(-7).AddTicks(-1)));
        world.BenchmarkRequests.Add(BenchmarkRequest.Create(Id(3), ["m1"], true, false, 1, "mona", now.AddHours(-1)));
        var done = BenchmarkRequest.Create(Id(4), ["m1"], true, false, 1, "mona", now);
        done.ChangeState(BenchmarkRequest.Done, null, now);
        world.BenchmarkRequests.Add(done);

        // Act
        var result = await service.GetRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([Id(3), Id(1)], result.Value.Select(request => request.RunId));
    }

    #endregion
}
