using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The price change table against a real PostgreSQL: the round trip, the reads by size and period, the revert and the index (MIL-051).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PriceChangeRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2027, 5, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly First = new(2027, 6, 1);
    private static readonly DateOnly Last = new(2027, 8, 31);

    private readonly PostgreSqlFixture _fixture;

    public PriceChangeRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region The migration and the round trip

    /// <summary>Integration: the migration is part of the chain, its table is empty after the migrations ran, and the index on size and first day exists.</summary>
    [Fact]
    public async Task Migrations_AppliedToAnEmptyDatabase_MatchTheModelAndHaveTheIndex()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var migrations = context.Database.GetMigrations();
        var count = await context.PriceChanges.CountAsync(TestContext.Current.CancellationToken);
        var indexes = await context.Database
            .SqlQueryRaw<string>("select indexname as \"Value\" from pg_indexes where tablename = 'price_change'")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(migrations, id => id.EndsWith("_AddPriceChange", StringComparison.Ordinal));
        Assert.Equal(0, count);
        Assert.Contains("ix_price_change_size_first_day", indexes);
    }

    /// <summary>Positive: an approved change comes back with every field, the percentage with two decimals, the days as dates and no revert.</summary>
    [Fact]
    public async Task GetByIdAsync_StoredChange_ReturnsItWithItsFields()
    {
        // Arrange
        var id = await AddAsync(RoomSize.Double, -12.5m, First, Last);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new PriceChangeRepository(context).GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal((RoomSize.Double, -12.5m, First, Last), (found.Size, found.Percentage, found.FirstDay, found.LastDay));
        Assert.Equal((PriceChangeState.Approved, "manager", At), (found.State, found.ApprovedBy, found.ApprovedAt));
        Assert.Null(found.RevertedBy);
        Assert.Null(found.RevertedAt);
        Assert.Null(found.EndedOn);
    }

    /// <summary>Negative: a key that is not stored finds nothing.</summary>
    [Fact]
    public async Task GetByIdAsync_NothingStored_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new PriceChangeRepository(context).GetByIdAsync(42, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>State: a revert is saved on the same row, with who, when and the end day, and the row is never deleted.</summary>
    [Fact]
    public async Task Revert_StoredChange_UpdatesTheSameRow()
    {
        // Arrange
        var id = await AddAsync(RoomSize.Single, 20m, First, Last);
        await using (var context = _fixture.CreateContext())
        {
            var stored = await new PriceChangeRepository(context).GetByIdAsync(id, TestContext.Current.CancellationToken);

            // Act
            stored!.Revert("other", At.AddDays(40), new DateOnly(2027, 7, 10));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = _fixture.CreateContext();
        var value = Assert.Single(check.PriceChanges);
        Assert.Equal((id, PriceChangeState.Reverted, "other"), (value.Id, value.State, value.RevertedBy));
        Assert.Equal(At.AddDays(40), value.RevertedAt);
        Assert.Equal(new DateOnly(2027, 7, 9), value.EndedOn);
    }

    #endregion

    #region Reads

    /// <summary>Positive: the list for a size and a period holds the changes of that size whose period touches it, ordered by first day, in any state.</summary>
    [Fact]
    public async Task ListForSizeAsync_PeriodTouchesSome_ReturnsOnlyThoseOfTheSizeInOrder()
    {
        // Arrange
        var late = await AddAsync(RoomSize.Single, 5m, new DateOnly(2027, 9, 1), new DateOnly(2027, 9, 30));
        var summer = await AddAsync(RoomSize.Single, 20m, First, Last);
        await AddAsync(RoomSize.Single, 10m, new DateOnly(2027, 1, 1), new DateOnly(2027, 5, 31));
        await AddAsync(RoomSize.Suite, 30m, First, Last);
        await using var context = _fixture.CreateContext();

        // Act
        var list = await new PriceChangeRepository(context).ListForSizeAsync(RoomSize.Single, Last, new DateOnly(2027, 9, 1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([summer, late], list.Select(value => value.Id));
    }

    /// <summary>Boundary: a period that ends the day before a change begins, or begins the day after it ends, does not touch it.</summary>
    [Fact]
    public async Task ListForSizeAsync_AdjacentPeriod_ReturnsNothing()
    {
        // Arrange
        await AddAsync(RoomSize.Single, 20m, First, Last);
        await using var context = _fixture.CreateContext();
        var repository = new PriceChangeRepository(context);

        // Act
        var before = await repository.ListForSizeAsync(RoomSize.Single, new DateOnly(2027, 5, 1), First.AddDays(-1), TestContext.Current.CancellationToken);
        var after = await repository.ListForSizeAsync(RoomSize.Single, Last.AddDays(1), new DateOnly(2027, 12, 31), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(before);
        Assert.Empty(after);
    }

    /// <summary>Positive: the full list is ordered by first day, then size, and holds reverted changes too.</summary>
    [Fact]
    public async Task ListAsync_SeveralChanges_ReturnsThemByFirstDayThenSize()
    {
        // Arrange
        var suite = await AddAsync(RoomSize.Suite, 30m, First, Last);
        var single = await AddAsync(RoomSize.Single, 20m, First, Last);
        var earlier = await AddAsync(RoomSize.Double, 10m, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31));
        await using (var context = _fixture.CreateContext())
        {
            var stored = await new PriceChangeRepository(context).GetByIdAsync(suite, TestContext.Current.CancellationToken);
            stored!.Revert("other", At, new DateOnly(2027, 5, 15));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var read = _fixture.CreateContext();

        // Act
        var list = await new PriceChangeRepository(read).ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([earlier, single, suite], list.Select(value => value.Id));
    }

    #endregion

    private async Task<int> AddAsync(RoomSize size, decimal percentage, DateOnly first, DateOnly last)
    {
        await using var context = _fixture.CreateContext();
        var change = PriceChange.Approve(size, percentage, first, last, "manager", At);
        new PriceChangeRepository(context).Add(change);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return change.Id;
    }
}
