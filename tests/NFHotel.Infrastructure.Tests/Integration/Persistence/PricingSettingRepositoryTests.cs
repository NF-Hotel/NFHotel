using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The pricing settings table against a real PostgreSQL: the round trip, the one-row rule and the migration (MIL-055).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PricingSettingRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public PricingSettingRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static PricingSetting Make(decimal surcharge = 25.5m, params RoomSize[] sizes) =>
        PricingSetting.Create(surcharge, 12.25m, 85m, 35m, 40m, 59.9m, sizes.Length == 0 ? [RoomSize.Single, RoomSize.Suite] : sizes, "manager", At);

    #region Reads and the migration

    /// <summary>Integration: the migration is part of the chain and its table exists and is empty.</summary>
    [Fact]
    public async Task Migrations_AppliedToAnEmptyDatabase_MatchTheModel()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        IEnumerable<string> migrations = context.Database.GetMigrations();
        int rows = await context.PricingSettings.CountAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(migrations, id => id.EndsWith("_AddPricingSettings", StringComparison.Ordinal));
        Assert.Equal(0, rows);
    }

    /// <summary>Negative: with nothing stored the read returns null.</summary>
    [Fact]
    public async Task GetAsync_NothingStored_ReturnsNull()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        PricingSetting? found = await new PricingSettingRepository(context).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Positive: a stored row comes back with every field, the sizes included.</summary>
    [Fact]
    public async Task GetAsync_StoredRow_ReturnsEveryFieldIncludingTheSizes()
    {
        // Arrange
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            new PricingSettingRepository(context).Add(Make());
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using HotelDbContext reader = _fixture.CreateContext();

        // Act
        PricingSetting? found = await new PricingSettingRepository(reader).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal((25.5m, 12.25m, 85m, 35m, 40m, 59.9m), (found.SurchargePercent, found.DiscountPercent, found.HighThresholdPercent, found.LowThresholdPercent, found.MaxChangePercent, found.MinimumNightlyPrice));
        Assert.Equal([RoomSize.Single, RoomSize.Suite], found.DiscountSizes);
        Assert.Equal(("manager", At), (found.SetBy, found.SetAt));
    }

    #endregion

    #region One row and changes

    /// <summary>Negative: a second row is refused by the unique index on the slot.</summary>
    [Fact]
    public async Task Add_SecondRow_IsRefusedByTheDatabase()
    {
        // Arrange
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            new PricingSettingRepository(context).Add(Make());
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using HotelDbContext second = _fixture.CreateContext();
        new PricingSettingRepository(second).Add(Make(surcharge: 10m));

        // Act
        Func<Task<int>> save = () => second.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    /// <summary>Negative: a row with another slot value is refused by the check constraint.</summary>
    [Fact]
    public async Task Insert_SlotOtherThanOne_IsRefusedByTheCheckConstraint()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        Func<Task<int>> insert = () => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO pricing_settings (slot, surcharge_percent, discount_percent, high_threshold_percent, low_threshold_percent, max_change_percent, minimum_nightly_price, discount_sizes, set_by, set_at) "
            + "VALUES (2, 20, 15, 80, 40, 30, 0, 'Single', 'manager', now())",
            TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<Exception>(insert);
    }

    /// <summary>State: a change to the stored row, sizes included, is saved on the same row.</summary>
    [Fact]
    public async Task Change_StoredRow_UpdatesTheSameRowAndItsSizes()
    {
        // Arrange
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            new PricingSettingRepository(context).Add(Make(sizes: RoomSize.Single));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (HotelDbContext context = _fixture.CreateContext())
        {
            PricingSetting? stored = await new PricingSettingRepository(context).GetAsync(TestContext.Current.CancellationToken);

            // Act
            stored!.Change(10m, 5m, 90m, 20m, 50m, 0m, [RoomSize.Double, RoomSize.Suite, RoomSize.Single], "other", At.AddHours(1));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        await using HotelDbContext check = _fixture.CreateContext();
        PricingSetting value = Assert.Single(check.PricingSettings);
        Assert.Equal((10m, "other"), (value.SurchargePercent, value.SetBy));
        Assert.Equal([RoomSize.Single, RoomSize.Double, RoomSize.Suite], value.DiscountSizes);
    }

    #endregion
}
