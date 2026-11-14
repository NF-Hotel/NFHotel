using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Implementation of the use cases of bookings.
/// </summary>
/// <remarks>
/// The service contains <b>no</b> overlap logic. It fetches candidates and asks
/// <see cref="NFHotel.Domain.Entities.Booking"/> (A-01, A-04). If the predicate existed here too, it would be
/// a third copy after the domain and the database exclusion constraint — and the three would drift
/// hinanden.
/// <para>
/// "Today" and "now" come exclusively from <see cref="IClock"/> (A-05).
/// </para>
/// </remarks>
public sealed class BookingService : IBookingService
{
    /// <summary>
    /// The statuses that occupy a room. Derived from the domain
    /// (<see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>), not written out as a list — otherwise
    /// a new status step would have to be remembered in two places.
    /// </summary>
    private static readonly IReadOnlyCollection<BookingStatus> BlockingStatuses =
        Enum.GetValues<BookingStatus>().Where(BookingStatusExtensions.BlocksRoom).ToArray();

    /// <summary>
    /// The most nights the availability calendar counts in one call: two months, so a month view with the weeks
    /// around it always fits, and a call can never count the whole future.
    /// </summary>
    public const int MaxAvailabilityNights = 62;

    /// <summary>All statuses. Used when the overview must also show cancelled ones (BR-13).</summary>
    private static readonly IReadOnlyCollection<BookingStatus> AllStatuses = Enum.GetValues<BookingStatus>();

    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IBookingQueries _bookingQueries;
    private readonly IRoomQueries _roomQueries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IStaffAuthorizationService _authorization;
    private readonly IPriceChangeService _prices;
    private readonly IBookingMarketDetailRepository _marketDetails;
    private readonly IWarningLog _warnings;
    private readonly ILoginThrottle _receiptThrottle;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="bookingRepository">Data access for bookings.</param>
    /// <param name="roomRepository">Data access for rooms — used for BR-110 and for departure cleaning.</param>
    /// <param name="guestRepository">Data access for guests — used for BR-47.</param>
    /// <param name="bookingQueries">Reading bookings as projections (A-11).</param>
    /// <param name="roomQueries">Reading rooms as projections — used for the availability lookup (BR-110).</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    /// <param name="authorization">The service for checking staff permissions.</param>
    /// <param name="prices">Gives the effective price of a room size on the arrival date (MIL-055).</param>
    /// <param name="marketDetails">Stores the price per night of a new booking (MIL-055).</param>
    /// <param name="warnings">Where a lost price of a saved booking is reported (MIL-055).</param>
    /// <param name="receiptThrottle">
    /// Counts the failed lookups of a booking number by a guest (MIL-079): the throttle of the staff sign-in, with a key of
    /// its own per booking.
    /// </param>
    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IBookingQueries bookingQueries,
        IRoomQueries roomQueries,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffAuthorizationService authorization,
        IPriceChangeService prices,
        IBookingMarketDetailRepository marketDetails,
        IWarningLog warnings,
        ILoginThrottle receiptThrottle)
    {
        ArgumentNullException.ThrowIfNull(bookingRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(guestRepository);
        ArgumentNullException.ThrowIfNull(bookingQueries);
        ArgumentNullException.ThrowIfNull(roomQueries);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(marketDetails);
        ArgumentNullException.ThrowIfNull(warnings);
        ArgumentNullException.ThrowIfNull(receiptThrottle);

        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _guestRepository = guestRepository;
        _bookingQueries = bookingQueries;
        _roomQueries = roomQueries;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _authorization = authorization;
        _prices = prices;
        _marketDetails = marketDetails;
        _warnings = warnings;
        _receiptThrottle = receiptThrottle;
    }

    // --------------------------------------------------------
    // READING
    // --------------------------------------------------------

    /// <summary>Fetches one booking (BR-115).</summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the booking, or <see cref="ErrorCodes.Booking.NotFound"/>.</returns>
    public async Task<Result<BookingDetailsDto>> GetByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        // The details hold the guest, so only staff who may see the bookings read them (MIL-079). The check is here as
        // well as on the endpoint, as for the other reads: the endpoint is the door, the use case the rule.
        if (!await _authorization.HasPermissionAsync(Permissions.BookingsView, cancellationToken).ConfigureAwait(false))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        var readModel = await _bookingQueries
            .GetReadModelAsync(bookingId, cancellationToken)
            .ConfigureAwait(false);

        return readModel is null
            ? Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound)
            : Result<BookingDetailsDto>.Success(readModel.ToDetails(_clock.Today));
    }

    /// <summary>
    /// Looks a booking up for its guest, by the number and the email of the booking (MIL-079, BR-C-06).
    /// </summary>
    /// <param name="request">The booking key and the email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Only the receipt of the booking, or the failure that <see cref="IBookingService.LookUpReceiptAsync"/> names.</returns>
    /// <remarks>
    /// A wrong email and an unknown number give the same <see cref="ErrorCodes.Booking.NotFound"/>, and the booking is read
    /// in both cases, so neither the answer nor its cost tells a guesser whether a number exists. Each failure counts
    /// against the number; after the most failures that the staff sign-in allows in its window, the number is refused
    /// with <see cref="ErrorCodes.Booking.TooManyLookups"/> for the rest of the window, so the email of a known number
    /// cannot be found by trying. A lookup that succeeds clears the count.
    /// </remarks>
    public async Task<Result<BookingReceiptDto>> LookUpReceiptAsync(
        BookingReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<BookingReceiptDto>.Failure(ErrorCodes.Booking.ReceiptEmailRequired);
        }

        if (request.BookingId <= 0)
        {
            return Result<BookingReceiptDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        string throttleKey = ReceiptThrottleKey(request.BookingId);
        if (_receiptThrottle.IsBlocked(throttleKey))
        {
            return Result<BookingReceiptDto>.Failure(ErrorCodes.Booking.TooManyLookups);
        }

        BookingReadModel? readModel = await _bookingQueries
            .GetReadModelAsync(request.BookingId, cancellationToken)
            .ConfigureAwait(false);
        BookingDetailsDto? details = readModel?.ToDetails(_clock.Today);
        if (details is null || !IsSameEmail(details.Guest.Email, request.Email))
        {
            _receiptThrottle.RecordFailure(throttleKey);
            return Result<BookingReceiptDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        _receiptThrottle.Reset(throttleKey);
        return Result<BookingReceiptDto>.Success(BookingReceiptDto.From(details));
    }

    /// <summary>The key of a booking in the shared throttle; the prefix keeps it apart from the user names of staff.</summary>
    private static string ReceiptThrottleKey(int bookingId) =>
        string.Create(CultureInfo.InvariantCulture, $"booking-receipt:{bookingId}");

    /// <summary>
    /// Compares two emails without letter case and surrounding spaces, in a time that does not depend on where they
    /// first differ.
    /// </summary>
    private static bool IsSameEmail(string stored, string given) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stored.Trim().ToUpperInvariant()),
            Encoding.UTF8.GetBytes(given.Trim().ToUpperInvariant()));

    /// <summary>Fetches the booking overview (BR-23, BR-24, BR-25, BR-32, BR-111).</summary>
    /// <param name="query">Date window, free text, sorting and whether cancelled bookings are included.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows of the overview, sorted as requested.</returns>
    public async Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(
        BookingOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Checks whether the staff member can view the booking overview.
        if (!await _authorization.HasPermissionAsync(
            Permissions.BookingsView,
            cancellationToken))
        {
            return Result<IReadOnlyList<BookingListItemDto>>.Failure(
                ErrorCodes.Api.Forbidden);
        }

        // MIL-013: an end before the start is a mistake, not an empty period. An end equal to the start
        // is the view of one day and is allowed, unlike a stay, which needs at least one night.
        if (query.PeriodEnd < query.PeriodStart)
        {
            return Result<IReadOnlyList<BookingListItemDto>>.Failure(ErrorCodes.Booking.OverviewPeriodInvalid);
        }

        // BR-13: cancelled bookings are not deleted, so they must be possible to hide in the overview.
        var statuses = query.IncludeCancelled ? AllStatuses : BlockingStatuses;

        var readModels = await _bookingQueries
            .GetOverviewAsync(query.PeriodStart, query.PeriodEnd, statuses, query.SearchText, cancellationToken)
            .ConfigureAwait(false);

        var today = _clock.Today;
        var items = readModels.Select(readModel => readModel.ToListItem(today));

        return Result<IReadOnlyList<BookingListItemDto>>.Success(Sort(items, query.SortBy, query.Direction));
    }

    /// <summary>
    /// Finds free rooms in a period (BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-110).
    /// </summary>
    /// <param name="checkInDate">Arrival date, inclusive.</param>
    /// <param name="checkOutDate">Departure date, exclusive.</param>
    /// <param name="excludeBookingId">The booking being edited, or <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rooms that are both bookable and free for the whole period.</returns>
    public async Task<Result<IReadOnlyList<AvailableRoomDto>>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int? excludeBookingId,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePeriod(checkInDate, checkOutDate, out var period, out var periodError))
        {
            return Result<IReadOnlyList<AvailableRoomDto>>.Failure(periodError);
        }

        // BR-110: only rooms that may be let at all. The service chooses the status — the repository
        // no longer decides for itself what "available" means.
        var bookableRooms = await _roomQueries
            .GetByStatusAsync(RoomStatus.Available, cancellationToken)
            .ConfigureAwait(false);

        var occupancies = await _bookingQueries
            .GetOccupancyAsync(period.Start, period.End, BlockingStatuses, cancellationToken)
            .ConfigureAwait(false);

        var occupiedRoomIds = new HashSet<int>();

        foreach (var occupancy in occupancies)
        {
            if (excludeBookingId is not null && occupancy.BookingId == excludeBookingId.Value)
            {
                continue;
            }

            // The overlap decision is made by the domain, never here (A-01, A-04). The half-open
            // interval of BR-39 means that departure and arrival on the same day are not an overlap.
            if (!occupancy.Status.BlocksRoom())
            {
                continue;
            }

            if (new DateRange(occupancy.StartDate, occupancy.EffectiveEndDate).Overlaps(period))
            {
                occupiedRoomIds.Add(occupancy.RoomId);
            }
        }

        var available = bookableRooms
            .Where(room => !occupiedRoomIds.Contains(room.RoomId))
            .ToArray();

        return Result<IReadOnlyList<AvailableRoomDto>>.Success(available);
    }

    /// <summary>
    /// Counts the free rooms of each room size for every night of a period (BR-34, BR-39, BR-110).
    /// </summary>
    /// <param name="from">The first night, inclusive.</param>
    /// <param name="to">The day after the last night, exclusive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One entry per night, in date order.</returns>
    public async Task<Result<IReadOnlyList<NightAvailabilityDto>>> GetAvailabilityAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        // The calendar shows the occupancy of the whole hotel, so it is for staff who may see the bookings.
        if (!await _authorization.HasPermissionAsync(Permissions.BookingsView, cancellationToken).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<NightAvailabilityDto>>.Failure(ErrorCodes.Api.Forbidden);
        }

        if (!TryCreatePeriod(from, to, out DateRange period, out string periodError))
        {
            return Result<IReadOnlyList<NightAvailabilityDto>>.Failure(periodError);
        }

        if (period.Nights > MaxAvailabilityNights)
        {
            return Result<IReadOnlyList<NightAvailabilityDto>>.Failure(ErrorCodes.Booking.AvailabilityPeriodTooLong);
        }

        // BR-110: the same rooms as the lookup of free rooms, so the calendar never counts a room the flow refuses.
        IReadOnlyList<AvailableRoomDto> sellableRooms = await _roomQueries
            .GetByStatusAsync(RoomStatus.Available, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RoomOccupancyDto> occupancies = await _bookingQueries
            .GetOccupancyAsync(period.Start, period.End, BlockingStatuses, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<(int RoomId, DateRange Stay)> stays = BlockingStays(occupancies);
        RoomSize[] sizes = Enum.GetValues<RoomSize>();
        List<NightAvailabilityDto> nights = new(period.Nights);

        for (DateOnly night = period.Start; night < period.End; night = night.AddDays(1))
        {
            // The domain decides whether a stay covers the night. The half-open period of BR-39 leaves the
            // departure day free, so a guest who leaves in the morning does not block the night.
            DateOnly current = night;
            HashSet<int> takenRoomIds = stays
                .Where(stay => stay.Stay.Contains(current))
                .Select(stay => stay.RoomId)
                .ToHashSet();

            RoomTypeAvailabilityDto[] types = sizes
                .Select(size => CountFreeRooms(size, sellableRooms, takenRoomIds))
                .ToArray();

            nights.Add(new NightAvailabilityDto(night, types));
        }

        return Result<IReadOnlyList<NightAvailabilityDto>>.Success(nights);
    }

    /// <summary>
    /// The stays that block their room, as periods. A stay that ended on the day it began covers no night and is
    /// left out, because a period needs at least one night.
    /// </summary>
    private static IReadOnlyList<(int RoomId, DateRange Stay)> BlockingStays(IReadOnlyList<RoomOccupancyDto> occupancies) =>
        occupancies
            .Where(occupancy => occupancy.Status.BlocksRoom() && occupancy.EffectiveEndDate > occupancy.StartDate)
            .Select(occupancy => (occupancy.RoomId, new DateRange(occupancy.StartDate, occupancy.EffectiveEndDate)))
            .ToArray();

    /// <summary>The free rooms of one size on one night, with the numbers sorted as people read them: 101 before 1001.</summary>
    private static RoomTypeAvailabilityDto CountFreeRooms(
        RoomSize size,
        IReadOnlyList<AvailableRoomDto> sellableRooms,
        HashSet<int> takenRoomIds)
    {
        AvailableRoomDto[] ofSize = sellableRooms.Where(room => room.Size == size).ToArray();
        string[] freeRooms = ofSize
            .Where(room => !takenRoomIds.Contains(room.RoomId))
            .Select(room => room.RoomNumber)
            .OrderBy(number => number.Length)
            .ThenBy(number => number, StringComparer.Ordinal)
            .ToArray();

        return new RoomTypeAvailabilityDto(size, freeRooms.Length, ofSize.Length, freeRooms);
    }

    // --------------------------------------------------------
    // KOMMANDOER
    // --------------------------------------------------------

    /// <summary>
    /// Creates a booking (BR-01, BR-19, BR-41 to BR-49, BR-51, BR-101 to BR-106, B-03).
    /// </summary>
    /// <param name="request">Period, room and either an existing or a new guest (BR-47).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created booking, or the broken rules.</returns>
    public async Task<Result<BookingDetailsDto>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Checks whether the staff member can create bookings.
        if (!await _authorization.HasPermissionAsync(
            Permissions.CreateBooking,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        // Calls the shared booking creation logic.
        return await CreateBookingInternalAsync(request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a booking through the public guest booking flow. The guest chooses a room type; the service picks the
    /// free room of that type that fits the party most closely. Only new guest details are accepted.
    /// </summary>
    public async Task<Result<BookingDetailsDto>> CreatePublicAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Public bookings must use new guest details.
        if (request.NewGuest is null)
        {
            return Result<BookingDetailsDto>.Failure(
                ErrorCodes.Booking.GuestSelectionInvalid);
        }

        Result<IReadOnlyList<AvailableRoomDto>> available = await GetAvailableRoomsAsync(
            request.CheckInDate,
            request.CheckOutDate,
            null,
            cancellationToken).ConfigureAwait(false);

        if (available.IsFailure)
        {
            return Result<BookingDetailsDto>.Failure(available.Errors);
        }

        AvailableRoomDto? room = ChooseRoom(available.Value, request.RoomSize, request.Guests);

        if (room is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.RoomTypeNotAvailable);
        }

        // The shared booking logic checks the room again, so two guests who get the same last room cannot both
        // book it: the second one is told the room is taken (BR-19, B-03).
        CreateBookingRequest booking = new(request.CheckInDate, request.CheckOutDate, room.RoomId, null, request.NewGuest);

        return await CreateBookingInternalAsync(booking, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// The free room of the type that sleeps the party with the fewest spare beds, so the bigger rooms stay free for
    /// bigger parties; the room number breaks a tie, so the choice is the same every time.
    /// </summary>
    private static AvailableRoomDto? ChooseRoom(IReadOnlyList<AvailableRoomDto> available, RoomSize size, int guests)
    {
        int party = Math.Max(1, guests);

        return available
            .Where(room => room.Size == size && room.Capacity >= party)
            .OrderBy(room => room.Capacity)
            .ThenBy(room => room.RoomNumber, StringComparer.Ordinal)
            .ThenBy(room => room.RoomId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Creates a booking using the shared booking rules.
    /// </summary>
    private async Task<Result<BookingDetailsDto>> CreateBookingInternalAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        // BR-47: exactly one of the two guest paths. Both and neither are a rule error,
        // not a NullReferenceException down in a form.
        var hasExistingGuest = request.GuestId is > 0;
        var hasNewGuest = request.NewGuest is not null;

        if (hasExistingGuest == hasNewGuest)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.GuestSelectionInvalid);
        }

        if (!TryCreatePeriod(request.CheckInDate, request.CheckOutDate, out var period, out var periodError))
        {
            return Result<BookingDetailsDto>.Failure(periodError);
        }

        var roomCheck = await CheckRoomIsBookableAsync(request.RoomId, cancellationToken).ConfigureAwait(false);

        if (roomCheck is not null)
        {
            return Result<BookingDetailsDto>.Failure(roomCheck);
        }

        var overlapError = await FindOverlapAsync(request.RoomId, period, null, cancellationToken)
            .ConfigureAwait(false);

        if (overlapError is not null)
        {
            return Result<BookingDetailsDto>.Failure(overlapError);
        }

        var guestResult = await ResolveGuestIdAsync(request, cancellationToken).ConfigureAwait(false);

        if (guestResult.IsFailure)
        {
            return Result<BookingDetailsDto>.Failure(guestResult.Errors);
        }

        Booking booking;

        try
        {
            booking = Booking.Create(period, request.RoomId, guestResult.Value, _clock.Today);
        }
        catch (DomainException exception)
        {
            return Result<BookingDetailsDto>.Failure(exception.Code);
        }

        // MIL-055: the price is looked up before the booking is saved and stored after it; a booking made
        // without a price (no base rate, or the lookup failed) is created exactly as before.
        decimal? pricePerNight = await FindPricePerNightAsync(request.RoomId, period.Start, cancellationToken)
            .ConfigureAwait(false);

        _bookingRepository.Add(booking);

        Result<BookingDetailsDto> created = await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);

        if (created.IsSuccess && pricePerNight is not null)
        {
            await StorePricePerNightAsync(booking.Id, pricePerNight.Value, cancellationToken).ConfigureAwait(false);
        }

        return created;
    }

    /// <summary>
    /// Finds the effective price of the size of the booked room on the arrival date (MIL-055).
    /// </summary>
    /// <param name="roomId">The booked room.</param>
    /// <param name="arrival">The arrival date.</param>
    /// <param name="cancellationToken">The token that cancels the work.</param>
    /// <returns>The price per night, or <c>null</c> when the size has no base rate or the lookup failed.</returns>
    /// <remarks>
    /// Pricing never stops a booking when the lookup answers with a failed result or the room is not found: both are
    /// treated as "no price". An exception thrown by the lookup (a failing store, say) is not caught here; it fails the
    /// request before the booking is saved.
    /// </remarks>
    private async Task<decimal?> FindPricePerNightAsync(int roomId, DateOnly arrival, CancellationToken cancellationToken)
    {
        Room? room = await _roomRepository.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return null;
        }

        Result<EffectivePriceDto> price = await _prices
            .GetEffectivePriceAsync(room.Size.ToString(), arrival, cancellationToken)
            .ConfigureAwait(false);

        return price.IsSuccess ? price.Value.Price : null;
    }

    /// <summary>
    /// Stores the price per night of a booking that has just been saved, in the market details that the market
    /// analysis reads (MIL-055).
    /// </summary>
    /// <param name="bookingId">The saved booking.</param>
    /// <param name="pricePerNight">The price per night.</param>
    /// <param name="cancellationToken">The token that cancels the work.</param>
    /// <returns>A task that completes when the price is stored, or deliberately skipped.</returns>
    /// <remarks>
    /// The booking has its key only after its save, and the details need it, so the details are a save of their own right
    /// after it. The booking is made and stays, so a failure here is not rethrown: the booking is then without a price, and a
    /// warning with the booking id and the type and text of the exception is written to the log. The
    /// guest counts are 0 because a booking holds none; the market analysis feed reads such a row as
    /// "party unknown".
    /// </remarks>
    private async Task StorePricePerNightAsync(int bookingId, decimal pricePerNight, CancellationToken cancellationToken)
    {
        BookingMarketDetail detail;

        try
        {
            detail = BookingMarketDetail.Create(
                bookingId,
                _clock.Today,
                new BookingMarketValues(pricePerNight, 0, 0, 0, null, null, null, null, null, false, 0, 0, 0, 0));
        }
        catch (DomainException)
        {
            return;
        }

        _marketDetails.Add(detail);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _marketDetails.Discard(detail);

            // Only the id and the type of the exception are named, as IWarningLog asks: the message of an exception from the
            // database can quote the values of the row, so it is left out.
            _warnings.Warn($"The price per night of booking {bookingId} could not be stored; the booking is kept without a price. {exception.GetType().Name}");
        }
    }

    /// <summary>Confirms a booking (BR-02, BR-03, BR-05, BR-121).</summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.ConfirmNotAllowed"/>.</returns>
    public async Task<Result<BookingDetailsDto>> ConfirmAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        // Checks whether the staff member can confirm bookings.
        if (!await _authorization.HasPermissionAsync(
            Permissions.EditOtherBookings,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        return await TransitionAsync(
            bookingId,
            static booking => booking.Confirm(),
            cancellationToken);
    }

    /// <summary>Checks the guest in (BR-06, BR-07, BR-08, B-01).</summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CheckInNotAllowed"/>.</returns>
    public async Task<Result<BookingDetailsDto>> CheckInAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        // Checks whether the staff member can check guests in.
        if (!await _authorization.HasPermissionAsync(
            Permissions.EditOtherBookings,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        return await TransitionAsync(
            bookingId,
            booking => booking.CheckIn(_clock.UtcNow, _clock.Today),
            cancellationToken,
            RoomMustBeBookableForCheckInAsync);
    }

    /// <summary>Checks the guest out (BR-09, BR-10, BR-107, A-01, A-03, BR-N-06).</summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CheckOutNotAllowed"/>.</returns>
    /// <remarks>
    /// The hotel-local check-out date is supplied by the clock, because converting a timestamp →
    /// calendar date is a time zone policy that neither the domain nor the database can perform
    /// deterministisk (A-02, A-03).
    /// <para>
    /// The two-sided guard of the domain rejects a check-out date before the arrival
    /// (<see cref="ErrorCodes.Booking.CheckOutDateBeforeStartDate"/>) and after today
    /// (<see cref="ErrorCodes.Booking.CheckOutDateInFuture"/>). The latter cannot be triggered by this
    /// use case itself — it checks out right now — but the guard holds if a
    /// "record a check-out retroactively" use case is added later.
    /// </para>
    /// </remarks>
    public async Task<Result<BookingDetailsDto>> CheckOutAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        // Checks whether the staff member has permission to check guests out.
        if (!await _authorization.HasPermissionAsync(
            Permissions.EditOtherBookings,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);

        if (booking is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        // The check-out happens now, so the hotel-local check-out date IS today's date. The two
        // parameters are nevertheless separate in the domain, because a later retroactive
        // registration must be able to give an earlier date without moving "today".
        var today = _clock.Today;

        try
        {
            booking.CheckOut(_clock.UtcNow, checkOutDate: today, today: today);
        }
        catch (DomainException exception)
        {
            return Result<BookingDetailsDto>.Failure(exception.Code);
        }

        _bookingRepository.Update(booking);

        await MarkRoomForDepartureCleaningAsync(booking.RoomId, cancellationToken).ConfigureAwait(false);

        return await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a booking (BR-11, BR-13, BR-122).</summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CancelNotAllowed"/>.</returns>
    public async Task<Result<BookingDetailsDto>> CancelAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        // Checks whether the staff member has permission to cancel bookings.
        if (!await _authorization.HasPermissionAsync(
            Permissions.EditOtherBookings,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        return await TransitionAsync(
            bookingId,
            static booking => booking.Cancel(),
            cancellationToken);
    }

    /// <summary>
    /// Moves a booking (BR-14, BR-17, BR-18, BR-19, BR-20, BR-108, BR-123, B-03).
    /// </summary>
    /// <param name="request">The id of the booking, the new period and the room.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The moved booking, or the broken rules.</returns>
    public async Task<Result<BookingDetailsDto>> RescheduleAsync(
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BookingId);

        // Checks whether the staff member has permission to reschedule bookings.
        if (!await _authorization.HasPermissionAsync(
            Permissions.EditOtherBookings,
            cancellationToken))
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Api.Forbidden);
        }

        if (!TryCreatePeriod(request.NewStartDate, request.NewEndDate, out var period, out var periodError))
        {
            return Result<BookingDetailsDto>.Failure(periodError);
        }

        var booking = await _bookingRepository
            .GetByIdAsync(request.BookingId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        var roomCheck = await CheckRoomIsBookableAsync(request.RoomId, cancellationToken).ConfigureAwait(false);

        if (roomCheck is not null)
        {
            return Result<BookingDetailsDto>.Failure(roomCheck);
        }

        // BR-19 with the same function as CreateAsync, and with the booking itself left out so it does not
        // block its own move.
        var overlapError = await FindOverlapAsync(request.RoomId, period, request.BookingId, cancellationToken)
            .ConfigureAwait(false);

        if (overlapError is not null)
        {
            return Result<BookingDetailsDto>.Failure(overlapError);
        }

        try
        {
            booking.Reschedule(period, request.RoomId, _clock.Today);
        }
        catch (DomainException exception)
        {
            return Result<BookingDetailsDto>.Failure(exception.Code);
        }

        _bookingRepository.Update(booking);

        return await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    // --------------------------------------------------------
    // COMMON
    // --------------------------------------------------------

    /// <summary>
    /// Runs one domain transition on a booking and saves.
    /// </summary>
    /// <param name="bookingId">The booking id.</param>
    /// <param name="transition">The transition — always exactly one method call on <see cref="NFHotel.Domain.Entities.Booking"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="precondition">An optional check that needs more than the booking, run before the transition; returns an error code or <c>null</c>.</param>
    /// <returns>The details of the booking, or the error code of the domain if the transition was rejected.</returns>
    private async Task<Result<BookingDetailsDto>> TransitionAsync(
        int bookingId,
        Action<Booking> transition,
        CancellationToken cancellationToken,
        Func<Booking, CancellationToken, Task<string?>>? precondition = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);

        if (booking is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        // A precondition that depends on something outside the booking is checked before the transition,
        // so a refusal leaves the booking untouched and unsaved.
        if (precondition is not null)
        {
            var refusal = await precondition(booking, cancellationToken).ConfigureAwait(false);
            if (refusal is not null)
            {
                return Result<BookingDetailsDto>.Failure(refusal);
            }
        }

        try
        {
            transition(booking);
        }
        catch (DomainException exception)
        {
            // The code of the domain has the form "booking.<transition>.invalid_state" and is thus
            // identical to the constants in ErrorCodes.Booking (A-09).
            return Result<BookingDetailsDto>.Failure(exception.Code);
        }

        _bookingRepository.Update(booking);

        return await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the booking period and translates the date errors of the domain to an error code.
    /// </summary>
    /// <param name="start">Arrival date.</param>
    /// <param name="end">Departure date, exclusive.</param>
    /// <param name="period">The period, if it is valid.</param>
    /// <param name="errorCode">The error code, if it is not.</param>
    /// <returns>True if the period could be built.</returns>
    /// <remarks>
    /// The rules BR-101, BR-102 and BR-103 are enforced by the constructor of <see cref="DateRange"/>.
    /// The service does not repeat them — it catches and translates.
    /// </remarks>
    private static bool TryCreatePeriod(DateOnly start, DateOnly end, out DateRange period, out string errorCode)
    {
        try
        {
            period = new DateRange(start, end);
            errorCode = string.Empty;

            return true;
        }
        catch (DomainException exception)
        {
            period = default;
            errorCode = exception.Code;

            return false;
        }
    }

    /// <summary>
    /// MIL-013: a guest is checked in only to a room that can be let (BR-110). When the booking cannot be
    /// checked in anyway, the transition reports that first, so the user sees the real reason.
    /// </summary>
    /// <param name="booking">The booking about to be checked in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error code, or <c>null</c> if the check-in may go on.</returns>
    private Task<string?> RoomMustBeBookableForCheckInAsync(Booking booking, CancellationToken cancellationToken) =>
        booking.CanCheckIn(_clock.Today)
            ? CheckRoomIsBookableAsync(booking.RoomId, cancellationToken)
            : Task.FromResult<string?>(null);

    /// <summary>
    /// Decides whether the room exists and may be let at all (BR-45, BR-105, BR-110).
    /// </summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error code, or <c>null</c> if the room is in order.</returns>
    private async Task<string?> CheckRoomIsBookableAsync(int roomId, CancellationToken cancellationToken)
    {
        if (roomId <= 0)
        {
            return ErrorCodes.Booking.RoomRequired;
        }

        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return ErrorCodes.Room.NotFound;
        }

        // BR-110 comes from the domain. The service does not derive it from RoomStatus itself, and
        // the cleaning state is deliberately not included (B-04).
        return room.IsBookable ? null : ErrorCodes.Booking.RoomNotBookable;
    }

    /// <summary>
    /// Asks the domain whether any existing booking blocks the period (BR-19, A-01, A-04).
    /// </summary>
    /// <param name="roomId">The room.</param>
    /// <param name="period">The desired period.</param>
    /// <param name="excludeBookingId">The booking being edited, or <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="ErrorCodes.Booking.RoomNotAvailable"/>, or <c>null</c> if the room is free.</returns>
    /// <remarks>
    /// The repository filters coarsely; <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/> decides. The predicate
    /// therefore exists only in the domain — and in the database exclusion constraint, which is generated
    /// from the same definition and pinned by a parity test (A-04).
    /// </remarks>
    private async Task<string?> FindOverlapAsync(
        int roomId,
        DateRange period,
        int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        var candidates = await _bookingRepository
            .GetOverlapCandidatesAsync(
                roomId,
                period.Start,
                period.End,
                BlockingStatuses,
                excludeBookingId,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var candidate in candidates)
        {
            if (candidate.ConflictsWith(roomId, period))
            {
                return ErrorCodes.Booking.RoomNotAvailable;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the id of the guest — either the chosen guest or one just created (BR-47).
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The id of the guest, or the broken guest rules.</returns>
    /// <remarks>
    /// A new guest is saved in its own call, because <see cref="Booking.Create"/> requires an id
    /// greater than 0 and the id exists only after an insert. If the booking fails afterwards (for example on
    /// the race safeguard of the database), the guest is deliberately left behind: she is a valid guest, and
    /// the staff's input should not be lost on a retry.
    /// </remarks>
    private async Task<Result<int>> ResolveGuestIdAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.GuestId is int existingGuestId and > 0)
        {
            var exists = await _guestRepository.ExistsAsync(existingGuestId, cancellationToken).ConfigureAwait(false);

            return exists
                ? Result<int>.Success(existingGuestId)
                : Result<int>.Failure(ErrorCodes.Guest.NotFound);
        }

        // ! is justified: CreateAsync already rejects a request where not exactly one of GuestId
        // and NewGuest is set (BR-47), and GuestId was not used above, so NewGuest is set here.
        var fields = request.NewGuest!;
        var errors = GuestService.ValidateInternal(fields);

        if (errors.Count > 0)
        {
            return Result<int>.FromCodes(errors);
        }

        Guest guest;

        try
        {
            guest = Guest.Create(
                fields.FirstName,
                fields.LastName,
                fields.Email,
                fields.PhoneNumber,
                fields.Country,
                fields.PassportNumber);
        }
        catch (DomainException exception)
        {
            return Result<int>.Failure(exception.Code);
        }

        _guestRepository.Add(guest);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<int>.Failure(ErrorCodes.Guest.ConcurrencyConflict);
        }

        return Result<int>.Success(guest.Id);
    }

    /// <summary>
    /// Sets the room to departure cleaning after a check-out (BR-N-06).
    /// </summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the room is marked, or deliberately skipped.</returns>
    /// <remarks>
    /// If the room is already under cleaning or service, the domain rejects the transition — and that
    /// is correct: a task in progress must not be overwritten. The check-out must not fail
    /// for that reason, so the rejection is swallowed here and only here.
    /// </remarks>
    private async Task MarkRoomForDepartureCleaningAsync(int roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return;
        }

        try
        {
            room.MarkDepartureCleaningDue();
        }
        catch (InvalidStateTransitionException)
        {
            return;
        }

        _roomRepository.Update(room);
    }

    /// <summary>
    /// Saves and builds the answer. Collects the translation of infrastructure errors in one place.
    /// </summary>
    /// <param name="booking">The booking that has just been changed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the booking, or the error code of the safeguard that kicked in.</returns>
    /// <remarks>
    /// The details are read back as a projection instead of being mapped from the entity, so room
    /// and guest come along without the call having to ask for navigation data (A-11).
    /// </remarks>
    private async Task<Result<BookingDetailsDto>> SaveAndDescribeAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (BookingOverlapConflictException)
        {
            // The race safeguard behind the service check (B-03). The same error code, so the user sees the
            // same thing whichever of the two safeguards stopped her.
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.RoomNotAvailable);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.ConcurrencyConflict);
        }

        var readModel = await _bookingQueries
            .GetReadModelAsync(booking.Id, cancellationToken)
            .ConfigureAwait(false);

        return readModel is null
            ? Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound)
            : Result<BookingDetailsDto>.Success(readModel.ToDetails(_clock.Today));
    }

    /// <summary>
    /// Sorts the overview (BR-32).
    /// </summary>
    /// <param name="items">The rows.</param>
    /// <param name="sortBy">The field to sort on.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The sorted rows.</returns>
    /// <remarks>
    /// The sorting happens in memory, but only <i>after</i> the date window has limited the
    /// set in SQL. That is the difference between this and BR-111, where the whole table was fetched.
    /// </remarks>
    private static IReadOnlyList<BookingListItemDto> Sort(
        IEnumerable<BookingListItemDto> items,
        BookingSortField sortBy,
        SortDirection direction)
    {
        var ascending = direction == SortDirection.Ascending;

        IOrderedEnumerable<BookingListItemDto> sorted = sortBy switch
        {
            BookingSortField.BookingNumber => OrderBy(items, static item => item.BookingId, ascending),
            BookingSortField.GuestName => OrderBy(items, static item => item.GuestFullName, ascending),
            BookingSortField.RoomNumber => OrderBy(items, static item => item.RoomNumber, ascending),
            BookingSortField.StartDate => OrderBy(items, static item => item.StartDate, ascending),
            BookingSortField.EndDate => OrderBy(items, static item => item.EndDate, ascending),
            BookingSortField.Status => OrderBy(items, static item => item.Status, ascending),
            BookingSortField.Nights => OrderBy(items, static item => item.Nights, ascending),
            _ => OrderBy(items, static item => item.StartDate, ascending)
        };

        return sorted.ThenBy(static item => item.BookingId).ToArray();
    }

    /// <summary>
    /// Sorts ascending or descending on one key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="items">The rows.</param>
    /// <param name="keySelector">The key.</param>
    /// <param name="ascending">True for ascending.</param>
    /// <returns>The sorted rows.</returns>
    private static IOrderedEnumerable<BookingListItemDto> OrderBy<TKey>(
        IEnumerable<BookingListItemDto> items,
        Func<BookingListItemDto, TKey> keySelector,
        bool ascending) =>
        ascending ? items.OrderBy(keySelector) : items.OrderByDescending(keySelector);
}
