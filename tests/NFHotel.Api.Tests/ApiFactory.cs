using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// Hosts the real API (its pipeline, token check and controllers) with the real use-case services over
/// in-memory repositories, so a test goes through HTTP without a database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The token that the API under test accepts.</summary>
    public const string Token = "test-token-0123456789-test-token-0123456789";

    /// <summary>The in-memory world behind the services; a test seeds it and reads it back.</summary>
    public ServiceWorld World { get; } = new();

    /// <summary>Creates a client that presents the right token.</summary>
    /// <returns>The client.</returns>
    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token);
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        // UseSetting, not ConfigureAppConfiguration: the API reads these while it builds its services, which
        // happens before a configuration callback of the test host would apply.
        builder.UseSetting("Api:Token", Token);
        builder.UseSetting("ConnectionStrings:HotelDatabase", "Host=localhost;Database=never-connected");
        builder.UseSetting("Encryption:PassportKey", Convert.ToBase64String(new byte[32]));
        builder.UseSetting("HotelTime:TimeZoneId", "Europe/Copenhagen");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBookingService>();
            services.RemoveAll<IGuestService>();
            services.RemoveAll<IRoomService>();
            services.AddSingleton<IBookingService>(_ => World.BookingService());
            services.AddSingleton<IGuestService>(_ => World.GuestService());
            services.AddSingleton<IRoomService>(_ => World.RoomService());
        });
    }
}