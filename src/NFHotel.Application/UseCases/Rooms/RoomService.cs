using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.Rules;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Implementering af rummenes use cases.
/// </summary>
/// <remarks>
/// Servicen træffer ingen tilstandsbeslutninger selv: hver overgang er ét kald til en
/// metode på <see cref="Room"/>, og domænets <see cref="InvalidStateTransitionException"/>
/// oversættes til en fejlkode. Ville servicen selv afgøre hvilke overgange der er lovlige,
/// ville tilstandsmaskinen findes to steder (A-07).
/// </remarks>
public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomQueries _roomQueries;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Opretter servicen.
    /// </summary>
    /// <param name="roomRepository">Dataadgang for rum.</param>
    /// <param name="roomQueries">Læsning af rum som projektioner (A-11).</param>
    /// <param name="unitOfWork">Transaktionsgrænsen.</param>
    public RoomService(
        IRoomRepository roomRepository,
        IRoomQueries roomQueries,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(roomQueries);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _roomRepository = roomRepository;
        _roomQueries = roomQueries;
        _unitOfWork = unitOfWork;
    }

    // --------------------------------------------------------
    // LÆSNING
    // --------------------------------------------------------

    /// <summary>Henter ét rum (BR-115).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller <see cref="ErrorCodes.Room.NotFound"/>.</returns>
    public async Task<Result<RoomDetailsDto>> GetByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);

        var details = await _roomQueries.GetDetailsAsync(roomId, cancellationToken).ConfigureAwait(false);

        return details is null
            ? Result<RoomDetailsDto>.Failure(ErrorCodes.Room.NotFound)
            : Result<RoomDetailsDto>.Success(details);
    }

    /// <summary>Henter alle rum.</summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Alle rum som listerækker.</returns>
    public async Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await _roomQueries.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomListItemDto>>.Success(rooms);
    }

    /// <summary>Søger rum med filter (BR-74, BR-76).</summary>
    /// <param name="filter">Filteret. Et tomt filter betyder alle rum.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende rum.</returns>
    public async Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var rooms = await _roomQueries.GetByFilterAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomListItemDto>>.Success(rooms);
    }

    /// <summary>Henter filterets valgmuligheder (BR-75, BR-126).</summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Etager fra data; de tre andre lister fra enums.</returns>
    /// <remarks>
    /// Kun etager kan variere med data. At størrelse, status og rengøringsstand kommer
    /// direkte fra enums er det der lukker BR-126: dropdown og enum kan ikke længere være uenige.
    /// </remarks>
    public async Task<Result<RoomFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var floors = await _roomQueries.GetDistinctFloorsAsync(cancellationToken).ConfigureAwait(false);

        var options = new RoomFilterOptionsDto(
            floors,
            Enum.GetValues<RoomSize>(),
            Enum.GetValues<RoomStatus>(),
            Enum.GetValues<HousekeepingStatus>());

        return Result<RoomFilterOptionsDto>.Success(options);
    }

    // --------------------------------------------------------
    // STAMDATA
    // --------------------------------------------------------

    /// <summary>
    /// Opretter et rum (BR-81, BR-84, BR-97, BR-98, BR-99, BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">Rummets stamdata.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Det oprettede rum, eller alle valideringsfejl på én gang.</returns>
    /// <remarks>
    /// BR-84 (formularen ignorerede valideringen) kan ikke gentages: rummet kan ikke
    /// konstrueres ugyldigt, og servicen gemmer ikke ved fejl.
    /// </remarks>
    public async Task<Result<RoomDetailsDto>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = RoomRules.Validate(request.RoomNumber, request.Floor, request.Size, request.Capacity);

        if (errors.Count > 0)
        {
            return Result<RoomDetailsDto>.FromCodes(errors);
        }

        var numberTaken = await _roomRepository
            .RoomNumberExistsAsync(request.RoomNumber, excludeRoomId: null, cancellationToken)
            .ConfigureAwait(false);

        if (numberTaken)
        {
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.RoomNumberAlreadyExists);
        }

        Room room;

        try
        {
            room = Room.Create(request.RoomNumber, request.Floor, request.Size, request.Capacity);
        }
        catch (DomainException exception)
        {
            return Result<RoomDetailsDto>.Failure(exception.Code);
        }

        _roomRepository.Add(room);

        return await SaveAndDescribeAsync(room, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retter et rums stamdata (BR-82, BR-84, BR-97 til BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">De nye stamdata. Bærer ikke status (A-08).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Det opdaterede rum, eller alle valideringsfejl på én gang.</returns>
    public async Task<Result<RoomDetailsDto>> UpdateAsync(
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.RoomId);

        var errors = RoomRules.Validate(request.RoomNumber, request.Floor, request.Size, request.Capacity);

        if (errors.Count > 0)
        {
            return Result<RoomDetailsDto>.FromCodes(errors);
        }

        var room = await _roomRepository.GetByIdAsync(request.RoomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.NotFound);
        }

        var numberTaken = await _roomRepository
            .RoomNumberExistsAsync(request.RoomNumber, request.RoomId, cancellationToken)
            .ConfigureAwait(false);

        if (numberTaken)
        {
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.RoomNumberAlreadyExists);
        }

        try
        {
            room.UpdateDetails(request.RoomNumber, request.Floor, request.Size, request.Capacity);
        }
        catch (DomainException exception)
        {
            return Result<RoomDetailsDto>.Failure(exception.Code);
        }

        _roomRepository.Update(room);

        return await SaveAndDescribeAsync(room, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sletter et rum (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Succes, eller <see cref="ErrorCodes.Room.HasBookings"/>.</returns>
    /// <remarks>
    /// Guarden er eksplicit og ikke en <c>catch</c> på en fremmednøglefejl. Fremmednøglen
    /// bevares som sidste værn, men reglen er læsbar her — BR-77's advarselsdialog er UI.
    /// </remarks>
    public async Task<Result> DeleteAsync(int roomId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);

        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return Result.Failure(ErrorCodes.Room.NotFound);
        }

        var hasBookings = await _roomRepository.HasAnyBookingsAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (hasBookings)
        {
            return Result.Failure(ErrorCodes.Room.HasBookings);
        }

        _roomRepository.Remove(room);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ErrorCodes.Room.ConcurrencyConflict);
        }

        return Result.Success();
    }

    // --------------------------------------------------------
    // BOOKBARHED (RoomStatus) — B-04, A-07, A-08
    // --------------------------------------------------------

    /// <summary>Tager rummet ud af drift (BR-110, BR-126). Idempotent (A-08).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen.</returns>
    public Task<Result<RoomDetailsDto>> TakeOutOfServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.TakeOutOfService(), cancellationToken);

    /// <summary>Spærrer rummet midlertidigt (BR-110, BR-126). Idempotent (A-08).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen.</returns>
    public Task<Result<RoomDetailsDto>> SendToMaintenanceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.SendToMaintenance(), cancellationToken);

    /// <summary>Gør rummet bookbart igen (BR-110, BR-126). Rører ikke rengøringsstanden (B-04).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer efter overgangen.</returns>
    public Task<Result<RoomDetailsDto>> ReturnToServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.ReturnToService(), cancellationToken);

    // --------------------------------------------------------
    // RENGØRING OG SERVICE (HousekeepingStatus) — BR-N-06, A-07
    // --------------------------------------------------------

    /// <summary>Markerer daglig rengøring som påkrævet (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> MarkDailyCleaningDueAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.MarkDailyCleaningDue(), cancellationToken);

    /// <summary>Markerer slutrengøring som påkrævet efter afrejse (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> MarkDepartureCleaningDueAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.MarkDepartureCleaningDue(), cancellationToken);

    /// <summary>Rengøringspersonalet kvitterer og går i gang (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> StartCleaningAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.StartCleaning(), cancellationToken);

    /// <summary>Rengøringen er færdig (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> CompleteCleaningAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.CompleteCleaning(), cancellationToken);

    /// <summary>Rapporterer behov for en servicetekniker (BR-N-06). Spærrer ikke rummet (B-04).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> ReportServiceNeededAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.ReportServiceNeeded(), cancellationToken);

    /// <summary>Serviceteknikeren kvitterer og går i gang (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> StartServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.StartService(), cancellationToken);

    /// <summary>Servicen er afsluttet (BR-N-06).</summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="requiresCleaning">Sand hvis arbejdet efterlod rummet snavset.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller en fejlkode hvis overgangen er ulovlig.</returns>
    public Task<Result<RoomDetailsDto>> CompleteServiceAsync(
        int roomId,
        bool requiresCleaning,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, room => room.CompleteService(requiresCleaning), cancellationToken);

    // --------------------------------------------------------
    // FÆLLES
    // --------------------------------------------------------

    /// <summary>
    /// Kører én domæneovergang på et rum og gemmer.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="transition">
    /// Overgangen — altid præcis ét metodekald på <see cref="Room"/>. Servicen afgør aldrig
    /// selv om overgangen er lovlig; det gør entiteten (A-07).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller domænets fejlkode hvis overgangen blev afvist.</returns>
    private async Task<Result<RoomDetailsDto>> TransitionAsync(
        int roomId,
        Action<Room> transition,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roomId);

        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken).ConfigureAwait(false);

        if (room is null)
        {
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.NotFound);
        }

        try
        {
            transition(room);
        }
        catch (DomainException exception)
        {
            // Domænets kode har allerede formen "room.<overgang>.invalid_state" og er
            // dermed identisk med konstanterne i ErrorCodes.Room (A-09).
            return Result<RoomDetailsDto>.Failure(exception.Code);
        }

        _roomRepository.Update(room);

        return await SaveAndDescribeAsync(room, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gemmer og bygger svaret. Samler oversættelsen af infrastrukturfejl ét sted.
    /// </summary>
    /// <param name="room">Rummet der netop er ændret.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Rummets detaljer, eller fejlkoden for den constraint der slog til.</returns>
    private async Task<Result<RoomDetailsDto>> SaveAndDescribeAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Kapløbsværnet bag RoomNumberExistsAsync (BR-N-01). Samme fejlkode som
            // servicetjekket, så brugeren ser det samme uanset hvem der stoppede hende.
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.RoomNumberAlreadyExists);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<RoomDetailsDto>.Failure(ErrorCodes.Room.ConcurrencyConflict);
        }

        var hasBookings = await _roomRepository
            .HasAnyBookingsAsync(room.Id, cancellationToken)
            .ConfigureAwait(false);

        return Result<RoomDetailsDto>.Success(room.ToDetails(hasBookings));
    }
}