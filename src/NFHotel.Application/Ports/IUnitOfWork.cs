using NFHotel.Application.Errors;

namespace NFHotel.Application.Ports;

/// <summary>
/// The transaction boundary of a use case. The only way for the Application layer to persist.
/// </summary>
/// <remarks>
/// Repositories mark changes (<c>Add</c>, <c>Update</c>, <c>Remove</c>) but never save
/// themselves. Self-saving repositories would share the same underlying session and
/// thereby commit each other's pending changes invisibly — and BR-47 ("create booking for
/// new guest") requires that two entities are written together.
/// <para>
/// The interface is deliberately minimal: no <c>BeginTransaction</c>, no
/// <c>Rollback</c>, no repository collection. An explicit transaction API is added the
/// day a use case writes in two rounds — see <c>BookingService.CreateAsync</c>, which today
/// is the only one that saves twice, and where the consequence is documented.
/// </para>
/// <para>
/// The one transaction of its own is <see cref="RunUnderLockAsync"/>: a use case that must count and write without another
/// request counting in between (MIL-088) runs that part under a lock of the database.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes in one transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    /// <exception cref="BookingOverlapConflictException">
    /// Thrown by Infrastructure when the database exclusion constraint rejects a
    /// dobbeltbooking (B-03).
    /// </exception>
    /// <exception cref="UniqueConstraintViolationException">
    /// Thrown by Infrastructure on a breach of a unique constraint, for example room number (BR-N-01).
    /// </exception>
    /// <exception cref="ConcurrencyConflictException">
    /// Thrown by Infrastructure when the row has been changed by someone else since it was read.
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a piece of work in one transaction that first takes a lock of the database by its name, so two pieces of work
    /// under the same name never run at the same time, in this instance or another (MIL-088, ADR-0052 decision 6: a
    /// booking request counts its mails, checks the fuse and writes in one go). The work reads, marks and calls
    /// <see cref="SaveChangesAsync"/>; the transaction commits when the work returns and rolls back when it throws.
    /// </summary>
    /// <typeparam name="TResult">The result of the work.</typeparam>
    /// <param name="lockName">The name of the lock.</param>
    /// <param name="work">The work, which gets the cancellation token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the work.</returns>
    Task<TResult> RunUnderLockAsync<TResult>(
        string lockName,
        Func<CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default);
}
