using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Ports;

/// <summary>
/// Reading rooms as projections (A-11). Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// Entities are fetched only on command paths, via <c>IRoomRepository</c> in Domain. Filters are
/// parameters, not hard-coded choices — the caller decides what "available" means (BR-110).
/// </remarks>
public interface IRoomQueries
{
    /// <summary>
    /// Fetches the details of a room as a projection.
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details, or <c>null</c> if the room does not exist (BR-115).</returns>
    Task<RoomDetailsDto?> GetDetailsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all rooms as list rows.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>All rooms, sorted by room number.</returns>
    Task<IReadOnlyList<RoomListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the rooms that match the filter (BR-74, BR-76).
    /// </summary>
    /// <param name="filter">The filter. An empty filter means all rooms.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The matching rooms. The filtering happens in the database, not in memory (BR-111).</returns>
    Task<IReadOnlyList<RoomListItemDto>> GetByFilterAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the rooms with the given bookability as availability rows.
    /// </summary>
    /// <param name="status">The status the caller wants. The caller — not the repository — chooses (BR-110).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The rooms, sorted by room number.</returns>
    Task<IReadOnlyList<AvailableRoomDto>> GetByStatusAsync(
        RoomStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the floors that actually have rooms (BR-75).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Floor numbers without duplicates, ascending.</returns>
    Task<IReadOnlyList<int>> GetDistinctFloorsAsync(CancellationToken cancellationToken = default);
}
