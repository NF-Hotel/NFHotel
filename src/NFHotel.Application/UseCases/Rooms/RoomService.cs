using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Implementation of the use cases of rooms.
/// </summary>
/// <remarks>
/// The service makes no state decisions itself: every transition is one call to a
/// method on <see cref="Room"/>, and the domain's <see cref="InvalidStateTransitionException"/>
/// is translated to an error code. If the service decided itself which transitions are legal,
/// the state machine would exist in two places (A-07).
/// </remarks>
public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomQueries _roomQueries;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="roomRepository">Data access for rooms.</param>
    /// <param name="roomQueries">Reading rooms as projections (A-11).</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
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
    // READING
    // --------------------------------------------------------

    /// <summary>Fetches one room (BR-115).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or <see cref="ErrorCodes.Room.NotFound"/>.</returns>
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

    /// <summary>Fetches all rooms.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All rooms as list rows.</returns>
    public async Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await _roomQueries.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomListItemDto>>.Success(rooms);
    }

    /// <summary>Searches rooms with a filter (BR-74, BR-76).</summary>
    /// <param name="filter">The filter. An empty filter means all rooms.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching rooms.</returns>
    public async Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var rooms = await _roomQueries.GetByFilterAsync(filter, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoomListItemDto>>.Success(rooms);
    }

    /// <summary>Fetches the options of the filter (BR-75, BR-126).</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Floors from data; the three other lists from enums.</returns>
    /// <remarks>
    /// Only floors can vary with data. That size, status and cleaning state come
    /// directly from enums is what closes BR-126: the dropdown and the enum can no longer disagree.
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
    /// Creates a room (BR-81, BR-84, BR-97, BR-98, BR-99, BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">The master data of the room.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created room, or all validation errors at once.</returns>
    /// <remarks>
    /// BR-84 (the form ignored the validation) cannot be repeated: the room cannot be
    /// constructed invalid, and the service does not save on error.
    /// </remarks>
    public async Task<Result<RoomDetailsDto>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = FieldErrors(request.RoomNumber, request.Floor, request.Size, request.Capacity);

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
    /// Changes the master data of a room (BR-82, BR-84, BR-97 to BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">The new master data. Does not carry status (A-08).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated room, or all validation errors at once.</returns>
    public async Task<Result<RoomDetailsDto>> UpdateAsync(
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.RoomId);

        var errors = FieldErrors(request.RoomNumber, request.Floor, request.Size, request.Capacity);

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
    /// Deletes a room (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or <see cref="ErrorCodes.Room.HasBookings"/>.</returns>
    /// <remarks>
    /// The guard is explicit and not a <c>catch</c> on a foreign key error. The foreign key
    /// is kept as the last safeguard, but the rule is readable here — the warning dialog of BR-77 is UI.
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

    /// <summary>Takes the room out of service (BR-110, BR-126). Idempotent (A-08).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room after the transition.</returns>
    public Task<Result<RoomDetailsDto>> TakeOutOfServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.TakeOutOfService(), cancellationToken);

    /// <summary>Blocks the room temporarily (BR-110, BR-126). Idempotent (A-08).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room after the transition.</returns>
    public Task<Result<RoomDetailsDto>> SendToMaintenanceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.SendToMaintenance(), cancellationToken);

    /// <summary>Makes the room bookable again (BR-110, BR-126). Does not touch the cleaning state (B-04).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room after the transition.</returns>
    public Task<Result<RoomDetailsDto>> ReturnToServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.ReturnToService(), cancellationToken);

    // --------------------------------------------------------
    // CLEANING AND SERVICE (HousekeepingStatus) — BR-N-06, A-07
    // --------------------------------------------------------

    /// <summary>Marks daily cleaning as required (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> MarkDailyCleaningDueAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.MarkDailyCleaningDue(), cancellationToken);

    /// <summary>Marks departure cleaning as required after departure (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> MarkDepartureCleaningDueAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.MarkDepartureCleaningDue(), cancellationToken);

    /// <summary>The cleaning staff acknowledge and start work (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> StartCleaningAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.StartCleaning(), cancellationToken);

    /// <summary>The cleaning is finished (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> CompleteCleaningAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.CompleteCleaning(), cancellationToken);

    /// <summary>Reports the need for a service technician (BR-N-06). Does not block the room (B-04).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> ReportServiceNeededAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.ReportServiceNeeded(), cancellationToken);

    /// <summary>The service technician acknowledges and starts work (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> StartServiceAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, static room => room.StartService(), cancellationToken);

    /// <summary>The service is finished (BR-N-06).</summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="requiresCleaning">True if the work left the room dirty.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or an error code if the transition is illegal.</returns>
    public Task<Result<RoomDetailsDto>> CompleteServiceAsync(
        int roomId,
        bool requiresCleaning,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(roomId, room => room.CompleteService(requiresCleaning), cancellationToken);

    // --------------------------------------------------------
    // COMMON
    // --------------------------------------------------------

    /// <summary>
    /// Runs one domain transition on a room and saves.
    /// </summary>
    /// <param name="roomId">The room id.</param>
    /// <param name="transition">
    /// The transition — always exactly one method call on <see cref="Room"/>. The service never decides
    /// itself whether the transition is legal; the entity does (A-07).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or the error code of the domain if the transition was rejected.</returns>
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
            // The code of the domain already has the form "room.<transition>.invalid_state" and is
            // thus identical to the constants in ErrorCodes.Room (A-09).
            return Result<RoomDetailsDto>.Failure(exception.Code);
        }

        _roomRepository.Update(room);

        return await SaveAndDescribeAsync(room, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves and builds the answer. Collects the translation of infrastructure errors in one place.
    /// </summary>
    /// <param name="room">The room that has just been changed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details of the room, or the error code of the constraint that kicked in.</returns>
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
            // The race safeguard behind RoomNumberExistsAsync (BR-N-01). The same error code as
            // the service check, so the user sees the same thing whichever safeguard stopped her.
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

    /// <summary>
    /// Collects every field error of a room by building it. The entity holds the rules as
    /// attributes (ADR-0002); no room is kept.
    /// </summary>
    /// <param name="roomNumber">The room number.</param>
    /// <param name="floor">The floor.</param>
    /// <param name="size">The room size.</param>
    /// <param name="capacity">The capacity.</param>
    /// <returns>The error codes in field order. An empty list means valid.</returns>
    private static IReadOnlyList<string> FieldErrors(string roomNumber, int floor, RoomSize size, int capacity)
    {
        try
        {
            _ = Room.Create(roomNumber, floor, size, capacity);

            return [];
        }
        catch (DomainException exception)
        {
            return exception.Codes;
        }
    }
}
