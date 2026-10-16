using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// Bygger en <see cref="HotelDbContext"/> til <c>dotnet ef</c>.
/// </summary>
/// <remarks>
/// Fabrikken findes af to grunde. Dels har <see cref="HotelDbContext"/> en afhængighed ud
/// over <c>DbContextOptions</c> (<see cref="IStringEncryptor"/>, B-09), så EF ikke selv kan
/// instantiere den. Dels skal migrationer kunne genereres og skriptes <b>uden</b> en
/// kørende database — derfor bruges der en connection string uden at der forbindes.
/// <para>
/// Connection stringen kan sættes med miljøvariablen
/// <see cref="ConnectionStringEnvironmentVariable"/> når <c>dotnet ef database update</c>
/// faktisk skal ramme en database.
/// </para>
/// </remarks>
public sealed class HotelDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
{
    /// <summary>Miljøvariablen der kan overstyre connection stringen ved designtid.</summary>
    public const string ConnectionStringEnvironmentVariable = "NFHOTEL_CONNECTION";

    private const string PlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=nfhotel;Username=postgres;Password=postgres";

    /// <inheritdoc />
    public HotelDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
            ?? PlaceholderConnectionString;

        var options = new DbContextOptionsBuilder<HotelDbContext>();

        DependencyInjection.ConfigureHotelDbContext(options, connectionString);

        return new HotelDbContext(options.Options, new DesignTimeStringEncryptor());
    }

    /// <summary>
    /// Krypteringstjeneste der aldrig krypterer.
    /// </summary>
    /// <remarks>
    /// Migrationsgenerering bygger kun modellen; value converterens udtryk oversættes til
    /// et kolonneudtryk og kaldes aldrig. En implementering der kaster er derfor ærligere
    /// end en indbygget designtidsnøgle, som kunne nå at kryptere rigtige data ved et uheld.
    /// </remarks>
    private sealed class DesignTimeStringEncryptor : IStringEncryptor
    {
        public string Encrypt(string plainText) => throw new NotSupportedException(Message);

        public string Decrypt(string cipherText) => throw new NotSupportedException(Message);

        private const string Message =
            "Designtidskonteksten kan ikke kryptere. Sæt en rigtig nøgle via AddInfrastructure "
            + "hvis du skal læse eller skrive data.";
    }
}