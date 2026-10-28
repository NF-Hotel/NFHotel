using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The records of started benchmark runs against a real PostgreSQL, after the migration: the models as a text array, one
/// record per run, the state changes and the list of recent runs that are not done (ADR-0031).
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BenchmarkRequestRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2026, 10, 26, 0, 9, 0, TimeSpan.Zero);
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    private readonly PostgreSqlFixture _fixture;

    public BenchmarkRequestRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Positive: a stored record is found by its run id with every field, the models in their order.</summary>
    [Fact]
    public async Task FindByRunIdAsync_StoredRecord_ReturnsItWithItsFields()
    {
        // Arrange
        await AddAsync(RunId, At, "phi4-mini:latest", "llama3.1:latest");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new BenchmarkRequestRepository(context).FindByRunIdAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(["phi4-mini:latest", "llama3.1:latest"], found.Models);
        Assert.Equal((true, false, 2, "mona", At, BenchmarkRequest.Queued), (found.HotelAnalysis, found.DeterministicTasks, found.Runs, found.RequestedBy, found.RequestedAt, found.State));
    }

    /// <summary>Negative: a second record of the same run is refused by the unique index.</summary>
    [Fact]
    public async Task Add_SameRunTwice_IsRefusedByTheDatabase()
    {
        // Arrange
        await AddAsync(RunId, At, "m1");
        await using var context = _fixture.CreateContext();
        new BenchmarkRequestRepository(context).Add(BenchmarkRequest.Create(RunId, ["m2"], true, false, 1, "other", At));

        // Act
        var save = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    /// <summary>State: a change of state is saved on the same row, with the error and the time.</summary>
    [Fact]
    public async Task ChangeState_StoredRecord_IsSaved()
    {
        // Arrange
        await AddAsync(RunId, At, "m1");
        await using (var context = _fixture.CreateContext())
        {
            var stored = await new BenchmarkRequestRepository(context).FindByRunIdAsync(RunId, TestContext.Current.CancellationToken);

            // Act
            stored!.ChangeState(BenchmarkRequest.Interrupted, "restarted", At.AddMinutes(12));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = _fixture.CreateContext();
        var value = Assert.Single(check.BenchmarkRequests);
        Assert.Equal((BenchmarkRequest.Interrupted, "restarted", At.AddMinutes(12)), (value.State, value.Error, value.UpdatedAt));
    }

    /// <summary>Boundary: the list holds the records from the given moment on that are not done, newest first.</summary>
    [Fact]
    public async Task ListNotDoneSinceAsync_MixedRecords_ReturnsTheRecentNotDoneNewestFirst()
    {
        // Arrange
        var older = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000002");
        var atLimit = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000003");
        var done = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000004");
        await AddAsync(older, At.AddDays(-8), "m1");
        await AddAsync(atLimit, At.AddDays(-7), "m1");
        await AddAsync(RunId, At, "m1");
        await AddAsync(done, At.AddMinutes(1), "m1");
        await using (var context = _fixture.CreateContext())
        {
            var finished = await new BenchmarkRequestRepository(context).FindByRunIdAsync(done, TestContext.Current.CancellationToken);
            finished!.ChangeState(BenchmarkRequest.Done, null, At.AddMinutes(30));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var check = _fixture.CreateContext();

        // Act
        var list = await new BenchmarkRequestRepository(check).ListNotDoneSinceAsync(At.AddDays(-7), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([RunId, atLimit], list.Select(request => request.RunId));
    }

    private async Task AddAsync(Guid runId, DateTimeOffset at, params string[] models)
    {
        await using var context = _fixture.CreateContext();
        new BenchmarkRequestRepository(context).Add(BenchmarkRequest.Create(runId, models, true, false, 2, "mona", at));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
