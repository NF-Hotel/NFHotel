using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.TestSupport;

using Npgsql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The booking request and its verification through the composition root of the API against a real PostgreSQL (MIL-088
/// criterion 4, ADR-0052 decision 5): each request and each verification in a scope of its own, as separate calls of the
/// API are, with the relay and the events of the test.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingVerificationRaceTests : IAsyncLifetime
{
    private const int Guests = 20;

    private static readonly DateOnly Arrival = new(2026, 3, 1);

    private readonly PostgreSqlFixture _fixture;
    private readonly FakeMailSender _relay = new();
    private ServiceProvider? _provider;

    public BookingVerificationRaceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private ServiceProvider Provider => _provider ?? throw new InvalidOperationException("The test has not started.");

    /// <summary>Runs before each test: an empty database, one room of the size, and the services of the API.</summary>
    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            new RoomRepository(context).Add(Room.Create("101", 1, RoomSize.Double, 2));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        _provider = BuildProvider();
    }

    /// <summary>Releases the services of the test.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    #region The verification

    /// <summary>
    /// Security (criterion 4, ADR-0052 decision 5, ASVS 2.3.4): of 20 verifications at once for the last room of a size,
    /// exactly one gets the booking and the other 19 are told that the room is gone; every request is claimed, and no guest
    /// is left without a booking.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_TwentyAtOnceForTheLastRoom_OneBooksAndNineteenAreToldTheRoomIsGone()
    {
        // Arrange
        string[] tokens = new string[Guests];
        for (int guest = 0; guest < Guests; guest++)
        {
            Result<BookingRequestReceiptDto> asked = await InScopeAsync(service => service.AskAsync(Request(guest), TestContext.Current.CancellationToken));
            Assert.True(asked.IsSuccess);
            tokens[guest] = _relay.LastToken();
        }

        // Act
        Result<PublicBookingDto>[] verified = await Task.WhenAll(tokens.Select(token => Task.Run(() =>
            InScopeAsync(service => service.VerifyAsync(new VerifyBookingRequest(token), TestContext.Current.CancellationToken)))));

        // Assert
        Assert.Single(verified, result => result.IsSuccess);
        Assert.All(
            verified.Where(result => result.IsFailure),
            result => Assert.Equal(ErrorCodes.Booking.RoomTypeNotAvailable, result.FirstError?.Code));
        Assert.Equal(19, verified.Count(result => result.IsFailure));
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM booking"));
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM guest"));
        Assert.Equal("0", await ScalarAsync("SELECT count(*)::text FROM booking_request"));
        Assert.Equal(Guests + 1, _relay.Sent.Count);
    }

    /// <summary>
    /// Positive (criterion 4, ADR-0052 decision 5): a verification stores the guest and the booking, <c>Pending</c> with its
    /// reference and its mark of the public site, and the request is gone.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_TokenOfTheMail_StoresTheBookingAndItsGuestAndLeavesNoRequest()
    {
        // Arrange
        Result<BookingRequestReceiptDto> asked = await InScopeAsync(service => service.AskAsync(Request(0), TestContext.Current.CancellationToken));

        // Act
        Result<PublicBookingDto> verified = await InScopeAsync(service => service.VerifyAsync(new VerifyBookingRequest(_relay.LastToken()), TestContext.Current.CancellationToken));

        // Assert
        Assert.True(asked.IsSuccess);
        Assert.True(verified.IsSuccess);
        Assert.Equal(
            $"{(int)BookingStatus.Pending}|true|{verified.Value.Receipt.Reference}|guest0@example.com",
            await ScalarAsync(
                "SELECT b.status::text || '|' || b.made_on_public_site::text || '|' || b.reference::text || '|' || g.email "
                + "FROM booking b JOIN guest g ON g.guest_id = b.guest_id"));
        Assert.Equal("0", await ScalarAsync("SELECT count(*)::text FROM booking_request"));
    }

    #endregion

    #region The public booking without the verification

    /// <summary>
    /// Security (ADR-0052 decisions 5 and 8, ASVS 2.3.4): with the verification off, as in production until the relay is
    /// set, of 20 public bookings at once for the last room of a size exactly one books and the other 19 are told that the
    /// room is gone; none ends in an error of the database, and no guest is left without a booking.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_TwentyAtOnceForTheLastRoomWithTheVerificationOff_OneBooksAndNineteenAreToldTheRoomIsGone()
    {
        // Arrange
        await using ServiceProvider off = BuildProvider(verification: false);

        // Act
        Result<BookingDetailsDto>[] booked = await Task.WhenAll(Enumerable.Range(0, Guests).Select(guest => Task.Run(async () =>
        {
            await using AsyncServiceScope scope = off.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IPublicBookingService>()
                .CreatePublicAsync(Request(guest), TestContext.Current.CancellationToken);
        })));

        // Assert
        Assert.Single(booked, result => result.IsSuccess);
        Assert.All(
            booked.Where(result => result.IsFailure),
            result => Assert.Equal(ErrorCodes.Booking.RoomTypeNotAvailable, result.FirstError?.Code));
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM booking"));
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM guest"));
        Assert.Empty(_relay.Sent);
    }

    #endregion

    #region Helpers

    private static CreatePublicBookingRequest Request(int guest) => new(
        Arrival,
        Arrival.AddDays(2),
        RoomSize.Double,
        1,
        new GuestFields("Anna", "Jensen", $"guest{guest}@example.com", "+4512345678", "Denmark", null));

    // The services of the API, with the verification on unless a test turns it off, the relay of the test and a clock
    // that stands still.
    private ServiceProvider BuildProvider(bool verification = true)
    {
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:HotelDatabase"] = _fixture.ConnectionString,
            ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
            ["Staff:Token:SigningKey"] = "staff-signing-key-0123456789-staff-signing-key",
            ["Smtp:Host"] = "smtp.example.com",
            ["Smtp:Port"] = "465",
            ["Smtp:UserName"] = "hotel@example.com",
            ["Smtp:Password"] = "relay-password-of-the-test",
            ["Smtp:FromAddress"] = "hotel@example.com",
            ["BookingVerification:Enabled"] = verification ? "true" : "false",
            ["BookingVerification:SiteAddress"] = "https://hotel.example.com/",
        };

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IClock>(new FakeClock());
        services.AddSingleton<IBookingRequestEvents>(new RecordingBookingRequestEvents());
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.Replace(ServiceDescriptor.Singleton<IMailSender>(_relay));
        return services.BuildServiceProvider(validateScopes: true);
    }

    private async Task<T> InScopeAsync<T>(Func<IBookingRequestService, Task<T>> call)
    {
        await using AsyncServiceScope scope = Provider.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IBookingRequestService>());
    }

    private async Task<string> ScalarAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    #endregion
}
