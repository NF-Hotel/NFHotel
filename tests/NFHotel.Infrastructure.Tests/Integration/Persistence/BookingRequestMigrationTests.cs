using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The migration of MIL-088 (ADR-0052 decisions 6 and 7) against a real PostgreSQL: it only adds the tables of the booking
/// requests and of the records of the mails, so the release before keeps working against the migrated database, and it
/// goes back down without losing a booking. Each test makes a database of its own in the shared container.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingRequestMigrationTests : IAsyncLifetime
{
    private const string RequestMigration = "_AddBookingRequests";
    private const string NewTables = "('booking_request', 'sent_mail')";
    private const string TablesSql = "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN " + NewTables;
    private const string OtherColumnsSql =
        "SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name NOT IN " + NewTables;
    private const string OtherIndexesSql = "SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename NOT IN " + NewTables;
    private const string BookingsSql = "SELECT count(*) FROM booking";

    private readonly MigrationDatabase _database;

    public BookingRequestMigrationTests(PostgreSqlFixture fixture)
    {
        _database = new MigrationDatabase(fixture);
    }

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    /// <summary>
    /// Integration (MIL-088 task 5): the migration adds the two tables and changes no column and no index of another
    /// table, so a rollback of the release stays possible.
    /// </summary>
    [Fact]
    public async Task Migrate_TheRequestMigration_AddsTheTwoTablesOnly()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        (string before, string migration) = MigrationDatabase.Migrations(context, RequestMigration);
        await MigrationDatabase.MigrateToAsync(context, before);
        long columnsBefore = await _database.CountAsync(OtherColumnsSql);
        long indexesBefore = await _database.CountAsync(OtherIndexesSql);

        // Act
        await MigrationDatabase.MigrateToAsync(context, migration);

        // Assert
        Assert.Equal(2, await _database.CountAsync(TablesSql));
        Assert.Equal(columnsBefore, await _database.CountAsync(OtherColumnsSql));
        Assert.Equal(indexesBefore, await _database.CountAsync(OtherIndexesSql));
    }

    /// <summary>Integration: the migration goes back down: the two tables are gone, and the bookings stay.</summary>
    [Fact]
    public async Task Migrate_DownFromTheRequestMigration_DropsTheTablesAndKeepsTheBookings()
    {
        // Arrange
        await using HotelDbContext context = _database.CreateContext();
        (string before, string migration) = MigrationDatabase.Migrations(context, RequestMigration);
        await MigrationDatabase.MigrateToAsync(context, migration);
        await _database.ExecuteAsync(MigrationDatabase.InsertRoomAndGuestSql);
        await _database.ExecuteAsync(MigrationDatabase.InsertBookingsSql);

        // Act
        await MigrationDatabase.MigrateToAsync(context, before);

        // Assert
        Assert.Equal(0, await _database.CountAsync(TablesSql));
        Assert.Equal(MigrationDatabase.SeededBookings, await _database.CountAsync(BookingsSql));
    }
}
