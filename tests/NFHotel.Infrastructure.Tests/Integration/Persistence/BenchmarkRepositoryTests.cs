using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The benchmark tables against a real PostgreSQL: idempotency, the reads and the answer kept apart (ADR-0013).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BenchmarkRepositoryTests : IAsyncLifetime
{
    private static readonly Guid FirstRun = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly Guid SecondRun = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public BenchmarkRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Storing

    /// <summary>Positive: a stored run is found by its run id with all its requests.</summary>
    [Fact]
    public async Task FindByRunIdAsync_StoredRun_ReturnsItWithItsRequests()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m1", "m2");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new BenchmarkRunRepository(context).FindByRunIdAsync(FirstRun, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("d.csv", found.DatasetName);
        Assert.Equal(["m1", "m2"], context.BenchmarkResults.Where(result => result.BenchmarkRunId == found.Id).OrderBy(result => result.Id).Select(result => result.Model));
    }

    /// <summary>Negative: an unknown run id finds nothing.</summary>
    [Fact]
    public async Task FindByRunIdAsync_UnknownRunId_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new BenchmarkRunRepository(context).FindByRunIdAsync(FirstRun, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Race: storing the same run id a second time is refused by the unique index, which the service treats as a repeat.</summary>
    [Fact]
    public async Task SaveChangesAsync_SameRunIdTwice_ThrowsAUniqueConstraintViolation()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m1");
        await using var context = _fixture.CreateContext();
        new BenchmarkRunRepository(context).Add(NewRun(FirstRun, Start, "m1"));

        // Act and assert
        var error = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() =>
            new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Equal("ux_benchmark_run_run_id", error.ConstraintName);
    }

    /// <summary>Boundary: a time with an offset other than UTC is stored as the same instant.</summary>
    [Fact]
    public async Task SaveChangesAsync_TimeWithAnOffset_IsStoredAsTheSameInstant()
    {
        // Arrange
        var local = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.FromHours(2));
        await SeedAsync(FirstRun, local, "m1");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new BenchmarkRunRepository(context).FindByRunIdAsync(FirstRun, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(local, found.StartedAt);
    }

    #endregion

    #region Deleting

    /// <summary>Positive: removing a run removes its requests with it and leaves another run alone.</summary>
    [Fact]
    public async Task Remove_StoredRun_RemovesItsRequestsAndKeepsTheOtherRun()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m1", "m2");
        await SeedAsync(SecondRun, Start.AddHours(1), "m3");
        await using (var context = _fixture.CreateContext())
        {
            var repository = new BenchmarkRunRepository(context);
            var run = await repository.FindByRunIdAsync(FirstRun, TestContext.Current.CancellationToken);

            // Act
            repository.Remove(run!);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = _fixture.CreateContext();
        var remaining = check.BenchmarkRuns.Select(run => run.RunId).ToList();
        Assert.Equal(SecondRun, Assert.Single(remaining));
        Assert.Equal(["m3"], check.BenchmarkResults.Select(result => result.Model));
    }

    #endregion

    #region Reading

    /// <summary>Positive: the list is newest first, shows each model once and counts the requests.</summary>
    [Fact]
    public async Task GetRunsAsync_TwoRuns_ReturnsNewestFirstWithModelsAndCounts()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m2", "m1", "m1");
        await SeedAsync(SecondRun, Start.AddHours(1), "m3");
        await using var context = _fixture.CreateContext();

        // Act
        var runs = await new BenchmarkRunRepository(context).GetRunsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([SecondRun, FirstRun], runs.Select(run => run.RunId));
        Assert.Equal(["m1", "m2"], runs[1].Models);
        Assert.Equal(3, runs[1].RequestCount);
        Assert.Equal(["m3"], runs[0].Models);
        Assert.Null(runs[0].Gpu);
    }

    /// <summary>Positive: the details have every request in order and no answer text.</summary>
    [Fact]
    public async Task GetRunAsync_StoredRun_ReturnsTheRequestsInOrder()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m1", "m2");
        await using var context = _fixture.CreateContext();

        // Act
        var details = await new BenchmarkRunRepository(context).GetRunAsync(FirstRun, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(["m1", "m2"], details.Results.Select(result => result.Model));
        Assert.Equal(2, details.Header.RequestCount);
        Assert.Equal(new string('a', 64), details.DatasetHash);
        Assert.All(details.Results, result => Assert.False(result.HasError));
    }

    /// <summary>Negative: an unknown run id has no details.</summary>
    [Fact]
    public async Task GetRunAsync_UnknownRunId_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var details = await new BenchmarkRunRepository(context).GetRunAsync(FirstRun, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(details);
    }

    /// <summary>State: the answer text is read on its own by the key of the request, and an unknown key has none.</summary>
    [Fact]
    public async Task GetOutputAsync_KnownAndUnknownKey_ReturnsTheAnswerOrNull()
    {
        // Arrange
        await SeedAsync(FirstRun, Start, "m1");
        await using var context = _fixture.CreateContext();
        var repository = new BenchmarkRunRepository(context);
        var key = context.BenchmarkResults.Select(result => result.Id).Single();

        // Act
        var known = await repository.GetOutputAsync(key, TestContext.Current.CancellationToken);
        var unknown = await repository.GetOutputAsync(key + 1000, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(known);
        Assert.Equal("{}", known.Output);
        Assert.Null(unknown);
    }

    #endregion

    #region Helpers

    private async Task SeedAsync(Guid runId, DateTimeOffset start, params string[] models)
    {
        await using var context = _fixture.CreateContext();
        new BenchmarkRunRepository(context).Add(NewRun(runId, start, models));
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static BenchmarkRun NewRun(Guid runId, DateTimeOffset start, params string[] models) =>
        BenchmarkRun.Create(
            runId,
            start,
            start.AddMinutes(5),
            "d.csv",
            new string('a', 64),
            10,
            "hotel-v1",
            new BenchmarkMachine("Test CPU", 8, 16384, null, "Linux 6.8"),
            "ollama",
            "0.5.1",
            models.Select(model => Result(start, model)).ToArray());

    private static BenchmarkResultData Result(DateTimeOffset start, string model) =>
        new(
            start,
            "ollama",
            model,
            BenchmarkResult.HotelCancellation,
            "hotel-cancellation",
            "analysis",
            0,
            true,
            0.5,
            2.0,
            10,
            20,
            5.0,
            false,
            "stop",
            null,
            null,
            1000,
            null,
            null,
            null,
            null,
            true,
            null,
            "{}",
            null);

    #endregion
}
