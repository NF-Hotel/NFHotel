using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Data access for guests — the entity part. Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// The write methods mark and do not persist — <c>IUnitOfWork</c> does. There is
/// deliberately no search and no sorting on the passport number: the field is encrypted with a
/// randomised cipher (B-09), so even an equality lookup would not work. Projections are
/// i <c>IGuestQueries</c> (Application).
/// </remarks>
public interface IGuestRepository : IRepository<Guest, int>
{
    /// <summary>
    /// Decides whether the guest exists (BR-114).
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the guest exists. Does not fetch the whole graph.</returns>
    Task<bool> ExistsAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decides whether there are bookings for the guest.
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if at least one booking points to the guest.</returns>
    /// <remarks>Guard for deletion (BR-N-02) and a trail for D-05's personal data deletion.</remarks>
    Task<bool> HasAnyBookingsAsync(int guestId, CancellationToken cancellationToken = default);


    /// <summary>Marks a guest for deletion (BR-N-02). Does not persist.</summary>
    /// <param name="guest">The guest.</param>
    void Remove(Guest guest);
}
