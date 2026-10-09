using Microsoft.EntityFrameworkCore;



using NFHotel.Infrastructure.Security;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Domain.Guests;

namespace NFHotel.Infrastructure.Tests.Persistence;

/// <summary>
/// Integrationstests for kryptering af Guest passportdata i PostgreSQL.
/// </summary>
[Collection(PostgreSqlCollection.Name)]

// Testklasse der skal teste kryptering af Guest-data mod PostgreSQL
public sealed class GuestEncryptionTests : IAsyncLifetime
{

    // Gemmer en reference til den fælles PostgreSQL-fixture
    // Testene i klassen kan bruge testdatabasen/containeren
    private readonly PostgreSqlFixture _fixture;


    // Constructor som xUnit kalder og automatisk giver den fælles PostgreSqlFixture til.
    public GuestEncryptionTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }



    // Kører før hver test og nulstiller testdatabasen så testen starter med en ren database.
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    // Kører efter hver test.
    // databasen nulstilles før næste test.
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }


    // Test Metode
    // Testen skal verificere at PassportNumber bliver krypteret i databasen og dekrypteret korrekt af EF Core.
    [Fact]
    public async Task PassportNumber_ShouldBeEncryptedInDatabase_AndDecryptedByEfCore()
    {
        // Arrange
        const string passportNumber = "ABCD1234";

        var guest = Guest.Create(
            firstName: "Test",
            lastName: "Guest",
            email: "test@example.com",
            phoneNumber: "12345678",
            country: "Testland",
            passportNumber: passportNumber);

        // Opretter en fake AES-256 nøgle kun til integrationstesten.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Opretter konfigurationen til HotelDbContext.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // Bruger samme EF Core-konfiguration som projektet,
        // men connection stringen peger på den midlertidige test-container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            _fixture.ConnectionString);

        // Opretter en DbContext til selve integrationstesten.
        await using var context = new HotelDbContext(optionsBuilder.Options, encryptor);




        // Act
        // Tilføjer gæsten til EF Core og gemmer den i testdatabasen.
        context.Guests.Add(guest);
        // Gemmer ændringerne fra contexten i databasen asynkront.
        await context.SaveChangesAsync();

        // Henter den underliggende databaseforbindelse,
        // læse passport_number direkte med raw SQL.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();


        // Opretter en raw SQL-kommando direkte mod PostgreSQL.
        await using var command = connection.CreateCommand();

        command.CommandText =
            "SELECT passport_number FROM guest WHERE guest_id = @guestId";


        // Tilføjer GuestId som parameter til SQL-kommandoen.
        var guestIdParameter = command.CreateParameter();

        // Angiver parameterens navn, som skal matche @guestId i SQL-kommandoen.
        guestIdParameter.ParameterName = "@guestId";
        guestIdParameter.Value = guest.GuestId;

        // Tilføjer @guestId-parameteren til SQL-kommandoen,
        // så databasen kan bruge værdien når kommandoen udføres.
        command.Parameters.Add(guestIdParameter);

        // Kører SQL-kommandoen og henter passport_number direkte fra databasen.
        var storedPassportNumber = await command.ExecuteScalarAsync();




        // Assert
        // Pasnummeret i databasen må ikke være gemt som plaintext.
        Assert.NotEqual(passportNumber, storedPassportNumber?.ToString());

        // Fjerner den allerede trackede Guest fra contexten,
        // EF Core bliver tvunget til at hente gæsten igen fra databasen.
        context.ChangeTracker.Clear();

        // Henter gæsten igen gennem EF Core.
        var loadedGuest = await context.Guests
            .SingleAsync(g => g.GuestId == guest.GuestId);

        // Verificerer EF Core har dekrypteret PassportNumber korrekt.
        Assert.Equal(passportNumber, loadedGuest.PassportNumber);

    }


    [Fact]
    public async Task NullPassportNumber_ShouldRemainNullInDatabase()
    {
        // Arrange
        var guest = Guest.Create(
          firstName: "Test",
          lastName: "NoPassport",
          email: "nopassport@example.com",
          phoneNumber: "12345678",
          country: "Testland",
          passportNumber: null);


        // Opretter en fake AES-256 nøgle kun til integrationstesten.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Opretter konfigurationen til HotelDbContext.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // Forbinder til den midlertidige PostgreSQL test-container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            _fixture.ConnectionString);

        // Opretter en DbContext til integrationstesten.
        await using var context = new HotelDbContext(
            optionsBuilder.Options,
            encryptor);




        // Act
        // Saver gæsten uden pasnummer i testdatabasen.
        context.Guests.Add(guest);
        await context.SaveChangesAsync();

        // Henter den underliggende databaseforbindelse,
        // kontrollere passport_number direkte med raw SQL.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        // Opretter en raw SQL-kommando direkte mod PostgreSQL.
        await using var command = connection.CreateCommand();

        command.CommandText =
            "SELECT passport_number IS NULL FROM guest WHERE guest_id = @guestId";

        // Opretter en parameter til SQL-kommandoen.
        var guestIdParameter = command.CreateParameter();
        guestIdParameter.ParameterName = "@guestId";
        guestIdParameter.Value = guest.GuestId;

        // Tilføjer parameteren til SQL-kommandoen.
        command.Parameters.Add(guestIdParameter);

        // Kører SQL-kommandoen og henter resultatet.
        var result = await command.ExecuteScalarAsync();

        // Assert
        // Verificerer at passport_number faktisk er SQL NULL i databasen.
        Assert.Equal(true, result);

    }
}