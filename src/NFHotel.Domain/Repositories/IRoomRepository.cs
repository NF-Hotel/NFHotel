using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Data access for rooms — the entity part. Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// The repository does <b>not decide</b>: filters are parameters, not hard-coded choices (BR-110).
/// The write methods mark and do not persist. Projections are in <c>IRoomQueries</c>
/// (Application).
/// </remarks>
public interface IRoomRepository : IRepository<Room, int>
{
    /// <summary>
    /// Decides whether the room number is already in use (BR-N-01).
    /// </summary>
    /// <param name="roomNumber">The room number being checked.</param>
    /// <param name="excludeRoomId">
    /// The room being edited, so it does not collide with itself. <c>null</c> on creation.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>True if another room has the number.</returns>
    Task<bool> RoomNumberExistsAsync(
        string roomNumber,
        int? excludeRoomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decides whether there are bookings for the room (BR-78, BR-112).
    /// </summary>
    /// <param name="roomId">Rummets id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>True if at least one booking points to the room.</returns>
    /// <remarks>
    /// Replaces the old system's <c>catch (SqlException 547)</c>. The foreign key is kept as
    /// the last safeguard, but the rule no longer lives in a catch block with user-facing English text.
    /// </remarks>
    Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Marks a room for deletion. Does not persist.</summary>
    /// <param name="room">The room. The entity and not the id, because the service has fetched it anyway.</param>
    void Remove(Room room);
}
