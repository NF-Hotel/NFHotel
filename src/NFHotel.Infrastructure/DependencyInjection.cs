using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Holidays;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.Infrastructure.Security;
using NFHotel.Infrastructure.Time;

namespace NFHotel.Infrastructure;

/// <summary>
/// Registrerer infrastrukturen — og Application-lagets services — i DI-containeren.
/// </summary>
/// <remarks>
/// <b>Hvorfor Application-servicerne registreres her.</b> Application har nul
/// NuGet-pakker og må derfor ikke referere <c>Microsoft.Extensions.DependencyInjection</c>.
/// Servicerne registreres derfor herfra. Web/Program.cs kalder én metode og kender hverken
/// EF Core, Npgsql eller repository-typerne.
/// <para>
/// Ingen assembly-scanning. Ti eksplicitte linjer er lettere at læse end en konvention der
/// finder typerne, og en glemt registrering fejler ved opstart i stedet for at blive
/// opdaget ved et tilfælde.
/// </para>
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Navnet på connection stringen i konfigurationen.</summary>
    public const string ConnectionStringName = "HotelDatabase";

    /// <summary>
    /// Registrerer databaseadgang, repositories, ur, kryptering og Application-lagets services.
    /// </summary>
    /// <param name="services">Servicesamlingen.</param>
    /// <param name="configuration">Konfigurationen. Skal indeholde connection string og krypteringsnøgle.</param>
    /// <returns>Samme servicesamling, så kald kan kædes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Kastes hvis <paramref name="services"/> eller <paramref name="configuration"/> er <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Kastes med det samme hvis connection stringen eller krypteringsnøglen mangler.
    /// <b>Appen skal nægte at starte uden nøgle</b> (B-09) — alternativet er at pasnumre
    /// gemmes i klartekst uden at nogen opdager det. Valideringen sker her og ikke som
    /// <c>ValidateOnStart</c>, så fejlen kommer allerede når containeren bygges.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = ReadConnectionString(configuration);
        var encryptionOptions = ReadEncryptionOptions(configuration);
        var timeOptions = ReadTimeOptions(configuration);
        var holidayApiOptions = ReadHolidayApiOptions(configuration);

        AddPersistence(services, connectionString);
        AddSecurity(services, encryptionOptions);
        AddTime(services, timeOptions);
        AddHolidayApi(services, holidayApiOptions);
        AddApplicationServices(services);

        return services;
    }

    /// <summary>
    /// Sætter provider og navnekonvention på en kontekst-builder.
    /// </summary>
    /// <param name="options">Builderen der konfigureres.</param>
    /// <param name="connectionString">Connection stringen til PostgreSQL.</param>
    /// <remarks>
    /// Delt af DI-registreringen og <see cref="HotelDbContextFactory"/>, så designtidens
    /// model er nøjagtig den samme som driftens. Uden det kunne en migration blive
    /// genereret mod et andet skema end det appen kører på.
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

                // Genforsøg dækker et kortvarigt netværksglip, ikke en nede database.
                // Standardindstillingen bruger ~60 sekunder pr. kald på at give op, og
                // det rammer receptionisten som en side der hænger — værre end en fejl
                // hun kan reagere på. Tre forsøg over højst tre sekunder fanger glippet
                // og fejler hurtigt på alt andet.
                .EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(1),
                    errorCodesToAdd: null)

                // Uden en grænse kan en enkelt tung forespørgsel holde et SignalR-kredsløb
                // i gidsel på ubestemt tid.
                .CommandTimeout(20))

            // B-06: BookingId → booking_id, Booking → booking. C# forbliver PascalCase, og
            // PostgreSQL slipper for quotede identifiers — U-19's casing-kaos har ingen
            // steder at opstå.
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>Registrerer kontekst, repositories og transaktionsgrænse.</summary>
    private static void AddPersistence(IServiceCollection services, string connectionString)
    {
        // Blazor Server: en scope er hele SignalR-kredsløbet, potentielt timer. Fabrikken
        // gør det muligt for Web at oprette en frisk kontekst pr. use case, mens den
        // scopede adapter giver repositories og UnitOfWork præcis SAMME instans — ellers
        // ville UnitOfWork committe en anden kontekst end den repositories skrev til.
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
    }

    /// <summary>Registrerer krypteringen. Singleton: nøglen er uforanderlig, og AesGcm laves pr. operation.</summary>
    private static void AddSecurity(IServiceCollection services, EncryptionOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IStringEncryptor, AesGcmStringEncryptor>();
    }

    /// <summary>Registrerer uret. Singleton: tilstandsløst bortset fra den uforanderlige tidszone.</summary>
    private static void AddTime(IServiceCollection services, HotelTimeOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IClock, SystemClock>();
    }

    /// <summary>
    /// Registrerer klienten til analysetjenestens helligdagskalender som en typed
    /// <see cref="HttpClient"/> (<see cref="IHttpClientFactory"/> ejer handlerens levetid).
    /// </summary>
    private static void AddHolidayApi(IServiceCollection services, HolidayApiOptions options)
    {
        services.AddSingleton(options);

        services.AddHttpClient<IHolidayCalendarClient, HolidayApiClient>(client =>
        {
            // Ugyldig adresse eller manglende nøgle er ikke en opstartsfejl: klienten svarer
            // holidays.not_configured (se HolidayApiOptions). Vi konfigurerer derfor kun det der kan.
            if (options.TryGetBaseUri(out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add(HolidayApiOptions.ApiKeyHeaderName, options.ApiKey);
            }
        });
    }

    /// <summary>
    /// Registrerer Application-lagets tre services.
    /// </summary>
    /// <remarks>
    /// Scoped, fordi de holder repositories der holder en <c>DbContext</c>. De er
    /// tilstandsløse i sig selv, men må ikke overleve deres kontekst.
    /// </remarks>
    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IGuestService, GuestService>();
    }

    /// <summary>Læser connection stringen og fejler hurtigt hvis den mangler.</summary>
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

    /// <summary>Læser krypteringsindstillingerne. Selve nøglevalideringen sker i encryptoren.</summary>
    private static EncryptionOptions ReadEncryptionOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(EncryptionOptions.SectionName);
        var options = new EncryptionOptions
        {
            PassportKey = section[EncryptionOptions.PassportKeyName] ?? string.Empty
        };

        // Fail fast: konstruér encryptoren nu, så en manglende eller ugyldig nøgle stopper
        // opstarten frem for den første gæsteopdatering (B-09).
        _ = new AesGcmStringEncryptor(options);

        return options;
    }

    /// <summary>
    /// Læser indstillingerne til analysetjenesten. Fejler ikke ved manglende værdier — se
    /// <see cref="HolidayApiOptions"/> — men falder tilbage til standardadresse og -timeout.
    /// </summary>
    private static HolidayApiOptions ReadHolidayApiOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(HolidayApiOptions.SectionName);
        var baseUrl = section[HolidayApiOptions.BaseUrlName];

        var timeout = int.TryParse(
            section[HolidayApiOptions.TimeoutSecondsName],
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds) && seconds > 0
            ? seconds
            : HolidayApiOptions.DefaultTimeoutSeconds;

        return new HolidayApiOptions
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? HolidayApiOptions.DefaultBaseUrl : baseUrl,
            ApiKey = section[HolidayApiOptions.ApiKeyName]?.Trim() ?? string.Empty,
            TimeoutSeconds = timeout,
        };
    }

    /// <summary>Læser tidsindstillingerne og verificerer at tidszonen findes på maskinen.</summary>
    private static HotelTimeOptions ReadTimeOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(HotelTimeOptions.SectionName);
        var timeZoneId = section[HotelTimeOptions.TimeZoneIdName];

        var options = new HotelTimeOptions
        {
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
                ? HotelTimeOptions.DefaultTimeZoneId
                : timeZoneId
        };

        // Fail fast: en ukendt tidszone må ikke først opdages når nogen tjekker ind (A-05).
        _ = new SystemClock(options);

        return options;
    }
}