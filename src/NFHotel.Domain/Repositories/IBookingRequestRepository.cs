using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Storage of the booking requests of the public site (MIL-088, ADR-0052). <see cref="Add"/> and <see cref="Remove"/> mark
/// and do not persist; <see cref="ClaimOpenAsync"/> and <see cref="DeleteLapsedAsync"/>, the clean-up, delete in the
/// database at once.
/// </summary>
public interface IBookingRequestRepository
{
    #region Public methods

    /// <summary>Marks a new request for insertion. Does not persist.</summary>
    /// <param name="request">The request.</param>
    void Add(BookingRequest request);

    /// <summary>
    /// Marks a request for deletion. Does not persist; the save fails when the row is gone or was renewed since it was
    /// read.
    /// </summary>
    /// <param name="request">The request, as <see cref="FindByEmailKeyAsync"/> returned it or as it was added.</param>
    void Remove(BookingRequest request);

    /// <summary>Finds the request of a mailbox, lapsed or not.</summary>
    /// <param name="emailKey">The key of the mailbox, as 64 lower-case hex digits.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The request, tracked, or <see langword="null"/>.</returns>
    Task<BookingRequest?> FindByEmailKeyAsync(string emailKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims the open request of a token hash (ADR-0052 decision 5): one statement deletes, in the database at once, the
    /// request of the hash whose link has not lapsed and returns it, so of two claims at the same time one gets it.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 of the token, as 64 lower-case hex digits.</param>
    /// <param name="now">The moment; a request whose link lapsed at it or before is not claimed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The request as it was, not tracked, or <see langword="null"/> when no open request has the hash.</returns>
    Task<BookingRequest?> ClaimOpenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Deletes, in the database at once, every request whose link lapsed at or before a moment.</summary>
    /// <param name="now">The moment.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of requests deleted.</returns>
    Task<int> DeleteLapsedAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    #endregion Public methods
}
