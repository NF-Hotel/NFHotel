using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Api.Contracts;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// Hosts the real API (its pipeline, token checks and controllers) with the real use-case services over
/// in-memory repositories, so a test goes through HTTP without a database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The token that the API under test accepts from the web application.</summary>
    public const string Token = "test-token-0123456789-test-token-0123456789";

    /// <summary>The key that signs the staff tokens of the API under test.</summary>
    public const string SigningKey = "staff-signing-key-0123456789-staff-signing-key";

    /// <summary>The in-memory world behind the services; a test seeds it and reads it back.</summary>
    public ServiceWorld World { get; } = new();

    /// <summary>Creates a client that presents the token of the web application and the staff token of a Manager.</summary>
    /// <returns>The client.</returns>
    public HttpClient CreateAuthorizedClient() => CreateStaffClient(StaffRole.Manager);

    /// <summary>Creates a client that presents the token of the web application and the staff token of a role.</summary>
    /// <param name="role">The role of the signed-in member of staff.</param>
    /// <returns>The client.</returns>
    public HttpClient CreateStaffClient(StaffRole role)
    {
        var client = CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, IssueStaffToken(role));
        return client;
    }

    /// <summary>Creates a client that presents only the token of the web application, as the public booking pages do.</summary>
    /// <returns>The client.</returns>
    public HttpClient CreateServiceClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token);
        return client;
    }

    /// <summary>Issues a real staff token with the issuer of the API under test.</summary>
    /// <param name="role">The role.</param>
    /// <returns>The signed token.</returns>
    public string IssueStaffToken(StaffRole role) =>
        Services.GetRequiredService<IStaffTokenIssuer>().Issue(StaffMember.Create("tester", "any-hash", role)).Token;

    /// <summary>Stores an account whose password is hashed by the hasher of the API under test.</summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="role">The role.</param>
    /// <returns>The stored account.</returns>
    public StaffMember AddStaff(string userName, string password, StaffRole role)
    {
        var member = StaffMember.Create(userName, Services.GetRequiredService<IPasswordHasher>().Hash(password), role);
        World.Staff.Add(member);
        return member;
    }

    /// <summary>Extra settings for the host; a derived factory adds its own.</summary>
    /// <returns>Pairs of setting name and value.</returns>
    protected virtual IEnumerable<KeyValuePair<string, string>> ExtraSettings() => [];

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
        builder.UseSetting("Staff:Token:SigningKey", SigningKey);
        builder.UseSetting("Staff:PasswordIterations", "1000");
        foreach (var (name, value) in ExtraSettings())
        {
            builder.UseSetting(name, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBookingService>();
            services.RemoveAll<IGuestService>();
            services.RemoveAll<IRoomService>();
            services.AddSingleton<IBookingService>(_ => World.BookingService());
            services.AddSingleton<IGuestService>(_ => World.GuestService());
            services.AddSingleton<IRoomService>(_ => World.RoomService());

            services.RemoveAll<IStaffRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IStaffRepository>(World.Staff);
            services.AddSingleton<IUnitOfWork>(World.UnitOfWork);
        });
    }
}
