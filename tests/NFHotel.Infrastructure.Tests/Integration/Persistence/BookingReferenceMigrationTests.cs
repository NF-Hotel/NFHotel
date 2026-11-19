using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Persistence;

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

    /// <summary>The text of a version 4 UUID with the variant of RFC 9562, as PostgreSQL writes it.</summary>
    private const string IsVersion4 =
        "reference::text ~ '^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'";

    private readonly MigrationDatabase _database;

    public BookingReferenceMigrationTests(PostgreSqlFixture fixture)
    {
        _database = new MigrationDatabase(fixture);
    }

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    #region On an empty database

    /// <summary>
    /// Integration: all migrations on an empty database give the booking table a reference column that is a uuid, not
    /// null, with a random default, and a unique index on it; the model has no pending change.
    /// </summary>
    [Fact]
    public async Task Migrate_EmptyDatabaseToTheLatest_AddsTheReferenceColumnAndItsUniqueIndex()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();

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
        await using HotelDbContext context = _database.CreateContext();
        (string before, string migration) = MigrationDatabase.Migrations(context, ReferenceMigration);
        await MigrationDatabase.MigrateToAsync(context, before);
        long columnsBefore = await _database.CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public'");
        long indexesBefore = await _database.CountAsync("SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'");
        long constraintsBefore = await _database.CountAsync("SELECT count(*) FROM pg_constraint");

        // Act
        await MigrationDatabase.MigrateToAsync(context, migration);

        // Assert
        Assert.Equal(columnsBefore + 1, await _database.CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public'"));
        Assert.Equal(indexesBefore + 1, await _database.CountAsync("SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'"));
        Assert.Equal(constraintsBefore, await _database.CountAsync("SELECT count(*) FROM pg_constraint"));
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
        await using HotelDbContext context = _database.CreateContext();
        await MigrationDatabase.MigrateToAsync(context, MigrationDatabase.Migrations(context, ReferenceMigration).Before);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync("SELECT count(*) FROM booking"));
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync("SELECT count(DISTINCT reference) FROM booking"));
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync($"SELECT count(*) FROM booking WHERE {IsVersion4}"));
    }

    /// <summary>
    /// Integration (ADR-0049 decision 4): a booking that the release before inserts after the migration, without the
    /// column, gets a version 4 reference from the database.
    /// </summary>
    [Fact]
    public async Task Insert_ByTheReleaseBefore_GetsAReferenceFromTheDatabase()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);

        // Act
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Assert
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync($"SELECT count(*) FROM booking WHERE reference IS NOT NULL AND {IsVersion4}"));
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync("SELECT count(DISTINCT reference) FROM booking"));
    }

    /// <summary>Security: the unique index refuses a second booking with a reference that is already taken.</summary>
    [Fact]
    public async Task Insert_ReferenceThatIsTaken_IsRefused()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Act
        PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => _database.ExecuteAsync(
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
        await using HotelDbContext context = _database.CreateContext();
        string before = MigrationDatabase.Migrations(context, ReferenceMigration).Before;
        await MigrationDatabase.MigrateToAsync(context, before);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Act
        await MigrationDatabase.MigrateToAsync(context, before);

        // Assert
        await AssertReferenceExistsAsync(false);
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync("SELECT count(*) FROM booking"));

        // Act again: up to the latest once more.
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert again
        await AssertReferenceExistsAsync(true);
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync($"SELECT count(*) FROM booking WHERE {IsVersion4}"));
    }

    #endregion

    #region Helpers

    private async Task AssertReferenceExistsAsync(bool expected)
    {
        int expectedCount = expected ? 1 : 0;
        Assert.Equal(expectedCount, await _database.CountAsync(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'booking' AND column_name = 'reference' "
            + "AND data_type = 'uuid' AND is_nullable = 'NO' AND column_default = 'gen_random_uuid()'"));
        Assert.Equal(expectedCount, await _database.CountAsync(
            "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'ux_booking_reference' "
            + "AND indexdef LIKE 'CREATE UNIQUE INDEX%(reference)'"));
    }

    #endregion
}
