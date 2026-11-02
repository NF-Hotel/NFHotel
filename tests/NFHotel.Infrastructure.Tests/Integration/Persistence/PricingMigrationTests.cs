using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

using Npgsql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The three migrations of MIL-055 (room rate and audit detail, pricing settings, price change) against a real PostgreSQL:
/// on an empty database, over a database with the 187 rooms, and back down (milestone criterion 9). Each test makes a
/// database of its own in the shared container, so the database the other tests share is not touched.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PricingMigrationTests : IAsyncLifetime
{
    private const string FirstPricingMigration = "_AddRoomRateAndAuditDetail";
    private const string LastPricingMigration = "_AddPriceChange";
    private const int SeededRooms = 187;

    private const string InsertRoomsSql =
        "INSERT INTO room (room_number, floor, size, capacity, status, housekeeping_status) "
        + "SELECT (100 + g)::text, 1 + (g - 1) / 20, (g - 1) % 3, 1 + (g - 1) % 3, 0, 0 FROM generate_series(1, 187) AS g";

    private const string InsertAuditEventSql =
        "INSERT INTO staff_audit_event (occurred_at, actor_name, action, outcome) VALUES (now(), 'manager', 1, 'Success')";

    private readonly PostgreSqlFixture _fixture;
    private readonly string _databaseName = "mig_" + Guid.NewGuid().ToString("N");

    private string _connectionString = string.Empty;

    public PricingMigrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await using var admin = new NpgsqlConnection(_fixture.ConnectionString);
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
        await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        _connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = _databaseName,
            Pooling = false,
        }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await using var admin = new NpgsqlConnection(_fixture.ConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    #region On an empty database

    /// <summary>Integration: all migrations on an empty database create the tables, the column, the indexes and the check, and the model has no pending change.</summary>
    [Fact]
    public async Task Migrate_EmptyDatabaseToTheLatest_CreatesTheNewTablesColumnIndexesAndCheck()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        await AssertPricingObjectsExistAsync(true);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await CountAsync("SELECT count(*) FROM room_rate"));
    }

    #endregion

    #region Over the 187 rooms

    /// <summary>Integration: migrating up from the migration before MIL-055 over 187 rooms and an audit event keeps them and leaves the new tables empty.</summary>
    [Fact]
    public async Task Migrate_DatabaseWith187Rooms_KeepsTheRoomsAndTheAuditEventAndAddsEmptyTables()
    {
        // Arrange
        await using var context = CreateContext();
        await MigrateToAsync(context, BeforeThePricingMigrations(context));
        await ExecuteAsync(InsertRoomsSql);
        await ExecuteAsync(InsertAuditEventSql);

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(SeededRooms, await CountAsync("SELECT count(*) FROM room"));
        Assert.Equal(1, await CountAsync("SELECT count(*) FROM staff_audit_event"));
        Assert.Equal(1, await CountAsync("SELECT count(*) FROM staff_audit_event WHERE detail IS NULL AND outcome = 'Success'"));
        Assert.Equal(0, await CountAsync("SELECT count(*) FROM room_rate"));
        Assert.Equal(0, await CountAsync("SELECT count(*) FROM pricing_settings"));
        Assert.Equal(0, await CountAsync("SELECT count(*) FROM price_change"));
        await AssertPricingObjectsExistAsync(true);
    }

    #endregion

    #region Down

    /// <summary>Integration: Down of the three migrations over 187 rooms removes the new tables and the column, keeps the rooms and the audit event, and Up again restores them.</summary>
    [Fact]
    public async Task Migrate_DownOverDatabaseWith187Rooms_RemovesTheNewObjectsKeepsTheRoomsAndAppliesAgain()
    {
        // Arrange
        await using var context = CreateContext();
        var before = BeforeThePricingMigrations(context);
        await MigrateToAsync(context, before);
        await ExecuteAsync(InsertRoomsSql);
        await ExecuteAsync(InsertAuditEventSql);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Act
        await MigrateToAsync(context, before);

        // Assert
        await AssertPricingObjectsExistAsync(false);
        Assert.Equal(SeededRooms, await CountAsync("SELECT count(*) FROM room"));
        Assert.Equal(1, await CountAsync("SELECT count(*) FROM staff_audit_event"));

        // Act again: up to the latest once more.
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert again
        await AssertPricingObjectsExistAsync(true);
        Assert.Equal(SeededRooms, await CountAsync("SELECT count(*) FROM room"));
        Assert.Equal(1, await CountAsync("SELECT count(*) FROM staff_audit_event"));
    }

    #endregion

    #region Helpers

    private HotelDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();
        DependencyInjection.ConfigureHotelDbContext(optionsBuilder, _connectionString);
        var encryptor = new AesGcmStringEncryptor(new EncryptionOptions { PassportKey = Convert.ToBase64String(new byte[32]) });
        return new HotelDbContext(optionsBuilder.Options, encryptor);
    }

    /// <summary>The id of the migration that comes right before the first migration of MIL-055.</summary>
    private static string BeforeThePricingMigrations(HotelDbContext context)
    {
        var migrations = context.Database.GetMigrations().ToList();
        var first = migrations.FindIndex(id => id.EndsWith(FirstPricingMigration, StringComparison.Ordinal));
        var last = migrations.FindIndex(id => id.EndsWith(LastPricingMigration, StringComparison.Ordinal));
        Assert.True(first > 0, "The first migration of MIL-055 must have a predecessor.");
        Assert.Equal(first + 2, last);

        return migrations[first - 1];
    }

    private static Task MigrateToAsync(HotelDbContext context, string target) =>
        context.GetService<IMigrator>().MigrateAsync(target, TestContext.Current.CancellationToken);

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> CountAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private async Task AssertPricingObjectsExistAsync(bool expected)
    {
        var expectedCount = expected ? 1 : 0;
        foreach (var table in new[] { "room_rate", "pricing_settings", "price_change" })
        {
            Assert.Equal(expectedCount, await CountAsync($"SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{table}'"));
        }

        Assert.Equal(expectedCount, await CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_name = 'staff_audit_event' AND column_name = 'detail'"));
        foreach (var index in new[] { "ux_room_rate_size", "ux_pricing_settings_slot", "ix_price_change_size_first_day" })
        {
            Assert.Equal(expectedCount, await CountAsync($"SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname = '{index}'"));
        }

        Assert.Equal(expectedCount, await CountAsync("SELECT count(*) FROM pg_constraint WHERE conname = 'ck_pricing_settings_slot'"));
        if (expected)
        {
            Assert.Equal(1, await CountAsync("SELECT count(*) FROM pg_indexes WHERE indexname = 'ux_room_rate_size' AND indexdef LIKE 'CREATE UNIQUE%'"));
            Assert.Equal(1, await CountAsync("SELECT count(*) FROM pg_indexes WHERE indexname = 'ux_pricing_settings_slot' AND indexdef LIKE 'CREATE UNIQUE%'"));
        }
    }

    #endregion
}
