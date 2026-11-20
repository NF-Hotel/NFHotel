using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Logging;
using NFHotel.Infrastructure.Mail;
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
    /// <param name="services">The service collection.</param>
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
        GuestLookupOptions guestLookupOptions = ReadGuestLookupOptions(configuration);
        SenderLimitOptions senderLimitOptions = ReadSenderLimitOptions(configuration);
        SmtpOptions smtpOptions = MailSettings.ReadSmtp(configuration);
        BookingVerificationOptions verificationOptions = MailSettings.ReadVerification(configuration, smtpOptions);

        AddPersistence(services, connectionString);
        AddSecurity(services, encryptionOptions, staffOptions, guestLookupOptions, senderLimitOptions);
        AddMail(services, smtpOptions, verificationOptions);
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
        services.AddScoped<IBookingRequestRepository, BookingRequestRepository>();
        services.AddScoped<ISentMailRepository, SentMailRepository>();
        services.AddScoped<IBookingQueries, BookingRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomQueries, RoomRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IGuestQueries, GuestRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IStaffAuditRepository, StaffAuditRepository>();
        services.AddScoped<IBenchmarkRunRepository, BenchmarkRunRepository>();
        services.AddScoped<IBenchmarkRequestRepository, BenchmarkRequestRepository>();
        services.AddScoped<IAiDefaultRepository, AiDefaultRepository>();
        services.AddScoped<IRoomRateRepository, RoomRateRepository>();
        services.AddScoped<IBookingMarketDetailRepository, BookingMarketDetailRepository>();
        services.AddScoped<IPricingSettingRepository, PricingSettingRepository>();
        services.AddScoped<IPriceChangeRepository, PriceChangeRepository>();
        services.AddScoped<IMarketAnalysisRunRepository, MarketAnalysisRunRepository>();
        services.AddScoped<IBenchmarkQueries, BenchmarkRunRepository>();
    }

    /// <summary>Registers the encryption. Singleton: the key is immutable, and AesGcm is created per operation.</summary>
    private static void AddSecurity(
        IServiceCollection services,
        EncryptionOptions options,
        StaffSecurityOptions staffOptions,
        GuestLookupOptions guestLookupOptions,
        SenderLimitOptions senderLimitOptions)
    {
        services.AddSingleton(options);
        services.AddSingleton<IStringEncryptor, AesGcmStringEncryptor>();

        // ADR-0005: the staff sign-in. The throttle is a singleton because its counts must outlive a request.
        services.AddSingleton(staffOptions);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IStaffTokenIssuer, JwtStaffTokenIssuer>();
        services.AddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
        services.AddSingleton<IWarningLog, LoggerWarningLog>();

        // A caller that is not an HTTP request knows nobody; the API replaces this with the one that reads the token (ADR-0017).
        services.TryAddScoped<IStaffCallContext, AnonymousStaffCallContext>();

        // MIL-018: the guest token is signed with the same key, and a caller that is not an HTTP request is no guest; the
        // API replaces the context with the one that reads the guest token.
        services.AddSingleton<IGuestTokenIssuer, JwtGuestTokenIssuer>();
        services.TryAddScoped<IGuestCallContext, AnonymousGuestCallContext>();

        // ADR-0049 decision 6: the limits of the guest sign-in are one store for the whole process, apart from the
        // throttle of the staff sign-in; a caller that is not an HTTP request has no address, and the API replaces it.
        services.AddSingleton(guestLookupOptions);
        services.AddSingleton<IGuestSignInLimits, InMemoryGuestSignInLimits>();
        services.TryAddScoped<ICallerAddress, UnknownCallerAddress>();

        // ADR-0051 decision 5: the counts per address of the searches and of the public booking requests are one store
        // for the whole process, like the limits of the guest sign-in. The limit per email (decision 3) is counted in
        // the database; the use case reads its number through a port of its own (review RC-428).
        services.AddSingleton(senderLimitOptions);
        services.AddSingleton<ISenderLimits, InMemorySenderLimits>();
        services.AddSingleton<IPublicBookingEmailLimit, ConfiguredPublicBookingEmailLimit>();
    }

    /// <summary>
    /// Registers the settings of the verification and the adapter that sends the mails (MIL-088, ADR-0052 decision 8
    /// and ADR-0053). The adapter is a singleton: it holds only the settings, and each mail opens a connection of its own.
    /// It is made here with the bound of a send of production (ADR-0053 decision 1); its second constructor is the tests'.
    /// </summary>
    private static void AddMail(IServiceCollection services, SmtpOptions smtpOptions, BookingVerificationOptions verificationOptions)
    {
        services.AddSingleton(smtpOptions);
        services.AddSingleton(verificationOptions);
        services.AddSingleton<IBookingVerificationSettings, ConfiguredBookingVerificationSettings>();
        services.AddSingleton<IMailSender>(new SmtpMailSender(smtpOptions));
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
        // One booking service per scope behind two doors: the use cases of staff, and the public booking whose answer
        // holds the key of the new booking, which only the guest sign-in reads (ADR-0049 decision 3).
        services.AddScoped<BookingService>();
        services.AddScoped<IBookingService>(provider => provider.GetRequiredService<BookingService>());
        services.AddScoped<IPublicBookingService>(provider => provider.GetRequiredService<BookingService>());

        // MIL-088: the booking request and its verification, and the clean-up that the API runs on a timer.
        services.AddScoped<IBookingRequestService, BookingRequestService>();
        services.AddScoped<IBookingRequestCleanup, BookingRequestCleanup>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAiDefaultService, AiDefaultService>();
        services.AddScoped<IRoomRateService, RoomRateService>();
        services.AddScoped<IPricingSettingsService, PricingSettingsService>();
        services.AddScoped<IPriceChangeService, PriceChangeService>();
        services.AddScoped<IPriceSuggestionService, PriceSuggestionService>();
        services.AddScoped<IMarketAnalysisService, MarketAnalysisService>();
        services.AddScoped<MarketAnalysisRunner>();
        services.AddScoped<IMarketAnalysisBookingQueries, MarketAnalysisBookingQueries>();

        // One run at a time for the whole API, so the tracker outlives a request.
        services.AddSingleton<MarketAnalysisRunTracker>();
        services.AddScoped<BenchmarkService>();
        services.AddScoped<IBenchmarkService>(provider => provider.GetRequiredService<BenchmarkService>());
        services.AddScoped<IBenchmarkIntake>(provider => provider.GetRequiredService<BenchmarkService>());
        services.AddScoped<IBenchmarkRequestService, BenchmarkRequestService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<IGuestAccessService, GuestAccessService>();
        services.AddScoped<IGuestBookingService, GuestBookingService>();
        services.AddScoped<IStaffAuthService, StaffAuthService>();
        services.AddScoped<StaffAccountService>();
        services.AddScoped<IStaffAccountService>(provider => provider.GetRequiredService<StaffAccountService>());
        services.AddScoped<IStaffAdminService>(provider => provider.GetRequiredService<StaffAccountService>());

        // Registers the staff authorization service for dependency injection.
        services.AddScoped<IStaffAuthorizationService, StaffAuthorizationService>();
    }

    /// <summary>Reads the connection string and fails fast if it is missing.</summary>
    private static string ReadConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ConnectionStrings")[ConnectionStringName];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string 'ConnectionStrings:{ConnectionStringName}' is missing from the configuration.");
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

    /// <summary>
    /// Reads the limits of the guest sign-in (ADR-0049 decision 6). A setting below one stops the start-up, so a typing
    /// mistake cannot switch a limit off, and so does a limit of a window above the cap of its store (review RC-428).
    /// </summary>
    private static GuestLookupOptions ReadGuestLookupOptions(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(GuestLookupOptions.SectionName);
        GuestLookupOptions defaults = new();
        GuestLookupOptions options = new()
        {
            MaxFailuresPerReference = ReadWindowLimit(section, nameof(GuestLookupOptions.MaxFailuresPerReference), defaults.MaxFailuresPerReference),
            MaxFailuresPerAddress = ReadWindowLimit(section, nameof(GuestLookupOptions.MaxFailuresPerAddress), defaults.MaxFailuresPerAddress),
            WindowMinutes = ReadLimit(section, nameof(GuestLookupOptions.WindowMinutes), defaults.WindowMinutes),
            MaxTrackedKeys = ReadLimit(section, nameof(GuestLookupOptions.MaxTrackedKeys), defaults.MaxTrackedKeys),
        };

        return options;
    }

    /// <summary>
    /// Reads the limits of the booking flow (MIL-087, ADR-0051 decision 5). A setting below one stops the start-up, as
    /// for the guest sign-in, so a typing mistake cannot switch a limit off, and so does a limit of a window above the cap
    /// of its store (review RC-428).
    /// </summary>
    private static SenderLimitOptions ReadSenderLimitOptions(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(SenderLimitOptions.SectionName);
        SenderLimitOptions defaults = new();
        SenderLimitOptions options = new()
        {
            SearchesPerMinute = ReadWindowLimit(section, nameof(SenderLimitOptions.SearchesPerMinute), defaults.SearchesPerMinute),
            BookingRequestsPerHour = ReadWindowLimit(section, nameof(SenderLimitOptions.BookingRequestsPerHour), defaults.BookingRequestsPerHour),
            BookingRequestsPerDay = ReadWindowLimit(section, nameof(SenderLimitOptions.BookingRequestsPerDay), defaults.BookingRequestsPerDay),
            ComingPublicBookingsPerEmail = ReadLimit(section, nameof(SenderLimitOptions.ComingPublicBookingsPerEmail), defaults.ComingPublicBookingsPerEmail),
            MaxTrackedKeys = ReadLimit(section, nameof(SenderLimitOptions.MaxTrackedKeys), defaults.MaxTrackedKeys),
        };

        return options;
    }

    /// <summary>Reads the number of a limit, which must be at least one.</summary>
    /// <exception cref="InvalidOperationException">The setting is below one; the message names it by its path.</exception>
    private static int ReadLimit(IConfigurationSection section, string key, int fallback)
    {
        int value = ReadInt(section, key, fallback);
        return value >= 1
            ? value
            : throw new InvalidOperationException($"'{section.Path}:{key}' must be at least 1.");
    }

    /// <summary>
    /// Reads the number of a limit that a store of windows counts up to, which must be at least one and at most
    /// <see cref="BoundedWindowStore{TKey}.MaxLimit"/>, so the start-up refuses what the store would refuse later.
    /// </summary>
    /// <exception cref="InvalidOperationException">The setting is below one or above the cap; the message names it by its path.</exception>
    private static int ReadWindowLimit(IConfigurationSection section, string key, int fallback)
    {
        int value = ReadLimit(section, key, fallback);
        return value <= BoundedWindowStore<string>.MaxLimit
            ? value
            : throw new InvalidOperationException(string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"'{section.Path}:{key}' must be at most {BoundedWindowStore<string>.MaxLimit}."));
    }

    private static int ReadInt(IConfigurationSection section, string key, int fallback) =>
        int.TryParse(section[key], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
}
