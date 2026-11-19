using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

using Npgsql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The migration of MIL-085 (ADR-0049 decision 4) against a real PostgreSQL: it adds the reference column and its unique
/// index and nothing else, gives every booking that exists a version 4 value of its own, fills a booking that the release
/// before inserts without the column, and goes back down without losing a booking. Each test makes a database of its
/// own in the shared container, so the database the other tests share is not touched.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingReferenceMigrationTests : IAsyncLifetime
{
    private const string ReferenceMigration = "_AddBookingReference";
    private const int SeededBookings = 50;

    private const string InsertRoomAndGuestSql =
        "INSERT INTO room (room_number, floor, size, capacity, status, housekeeping_status) VALUES ('901', 9, 1, 2, 0, 0); "
        + "INSERT INTO guest (first_name, last_name, email, phone_number, country) "
        + "VALUES ('Anna', 'Jensen', 'anna@example.com', '+4512345678', 'Denmark')";

    /// <summary>Bookings as the release before writes them: without the reference column, one after the other in one room.</summary>
    private const string InsertBookingsSql =
        "INSERT INTO booking (room_id, guest_id, start_date, end_date, status) "
        + "SELECT (SELECT room_id FROM room WHERE room_number = '901'), (SELECT guest_id FROM guest WHERE email = 'anna@example.com'), "
        + "DATE '2027-01-01' + g * 3, DATE '2027-01-01' + g * 3 + 2, 0 FROM generate_series(1, 50) AS g";

    /// <summary>The text of a version 4 UUID with the variant of RFC 9562, as PostgreSQL writes it.</summary>
    private const string IsVersion4 =
        "reference::text ~ '^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'";

    private readonly PostgreSqlFixture _fixture;
    private readonly string _databaseName = "mig_" + Guid.NewGuid().ToString("N");

    private string _connectionString = string.Empty;

    public BookingReferenceMigrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await using NpgsqlConnection admin = new(_fixture.ConnectionString);
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand create = new($"CREATE DATABASE \"{_databaseName}\"", admin);
        await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        _connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = _databaseName,
            Pooling = false,
        }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await using NpgsqlConnection admin = new(_fixture.ConnectionString);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    #region On an empty database

    /// <summary>
    /// Integration: all migrations on an empty database give the booking table a reference column that is a uuid, not
    /// null, with a random default, and a unique index on it; the model has no pending change.
    /// </summary>
    [Fact]
    public async Task Migrate_EmptyDatabaseToTheLatest_AddsTheReferenceColumnAndItsUniqueIndex()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        await AssertReferenceExistsAsync(true);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Security (ADR-0049 decision 4): the migration adds the column and the index and nothing else, so the release before
    /// keeps working against the migrated database and a rollback stays possible.
    /// </summary>
    [Fact]
    public async Task Migrate_TheReferenceMigration_AddsOneColumnAndOneIndexOnly()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();
        await MigrateToAsync(context, BeforeTheReferenceMigration(context));
        long columnsBefore = await CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public'");
        long indexesBefore = await CountAsync("SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'");
        long constraintsBefore = await CountAsync("SELECT count(*) FROM pg_constraint");

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(columnsBefore + 1, await CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public'"));
        Assert.Equal(indexesBefore + 1, await CountAsync("SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'"));
        Assert.Equal(constraintsBefore, await CountAsync("SELECT count(*) FROM pg_constraint"));
    }

    #endregion

    #region Over bookings that exist

    /// <summary>
    /// Integration (ADR-0049 decision 4): migrating over 50 bookings keeps them and gives each a version 4 reference of
    /// its own.
    /// </summary>
    [Fact]
    public async Task Migrate_OverFiftyBookings_GivesEachItsOwnVersion4Reference()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();
        await MigrateToAsync(context, BeforeTheReferenceMigration(context));
        await ExecuteAsync(InsertRoomAndGuestSql);
        await ExecuteAsync(InsertBookingsSql);

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(SeededBookings, await CountAsync("SELECT count(*) FROM booking"));
        Assert.Equal(SeededBookings, await CountAsync("SELECT count(DISTINCT reference) FROM booking"));
        Assert.Equal(SeededBookings, await CountAsync($"SELECT count(*) FROM booking WHERE {IsVersion4}"));
    }

    /// <summary>
    /// Integration (ADR-0049 decision 4): a booking that the release before inserts after the migration, without the
    /// column, gets a version 4 reference from the database.
    /// </summary>
    [Fact]
    public async Task Insert_ByTheReleaseBefore_GetsAReferenceFromTheDatabase()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(InsertRoomAndGuestSql);

        // Act
        await ExecuteAsync(InsertBookingsSql);

        // Assert
        Assert.Equal(SeededBookings, await CountAsync($"SELECT count(*) FROM booking WHERE reference IS NOT NULL AND {IsVersion4}"));
        Assert.Equal(SeededBookings, await CountAsync("SELECT count(DISTINCT reference) FROM booking"));
    }

    /// <summary>Security: the unique index refuses a second booking with a reference that is already taken.</summary>
    [Fact]
    public async Task Insert_ReferenceThatIsTaken_IsRefused()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(InsertRoomAndGuestSql);
        await ExecuteAsync(InsertBookingsSql);

        // Act
        PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            "UPDATE booking SET reference = (SELECT reference FROM booking ORDER BY booking_id LIMIT 1) "
            + "WHERE booking_id = (SELECT max(booking_id) FROM booking)"));

        // Assert
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal("ux_booking_reference", error.ConstraintName);
    }

    #endregion

    #region Down

    /// <summary>Integration: Down removes the column and the index, keeps the 50 bookings, and Up again gives them new references.</summary>
    [Fact]
    public async Task Migrate_DownOverFiftyBookings_RemovesTheColumnKeepsTheBookingsAndAppliesAgain()
    {
        // Arrange
        await using HotelDbContext context = CreateContext();
        string before = BeforeTheReferenceMigration(context);
        await MigrateToAsync(context, before);
        await ExecuteAsync(InsertRoomAndGuestSql);
        await ExecuteAsync(InsertBookingsSql);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Act
        await MigrateToAsync(context, before);

        // Assert
        await AssertReferenceExistsAsync(false);
        Assert.Equal(SeededBookings, await CountAsync("SELECT count(*) FROM booking"));

        // Act again: up to the latest once more.
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert again
        await AssertReferenceExistsAsync(true);
        Assert.Equal(SeededBookings, await CountAsync($"SELECT count(*) FROM booking WHERE {IsVersion4}"));
    }

    #endregion

    #region Helpers

    private HotelDbContext CreateContext()
    {
        DbContextOptionsBuilder<HotelDbContext> optionsBuilder = new();
        DependencyInjection.ConfigureHotelDbContext(optionsBuilder, _connectionString);
        AesGcmStringEncryptor encryptor = new(new EncryptionOptions { PassportKey = Convert.ToBase64String(new byte[32]) });
        return new HotelDbContext(optionsBuilder.Options, encryptor);
    }

    /// <summary>The id of the migration right before the migration of MIL-085, which must be the latest.</summary>
    private static string BeforeTheReferenceMigration(HotelDbContext context)
    {
        List<string> migrations = context.Database.GetMigrations().ToList();
        int index = migrations.FindIndex(id => id.EndsWith(ReferenceMigration, StringComparison.Ordinal));
        Assert.True(index > 0, "The migration of MIL-085 must exist and have a predecessor.");
        Assert.Equal(migrations.Count - 1, index);

        return migrations[index - 1];
    }

    private static Task MigrateToAsync(HotelDbContext context, string target) =>
        context.GetService<IMigrator>().MigrateAsync(target, TestContext.Current.CancellationToken);

    private async Task ExecuteAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<long> CountAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);

        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private async Task AssertReferenceExistsAsync(bool expected)
    {
        int expectedCount = expected ? 1 : 0;
        Assert.Equal(expectedCount, await CountAsync(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'booking' AND column_name = 'reference' "
            + "AND data_type = 'uuid' AND is_nullable = 'NO' AND column_default = 'gen_random_uuid()'"));
        Assert.Equal(expectedCount, await CountAsync(
            "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'ux_booking_reference' "
            + "AND indexdef LIKE 'CREATE UNIQUE INDEX%(reference)'"));
    }

    #endregion
}
