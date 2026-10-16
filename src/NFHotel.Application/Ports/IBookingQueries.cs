using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Ports;

/// <summary>
/// Reading bookings as projections (A-11). Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// Entities are fetched only on command paths, via <c>IBookingRepository</c> in Domain; here
/// DTOs and read models are returned, which Domain must not know (ADR-0001).
/// </remarks>
public interface IBookingQueries
{
    /// <summary>
    /// Fetches the read model of one booking with room and guest.
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The read model, or <c>null</c> if the booking does not exist (BR-115).</returns>
    Task<BookingReadModel?> GetReadModelAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the booking overview for a date window (BR-23, BR-24, BR-25).
    /// </summary>
    /// <param name="periodStart">The first date of the window, inclusive.</param>
    /// <param name="periodEnd">Vinduets sidste dato, eksklusiv.</param>
    /// <param name="includedStatuses">
    /// The statuses to include. The caller chooses — the repository does not hard-code
    /// "without cancelled".
    /// </param>
    /// <param name="searchText">
    /// Free text on guest name or room number, or <c>null</c> for all. Filtered in
    /// the database, not in memory (BR-111).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The bookings whose effective period touches the window.</returns>
    Task<IReadOnlyList<BookingReadModel>> GetOverviewAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> includedStatuses,
        string? searchText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the occupations in a date window, for the availability lookup (BR-34, BR-39).
    /// </summary>
    /// <param name="periodStart">The first date of the window, inclusive.</param>
    /// <param name="periodEnd">Vinduets sidste dato, eksklusiv.</param>
    /// <param name="blockingStatuses">
    /// The statuses that block a room. Comes from <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Coarsely filtered occupations. The final overlap decision is made by the domain.</returns>
    /// <remarks>
    /// Returns projections and not entities, because the call is used only as a negation in
    /// the availability lookup. Fetching the whole table and filtering in memory is exactly
    /// the fault of BR-111.
    /// </remarks>
    Task<IReadOnlyList<RoomOccupancyDto>> GetOccupancyAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        CancellationToken cancellationToken = default);
}