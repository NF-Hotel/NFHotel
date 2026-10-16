using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Implementering af bookingernes use cases.
/// </summary>
/// <remarks>
/// Servicen indeholder <b>ingen</b> overlapslogik. Den henter kandidater og spørger
/// <see cref="NFHotel.Domain.Entities.Booking"/> (A-01, A-04). Fandtes prædikatet også her, ville det være
/// tredje kopi efter domænet og databasens exclusion constraint — og de tre ville drive fra
/// hinanden.
/// <para>
/// "I dag" og "nu" kommer udelukkende fra <see cref="IClock"/> (A-05).
/// </para>
/// </remarks>
public sealed class BookingService : IBookingService
{
    /// <summary>
    /// De statusser der lægger beslag på et rum. Udledt af domænet
    /// (<see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>), ikke skrevet af som en liste — ellers ville
    /// et nyt statustrin skulle huskes to steder.
    /// </summary>
    private static readonly IReadOnlyCollection<BookingStatus> BlockingStatuses =
        Enum.GetValues<BookingStatus>().Where(BookingStatusExtensions.BlocksRoom).ToArray();

    /// <summary>Alle statusser. Bruges når oversigten også skal vise annullerede (BR-13).</summary>
    private static readonly IReadOnlyCollection<BookingStatus> AllStatuses = Enum.GetValues<BookingStatus>();

    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IBookingQueries _bookingQueries;
    private readonly IRoomQueries _roomQueries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// Opretter servicen.
    /// </summary>
    /// <param name="bookingRepository">Dataadgang for bookinger.</param>
    /// <param name="roomRepository">Dataadgang for rum — bruges til BR-110 og til slutrengøring.</param>
    /// <param name="guestRepository">Dataadgang for gæster — bruges til BR-47.</param>
    /// <param name="bookingQueries">Læsning af bookinger som projektioner (A-11).</param>
    /// <param name="roomQueries">Læsning af rum som projektioner — bruges til tilgængelighedsopslaget (BR-110).</param>
    /// <param name="unitOfWork">Transaktionsgrænsen.</param>
    /// <param name="clock">Hotellets ur (A-05).</param>
    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IBookingQueries bookingQueries,
        IRoomQueries roomQueries,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(bookingRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(guestRepository);
        ArgumentNullException.ThrowIfNull(bookingQueries);
        ArgumentNullException.ThrowIfNull(roomQueries);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _guestRepository = guestRepository;
        _bookingQueries = bookingQueries;
        _roomQueries = roomQueries;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    // --------------------------------------------------------
    // LÆSNING
    // --------------------------------------------------------

    /// <summary>Henter én booking (BR-115).</summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingens detaljer, eller <see cref="ErrorCodes.Booking.NotFound"/>.</returns>
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

    /// <summary>Henter bookingoversigten (BR-23, BR-24, BR-25, BR-32, BR-111).</summary>
    /// <param name="query">Datovindue, fritekst, sortering og om annullerede skal med.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Oversigtens rækker, sorteret som forespurgt.</returns>
    public async Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(
        BookingOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // BR-13: annullerede bookinger slettes ikke, så de skal kunne skjules i oversigten.
        var statuses = query.IncludeCancelled ? AllStatuses : BlockingStatuses;

        var readModels = await _bookingQueries
            .GetOverviewAsync(query.PeriodStart, query.PeriodEnd, statuses, query.SearchText, cancellationToken)
            .ConfigureAwait(false);

        var today = _clock.Today;
        var items = readModels.Select(readModel => readModel.ToListItem(today));

        return Result<IReadOnlyList<BookingListItemDto>>.Success(Sort(items, query.SortBy, query.Direction));
    }

    /// <summary>
    /// Finder ledige rum i en periode (BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-110).
    /// </summary>
    /// <param name="checkInDate">Ankomstdato, inklusiv.</param>
    /// <param name="checkOutDate">Afrejsedato, eksklusiv.</param>
    /// <param name="excludeBookingId">Bookingen der redigeres, eller <c>null</c>.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De rum der både er bookbare og ledige i hele perioden.</returns>
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

        // BR-110: kun rum der overhovedet må udlejes. Servicen vælger statussen — repositoryet
        // afgør ikke længere selv hvad "tilgængelig" betyder.
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

            // Overlapsafgørelsen træffes af domænet, aldrig her (A-01, A-04). BR-39's
            // halvåbne interval betyder at afrejse- og ankomstdag samme dag ikke er overlap.
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
    /// Opretter en booking (BR-01, BR-19, BR-41 til BR-49, BR-51, BR-101 til BR-106, B-03).
    /// </summary>
    /// <param name="request">Periode, rum og enten en eksisterende eller en ny gæst (BR-47).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den oprettede booking, eller de brudte regler.</returns>
    public async Task<Result<BookingDetailsDto>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // BR-47: præcis én af de to gæsteveje. Både-og og hverken-eller er en regelfejl,
        // ikke en NullReferenceException nede i en formular.
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

    /// <summary>Bekræfter en booking (BR-02, BR-03, BR-05, BR-121).</summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.ConfirmNotAllowed"/>.</returns>
    public Task<Result<BookingDetailsDto>> ConfirmAsync(
        int bookingId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(bookingId, static booking => booking.Confirm(), cancellationToken);

    /// <summary>Tjekker gæsten ind (BR-06, BR-07, BR-08, B-01).</summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CheckInNotAllowed"/>.</returns>
    public Task<Result<BookingDetailsDto>> CheckInAsync(
        int bookingId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(bookingId, booking => booking.CheckIn(_clock.UtcNow, _clock.Today), cancellationToken);

    /// <summary>Tjekker gæsten ud (BR-09, BR-10, BR-107, A-01, A-03, BR-N-06).</summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CheckOutNotAllowed"/>.</returns>
    /// <remarks>
    /// Den hotel-lokale udtjekningsdato leveres af uret, fordi omregningen tidsstempel →
    /// kalenderdato er en tidszonepolitik som hverken domænet eller databasen kan udføre
    /// deterministisk (A-02, A-03).
    /// <para>
    /// Domænets tosidede guard afviser en udtjekningsdato før ankomsten
    /// (<see cref="ErrorCodes.Booking.CheckOutDateBeforeStartDate"/>) og efter i dag
    /// (<see cref="ErrorCodes.Booking.CheckOutDateInFuture"/>). Sidstnævnte kan denne
    /// use case ikke selv udløse — den tjekker ud i nuet — men guarden holder, hvis der
    /// senere kommer en "registrér udtjekning bagud i tid"-use case.
    /// </para>
    /// </remarks>
    public async Task<Result<BookingDetailsDto>> CheckOutAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);

        if (booking is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        // Udtjekningen sker nu, så den hotel-lokale udtjekningsdato ER dagens dato. De to
        // parametre er alligevel adskilte i domænet, fordi en senere bagudrettet
        // registrering skal kunne angive en tidligere dato uden at flytte "i dag".
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

    /// <summary>Annullerer en booking (BR-11, BR-13, BR-122).</summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingen, eller <see cref="ErrorCodes.Booking.CancelNotAllowed"/>.</returns>
    public Task<Result<BookingDetailsDto>> CancelAsync(
        int bookingId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(bookingId, static booking => booking.Cancel(), cancellationToken);

    /// <summary>
    /// Flytter en booking (BR-14, BR-17, BR-18, BR-19, BR-20, BR-108, BR-123, B-03).
    /// </summary>
    /// <param name="request">Bookingens id, den nye periode og rummet.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den flyttede booking, eller de brudte regler.</returns>
    public async Task<Result<BookingDetailsDto>> RescheduleAsync(
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BookingId);

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

        // BR-19 med samme funktion som CreateAsync, og med bookingen selv udeladt så den ikke
        // spærrer for sin egen flytning.
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
    // FÆLLES
    // --------------------------------------------------------

    /// <summary>
    /// Kører én domæneovergang på en booking og gemmer.
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="transition">Overgangen — altid præcis ét metodekald på <see cref="NFHotel.Domain.Entities.Booking"/>.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingens detaljer, eller domænets fejlkode hvis overgangen blev afvist.</returns>
    private async Task<Result<BookingDetailsDto>> TransitionAsync(
        int bookingId,
        Action<Booking> transition,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);

        if (booking is null)
        {
            return Result<BookingDetailsDto>.Failure(ErrorCodes.Booking.NotFound);
        }

        try
        {
            transition(booking);
        }
        catch (DomainException exception)
        {
            // Domænets kode har formen "booking.<overgang>.invalid_state" og er dermed
            // identisk med konstanterne i ErrorCodes.Booking (A-09).
            return Result<BookingDetailsDto>.Failure(exception.Code);
        }

        _bookingRepository.Update(booking);

        return await SaveAndDescribeAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Bygger bookingperioden og oversætter domænets datofejl til en fejlkode.
    /// </summary>
    /// <param name="start">Ankomstdato.</param>
    /// <param name="end">Afrejsedato, eksklusiv.</param>
    /// <param name="period">Perioden, hvis den er gyldig.</param>
    /// <param name="errorCode">Fejlkoden, hvis den ikke er.</param>
    /// <returns>Sand hvis perioden kunne bygges.</returns>
    /// <remarks>
    /// Reglerne BR-101, BR-102 og BR-103 håndhæves af <see cref="DateRange"/>s konstruktør.
    /// Servicen gentager dem ikke — den fanger og oversætter.
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
    /// Afgør om rummet findes og overhovedet må udlejes (BR-45, BR-105, BR-110).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>En fejlkode, eller <c>null</c> hvis rummet er i orden.</returns>
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

        // BR-110 kommer fra domænet. Servicen udleder den ikke af RoomStatus selv, og
        // rengøringsstand indgår bevidst ikke (B-04).
        return room.IsBookable ? null : ErrorCodes.Booking.RoomNotBookable;
    }

    /// <summary>
    /// Spørger domænet om nogen eksisterende booking spærrer for perioden (BR-19, A-01, A-04).
    /// </summary>
    /// <param name="roomId">Rummet.</param>
    /// <param name="period">Den ønskede periode.</param>
    /// <param name="excludeBookingId">Bookingen der redigeres, eller <c>null</c>.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns><see cref="ErrorCodes.Booking.RoomNotAvailable"/>, eller <c>null</c> hvis rummet er frit.</returns>
    /// <remarks>
    /// Repositoryet grovfiltrerer; <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/> afgør. Prædikatet
    /// findes derfor kun i domænet — og i databasens exclusion constraint, som er genereret
    /// ud fra samme definition og pinnet af en paritetstest (A-04).
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
    /// Finder gæstens id — enten den valgte gæst eller en netop oprettet (BR-47).
    /// </summary>
    /// <param name="request">Oprettelsesanmodningen.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Gæstens id, eller de brudte gæsteregler.</returns>
    /// <remarks>
    /// En ny gæst gemmes i sit eget kald, fordi <see cref="Booking.Create"/> kræver et id
    /// større end 0 og id'et først findes efter en insert. Fejler bookingen bagefter (fx på
    /// databasens kapløbsværn), bliver gæsten stående med vilje: hun er en gyldig gæst, og
    /// personalets indtastning skal ikke gå tabt ved et retry.
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

        // ! er begrundet: CreateAsync afviser allerede en anmodning hvor ikke præcis én af GuestId
        // og NewGuest er sat (BR-47), og GuestId blev ikke brugt ovenfor, så NewGuest er sat her.
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
    /// Sætter rummet til slutrengøring efter en udtjekning (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>En opgave der er færdig når rummet er markeret, eller bevidst sprunget over.</returns>
    /// <remarks>
    /// Er rummet allerede under rengøring eller service, afviser domænet overgangen — og det
    /// er korrekt: en igangværende opgave må ikke overskrives. Udtjekningen skal ikke fejle
    /// af den grund, så afvisningen sluges her og kun her.
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
    /// Gemmer og bygger svaret. Samler oversættelsen af infrastrukturfejl ét sted.
    /// </summary>
    /// <param name="booking">Bookingen der netop er ændret.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Bookingens detaljer, eller fejlkoden for det værn der slog til.</returns>
    /// <remarks>
    /// Detaljerne læses tilbage som projektion i stedet for at mappes fra entiteten, så rum
    /// og gæst kommer med uden at kaldet skal have bedt om navigationsdata (A-11).
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
            // Kapløbsværnet bag servicetjekket (B-03). Samme fejlkode, så brugeren ser det
            // samme uanset hvilket af de to værn der stoppede hende.
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
    /// Sorterer oversigten (BR-32).
    /// </summary>
    /// <param name="items">Rækkerne.</param>
    /// <param name="sortBy">Feltet der sorteres på.</param>
    /// <param name="direction">Retningen.</param>
    /// <returns>De sorterede rækker.</returns>
    /// <remarks>
    /// Sorteringen sker i hukommelsen, men først <i>efter</i> at datovinduet har begrænset
    /// mængden i SQL. Det er forskellen på dette og BR-111, hvor hele tabellen blev hentet.
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
    /// Sorterer stigende eller faldende på én nøgle.
    /// </summary>
    /// <typeparam name="TKey">Nøglens type.</typeparam>
    /// <param name="items">Rækkerne.</param>
    /// <param name="keySelector">Nøglen.</param>
    /// <param name="ascending">Sand for stigende.</param>
    /// <returns>De sorterede rækker.</returns>
    private static IOrderedEnumerable<BookingListItemDto> OrderBy<TKey>(
        IEnumerable<BookingListItemDto> items,
        Func<BookingListItemDto, TKey> keySelector,
        bool ascending) =>
        ascending ? items.OrderBy(keySelector) : items.OrderByDescending(keySelector);
}