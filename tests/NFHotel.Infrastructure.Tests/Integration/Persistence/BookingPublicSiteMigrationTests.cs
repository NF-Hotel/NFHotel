using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The migration of MIL-087 (ADR-0051 decision 3) against a real PostgreSQL: it adds the column that marks a booking made
/// on the public site and nothing else, marks no booking that exists, so the limit per email counts from the deploy on,
/// leaves a booking that the release before inserts unmarked, and goes back down without losing a booking. Each test makes
/// a database of its own in the shared container.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingPublicSiteMigrationTests : IAsyncLifetime
{
    private const string PublicSiteMigration = "_AddBookingMadeOnPublicSite";

    private const string ColumnsSql = "SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public'";
    private const string IndexesSql = "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'";
    private const string ConstraintsSql = "SELECT count(*) FROM pg_constraint";
    private const string UnmarkedSql = "SELECT count(*) FROM booking WHERE NOT made_on_public_site";

    private readonly MigrationDatabase _database;

    public BookingPublicSiteMigrationTests(PostgreSqlFixture fixture)
    {
        _database = new MigrationDatabase(fixture);
    }

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    /// <summary>
    /// Integration: all migrations on an empty database give the booking table a boolean column that is not null and
    /// false by default; the model has no pending change.
    /// </summary>
    [Fact]
    public async Task Migrate_EmptyDatabaseToTheLatest_AddsTheColumn()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        await AssertColumnExistsAsync(true);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Security (ADR-0051 decision 3): the migration adds the one column and no index or constraint, so the release before
    /// keeps working against the migrated database and a rollback stays possible.
    /// </summary>
    [Fact]
    public async Task Migrate_ThePublicSiteMigration_AddsOneColumnOnly()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        (string before, string migration) = MigrationDatabase.Migrations(context, PublicSiteMigration);
        await MigrationDatabase.MigrateToAsync(context, before);
        long columnsBefore = await _database.CountAsync(ColumnsSql);
        long indexesBefore = await _database.CountAsync(IndexesSql);
        long constraintsBefore = await _database.CountAsync(ConstraintsSql);

        // Act
        await MigrationDatabase.MigrateToAsync(context, migration);

        // Assert
        Assert.Equal(columnsBefore + 1, await _database.CountAsync(ColumnsSql));
        Assert.Equal(indexesBefore, await _database.CountAsync(IndexesSql));
        Assert.Equal(constraintsBefore, await _database.CountAsync(ConstraintsSql));
    }

    /// <summary>
    /// Integration (ADR-0051 decision 3): migrating over 50 bookings keeps them and marks none as made on the public site,
    /// because nothing tells which of them were.
    /// </summary>
    [Fact]
    public async Task Migrate_OverFiftyBookings_MarksNone()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        await MigrationDatabase.MigrateToAsync(context, MigrationDatabase.Migrations(context, PublicSiteMigration).Before);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Act
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync(UnmarkedSql));
    }

    /// <summary>Integration: a booking that the release before inserts after the migration, without the column, is not marked.</summary>
    [Fact]
    public async Task Insert_ByTheReleaseBefore_IsNotMarked()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);

        // Act
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Assert
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync(UnmarkedSql));
    }

    /// <summary>Integration: Down removes the column and keeps the 50 bookings, and Up again adds it with none marked.</summary>
    [Fact]
    public async Task Migrate_DownOverFiftyBookings_RemovesTheColumnKeepsTheBookingsAndAppliesAgain()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Act
        await MigrationDatabase.MigrateToAsync(context, MigrationDatabase.Migrations(context, PublicSiteMigration).Before);

        // Assert
        await AssertColumnExistsAsync(false);
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync("SELECT count(*) FROM booking"));

        // Act again: up to the latest once more.
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // Assert again
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync(UnmarkedSql));
    }

    private async Task AssertColumnExistsAsync(bool expected)
    {
        int expectedCount = expected ? 1 : 0;
        Assert.Equal(expectedCount, await _database.CountAsync(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'booking' AND column_name = 'made_on_public_site' "
            + "AND data_type = 'boolean' AND is_nullable = 'NO' AND column_default = 'false'"));
    }
}
