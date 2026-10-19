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

    private static ServiceProvider BuildProvider(string? signingKey = SigningKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HotelDatabase"] = "Host=localhost;Database=model-only",
                ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
                ["Staff:Token:SigningKey"] = signingKey,
            })
            .Build();

        var services = new ServiceCollection();
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
    public void GetRequiredService_EveryPortAndService_IsRegistered(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.NotNull(service);
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
