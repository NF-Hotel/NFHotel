using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Data access for bookings — the entity part. Implemented in Infrastructure.
/// </summary>
/// <remarks>
/// The repository does <b>not decide</b>. If the service wants to know which bookings block a
/// room, it passes the status set itself — taken from <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>.
/// The rule thus lives in one place instead of in SQL. Reading projections is in
/// <c>IBookingQueries</c> (Application), because the domain does not know DTOs (ADR-0001).
/// </remarks>
public interface IBookingRepository : IRepository<Booking, int>
{
    /// <summary>
    /// Fetches the bookings that <i>may</i> conflict with a desired period on a room (BR-19, B-03).
    /// </summary>
    /// <param name="roomId">The room the desired period applies to.</param>
    /// <param name="periodStart">Desired arrival date.</param>
    /// <param name="periodEnd">Desired departure date, exclusive.</param>
    /// <param name="blockingStatuses">
    /// The statuses that block. Comes from <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/> — not from
    /// a hard-coded <c>WHERE</c>.
    /// </param>
    /// <param name="excludeBookingId">
    /// The booking being edited, so it does not block itself. <c>null</c> on creation.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>
    /// <b>Candidates</b>, not conflicts. The coarse filtering on room, status set and date window
    /// happens in SQL; the decision itself is made by <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/>, so
    /// the repository and the domain can never disagree about <c>&lt;</c> versus <c>&lt;=</c> (A-04).
    /// </returns>
    Task<IReadOnlyList<Booking>> GetOverlapCandidatesAsync(
        int roomId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);
}
