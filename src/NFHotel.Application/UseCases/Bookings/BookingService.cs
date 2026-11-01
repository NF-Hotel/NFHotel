using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Guests;
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
    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IBookingQueries bookingQueries,
        IRoomQueries roomQueries,
        IUnitOfWork unitOfWork,
        IClock clock,
        IStaffAuthorizationService authorization)
    {
        ArgumentNullException.ThrowIfNull(bookingRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(guestRepository);
        ArgumentNullException.ThrowIfNull(bookingQueries);
        ArgumentNullException.ThrowIfNull(roomQueries);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(authorization);

        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _guestRepository = guestRepository;
        _bookingQueries = bookingQueries;
        _roomQueries = roomQueries;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _authorization = authorization;
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

        var readModel = await _bookingQueries
            .GetReadModelAsync(bookingId, cancellationToken)
            .ConfigureAwait(false);

        return readModel is null
            ? Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound)
            : Result<BookingDetailsDto>.Success(readModel.ToDetails(_clock.Today));
    }

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
    /// Creates a booking through the public guest booking flow.
    /// Only new guest details are accepted.
    /// </summary>
    public async Task<Result<BookingDetailsDto>> CreatePublicAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Public bookings must use new guest details.
        if (request.GuestId is not null || request.NewGuest is null)
        {
            return Result<BookingDetailsDto>.Failure(
                ErrorCodes.Booking.GuestSelectionInvalid);
        }

        // Uses the shared booking logic without requiring staff permissions.
        return await CreateBookingInternalAsync(request, cancellationToken)
            .ConfigureAwait(false);
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

        _bookingRepository.Add(booking);

        return await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);
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
