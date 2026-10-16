using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Repositories;

namespace NFHotel.Infrastructure.Tests;

/// <summary>
/// Beviser at composition roden kan bygge hver use case-service og hver port, så en manglende
/// registrering fanges her og ikke først når en side bruges. Ingen database forbindes.
/// </summary>
public sealed class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HotelDatabase"] = "Host=localhost;Database=model-only",
                ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
                ["HotelTime:TimeZoneId"] = "Europe/Copenhagen",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
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
    [InlineData(typeof(IClock))]
    public void GetRequiredService_EveryPortAndService_IsRegistered(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.NotNull(service);
    }
}