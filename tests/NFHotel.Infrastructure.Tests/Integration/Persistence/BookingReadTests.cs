using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The read side of bookings against a real PostgreSQL: the projections that the screens use must translate to SQL
/// and return what was stored. The in-memory fakes of the service tests cannot prove that.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingReadTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2030, 1, 1);

    private readonly PostgreSqlFixture _fixture;

    public BookingReadTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Read model

    /// <summary>Integration and positive: a stored booking comes back as a read model with its room and guest.</summary>
    [Fact]
    public async Task GetReadModelAsync_StoredBooking_ReturnsItWithRoomAndGuest()
    {
        // Arrange
        var bookingId = await SeedBookingAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var readModel = await new BookingRepository(context).GetReadModelAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(readModel);
        Assert.Equal("101", readModel.Room.RoomNumber);
        Assert.Equal("Anna Jensen", readModel.Guest.FullName);
        Assert.Equal(Today.AddDays(3), readModel.EffectiveEndDate);
    }

    /// <summary>Negative: a key that does not exist gives no read model.</summary>
    [Fact]
    public async Task GetReadModelAsync_UnknownBooking_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var readModel = await new BookingRepository(context).GetReadModelAsync(999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(readModel);
    }

    #endregion

    #region Overview and occupancy

    /// <summary>Integration and positive: the overview of a period holds the booking that overlaps it.</summary>
    [Fact]
    public async Task GetOverviewAsync_PeriodWithABooking_ReturnsIt()
    {
        // Arrange
        var bookingId = await SeedBookingAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var rows = await new BookingRepository(context).GetOverviewAsync(
            Today, Today.AddDays(30), [BookingStatus.Pending, BookingStatus.Confirmed], null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(bookingId, Assert.Single(rows).BookingId);
    }

    /// <summary>Boundary: a period that starts on the day the booking ends does not hold it (half-open interval).</summary>
    [Fact]
    public async Task GetOverviewAsync_PeriodStartingOnTheDepartureDay_DoesNotReturnIt()
    {
        // Arrange
        await SeedBookingAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var rows = await new BookingRepository(context).GetOverviewAsync(
            Today.AddDays(3), Today.AddDays(10), [BookingStatus.Pending], null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(rows);
    }

    /// <summary>Equivalence: the search text finds the booking by guest name, case-insensitively.</summary>
    [Fact]
    public async Task GetOverviewAsync_SearchByGuestName_ReturnsTheBooking()
    {
        // Arrange
        await SeedBookingAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var rows = await new BookingRepository(context).GetOverviewAsync(
            Today, Today.AddDays(30), [BookingStatus.Pending], "JENSEN", TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(rows);
    }

    /// <summary>Integration: the occupancy of a period lists the room and the dates that the domain needs to decide an overlap.</summary>
    [Fact]
    public async Task GetOccupancyAsync_PeriodWithABooking_ReturnsTheOccupiedRoom()
    {
        // Arrange
        var bookingId = await SeedBookingAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var occupancy = await new BookingRepository(context).GetOccupancyAsync(
            Today, Today.AddDays(30), [BookingStatus.Pending], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(bookingId, Assert.Single(occupancy).BookingId);
    }

    #endregion

    #region Helpers

    private async Task<int> SeedBookingAsync()
    {
        await using var context = _fixture.CreateContext();
        var room = Room.Create("101", 1, RoomSize.Double, 2);
        var guest = Guest.Create("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null);
        new RoomRepository(context).Add(room);
        new GuestRepository(context).Add(guest);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        var booking = Booking.Create(new DateRange(Today, Today.AddDays(3)), room.Id, guest.Id, Today);
        new BookingRepository(context).Add(booking);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return booking.Id;
    }

    #endregion
}