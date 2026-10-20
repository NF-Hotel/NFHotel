using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// Builds a <see cref="HotelDbContext"/> for <c>dotnet ef</c>.
/// </summary>
/// <remarks>
/// The factory exists for two reasons. First, <see cref="HotelDbContext"/> has a dependency beyond
/// <c>DbContextOptions</c> (<see cref="IStringEncryptor"/>, B-09), so EF cannot instantiate it
/// itself. Second, migrations must be possible to generate and script <b>without</b> a
/// running database — therefore a connection string is used without connecting.
/// <para>
/// The connection string can be set with the environment variable
/// <see cref="ConnectionStringEnvironmentVariable"/> when <c>dotnet ef database update</c>
/// actually has to hit a database.
/// </para>
/// </remarks>
public sealed class HotelDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
{
    /// <summary>The environment variable that can override the connection string at design time.</summary>
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
    /// An encryption service that never encrypts.
    /// </summary>
    /// <remarks>
    /// Migration generation only builds the model; the expression of the value converter is translated to
    /// a column expression and never called. An implementation that throws is therefore more honest
    /// than a built-in design-time key, which could end up encrypting real data by accident.
    /// </remarks>
    private sealed class DesignTimeStringEncryptor : IStringEncryptor
    {
        public string Encrypt(string plainText) => throw new NotSupportedException(Message);

        public string Decrypt(string cipherText) => throw new NotSupportedException(Message);

        private const string Message =
            "The design-time context cannot encrypt. Set a real key through AddInfrastructure "
            + "if you need to read or write data.";
    }
}
