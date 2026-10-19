using System.Reflection;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.TestSupport;

/// <summary>A clock that stands still: "today" is set by the test, never read from the system.</summary>
public sealed class FakeClock : IClock
{
    public DateOnly Today { get; set; } = new(2026, 1, 1);

    /// <summary>The time of day on <see cref="Today"/>; a test moves it to let time pass.</summary>
    public TimeSpan TimeOfDay { get; set; } = TimeSpan.FromHours(12);

    public DateTimeOffset UtcNow => new(Today.ToDateTime(TimeOnly.MinValue) + TimeOfDay, TimeSpan.Zero);
}

/// <summary>A unit of work that counts saves and can fail on demand, as the database would.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
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
public abstract class InMemoryRepository<TEntity>
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

public sealed class InMemoryBookingRepository : InMemoryRepository<Booking>, IBookingRepository
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

public sealed class InMemoryRoomRepository : InMemoryRepository<Room>, IRoomRepository
{
    public HashSet<int> RoomIdsWithBookings { get; } = [];

    public Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeRoomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(room => room.RoomNumber == roomNumber && room.Id != excludeRoomId));

    public Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default) =>
        Task.FromResult(RoomIdsWithBookings.Contains(roomId));
}

public sealed class InMemoryGuestRepository : InMemoryRepository<Guest>, IGuestRepository
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
public sealed class ServiceWorld
{
    public ServiceWorld()
    {
        BookingQueries.GetReadModelAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadModelOrNull(call.Arg<int>())));
        ReadSideFromTheRepositories();
        BenchmarkReadSideFromTheRepository();
    }

    /// <summary>
    /// Makes the substitutes of the read-side ports answer from the in-memory repositories, so a service that
    /// reads (lists, searches, details) sees what a service that writes has stored. A test can still override
    /// any of them with its own <c>Returns</c>.
    /// </summary>
    private void ReadSideFromTheRepositories()
    {
        GuestQueries.GetDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Guests.Items.Where(guest => guest.Id == call.Arg<int>()).Select(GuestDetails).FirstOrDefault()));
        GuestQueries.SearchAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<GuestListItemDto>>(Guests.Items
                .Where(guest => (call.ArgAt<IReadOnlyCollection<string>?>(0) ?? []).All(term => GuestText(guest).Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(GuestListItem)
                .ToArray()));

        RoomQueries.GetDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Rooms.Items.Where(room => room.Id == call.Arg<int>()).Select(RoomDetails).FirstOrDefault()));
        RoomQueries.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<RoomListItemDto>>(Rooms.Items.Select(RoomListItem).ToArray()));
        RoomQueries.GetByFilterAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<RoomListItemDto>>(Rooms.Items
                .Where(room => Matches(room, call.ArgAt<RoomFilter?>(0) ?? new RoomFilter()))
                .Select(RoomListItem)
                .ToArray()));
        RoomQueries.GetByStatusAsync(Arg.Any<RoomStatus>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<AvailableRoomDto>>(Rooms.Items
                .Where(room => room.Status == call.Arg<RoomStatus>())
                .Select(room => new AvailableRoomDto(room.Id, room.RoomNumber, room.Floor, room.Size, room.Capacity))
                .ToArray()));
        RoomQueries.GetDistinctFloorsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<int>>(Rooms.Items.Select(room => room.Floor).Distinct().Order().ToArray()));

        BookingQueries.GetOverviewAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<IReadOnlyCollection<BookingStatus>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<BookingReadModel>>(Bookings.Items
                .Where(booking => (call.ArgAt<IReadOnlyCollection<BookingStatus>?>(2) ?? []).Contains(booking.Status)
                    && booking.StartDate <= call.ArgAt<DateOnly>(1)
                    && booking.EffectiveEndDate >= call.ArgAt<DateOnly>(0))
                .Select(ReadModelFor)
                .ToArray()));
        BookingQueries.GetOccupancyAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<IReadOnlyCollection<BookingStatus>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<RoomOccupancyDto>>(Bookings.Items
                .Where(booking => (call.ArgAt<IReadOnlyCollection<BookingStatus>?>(2) ?? []).Contains(booking.Status)
                    && booking.StartDate < call.ArgAt<DateOnly>(1)
                    && booking.EffectiveEndDate > call.ArgAt<DateOnly>(0))
                .Select(booking => new RoomOccupancyDto(booking.Id, booking.RoomId, booking.StartDate, booking.EffectiveEndDate, booking.Status))
                .ToArray()));
    }

    private void BenchmarkReadSideFromTheRepository()
    {
        BenchmarkQueries.GetRunsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<BenchmarkRunListItemDto>>(Benchmarks.Items
                .OrderByDescending(run => run.StartedAt)
                .Select(BenchmarkListItem)
                .ToArray()));
        BenchmarkQueries.GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Benchmarks.Items.Where(run => run.RunId == call.Arg<Guid>()).Select(BenchmarkDetails).FirstOrDefault()));
        BenchmarkQueries.GetOutputAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Benchmarks.Items
                .SelectMany(run => run.Results)
                .Where(result => result.Id == call.Arg<int>())
                .Select(result => new BenchmarkOutputDto(result.Id, result.Model, result.Output, result.ValidationError, result.Error))
                .FirstOrDefault()));
    }

    private static BenchmarkRunListItemDto BenchmarkListItem(BenchmarkRun run) =>
        new(
            run.RunId,
            run.StartedAt,
            run.FinishedAt,
            run.DatasetName,
            run.Cpu,
            run.MemoryMb,
            run.Gpu,
            run.BackendName,
            run.BackendVersion,
            run.Results.Select(result => result.Model).Distinct().Order(StringComparer.Ordinal).ToArray(),
            run.Results.Count);

    private static BenchmarkRunDetailsDto BenchmarkDetails(BenchmarkRun run) =>
        new(
            BenchmarkListItem(run),
            run.DatasetHash,
            run.DatasetRows,
            run.PromptVersion,
            run.CpuCores,
            run.OperatingSystem,
            run.Results
                .Select(result => new BenchmarkResultDto(
                    result.Id,
                    result.Timestamp,
                    result.Model,
                    result.AnalysisType,
                    result.TaskId,
                    result.RunNumber,
                    result.Cold,
                    result.TimeToFirstTokenSeconds,
                    result.TotalSeconds,
                    result.CompletionTokens,
                    result.TokensPerSecond,
                    result.TokensEstimated,
                    result.RamPeakMb,
                    result.VramPeakMb,
                    result.Passed,
                    result.ResponseValid,
                    result.Error is not null,
                    result.RamBeforeMb,
                    result.RamAfterMb,
                    result.VramBeforeMb,
                    result.VramAfterMb,
                    result.FinishReason))
                .ToArray());

    private static string GuestText(Guest guest) =>
        $"{guest.FirstName} {guest.LastName} {guest.Email} {guest.Country}";

    private static GuestDetailsDto GuestDetails(Guest guest) =>
        new(guest.Id, guest.FirstName, guest.LastName, $"{guest.FirstName} {guest.LastName}", guest.Email, guest.PhoneNumber, guest.Country, guest.PassportNumber);

    private static GuestListItemDto GuestListItem(Guest guest) =>
        new(guest.Id, guest.FirstName, guest.LastName, $"{guest.FirstName} {guest.LastName}", guest.Email, guest.PhoneNumber, guest.Country, guest.PassportNumber is not null);

    private RoomDetailsDto RoomDetails(Room room) =>
        new(room.Id, room.RoomNumber, room.Floor, room.Size, room.Capacity, room.Status, room.HousekeepingStatus, room.IsBookable, Rooms.RoomIdsWithBookings.Contains(room.Id));

    private static RoomListItemDto RoomListItem(Room room) =>
        new(room.Id, room.RoomNumber, room.Floor, room.Size, room.Capacity, room.Status, room.HousekeepingStatus);

    private static bool Matches(Room room, RoomFilter filter) =>
        (filter.Floor is null || room.Floor == filter.Floor)
        && (filter.Size is null || room.Size == filter.Size)
        && (filter.Status is null || room.Status == filter.Status)
        && (filter.HousekeepingStatus is null || room.HousekeepingStatus == filter.HousekeepingStatus);

    public FakeClock Clock { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public InMemoryBookingRepository Bookings { get; } = new();

    public InMemoryRoomRepository Rooms { get; } = new();

    public InMemoryGuestRepository Guests { get; } = new();

    public InMemoryStaffRepository Staff { get; } = new();

    public InMemoryBenchmarkRunRepository Benchmarks { get; } = new();

    public InMemoryAiDefaultRepository AiDefaults { get; } = new();

    public IBookingQueries BookingQueries { get; } = Substitute.For<IBookingQueries>();

    public IRoomQueries RoomQueries { get; } = Substitute.For<IRoomQueries>();

    public IGuestQueries GuestQueries { get; } = Substitute.For<IGuestQueries>();

    public IBenchmarkQueries BenchmarkQueries { get; } = Substitute.For<IBenchmarkQueries>();

    public BookingService BookingService() =>
        new(Bookings, Rooms, Guests, BookingQueries, RoomQueries, UnitOfWork, Clock);

    public GuestService GuestService() => new(Guests, GuestQueries, UnitOfWork);

    public RoomService RoomService() => new(Rooms, RoomQueries, UnitOfWork);

    public BenchmarkService BenchmarkService() => new(Benchmarks, BenchmarkQueries, UnitOfWork);

    public AiDefaultService AiDefaultService() => new(AiDefaults, UnitOfWork, Clock);

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
