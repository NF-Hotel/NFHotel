using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Rooms;

/// <summary>
/// Use cases for rooms: master data, bookability and cleaning flow.
/// </summary>
/// <remarks>
/// There is <b>no generic target-status method</b> (A-07). Every domain transition has its own
/// method. A method of the form <c>ChangeHousekeepingStatusAsync(newStatus)</c> would force
/// the service to translate (current, desired) to a method call through a table — that is,
/// re-implement the state machine one layer higher than it belongs.
/// <para>
/// All transitions are idempotent for the same target state (A-08): marking a room clean twice
/// is a no-op, not an error. The mobile app has no offline support and retries
/// over an unstable network; a retry must not give an error to the cleaning staff.
/// </para>
/// <para>
/// <b>Roles are documentation in Phase 1 (A-12).</b> The column "who may" in the design describes
/// who <i>will be allowed</i> when authorisation is built. Here anyone can call any transition.
/// </para>
/// </remarks>
public interface IRoomService
{
    // --------------------------------------------------------
    // READING
    // --------------------------------------------------------

    /// <summary>
    /// Fetches one room (BR-115).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the room, or <see cref="ErrorCodes.Room.NotFound"/>.</returns>
    Task<Result<RoomDetailsDto>> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all rooms.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>All rooms as list rows.</returns>
    Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches rooms with a filter (BR-74, BR-76).
    /// </summary>
    /// <param name="filter">The filter. An empty filter corresponds to "clear filter" and gives all rooms.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The matching rooms.</returns>
    Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the options of the filter (BR-75, BR-126).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Floors from data; size, status and cleaning state from enums.</returns>
    Task<Result<RoomFilterOptionsDto>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // STAMDATA
    // --------------------------------------------------------

    /// <summary>
    /// Creates a room (BR-81, BR-84, BR-97, BR-98, BR-99, BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">Rummets stamdata.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The created room, or all validation errors at once.</returns>
    Task<Result<RoomDetailsDto>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the master data of a room (BR-82, BR-84, BR-97 to BR-100, BR-113, BR-N-01).
    /// </summary>
    /// <param name="request">The new master data. Does not carry status (A-08).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The updated room, or all validation errors at once.</returns>
    Task<Result<RoomDetailsDto>> UpdateAsync(
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a room (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Success, or <see cref="ErrorCodes.Room.HasBookings"/> if the room is in use.</returns>
    Task<Result> DeleteAsync(int roomId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // BOOKBARHED (RoomStatus) — B-04, A-07, A-08
    // --------------------------------------------------------

    /// <summary>
    /// Takes the room out of service indefinitely (BR-110, BR-126). The room cannot be booked.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the room after the transition. A no-op if the room is already out of service.</returns>
    Task<Result<RoomDetailsDto>> TakeOutOfServiceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks the room temporarily because of a fault that makes it uninhabitable (BR-110, BR-126).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the room after the transition. A no-op if the room is already blocked.</returns>
    Task<Result<RoomDetailsDto>> SendToMaintenanceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the room bookable again (BR-110, BR-126). Does not touch the cleaning state (B-04).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the room after the transition. A no-op if the room is already bookable.</returns>
    Task<Result<RoomDetailsDto>> ReturnToServiceAsync(int roomId, CancellationToken cancellationToken = default);

    // --------------------------------------------------------
    // CLEANING AND SERVICE (HousekeepingStatus) — BR-N-06, A-07
    // --------------------------------------------------------

    /// <summary>
    /// Marks that the occupied room is waiting for daily cleaning (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or
    /// <see cref="ErrorCodes.Room.MarkDailyCleaningDueNotAllowed"/> if the room is waiting for
    /// departure cleaning or is under cleaning or service — departure cleaning must not be downgraded.
    /// </returns>
    Task<Result<RoomDetailsDto>> MarkDailyCleaningDueAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks that the room is waiting for departure cleaning after departure (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or
    /// <see cref="ErrorCodes.Room.MarkDepartureCleaningDueNotAllowed"/> if the room is under
    /// cleaning or service.
    /// </returns>
    /// <remarks>
    /// Also called automatically by <c>BookingService.CheckOutAsync</c> immediately after a
    /// check-out; this use case exists for the times the staff do it manually.
    /// </remarks>
    Task<Result<RoomDetailsDto>> MarkDepartureCleaningDueAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The cleaning staff acknowledge and start work (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or <see cref="ErrorCodes.Room.StartCleaningNotAllowed"/> if
    /// the room is not waiting for cleaning.
    /// </returns>
    Task<Result<RoomDetailsDto>> StartCleaningAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The cleaning is finished; the room is clean (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or <see cref="ErrorCodes.Room.CompleteCleaningNotAllowed"/> if
    /// the cleaning is not in progress.
    /// </returns>
    Task<Result<RoomDetailsDto>> CompleteCleaningAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports that the room must be seen by a service technician (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or <see cref="ErrorCodes.Room.ReportServiceNeededNotAllowed"/>
    /// if service is already in progress.
    /// </returns>
    /// <remarks>
    /// Does not block the room for booking — that requires <see cref="SendToMaintenanceAsync"/> (B-04).
    /// </remarks>
    Task<Result<RoomDetailsDto>> ReportServiceNeededAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The service technician acknowledges and starts work (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or <see cref="ErrorCodes.Room.StartServiceNotAllowed"/> if
    /// the room is not waiting for service.
    /// </returns>
    Task<Result<RoomDetailsDto>> StartServiceAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The service is finished (BR-N-06).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="requiresCleaning">
    /// True if the work left the room dirty; the room is then set to departure cleaning
    /// i stedet for rent.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// The details of the room, or <see cref="ErrorCodes.Room.CompleteServiceNotAllowed"/> if
    /// the service is not in progress.
    /// </returns>
    Task<Result<RoomDetailsDto>> CompleteServiceAsync(
        int roomId,
        bool requiresCleaning,
        CancellationToken cancellationToken = default);
}
