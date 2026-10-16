using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

using Respawn;

using Testcontainers.PostgreSql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;


/// <summary>
/// Shared PostgreSQL test container for Infrastructure integration tests.
/// </summary>

// Fixture that sets up a PostgreSQL database for integration tests.
// IAsyncLifetime is used for asynchronous set-up and clean-up
// before and after the integration tests.
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    // Keeps the PostgreSQL test container.
    // readonly = the container reference must not be replaced later.
    private readonly PostgreSqlContainer _container =
       new PostgreSqlBuilder("postgres:16")
           .Build();

    // Respawner is used to reset the database between the integration tests.
    private Respawner? _respawner;

    // Connection string for the temporary PostgreSQL test database.
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Opens a new context on the test database, as a separate request would.</summary>
    public HotelDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();
        DependencyInjection.ConfigureHotelDbContext(optionsBuilder, ConnectionString);
        var encryptor = new AesGcmStringEncryptor(new EncryptionOptions { PassportKey = Convert.ToBase64String(new byte[32]) });
        return new HotelDbContext(optionsBuilder.Options, encryptor);
    }

    // Starts the PostgreSQL container before the integration tests are run.
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Creates a fake AES-256 key only for integration tests.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        // Creates the encryptor HotelDbContext must use for PassportNumber.
        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Configures HotelDbContext with connection string and encryptor.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // connects to the temporary PostgreSQL test container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            ConnectionString);

        // Creates HotelDbContext with the test database and our fake test encryptor.
        await using var context = new HotelDbContext(optionsBuilder.Options, encryptor);

        // Runs the project's migrations on the temporary test database.
        await context.Database.MigrateAsync();


        // Fetches the database connection so Respawn can be configured for the temporary PostgreSQL test database.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        // Creates Respawner after migrations have been run so it knows the database structure.
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres
            });

    }

    // Method that resets the data in the test database so each integration test can start with a clean database.
    public async Task ResetDatabaseAsync()
    {

        // Checks that Respawner has been initialised.
        // If it is not ready the method is stopped with an exception.
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner did not initialise.");
        }


        // Creates a new connection to the PostgreSQL test database using the connection string of the test database.
        // await using ensures that the connection is released correctly afterwards.
        await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);

        await connection.OpenAsync();

        // Uses Respawner to reset data in the test database.
        await _respawner.ResetAsync(connection);
    }


    // Stops and cleans up the PostgreSQL container after the integration tests.
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

}