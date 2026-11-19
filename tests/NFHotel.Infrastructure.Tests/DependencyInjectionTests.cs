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
    public void GetRequiredService_EveryPortAndService_IsRegistered(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.NotNull(service);
    }

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

    /// <summary>A clock that stands still: the composition test needs one because the clock is registered by NFHotel.External.</summary>
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateOnly Today => new(2026, 1, 1);
    }

}
