using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using NFHotel.Application.Errors;
using NFHotel.Application.Ports;

using Npgsql;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// The transaction boundary of a use case (<see cref="IUnitOfWork"/>).
/// </summary>
/// <remarks>
/// Besides saving, the type has one more responsibility: to translate the database's errors to
/// the exception types of the Application layer. Without that translation
/// <c>PostgresException</c> would leak up into services, and the dependency direction would be broken
/// in practice even though it held on paper.
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly HotelDbContext _context;

    /// <summary>
    /// Creates the transaction boundary.
    /// </summary>
    /// <param name="context">The context. Must be the <b>same</b> instance the repositories use.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
    public UnitOfWork(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The row was changed by someone else between the read and the write (the xmin token did not match).",
                exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres)
        {
            throw Translate(postgres, exception);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception, exception);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The lock is a transaction lock of PostgreSQL (<c>pg_advisory_xact_lock</c>) with a key of 64 bits taken from the
    /// SHA-256 of the name, so every instance of the API takes the same lock, and it is let go at the commit or the
    /// rollback. The work runs inside the execution strategy of the context: a transient error of the database runs it
    /// again in a new transaction, and the entities that the failed attempt added or read are let go first, so a retry
    /// never saves them twice. A commit that fails on the network may have committed; the retry then finds what the
    /// first attempt wrote and counts it, which makes a limit stricter, never looser.
    /// </remarks>
    public async Task<TResult> RunUnderLockAsync<TResult>(
        string lockName,
        Func<CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockName);
        ArgumentNullException.ThrowIfNull(work);

        long key = LockKeyOf(lockName);
        HashSet<object> before = TrackedEntities();
        IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();
        bool firstAttempt = true;

        return await strategy.ExecuteAsync(
            async token =>
            {
                if (!firstAttempt)
                {
                    ForgetAllBut(before);
                }

                firstAttempt = false;
                IDbContextTransaction transaction = await _context.Database
                    .BeginTransactionAsync(token)
                    .ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false))
                {
                    await _context.Database
                        .ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", token)
                        .ConfigureAwait(false);
                    TResult result = await work(token).ConfigureAwait(false);
                    await transaction.CommitAsync(token).ConfigureAwait(false);
                    return result;
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The key of the lock of a name: the first 64 bits of the SHA-256 of its UTF-8 form.</summary>
    /// <param name="lockName">The name of the lock.</param>
    /// <returns>The key, the same in every instance and every start.</returns>
    internal static long LockKeyOf(string lockName) =>
        BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(lockName)));

    private HashSet<object> TrackedEntities() =>
        new(_context.ChangeTracker.Entries().Select(entry => entry.Entity), ReferenceEqualityComparer.Instance);

    // Lets go of what an attempt that failed added or read, so the next attempt starts from what the use case had.
    private void ForgetAllBut(HashSet<object> before)
    {
        foreach (var entry in _context.ChangeTracker.Entries().Where(entry => !before.Contains(entry.Entity)).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Translates a PostgreSQL error to the types of the Application layer.
    /// </summary>
    /// <param name="postgres">The underlying PostgreSQL error.</param>
    /// <param name="original">The error to keep as <c>InnerException</c> for the log.</param>
    /// <returns>The translated error, or <paramref name="original"/> if the code is unknown.</returns>
    private static Exception Translate(PostgresException postgres, Exception original) =>
        postgres.SqlState switch
        {
            // 23P01: the exclusion constraint ex_booking_room_period rejected a double booking.
            // The service has already asked the domain; this is the race between two simultaneous
            // creations that both saw a free room (B-03).
            PostgresErrorCodes.ExclusionViolation => new BookingOverlapConflictException(
                $"The exclusion constraint '{postgres.ConstraintName}' refused the booking (SQLSTATE {postgres.SqlState}).",
                original),

            // 23505: for example ux_room_room_number (BR-N-01).
            PostgresErrorCodes.UniqueViolation => new UniqueConstraintViolationException(
                $"The unique constraint '{postgres.ConstraintName}' was violated (SQLSTATE {postgres.SqlState}).",
                postgres.ConstraintName ?? string.Empty,
                original),

            _ => original
        };
}
