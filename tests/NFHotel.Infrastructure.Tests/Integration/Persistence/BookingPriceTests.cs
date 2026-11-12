using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>A new booking is given its price per night and the market analysis feed still returns it (MIL-055).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingPriceTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2030, 1, 1);
    private static readonly DateOnly Arrival = new(2030, 6, 10);

    private readonly PostgreSqlFixture _fixture;

    public BookingPriceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Creating a booking

    /// <summary>Integration: base rate 100 and a +20 % change give a booking with a detail row of 120.00, and the feed returns it with that price.</summary>
    [Fact]
    public async Task CreateAsync_ChangeCoversTheArrival_StoresTheDetailAndTheFeedReturnsIt()
    {
        // Arrange
        (int roomId, int guestId) = await SeedAsync(withRate: true);

        // Act
        int bookingId = await CreateBookingAsync(roomId, guestId);

        // Assert
        await using HotelDbContext context = _fixture.CreateContext();
        BookingMarketDetail detail = await context.BookingMarketDetails.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal((bookingId, 120.00m, Today), (detail.BookingId, detail.PricePerNight, detail.BookedAt));

        MarketBookingDto feed = Assert.Single(await new MarketAnalysisBookingQueries(context).GetBookingsAsync(null, null, TestContext.Current.CancellationToken));
        Assert.Equal(bookingId, feed.BookingId);
        Assert.Equal(120.00m, feed.PricePerNight);
        Assert.Equal(Today, feed.BookingDate);
        Assert.Equal(Arrival.DayNumber - Today.DayNumber, feed.LeadTime);
    }

    /// <summary>Integration: the zero counts of the detail are read as "not known", so the guest-mix analysis does not count a party of 0.</summary>
    [Fact]
    public async Task GetBookingsAsync_BookingMadeByTheHotel_HasNoGuestCountsOrFlags()
    {
        // Arrange
        (int roomId, int guestId) = await SeedAsync(withRate: true);
        await CreateBookingAsync(roomId, guestId);
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        MarketBookingDto feed = Assert.Single(await new MarketAnalysisBookingQueries(context).GetBookingsAsync(null, null, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal((null, null, null), (feed.Adults, feed.Children, feed.Babies));
        Assert.Equal((null, null, null), (feed.IsRepeatedGuest, feed.TotalOfSpecialRequests, feed.RequiredCarParkingSpaces));
        Assert.Equal((null, null, null, null, null), (feed.Meal, feed.MarketSegment, feed.CustomerType, feed.DepositType, feed.AssignedRoomType));
    }

    /// <summary>Integration and negative: a size without a base rate gives a booking without a detail row, as before.</summary>
    [Fact]
    public async Task CreateAsync_NoBaseRate_StoresNoDetailAndTheFeedReturnsTheBooking()
    {
        // Arrange
        (int roomId, int guestId) = await SeedAsync(withRate: false);

        // Act
        int bookingId = await CreateBookingAsync(roomId, guestId);

        // Assert
        await using HotelDbContext context = _fixture.CreateContext();
        Assert.Empty(context.BookingMarketDetails);
        MarketBookingDto feed = Assert.Single(await new MarketAnalysisBookingQueries(context).GetBookingsAsync(null, null, TestContext.Current.CancellationToken));
        Assert.Equal(bookingId, feed.BookingId);
        Assert.Null(feed.PricePerNight);
        Assert.Null(feed.BookingDate);
    }

    #endregion

    #region Helpers

    private async Task<(int RoomId, int GuestId)> SeedAsync(bool withRate)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        Room room = Room.Create("101", 1, RoomSize.Single, 1);
        Guest guest = Guest.Create("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null);
        DateTimeOffset at = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        new RoomRepository(context).Add(room);
        new GuestRepository(context).Add(guest);
        if (withRate)
        {
            new RoomRateRepository(context).Add(RoomRate.Create(RoomSize.Single, 100m, "manager", at));
            new PriceChangeRepository(context).Add(
                PriceChange.Approve(RoomSize.Single, 20m, new DateOnly(2030, 6, 1), new DateOnly(2030, 8, 31), "manager", at));
        }

        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return (room.Id, guest.Id);
    }

    private async Task<int> CreateBookingAsync(int roomId, int guestId)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        FakeClock clock = new() { Today = Today };
        UnitOfWork unitOfWork = new(context);
        PriceChangeService prices = new(
            new PriceChangeRepository(context),
            new RoomRateRepository(context),
            new StaffAuditRepository(context),
            unitOfWork,
            clock,
            new FakeStaffCallContext());
        BookingService service = new(
            new BookingRepository(context),
            new RoomRepository(context),
            new GuestRepository(context),
            new BookingRepository(context),
            new RoomRepository(context),
            unitOfWork,
            clock,
            new AllowAll(),
            prices,
            new BookingMarketDetailRepository(context),
            new RecordingWarningLog(),
            new InMemoryLoginThrottle(new StaffSecurityOptions { SigningKey = "unused" }, clock));

        Result<BookingDetailsDto> result = await service.CreateAsync(new CreateBookingRequest(Arrival, Arrival.AddDays(3), roomId, guestId, null), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        return result.Value.BookingId;
    }

    private sealed class AllowAll : IStaffAuthorizationService
    {
        public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    #endregion
}
