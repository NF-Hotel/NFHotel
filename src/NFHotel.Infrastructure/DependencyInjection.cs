using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure;

/// <summary>
/// Registers the infrastructure — and the services of the Application layer — in the DI container.
/// </summary>
/// <remarks>
/// <b>Why the Application services are registered here.</b> Application has zero
/// NuGet packages and therefore must not reference <c>Microsoft.Extensions.DependencyInjection</c>.
/// The services are therefore registered from here. Web/Program.cs calls one method and knows neither
/// EF Core, Npgsql nor the repository types.
/// <para>
/// No assembly scanning. Ten explicit lines are easier to read than a convention that
/// finds the types, and a forgotten registration then fails at start-up instead of being
/// found by chance.
/// </para>
/// </remarks>
public static class DependencyInjection
{
    /// <summary>The name of the connection string in the configuration.</summary>
    public const string ConnectionStringName = "HotelDatabase";

    /// <summary>
    /// Registers data access, repositories, clock, encryption and the services of the Application layer.
    /// </summary>
    /// <param name="services">Servicesamlingen.</param>
    /// <param name="configuration">The configuration. Must contain the connection string and the encryption key.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown immediately if the connection string or the encryption key is missing.
    /// <b>The app must refuse to start without a key</b> (B-09) — the alternative is that passport numbers
    /// are saved in plain text without anyone noticing. The validation happens here and not as
    /// <c>ValidateOnStart</c>, so the error comes already when the container is built.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = ReadConnectionString(configuration);
        var encryptionOptions = ReadEncryptionOptions(configuration);
        var staffOptions = ReadStaffOptions(configuration);

        AddPersistence(services, connectionString);
        AddSecurity(services, encryptionOptions, staffOptions);
        AddApplicationServices(services);

        return services;
    }

    /// <summary>
    /// Sets provider and naming convention on a context builder.
    /// </summary>
    /// <param name="options">The builder being configured.</param>
    /// <param name="connectionString">The connection string to PostgreSQL.</param>
    /// <remarks>
    /// Shared by the DI registration and <see cref="HotelDbContextFactory"/>, so the design-time
    /// model is exactly the same as the operational one. Without it a migration could be
    /// generated against a different schema from the one the app runs on.
    /// </remarks>
    public static void ConfigureHotelDbContext(
        DbContextOptionsBuilder<HotelDbContext> options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsAssembly(typeof(HotelDbContext).Assembly.FullName)

                // Retries cover a brief network glitch, not a database that is down.
                // The default setting uses ~60 seconds per call to give up, and
                // it hits the receptionist as a page that hangs — worse than an error
                // she can react to. Three attempts over at most three seconds catch the glitch
                // and fail quickly on everything else.
                .EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(1),
                    errorCodesToAdd: null)

                // Without a limit a single heavy query can hold a SignalR circuit
                // hostage indefinitely.
                .CommandTimeout(20))

            // B-06: BookingId → booking_id, Booking → booking. C# stays PascalCase, and
            // PostgreSQL is spared quoted identifiers — U-19's casing chaos has nowhere
            // to arise.
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>Registers context, repositories and transaction boundary.</summary>
    private static void AddPersistence(IServiceCollection services, string connectionString)
    {
        // Blazor Server: a scope is the whole SignalR circuit, potentially hours. The factory
        // makes it possible for Web to create a fresh context per use case, while the
        // scoped adapter gives repositories and UnitOfWork exactly the SAME instance — otherwise
        // UnitOfWork would commit a different context from the one the repositories wrote to.
        services.AddDbContextFactory<HotelDbContext>(
            (_, options) => ConfigureHotelDbContext(
                (DbContextOptionsBuilder<HotelDbContext>)options,
                connectionString),
            ServiceLifetime.Scoped);

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<HotelDbContext>>().CreateDbContext());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingQueries, BookingRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomQueries, RoomRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IGuestQueries, GuestRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
    }

    /// <summary>Registers the encryption. Singleton: the key is immutable, and AesGcm is created per operation.</summary>
    private static void AddSecurity(IServiceCollection services, EncryptionOptions options, StaffSecurityOptions staffOptions)
    {
        services.AddSingleton(options);
        services.AddSingleton<IStringEncryptor, AesGcmStringEncryptor>();

        // ADR-0005: the staff sign-in. The throttle is a singleton because its counts must outlive a request.
        services.AddSingleton(staffOptions);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IStaffTokenIssuer, JwtStaffTokenIssuer>();
        services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
    }

    /// <summary>
    /// Registers the three services of the Application layer.
    /// </summary>
    /// <remarks>
    /// Scoped, because they hold repositories that hold a <c>DbContext</c>. They are
    /// stateless in themselves, but must not outlive their context.
    /// </remarks>
    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<IStaffAuthService, StaffAuthService>();
        services.AddScoped<IStaffAccountService, StaffAccountService>();
    }

    /// <summary>Reads the connection string and fails fast if it is missing.</summary>
    private static string ReadConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ConnectionStrings")[ConnectionStringName];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection stringen 'ConnectionStrings:{ConnectionStringName}' mangler i konfigurationen.");
        }

        return connectionString;
    }

    /// <summary>Reads the encryption settings. The key validation itself happens in the encryptor.</summary>
    private static EncryptionOptions ReadEncryptionOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(EncryptionOptions.SectionName);
        var options = new EncryptionOptions
        {
            PassportKey = section[EncryptionOptions.PassportKeyName] ?? string.Empty
        };

        // Fail fast: construct the encryptor now, so a missing or invalid key stops
        // the start-up rather than the first guest update (B-09).
        _ = new AesGcmStringEncryptor(options);

        return options;
    }

    private static StaffSecurityOptions ReadStaffOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(StaffSecurityOptions.SectionName);
        var signingKey = section["Token:SigningKey"]?.Trim() ?? string.Empty;
        if (signingKey.Length < StaffSecurityOptions.MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"'{StaffSecurityOptions.SectionName}:Token:SigningKey' must be at least {StaffSecurityOptions.MinimumSigningKeyLength} characters. Create one with: openssl rand -base64 48");
        }

        return new StaffSecurityOptions
        {
            SigningKey = signingKey,
            TokenLifetimeMinutes = ReadInt(section, "Token:LifetimeMinutes", 480),
            PasswordIterations = ReadInt(section, "PasswordIterations", StaffSecurityOptions.DefaultPasswordIterations),
            MaxFailures = ReadInt(section, "Throttle:MaxFailures", 5),
            ThrottleWindowMinutes = ReadInt(section, "Throttle:WindowMinutes", 15),
        };
    }

    private static int ReadInt(IConfigurationSection section, string key, int fallback) =>
        int.TryParse(section[key], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
}
