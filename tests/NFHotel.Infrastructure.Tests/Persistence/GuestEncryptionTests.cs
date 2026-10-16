using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for encryption of Guest passport data in PostgreSQL.
/// </summary>
[Collection(PostgreSqlCollection.Name)]

// Test class that tests encryption of Guest data against PostgreSQL
public sealed class GuestEncryptionTests : IAsyncLifetime
{

    // Keeps a reference to the shared PostgreSQL fixture
    // The tests in the class can use the test database/container
    private readonly PostgreSqlFixture _fixture;


    // Constructor that xUnit calls and automatically gives the shared PostgreSqlFixture.
    public GuestEncryptionTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }



    // Runs before each test and resets the test database so the test starts with a clean database.
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    // Runs after each test.
    // the database is reset before the next test.
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }


    // Test Metode
    // The test must verify that PassportNumber is encrypted in the database and decrypted correctly by EF Core.
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

        // Creates a fake AES-256 key only for the integration test.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Creates the configuration for HotelDbContext.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // Uses the same EF Core configuration as the project,
        // but the connection string points to the temporary test container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            _fixture.ConnectionString);

        // Creates a DbContext for the integration test itself.
        await using var context = new HotelDbContext(optionsBuilder.Options, encryptor);




        // Act
        // Adds the guest to EF Core and saves it in the test database.
        context.Guests.Add(guest);
        // Saves the changes from the context in the database asynchronously.
        await context.SaveChangesAsync();

        // Fetches the underlying database connection,
        // reads passport_number directly with raw SQL.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();


        // Creates a raw SQL command directly against PostgreSQL.
        await using var command = connection.CreateCommand();

        command.CommandText =
            "SELECT passport_number FROM guest WHERE guest_id = @guestId";


        // Adds GuestId as a parameter to the SQL command.
        var guestIdParameter = command.CreateParameter();

        // Specifies the name of the parameter, which must match @guestId in the SQL command.
        guestIdParameter.ParameterName = "@guestId";
        guestIdParameter.Value = guest.Id;

        // Adds the @guestId parameter to the SQL command,
        // so the database can use the value when the command is executed.
        command.Parameters.Add(guestIdParameter);

        // Runs the SQL command and fetches passport_number directly from the database.
        var storedPassportNumber = await command.ExecuteScalarAsync();




        // Assert
        // The passport number in the database must not be stored as plaintext.
        Assert.NotEqual(passportNumber, storedPassportNumber?.ToString());

        // Removes the already tracked Guest from the context,
        // EF Core is forced to fetch the guest again from the database.
        context.ChangeTracker.Clear();

        // Fetches the guest again through EF Core.
        var loadedGuest = await context.Guests
            .SingleAsync(g => g.Id == guest.Id);

        // Verifies that EF Core has decrypted PassportNumber correctly.
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


        // Creates a fake AES-256 key only for the integration test.
        var encryptionOptions = new EncryptionOptions
        {
            PassportKey = Convert.ToBase64String(new byte[32])
        };

        var encryptor = new AesGcmStringEncryptor(encryptionOptions);

        // Creates the configuration for HotelDbContext.
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

        // Connects to the temporary PostgreSQL test container.
        DependencyInjection.ConfigureHotelDbContext(
            optionsBuilder,
            _fixture.ConnectionString);

        // Creates a DbContext for the integration test.
        await using var context = new HotelDbContext(
            optionsBuilder.Options,
            encryptor);




        // Act
        // Saves the guest without passport number in the test database.
        context.Guests.Add(guest);
        await context.SaveChangesAsync();

        // Fetches the underlying database connection,
        // checks passport_number directly with raw SQL.
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        // Creates a raw SQL command directly against PostgreSQL.
        await using var command = connection.CreateCommand();

        command.CommandText =
            "SELECT passport_number IS NULL FROM guest WHERE guest_id = @guestId";

        // Creates a parameter for the SQL command.
        var guestIdParameter = command.CreateParameter();
        guestIdParameter.ParameterName = "@guestId";
        guestIdParameter.Value = guest.Id;

        // Adds the parameter to the SQL command.
        command.Parameters.Add(guestIdParameter);

        // Runs the SQL command and fetches the result.
        var result = await command.ExecuteScalarAsync();

        // Assert
        // Verifies that passport_number is actually SQL NULL in the database.
        Assert.Equal(true, result);

    }
}