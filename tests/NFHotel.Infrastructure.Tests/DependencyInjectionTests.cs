using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Repositories;

namespace NFHotel.Infrastructure.Tests;

/// <summary>
/// Proves that the composition root can build every use case service and every port, so a missing
/// registration is caught here and not first when a page is used. No database is connected.
/// </summary>
public sealed class DependencyInjectionTests
{
    #region Setup

    private const string SigningKey = "staff-signing-key-0123456789-staff-signing-key";

    private static ServiceProvider BuildProvider(string? signingKey = SigningKey, Dictionary<string, string?>? extra = null)
    {
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:HotelDatabase"] = "Host=localhost;Database=model-only",
            ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
            ["Staff:Token:SigningKey"] = signingKey,
        };
        foreach ((string name, string? value) in extra ?? [])
        {
            settings[name] = value;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IClock>(new FixedClock());
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    #endregion

    #region Registrations

    [Theory]
    [InlineData(typeof(IBookingService))]
    [InlineData(typeof(IGuestService))]
    [InlineData(typeof(IRoomService))]
    [InlineData(typeof(IBookingRepository))]
    [InlineData(typeof(IGuestRepository))]
    [InlineData(typeof(IRoomRepository))]
    [InlineData(typeof(IBookingQueries))]
    [InlineData(typeof(IGuestQueries))]
    [InlineData(typeof(IRoomQueries))]
    [InlineData(typeof(IUnitOfWork))]
    [InlineData(typeof(IStaffRepository))]
    [InlineData(typeof(IStaffAuthService))]
    [InlineData(typeof(IStaffAccountService))]
    [InlineData(typeof(IPasswordHasher))]
    [InlineData(typeof(IStaffTokenIssuer))]
    [InlineData(typeof(ILoginThrottle))]
    [InlineData(typeof(IGuestTokenIssuer))]
    [InlineData(typeof(IGuestCallContext))]
    [InlineData(typeof(IGuestAccessService))]
    [InlineData(typeof(IGuestBookingService))]
    [InlineData(typeof(IPublicBookingService))]
    [InlineData(typeof(IGuestSignInLimits))]
    [InlineData(typeof(ICallerAddress))]
    [InlineData(typeof(ISenderLimits))]
    [InlineData(typeof(IPublicBookingEmailLimit))]
    public void GetRequiredService_EveryPortAndService_IsRegistered(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.NotNull(service);
    }

    #endregion

    #region The limits

    /// <summary>
    /// Security (ADR-0049 decision 6): the limits of the guest sign-in are one store for the whole process, apart from
    /// the throttle of the staff sign-in, and they read their numbers from the settings.
    /// </summary>
    [Fact]
    public void GuestSignInLimits_TwoScopes_AreOneStoreWithTheNumbersOfTheSettings()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider(extra: new Dictionary<string, string?> { ["GuestLookup:MaxFailuresPerReference"] = "2" });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        Guid reference = Guid.NewGuid();

        // Act
        IGuestSignInLimits limits = first.ServiceProvider.GetRequiredService<IGuestSignInLimits>();
        bool[] counted =
        [
            limits.TryCount(reference, "203.0.113.1"),
            second.ServiceProvider.GetRequiredService<IGuestSignInLimits>().TryCount(reference, "203.0.113.2"),
            limits.TryCount(reference, "203.0.113.3"),
        ];

        // Assert
        Assert.Same(limits, second.ServiceProvider.GetRequiredService<IGuestSignInLimits>());
        Assert.NotSame(limits, provider.GetRequiredService<ILoginThrottle>());
        Assert.Equal([true, true, false], counted);
    }

    /// <summary>
    /// Security (MIL-087, ADR-0051 decisions 1, 3 and 5): the limits per sender are one store for the whole process, and
    /// they read their numbers from the settings <c>SenderLimits</c>, the limit per email through a port of its own
    /// (review RC-428).
    /// </summary>
    [Fact]
    public void SenderLimits_TwoScopes_AreOneStoreWithTheNumbersOfTheSettings()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider(extra: new Dictionary<string, string?>
        {
            ["SenderLimits:SearchesPerMinute"] = "2",
            ["SenderLimits:ComingPublicBookingsPerEmail"] = "4",
        });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        // Act
        ISenderLimits limits = first.ServiceProvider.GetRequiredService<ISenderLimits>();
        bool[] counted =
        [
            limits.TryCountSearch("203.0.113.1").IsAllowed,
            second.ServiceProvider.GetRequiredService<ISenderLimits>().TryCountSearch("203.0.113.1").IsAllowed,
            limits.TryCountSearch("203.0.113.1").IsAllowed,
        ];
        int perEmail = second.ServiceProvider.GetRequiredService<IPublicBookingEmailLimit>().ComingPublicBookingsPerEmail;

        // Assert
        Assert.Same(limits, second.ServiceProvider.GetRequiredService<ISenderLimits>());
        Assert.Equal([true, true, false], counted);
        Assert.Equal(4, perEmail);
    }

    /// <summary>
    /// Negative (ADR-0051 decision 1): a number of the limits below one stops the start-up and names the setting, so a
    /// typing error cannot switch a limit off or refuse every request.
    /// </summary>
    /// <param name="key">The setting under <c>SenderLimits</c>.</param>
    /// <param name="value">The value given.</param>
    [Theory]
    [InlineData("SearchesPerMinute", "0")]
    [InlineData("BookingRequestsPerHour", "0")]
    [InlineData("BookingRequestsPerDay", "-1")]
    [InlineData("ComingPublicBookingsPerEmail", "0")]
    [InlineData("MaxTrackedKeys", "0")]
    public void AddInfrastructure_SenderLimitBelowOne_Throws(string key, string value)
    {
        // Arrange and act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => BuildProvider(extra: new Dictionary<string, string?> { [$"SenderLimits:{key}"] = value }));

        // Assert
        Assert.Contains($"SenderLimits:{key}", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Negative (ADR-0051 decision 5, review RC-428): a limit of a window above 100,000 stops the start-up and names the
    /// setting, before a store would refuse it at the first request that needs it.
    /// </summary>
    /// <param name="key">The setting, with its section.</param>
    [Theory]
    [InlineData("SenderLimits:SearchesPerMinute")]
    [InlineData("SenderLimits:BookingRequestsPerHour")]
    [InlineData("SenderLimits:BookingRequestsPerDay")]
    [InlineData("GuestLookup:MaxFailuresPerReference")]
    [InlineData("GuestLookup:MaxFailuresPerAddress")]
    public void AddInfrastructure_WindowLimitAboveTheCap_Throws(string key)
    {
        // Arrange and act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => BuildProvider(extra: new Dictionary<string, string?> { [key] = "100001" }));

        // Assert
        Assert.Contains(key, error.Message, StringComparison.Ordinal);
    }

    /// <summary>Boundary (review RC-428): a limit of a window of exactly 100,000, as the tests of the API set, starts.</summary>
    [Fact]
    public void AddInfrastructure_WindowLimitAtTheCap_Starts()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider(extra: new Dictionary<string, string?>
        {
            ["SenderLimits:SearchesPerMinute"] = "100000",
            ["GuestLookup:MaxFailuresPerAddress"] = "100000",
        });

        // Act
        bool counted = provider.GetRequiredService<ISenderLimits>().TryCountSearch("203.0.113.1").IsAllowed;

        // Assert
        Assert.True(counted);
        Assert.NotNull(provider.GetRequiredService<IGuestSignInLimits>());
    }

    #endregion

    #region The signing key

    /// <summary>Negative: a missing or too short signing key stops the start-up, so the API never issues tokens signed with a weak secret.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void AddInfrastructure_MissingOrShortSigningKey_Throws(string? signingKey)
    {
        // Arrange and act
        var error = Assert.Throws<InvalidOperationException>(() => BuildProvider(signingKey));

        // Assert
        Assert.Contains("Staff:Token:SigningKey", error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region Test doubles

    /// <summary>A clock that stands still: the composition test needs one because the clock is registered by NFHotel.External.</summary>
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly Today => new(2026, 1, 1);
    }

    #endregion
}
