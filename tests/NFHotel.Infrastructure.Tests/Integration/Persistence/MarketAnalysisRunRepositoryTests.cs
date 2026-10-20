using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The stored market analysis table against a real PostgreSQL: one row per run id and the read of the latest (MIL-026).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class MarketAnalysisRunRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public MarketAnalysisRunRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Positive: a stored run is found by its run id with every field, the content included.</summary>
    [Fact]
    public async Task FindByRunIdAsync_StoredRun_ReturnsItWithItsFields()
    {
        // Arrange
        var runId = Guid.NewGuid();
        await AddAsync(runId, 0, "{\"summary\":\"åäö 😀\"}");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new MarketAnalysisRunRepository(context).FindByRunIdAsync(runId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal((1, "ollama", "phi3.5:latest", "{\"summary\":\"åäö 😀\"}"), (found.SchemaVersion, found.Provider, found.Model, found.ContentJson));
        Assert.Equal(Start, found.StartedAt);
    }

    /// <summary>Negative: an unknown run id finds nothing.</summary>
    [Fact]
    public async Task FindByRunIdAsync_UnknownRun_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new MarketAnalysisRunRepository(context).FindByRunIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Boundary: with no stored run there is no latest run.</summary>
    [Fact]
    public async Task FindLatestAsync_NothingStored_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new MarketAnalysisRunRepository(context).FindLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Positive: of three runs the one that finished last is the latest, whatever the order they were stored in.</summary>
    [Fact]
    public async Task FindLatestAsync_ThreeRuns_ReturnsTheOneThatFinishedLast()
    {
        // Arrange
        await AddAsync(Guid.NewGuid(), 5, "{}");
        var newest = Guid.NewGuid();
        await AddAsync(newest, 9, "{}");
        await AddAsync(Guid.NewGuid(), 1, "{}");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new MarketAnalysisRunRepository(context).FindLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(newest, found!.RunId);
    }

    /// <summary>Negative: the same run id stored twice is refused by the unique index, which also covers two polls at once.</summary>
    [Fact]
    public async Task Add_SameRunIdTwice_IsRefusedByTheDatabase()
    {
        // Arrange
        var runId = Guid.NewGuid();
        await AddAsync(runId, 0, "{}");
        await using var context = _fixture.CreateContext();
        new MarketAnalysisRunRepository(context).Add(MarketAnalysisRun.Create(runId, 1, Start, Start, null, null, "{}"));

        // Act
        var save = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    private async Task AddAsync(Guid runId, int finishedAfterMinutes, string contentJson)
    {
        await using var context = _fixture.CreateContext();
        new MarketAnalysisRunRepository(context).Add(
            MarketAnalysisRun.Create(runId, 1, Start, Start.AddMinutes(finishedAfterMinutes), "ollama", "phi3.5:latest", contentJson));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
