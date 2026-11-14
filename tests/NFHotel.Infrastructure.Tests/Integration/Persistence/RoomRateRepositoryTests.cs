using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The room rate table against a real PostgreSQL: one rate per size, the reads, and the audit detail column (MIL-055).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class RoomRateRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public RoomRateRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Reads and the migration

    /// <summary>Integration: the migration is part of the chain, and its table exists and is empty after the migrations ran on an empty database.</summary>
    [Fact]
    public async Task Migrations_AppliedToAnEmptyDatabase_CreateTheEmptyTable()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        IEnumerable<string> migrations = context.Database.GetMigrations();
        int rates = await context.RoomRates.CountAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(migrations, id => id.EndsWith("_AddRoomRateAndAuditDetail", StringComparison.Ordinal));
        Assert.Equal(0, rates);
    }

    /// <summary>Positive: a stored rate is found by its size with every field, the price with two decimals.</summary>
    [Fact]
    public async Task FindBySizeAsync_StoredRate_ReturnsItWithItsFields()
    {
        // Arrange
        await AddAsync(RoomSize.Double, 129.5m);
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        RoomRate? found = await new RoomRateRepository(context).FindBySizeAsync(RoomSize.Double, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal((129.5m, "manager"), (found.NightlyPrice, found.SetBy));
        Assert.Equal(At, found.SetAt);
    }

    /// <summary>Negative: a size without a rate finds nothing.</summary>
    [Fact]
    public async Task FindBySizeAsync_NothingStored_ReturnsNull()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        RoomRate? found = await new RoomRateRepository(context).FindBySizeAsync(RoomSize.Suite, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Positive: the list holds every rate, ordered by size.</summary>
    [Fact]
    public async Task ListAsync_ThreeRates_ReturnsThemOrderedBySize()
    {
        // Arrange
        await AddAsync(RoomSize.Suite, 300m);
        await AddAsync(RoomSize.Single, 80m);
        await AddAsync(RoomSize.Double, 120m);
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        IReadOnlyList<RoomRate> list = await new RoomRateRepository(context).ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([RoomSize.Single, RoomSize.Double, RoomSize.Suite], list.Select(value => value.Size));
    }

    /// <summary>Negative: a second rate for the same size is refused by the unique index.</summary>
    [Fact]
    public async Task Add_SecondRateForTheSize_IsRefusedByTheDatabase()
    {
        // Arrange
        await AddAsync(RoomSize.Single, 80m);
        await using HotelDbContext context = _fixture.CreateContext();
        new RoomRateRepository(context).Add(RoomRate.Create(RoomSize.Single, 90m, "manager", At));

        // Act
        Func<Task<int>> save = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    #endregion

    #region Changes

    /// <summary>State: a change to a stored rate is saved on the same row.</summary>
    [Fact]
    public async Task Change_StoredRate_UpdatesTheSameRow()
    {
        // Arrange
        await AddAsync(RoomSize.Single, 80m);
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            RoomRate? stored = await new RoomRateRepository(context).FindBySizeAsync(RoomSize.Single, TestContext.Current.CancellationToken);

            // Act
            stored!.Change(95m, "other", At.AddHours(1));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using HotelDbContext check = _fixture.CreateContext();
        RoomRate value = Assert.Single(check.RoomRates);
        Assert.Equal((95m, "other"), (value.NightlyPrice, value.SetBy));
    }

    /// <summary>Integration: a rate and its audit event are saved in one unit of work, and the detail comes back.</summary>
    [Fact]
    public async Task SaveChanges_RateAndAuditEvent_AreStoredTogetherWithTheDetail()
    {
        // Arrange
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            new RoomRateRepository(context).Add(RoomRate.Create(RoomSize.Single, 90m, "manager", At));
            new StaffAuditRepository(context).Add(StaffAuditEvent.Create(At, StaffAuditAction.RoomRateChanged, "Success", 1, "manager", detail: "Single: (none) -> 90.00"));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using HotelDbContext reader = _fixture.CreateContext();
        StaffAuditEvent audit = Assert.Single(await new StaffAuditRepository(reader).ListAsync(0, 10, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(StaffAuditAction.RoomRateChanged, audit.Action);
        Assert.Equal("Single: (none) -> 90.00", audit.Detail);
        Assert.Equal(1, await reader.RoomRates.CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task AddAsync(RoomSize size, decimal price)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        new RoomRateRepository(context).Add(RoomRate.Create(size, price, "manager", At));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion
}
