using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

using Respawn;

using NFHotel.Infrastructure.Security;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Tests.Persistence;


/// <summary>
/// Fælles PostgreSQL test-container til Infrastructure integrationstests.
/// </summary>

// Fixture som opsætter en PostgreSQL-database til integrationstests.
// IAsyncLifetime bruges til asynkron opsætning og oprydning
// før og efter integrationstestene.
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    // Gemmer PostgreSQL test-containeren.
    // readonly = container-referencen skal ikke udskiftes senere.
    private readonly PostgreSqlContainer _container =
       new PostgreSqlBuilder("postgres:16")
           .Build();

    // Respawner bruges til at nulstille databasen mellem integrationstestene.
    private Respawner? _respawner;

    // Connection string til den midlertidige PostgreSQL test-database.
    public string ConnectionString => _container.GetConnectionString();

    // Starter PostgreSQL-containeren før integrationstestene køres.
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Oprette en fakeAES-256 nøgle kun til integrationstests.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        // Opretter den encryptor som HotelDbContext skal bruge til PassportNumber.
        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Konfigurerer HotelDbContext med connection string og encryptor.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // forbinder til den midlertidige PostgreSQL test-container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            ConnectionString);

        // Opretter HotelDbContext med testdatabasen og vores fake test-encryptor.
        await using var context = new HotelDbContext(optionsBuilder.Options, encryptor);

        // Kører projektets migrations på den midlertidige testdatabase.
        await context.Database.MigrateAsync();


        // Henter databaseforbindelsen så Respawn kan konfigureres til den midlertidige PostgreSQL testdatabase.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        // Opretter Respawner efter migrations er kørt så den kender databasestrukturen.
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres
            });

    }

    // Metode der nulstiller data i testdatabasen så hver integrationstest kan starte med en ren database.
    public async Task ResetDatabaseAsync()
    {

        // Kontrollerer at Respawner er blevet initialiseret.
        // Hvis den ikke er klar stoppes metoden med en exception.
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner did not initialise.");
        }


        // Opretter en ny forbindelse til PostgreSQL-testdatabasen ved hjælp af testdatabasens connection string.
        // await using sørger for at forbindelsen bliver frigivet korrekt bagefter.
        await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);

        await connection.OpenAsync();

        // Bruger Respawner til at nulstille data i testdatabasen.
        await _respawner.ResetAsync(connection);
    }


    // Stopper og rydder PostgreSQL-containeren op efter integrationstestene.
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

}
