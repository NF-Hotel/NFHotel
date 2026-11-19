using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

using Npgsql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// A database of its own in the shared PostgreSQL container, for the tests of a migration of the booking table: a test
/// migrates it up and down and seeds it with SQL as an earlier release writes it, so the database the other tests share is
/// not touched. The test class calls <see cref="InitializeAsync"/> and <see cref="DisposeAsync"/> from its own.
/// </summary>
public sealed class MigrationDatabase : IAsyncLifetime
{
    /// <summary>The number of bookings that <see cref="InsertBookingsSql"/> inserts.</summary>
    public const int SeededBookings = 50;

    /// <summary>A room and a guest for the bookings of <see cref="InsertBookingsSql"/>.</summary>
    public const string InsertRoomAndGuestSql =
        "INSERT INTO room (room_number, floor, size, capacity, status, housekeeping_status) VALUES ('901', 9, 1, 2, 0, 0); "
        + "INSERT INTO guest (first_name, last_name, email, phone_number, country) "
        + "VALUES ('Anna', 'Jensen', 'anna@example.com', '+4512345678', 'Denmark')";

    /// <summary>
    /// Bookings as an earlier release writes them: only the columns that every release since MIL-084 has, so not the
    /// reference of MIL-085 nor the mark of MIL-087, one after the other in one room.
    /// </summary>
    public const string InsertBookingsSql =
        "INSERT INTO booking (room_id, guest_id, start_date, end_date, status) "
        + "SELECT (SELECT room_id FROM room WHERE room_number = '901'), (SELECT guest_id FROM guest WHERE email = 'anna@example.com'), "
        + "DATE '2027-01-01' + g * 3, DATE '2027-01-01' + g * 3 + 2, 0 FROM generate_series(1, 50) AS g";

    private readonly PostgreSqlFixture _fixture;
    private readonly string _databaseName = "mig_" + Guid.NewGuid().ToString("N");

    private string _connectionString = string.Empty;

    public MigrationDatabase(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Creates the empty database; runs before each test.</summary>
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

    /// <summary>Drops the database, also when the test left a connection open.</summary>
    public async ValueTask DisposeAsync()
    {
        await using NpgsqlConnection admin = new(_fixture.ConnectionString);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>The id of the migration whose name ends with the suffix, and of the migration right before it.</summary>
    public static (string Before, string Migration) Migrations(HotelDbContext context, string suffix)
    {
        List<string> migrations = [.. context.Database.GetMigrations()];
        int index = migrations.FindIndex(id => id.EndsWith(suffix, StringComparison.Ordinal));
        Assert.True(index > 0, $"The migration {suffix} must exist and have a predecessor.");

        return (migrations[index - 1], migrations[index]);
    }

    /// <summary>Migrates up or down to the migration with the id.</summary>
    public static Task MigrateToAsync(HotelDbContext context, string target) =>
        context.GetService<IMigrator>().MigrateAsync(target, TestContext.Current.CancellationToken);

    /// <summary>Opens a context on the database with the encryptor of the tests.</summary>
    public HotelDbContext CreateContext()
    {
        DbContextOptionsBuilder<HotelDbContext> optionsBuilder = new();
        DependencyInjection.ConfigureHotelDbContext(optionsBuilder, _connectionString);
        AesGcmStringEncryptor encryptor = new(new EncryptionOptions { PassportKey = Convert.ToBase64String(new byte[32]) });
        return new HotelDbContext(optionsBuilder.Options, encryptor);
    }

    /// <summary>Runs SQL on the database, as an earlier release or an operator would.</summary>
    public async Task ExecuteAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The number that a <c>SELECT count(*)</c> answers.</summary>
    public async Task<long> CountAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        object? scalar = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return Assert.IsType<long>(scalar);
    }
}
