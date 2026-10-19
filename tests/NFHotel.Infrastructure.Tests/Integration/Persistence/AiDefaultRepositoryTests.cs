using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The default AI model table against a real PostgreSQL: one default per purpose and the reads (ADR-0014).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class AiDefaultRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    private readonly PostgreSqlFixture _fixture;

    public AiDefaultRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Positive: a stored default is found by its purpose with every field.</summary>
    [Fact]
    public async Task FindByPurposeAsync_StoredDefault_ReturnsItWithItsFields()
    {
        // Arrange
        await AddAsync(AiDefault.Website, "llama3.2:3b", RunId);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new AiDefaultRepository(context).FindByPurposeAsync(AiDefault.Website, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(("llama3.2:3b", "manager", RunId), (found.Model, found.SetBy, found.SourceRunId));
        Assert.Equal(At, found.SetAt);
    }

    /// <summary>Negative: a purpose without a default finds nothing.</summary>
    [Fact]
    public async Task FindByPurposeAsync_NothingStored_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new AiDefaultRepository(context).FindByPurposeAsync(AiDefault.MarketAnalysis, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Positive: the list holds every default, ordered by purpose.</summary>
    [Fact]
    public async Task ListAsync_TwoDefaults_ReturnsThemOrderedByPurpose()
    {
        // Arrange
        await AddAsync(AiDefault.Website, "m1", null);
        await AddAsync(AiDefault.MarketAnalysis, "m2", null);
        await using var context = _fixture.CreateContext();

        // Act
        var list = await new AiDefaultRepository(context).ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["market_analysis", "website"], list.Select(value => value.Purpose));
    }

    /// <summary>Negative: a second default for the same purpose is refused by the unique index.</summary>
    [Fact]
    public async Task Add_SecondDefaultForThePurpose_IsRefusedByTheDatabase()
    {
        // Arrange
        await AddAsync(AiDefault.Website, "m1", null);
        await using var context = _fixture.CreateContext();
        new AiDefaultRepository(context).Add(AiDefault.Create(AiDefault.Website, "m2", "manager", At, null));

        // Act
        var save = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    /// <summary>State: a change to a stored default is saved on the same row.</summary>
    [Fact]
    public async Task Change_StoredDefault_UpdatesTheSameRow()
    {
        // Arrange
        await AddAsync(AiDefault.Website, "m1", null);
        await using (var context = _fixture.CreateContext())
        {
            var stored = await new AiDefaultRepository(context).FindByPurposeAsync(AiDefault.Website, TestContext.Current.CancellationToken);

            // Act
            stored!.Change("m2", "other", At.AddHours(1), RunId);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = _fixture.CreateContext();
        var value = Assert.Single(check.AiDefaults);
        Assert.Equal(("m2", "other", RunId), (value.Model, value.SetBy, value.SourceRunId));
    }

    private async Task AddAsync(string purpose, string model, Guid? sourceRunId)
    {
        await using var context = _fixture.CreateContext();
        new AiDefaultRepository(context).Add(AiDefault.Create(purpose, model, "manager", At, sourceRunId));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
