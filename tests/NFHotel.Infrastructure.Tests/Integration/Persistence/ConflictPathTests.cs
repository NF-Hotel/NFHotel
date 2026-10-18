using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The conflict paths of the database against a real PostgreSQL: the exclusion constraint that stops a
/// double booking, the unique room number and the concurrency token. Races are forced with a latch and
/// two contexts, never with a sleep, so the outcome is the same on every run.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ConflictPathTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2030, 1, 1);

    private readonly PostgreSqlFixture _fixture;

    public ConflictPathTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Overlap exclusion constraint

    /// <summary>Integration and negative: two bookings of one room that overlap are stopped by the database (B-03).</summary>
    [Fact]
    public async Task SaveChangesAsync_OverlappingBookingsOfTheSameRoom_ThrowsBookingOverlapConflict()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 5);

        // Act and assert
        await Assert.ThrowsAsync<BookingOverlapConflictException>(
            () => SaveBookingAsync(roomId, guestId, daysFromToday: 3, nights: 4));
    }

    /// <summary>Boundary: arriving on the day the other guest leaves is no overlap, the interval is half-open (BR-39).</summary>
    [Fact]
    public async Task SaveChangesAsync_ArrivalOnTheDayAnotherBookingEnds_IsAccepted()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 3);

        // Act
        await SaveBookingAsync(roomId, guestId, daysFromToday: 3, nights: 2);

        // Assert
        await using var context = _fixture.CreateContext();
        Assert.Equal(2, context.Bookings.Count());
    }

    /// <summary>Equivalence: the same period in another room is no overlap.</summary>
    [Fact]
    public async Task SaveChangesAsync_SamePeriodInAnotherRoom_IsAccepted()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        var otherRoomId = await SaveRoomAsync("102");
        await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 3);

        // Act
        await SaveBookingAsync(otherRoomId, guestId, daysFromToday: 0, nights: 3);

        // Assert
        await using var context = _fixture.CreateContext();
        Assert.Equal(2, context.Bookings.Count());
    }

    /// <summary>Regression and equivalence: a cancelled booking does not hold the room, the period can be booked again.</summary>
    [Fact]
    public async Task SaveChangesAsync_PeriodOfACancelledBooking_IsAccepted()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 3, cancel: true);

        // Act
        await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 3);

        // Assert
        await using var context = _fixture.CreateContext();
        Assert.Equal(2, context.Bookings.Count());
    }

    #endregion

    #region Unique room number

    /// <summary>Integration and negative: a second room with the same number is stopped by the unique index (BR-N-01).</summary>
    [Fact]
    public async Task SaveChangesAsync_DuplicateRoomNumber_ThrowsUniqueConstraintViolation()
    {
        // Arrange
        await SaveRoomAsync("101");

        // Act
        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => SaveRoomAsync("101"));

        // Assert
        Assert.Contains("room_number", exception.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Concurrency token

    /// <summary>Race condition: two contexts read the same booking, the second write finds the row changed and is refused.</summary>
    [Fact]
    public async Task SaveChangesAsync_BookingChangedByAnotherContext_ThrowsConcurrencyConflict()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        var bookingId = await SaveBookingAsync(roomId, guestId, daysFromToday: 0, nights: 2);
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        var firstCopy = await first.Bookings.FindAsync([bookingId], TestContext.Current.CancellationToken);
        var secondCopy = await second.Bookings.FindAsync([bookingId], TestContext.Current.CancellationToken);
        firstCopy!.Confirm();
        await new UnitOfWork(first).SaveChangesAsync(TestContext.Current.CancellationToken);
        secondCopy!.Cancel();

        // Act and assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => new UnitOfWork(second).SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region Parallel creations

    /// <summary>Concurrency: two requests that both saw a free room save at the same moment, exactly one wins.</summary>
    [Fact]
    public async Task SaveChangesAsync_TwoParallelCreationsOfTheSamePeriod_ExactlyOneSucceeds()
    {
        // Arrange
        var (roomId, guestId) = await SeedRoomAndGuestAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = TrySaveWhenOpenedAsync(gate.Task, roomId, guestId);
        var second = TrySaveWhenOpenedAsync(gate.Task, roomId, guestId);

        // Act
        gate.SetResult();
        var outcomes = await Task.WhenAll(first, second);

        // Assert
        Assert.Equal(1, outcomes.Count(saved => saved));
        await using var context = _fixture.CreateContext();
        Assert.Single(context.Bookings);
    }

    #endregion

    #region Helpers

    private async Task<(int RoomId, int GuestId)> SeedRoomAndGuestAsync()
    {
        var roomId = await SaveRoomAsync("101");
        await using var context = _fixture.CreateContext();
        var guest = Guest.Create("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null);
        new GuestRepository(context).Add(guest);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return (roomId, guest.Id);
    }

    private async Task<int> SaveRoomAsync(string number)
    {
        await using var context = _fixture.CreateContext();
        var room = Room.Create(number, 1, RoomSize.Double, 2);
        new RoomRepository(context).Add(room);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return room.Id;
    }

    private async Task<int> SaveBookingAsync(int roomId, int guestId, int daysFromToday, int nights, bool cancel = false)
    {
        await using var context = _fixture.CreateContext();
        var booking = NewBooking(roomId, guestId, daysFromToday, nights);
        if (cancel)
        {
            booking.Cancel();
        }

        new BookingRepository(context).Add(booking);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return booking.Id;
    }

    private async Task<bool> TrySaveWhenOpenedAsync(Task gate, int roomId, int guestId)
    {
        await using var context = _fixture.CreateContext();
        new BookingRepository(context).Add(NewBooking(roomId, guestId, daysFromToday: 0, nights: 3));
        await gate;
        try
        {
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
            return true;
        }
        catch (BookingOverlapConflictException)
        {
            return false;
        }
    }

    private static Booking NewBooking(int roomId, int guestId, int daysFromToday, int nights)
    {
        var start = Today.AddDays(daysFromToday);
        return Booking.Create(new DateRange(start, start.AddDays(nights)), roomId, guestId, Today);
    }

    #endregion
}