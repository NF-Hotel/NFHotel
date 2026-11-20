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
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.TestSupport;

/// <summary>A clock that stands still: "today" is set by the test, never read from the system.</summary>
public sealed class FakeClock : IClock
{
    private DateOnly _today = new(2026, 1, 1);

    public DateOnly Today
    {
        get
        {
            DateOnly today = _today;
            if (NextDay is { } next)
            {
                _today = next;
                NextDay = null;
            }

            return today;
        }

        set => _today = value;
    }

    /// <summary>The day that <see cref="Today"/> turns to once it has been read: a test lets midnight pass between two reads.</summary>
    public DateOnly? NextDay { get; set; }

    /// <summary>The time of day on <see cref="Today"/>; a test moves it to let time pass.</summary>
    public TimeSpan TimeOfDay { get; set; } = TimeSpan.FromHours(12);

    public DateTimeOffset UtcNow => new(_today.ToDateTime(TimeOnly.MinValue) + TimeOfDay, TimeSpan.Zero);
}

/// <summary>Keeps the warnings the Application layer wrote.</summary>
public sealed class RecordingWarningLog : IWarningLog
{
    public List<string> Messages { get; } = [];

    public void Warn(string message) => Messages.Add(message);
}

/// <summary>A unit of work that counts saves and can fail on demand, as the database would.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Exception? FailWith { get; set; }

    /// <summary>Makes the save with this number (1 is the first since the last reset) fail, as a database error would.</summary>
    public int? FailOnSaveNumber { get; set; }

    /// <summary>Exceptions that the next saves throw, one each, before the saves work again (MIL-088: a lost race).</summary>
    public Queue<Exception> FailNextSaves { get; } = new();

    /// <summary>The names of the locks that the work under a lock took, in order (MIL-088).</summary>
    public List<string> Locks { get; } = [];

    /// <summary>
    /// Decides, for each lock, an error that the work under it ends with before it starts, as a transaction that the
    /// database still refuses after the retries would (MIL-088, RC-436 F1); <see langword="null"/> runs the work.
    /// </summary>
    public Func<string, Exception?> FailUnderLock { get; set; } = _ => null;

    /// <summary>Sets the count of saves back to 0.</summary>
    public void ResetSaveCount() => SaveCount = 0;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (FailNextSaves.TryDequeue(out Exception? next))
        {
            throw next;
        }

        if (FailOnSaveNumber == SaveCount + 1)
        {
            throw new InvalidOperationException("The database could not save.");
        }

        SaveCount++;
        return Task.FromResult(1);
    }

    /// <summary>Runs the work at once: the in-memory world has one caller at a time, so the lock only records its name.</summary>
    public async Task<TResult> RunUnderLockAsync<TResult>(
        string lockName,
        Func<CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        Locks.Add(lockName);
        if (FailUnderLock(lockName) is { } error)
        {
            throw error;
        }

        return await work(cancellationToken).ConfigureAwait(false);
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

    public virtual void Add(TEntity entity)
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
    private static readonly PropertyInfo GuestIdProperty = typeof(Booking).GetProperty(nameof(Booking.GuestId))!;

    /// <summary>
    /// The guests that a booking with a new guest stores along, as the one save of the database does (MIL-088,
    /// <see cref="Booking.CreateForNewGuest"/>); without it a new guest is not stored.
    /// </summary>
    public InMemoryGuestRepository? GuestStore { get; set; }

    /// <summary>The bookings taken back after a save that failed, in order (MIL-088).</summary>
    public List<Booking> Discarded { get; } = [];

    /// <summary>Stores the booking, and a new guest that it took along with its key, as the database gives both in one save.</summary>
    public override void Add(Booking entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Guest is { Id: 0 } guest && GuestStore is not null)
        {
            GuestStore.Add(guest);
            GuestIdProperty.SetValue(entity, guest.Id);
        }

        base.Add(entity);
    }

    /// <summary>Takes back a booking that could not be saved, and the new guest it took along.</summary>
    public void Discard(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Discarded.Add(booking);
        Items.Remove(booking);
        if (booking.Guest is { } guest && GuestStore is not null)
        {
            GuestStore.Items.Remove(guest);
        }
    }

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
    private MarketAnalysisRunTracker? _marketAnalysisTracker;

    public ServiceWorld()
    {
        BookingQueries.GetReadModelAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadModelOrNull(call.Arg<int>())));
        BookingQueries.GetReadModelByReferenceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(ReadModelOrNull(call.Arg<Guid>())));
        GuestSignInLimits.TryCount(Arg.Any<Guid?>(), Arg.Any<string>()).Returns(true);
        EmailLimit.ComingPublicBookingsPerEmail.Returns(3);
        Bookings.GuestStore = Guests;
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
        BookingQueries.CountComingPublicBookingsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(CountComingPublicBookings(call.ArgAt<string>(0), call.ArgAt<DateOnly>(1))));
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

    // The count per email of ADR-0051 decision 3 over the in-memory repositories, by mailbox since MIL-088 (ADR-0052
    // decision 6), as the query of the database compares it.
    private int CountComingPublicBookings(string email, DateOnly today)
    {
        string wanted = Mailbox.NormalFormOf(email);
        HashSet<int> guestIds = Guests.Items
            .Where(guest => Mailbox.NormalFormOf(guest.Email) == wanted)
            .Select(guest => guest.Id)
            .ToHashSet();

        return Bookings.Items.Count(booking => booking.MadeOnPublicSite
            && booking.Status != BookingStatus.Cancelled
            && booking.EndDate >= today
            && guestIds.Contains(booking.GuestId));
    }

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

    public InMemoryStaffAuditRepository StaffAudit { get; } = new();

    public FakeStaffCallContext CallContext { get; } = new();

    public InMemoryBenchmarkRunRepository Benchmarks { get; } = new();

    public InMemoryAiDefaultRepository AiDefaults { get; } = new();

    public InMemoryRoomRateRepository RoomRates { get; } = new();

    public InMemoryPricingSettingRepository PricingSettings { get; } = new();

    public InMemoryPriceChangeRepository PriceChanges { get; } = new();

    public InMemoryBenchmarkRequestRepository BenchmarkRequests { get; } = new();

    public InMemoryMarketAnalysisRunRepository MarketAnalysisRuns { get; } = new();

    public FakeMarketAnalysisServiceClient MarketAnalysisClient { get; } = new();

    public FakeMarketAnalysisBookingQueries MarketAnalysisBookings { get; } = new();

    public RecordingMarketAnalysisLauncher MarketAnalysisLauncher { get; } = new();

    public MarketAnalysisRunTracker MarketAnalysisTracker => _marketAnalysisTracker ??= new(Clock);

    public IBookingQueries BookingQueries { get; } = Substitute.For<IBookingQueries>();

    public IRoomQueries RoomQueries { get; } = Substitute.For<IRoomQueries>();

    public IGuestQueries GuestQueries { get; } = Substitute.For<IGuestQueries>();

    public IBenchmarkQueries BenchmarkQueries { get; } = Substitute.For<IBenchmarkQueries>();

    public InMemoryBookingMarketDetailRepository BookingMarketDetails { get; } = new();

    public RecordingWarningLog Warnings { get; } = new();

    /// <summary>The signed-in guest of the services' calls (MIL-018): none until a test sets a booking.</summary>
    public FakeGuestCallContext GuestCall { get; } = new();

    /// <summary>The issuer of the guest tokens of <see cref="GuestAccessService"/> (MIL-018).</summary>
    public FakeGuestTokenIssuer GuestTokens { get; } = new();

    /// <summary>
    /// The limits on the tries of the guest sign-in (ADR-0049 decision 6): a substitute that counts every try and never
    /// refuses, which a test can still configure or check.
    /// </summary>
    public IGuestSignInLimits GuestSignInLimits { get; } = Substitute.For<IGuestSignInLimits>();

    /// <summary>The address of the caller of the guest sign-in (ADR-0049 decision 6).</summary>
    public FakeCallerAddress CallerAddress { get; } = new();

    /// <summary>
    /// The limit per email of the public booking (MIL-087, ADR-0051 decision 3): a substitute that holds the number of the
    /// table, which a test can still configure.
    /// </summary>
    public IPublicBookingEmailLimit EmailLimit { get; } = Substitute.For<IPublicBookingEmailLimit>();

    /// <summary>The setting of the verification by email (MIL-088): off until a test switches it on.</summary>
    public FakeBookingVerificationSettings Verification { get; } = new();

    /// <summary>The booking requests (MIL-088).</summary>
    public InMemoryBookingRequestRepository BookingRequests { get; } = new();

    /// <summary>The records of the mails (MIL-088).</summary>
    public InMemorySentMailRepository SentMails { get; } = new();

    /// <summary>The relay of the mails (MIL-088).</summary>
    public FakeMailSender Mails { get; } = new();

    /// <summary>The security events of the booking request (MIL-088).</summary>
    public RecordingBookingRequestEvents RequestEvents { get; } = new();

    /// <summary>
    /// Builds the service; <paramref name="prices"/> replaces the real price lookup, for example to make it fail,
    /// <paramref name="guestCall"/> the signed-in guest, which is otherwise <see cref="GuestCall"/>,
    /// <paramref name="emailLimit"/> the limit per email, which is otherwise <see cref="EmailLimit"/>, and
    /// <paramref name="verification"/> the setting of the verification, which is otherwise <see cref="Verification"/>.
    /// </summary>
    public BookingService BookingService(
        IStaffAuthorizationService? authorization = null,
        IPriceChangeService? prices = null,
        IGuestCallContext? guestCall = null,
        IPublicBookingEmailLimit? emailLimit = null,
        IBookingVerificationSettings? verification = null) =>
        new(Bookings, Rooms, Guests, BookingQueries, RoomQueries, UnitOfWork, Clock, authorization ?? Substitute.For<IStaffAuthorizationService>(), prices ?? PriceChangeService(), BookingMarketDetails, Warnings, guestCall ?? GuestCall, emailLimit ?? EmailLimit, verification ?? Verification);

    /// <summary>
    /// Builds the booking request over <see cref="BookingService"/> with the in-memory requests, records, relay, setting and
    /// events of the world (MIL-088); <paramref name="bookings"/> and <paramref name="settings"/> replace them.
    /// </summary>
    public BookingRequestService BookingRequestService(IPublicBookingService? bookings = null, IBookingVerificationSettings? settings = null) =>
        new(bookings ?? BookingService(verification: settings), BookingRequests, SentMails, UnitOfWork, Mails, settings ?? Verification, GuestTokens, RequestEvents, Clock);

    /// <summary>Builds the clean-up of the booking requests and the records of the mails (MIL-088).</summary>
    public BookingRequestCleanup BookingRequestCleanup() => new(BookingRequests, SentMails, Clock);

    /// <summary>Builds the service. A guest reads no guest by its key since MIL-085 (ADR-0049 decision 3).</summary>
    public GuestService GuestService(IStaffAuthorizationService? authorization = null) =>
      new(Guests, GuestQueries, UnitOfWork, authorization ?? Substitute.For<IStaffAuthorizationService>());

    /// <summary>
    /// Builds the guest sign-in over <see cref="BookingService"/> and <see cref="BookingQueries"/>, with the tokens of
    /// <see cref="GuestTokens"/>, the limits of <see cref="GuestSignInLimits"/> unless <paramref name="limits"/> replaces
    /// them, and the address of <see cref="CallerAddress"/> (MIL-018, ADR-0049).
    /// </summary>
    public GuestAccessService GuestAccessService(IGuestSignInLimits? limits = null, IStaffAuthorizationService? authorization = null) =>
        new(BookingService(authorization), BookingQueries, GuestTokens, limits ?? GuestSignInLimits, CallerAddress);

    /// <summary>
    /// Builds the guest's own booking over <see cref="BookingService"/> (ADR-0049 decision 3); <paramref name="guestCall"/>
    /// replaces the signed-in guest, which is otherwise <see cref="GuestCall"/>.
    /// </summary>
    public GuestBookingService GuestBookingService(IGuestCallContext? guestCall = null) =>
        new(BookingService(guestCall: guestCall ?? GuestCall), guestCall ?? GuestCall);

    public StaffAuthService StaffAuthService(IPasswordHasher? hasher = null, IStaffTokenIssuer? tokens = null, ILoginThrottle? throttle = null) =>
        new(Staff, hasher ?? new FakePasswordHasher(), tokens ?? new FakeStaffTokenIssuer(), throttle ?? Substitute.For<ILoginThrottle>(), UnitOfWork, StaffAudit, CallContext, Clock);

    public StaffAccountService StaffAccountService(ILoginThrottle? throttle = null, IStaffAuthorizationService? authorization = null) =>
        new(Staff, new FakePasswordHasher(), UnitOfWork, throttle ?? Substitute.For<ILoginThrottle>(), StaffAudit, CallContext, Clock,
            authorization ?? Substitute.For<IStaffAuthorizationService>());

    public RoomService RoomService() => new(Rooms, RoomQueries, UnitOfWork);

    public BenchmarkService BenchmarkService() => new(Benchmarks, BenchmarkQueries, UnitOfWork);

    public AiDefaultService AiDefaultService() => new(AiDefaults, UnitOfWork, Clock);

    public RoomRateService RoomRateService(IStaffCallContext? context = null) =>
        new(RoomRates, StaffAudit, UnitOfWork, Clock, context ?? CallContext);

    public PriceChangeService PriceChangeService(IStaffCallContext? context = null) =>
        new(PriceChanges, RoomRates, StaffAudit, UnitOfWork, Clock, context ?? CallContext);

    public PriceSuggestionService PriceSuggestionService(IStaffCallContext? context = null) =>
        new(MarketAnalysisService(), RoomRates, PricingSettings, PriceChanges, PriceChangeService(context), Clock);

    public PricingSettingsService PricingSettingsService(IStaffCallContext? context = null) =>
        new(PricingSettings, StaffAudit, UnitOfWork, Clock, context ?? CallContext);

    public BenchmarkRequestService BenchmarkRequestService() => new(BenchmarkRequests, UnitOfWork, Clock);

    public MarketAnalysisService MarketAnalysisService() => new(MarketAnalysisTracker, MarketAnalysisLauncher, MarketAnalysisRuns);

    public MarketAnalysisRunner MarketAnalysisRunner() =>
        new(MarketAnalysisBookings, MarketAnalysisClient, MarketAnalysisRuns, UnitOfWork, MarketAnalysisTracker, Clock, PricingSettingsService());

    public Room AddRoom(string number = "101", int floor = 1, RoomSize size = RoomSize.Double, int capacity = 2)
    {
        var room = Room.Create(number, floor, size, capacity);
        Rooms.Add(room);
        return room;
    }

    public Guest AddGuest(string firstName = "Anna", string lastName = "Jensen") =>
        AddGuestCore(Guest.Create(firstName, lastName, "anna@example.com", "+4512345678", "Denmark", null));

    /// <summary>Stores a guest with an email and a passport number of the test's own, for the guest sign-in (MIL-018).</summary>
    public Guest AddGuest(string firstName, string lastName, string email, string? passportNumber) =>
        AddGuestCore(Guest.Create(firstName, lastName, email, "+4512345678", "Denmark", passportNumber));

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

    private BookingReadModel? ReadModelOrNull(Guid reference)
    {
        Booking? booking = Bookings.Items.SingleOrDefault(item => item.Reference == reference);
        return booking is null ? null : ReadModelFor(booking);
    }

    public BookingReadModel ReadModelFor(Booking booking)
    {
        var room = Rooms.Items.Single(item => item.Id == booking.RoomId);
        var guest = Guests.Items.Single(item => item.Id == booking.GuestId);
        return new BookingReadModel(
            booking.Id,
            booking.Reference,
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
