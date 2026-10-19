using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Api.Contracts;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.ApiClient;

/// <summary>Registration of the use-case interfaces over the database API (ADR-0004).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="IBookingService"/>, <see cref="IGuestService"/> and <see cref="IRoomService"/> as clients of
    /// the API. A missing address or token stops the start-up.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration with the section <c>Api</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ApiClientOptions.Read(configuration);
        services.AddSingleton(options);
        services.AddHttpClient<ApiGateway>(client =>
        {
            client.BaseAddress = options.BaseUri;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Authorization = new(ApiAuthentication.Scheme, options.Token);
        });
        services.AddScoped<IBookingService, ApiBookingService>();
        services.AddScoped<IGuestService, ApiGuestService>();
        services.AddScoped<IRoomService, ApiRoomService>();
        services.AddScoped<IStaffAuthService, ApiStaffAuthService>();
        services.AddScoped<StaffCallContext>();
        return services;
    }
}
