using System.Reflection;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Application.Tests.TestSupport;

/// <summary>A clock that stands still: "today" is set by the test, never read from the system.</summary>
internal sealed class FakeClock : IClock
{
    public DateOnly Today { get; set; } = new(2026, 1, 1);

    /// <summary>The time of day on <see cref="Today"/>; a test moves it to let time pass.</summary>
    public TimeSpan TimeOfDay { get; set; } = TimeSpan.FromHours(12);

    public DateTimeOffset UtcNow => new(Today.ToDateTime(TimeOnly.MinValue) + TimeOfDay, TimeSpan.Zero);
}

/// <summary>A unit of work that counts saves and can fail on demand, as the database would.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Exception? FailWith { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>An in-memory repository: keeps the entities in a list and gives them keys as the database does.</summary>
internal abstract class InMemoryRepository<TEntity>
    where TEntity : Entity<int>
{
    private static readonly PropertyInfo IdProperty = typeof(Entity<int>).GetProperty(nameof(Entity<int>.Id))!;

    private int _nextId = 1;

    public List<TEntity> Items { get; } = [];

    public List<TEntity> Updated { get; } = [];

    public List<TEntity> Removed { get; } = [];

    public Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

    public void Add(TEntity entity)
    {
        IdProperty.SetValue(entity, _nextId++);
        Items.Add(entity);
    }

    public void Update(TEntity entity) => Updated.Add(entity);

    public void Remove(TEntity entity)
    {
        Removed.Add(entity);
        Items.Remove(entity);
    }
}

internal sealed class InMemoryBookingRepository : InMemoryRepository<Booking>, IBookingRepository
{
    public Task<IReadOnlyList<Booking>> GetOverlapCandidatesAsync(
        int roomId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        int? excludeBookingId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Booking> candidates = Items
            .Where(booking => booking.RoomId == roomId
                && blockingStatuses.Contains(booking.Status)
                && booking.Id != excludeBookingId)
            .ToArray();
        return Task.FromResult(candidates);
    }
}

internal sealed class InMemoryRoomRepository : InMemoryRepository<Room>, IRoomRepository
{
    public HashSet<int> RoomIdsWithBookings { get; } = [];

    public Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeRoomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(room => room.RoomNumber == roomNumber && room.Id != excludeRoomId));

    public Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(RoomIdsWithBookings.Contains(roomId));
}

internal sealed class InMemoryGuestRepository : InMemoryRepository<Guest>, IGuestRepository
{
    public HashSet<int> GuestIdsWithBookings { get; } = [];

    public Task<bool> ExistsAsync(int guestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(guest => guest.Id == guestId));

    public Task<bool> HasAnyBookingsAsync(int guestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(GuestIdsWithBookings.Contains(guestId));
}

/// <summary>
/// Everything a service test needs: a fake clock, in-memory repositories, a fake unit of work and
/// substitutes for the read-side query ports, wired together and ready to build a service from.
/// </summary>
internal sealed class ServiceWorld
{
    public ServiceWorld()
    {
        BookingQueries.GetReadModelAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadModelOrNull(call.Arg<int>())));
    }

    public FakeClock Clock { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public InMemoryBookingRepository Bookings { get; } = new();

    public InMemoryRoomRepository Rooms { get; } = new();

    public InMemoryGuestRepository Guests { get; } = new();

    public IBookingQueries BookingQueries { get; } = Substitute.For<IBookingQueries>();

    public IRoomQueries RoomQueries { get; } = Substitute.For<IRoomQueries>();

    public IGuestQueries GuestQueries { get; } = Substitute.For<IGuestQueries>();

    public BookingService BookingService() =>
        new(Bookings, Rooms, Guests, BookingQueries, RoomQueries, UnitOfWork, Clock);

    public GuestService GuestService() => new(Guests, GuestQueries, UnitOfWork);

    public RoomService RoomService() => new(Rooms, RoomQueries, UnitOfWork);

    public Room AddRoom(string number = "101", int floor = 1, RoomSize size = RoomSize.Double, int capacity = 2)
    {
        var room = Room.Create(number, floor, size, capacity);
        Rooms.Add(room);
        return room;
    }

    public Guest AddGuest(string firstName = "Anna", string lastName = "Jensen") =>
        AddGuestCore(Guest.Create(firstName, lastName, "anna@example.com", "+4512345678", "Denmark", null));

    public Booking AddBooking(int roomId, int guestId, int startOffsetDays = 0, int nights = 3)
    {
        var start = Clock.Today.AddDays(startOffsetDays);
        var booking = Booking.Create(new DateRange(start, start.AddDays(nights)), roomId, guestId, Clock.Today);
        Bookings.Add(booking);
        return booking;
    }

    public static GuestFields ValidGuestFields() =>
        new("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null);

    private Guest AddGuestCore(Guest guest)
    {
        Guests.Add(guest);
        return guest;
    }

    private BookingReadModel? ReadModelOrNull(int bookingId)
    {
        var booking = Bookings.Items.SingleOrDefault(item => item.Id == bookingId);
        if (booking is null)
        {
            return null;
        }

        return ReadModelFor(booking);
    }

    public BookingReadModel ReadModelFor(Booking booking)
    {
        var room = Rooms.Items.Single(item => item.Id == booking.RoomId);
        var guest = Guests.Items.Single(item => item.Id == booking.GuestId);
        return new BookingReadModel(
            booking.Id,
            booking.StartDate,
            booking.EndDate,
            booking.EffectiveEndDate,
            booking.Status,
            booking.CheckInTime,
            booking.CheckOutTime,
            booking.CheckOutDate,
            new RoomListItemDto(room.Id, room.RoomNumber, room.Floor, room.Size, room.Capacity, room.Status, room.HousekeepingStatus),
            new GuestListItemDto(
                guest.Id,
                guest.FirstName,
                guest.LastName,
                $"{guest.FirstName} {guest.LastName}",
                guest.Email,
                guest.PhoneNumber,
                guest.Country,
                guest.PassportNumber is not null));
    }
}