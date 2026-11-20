using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the booking requests that wait for their verification (MIL-088, ADR-0052).</summary>
public sealed class BookingRequestRepository : Repository<BookingRequest, int>, IBookingRequestRepository
{
    #region Constructors

    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public BookingRequestRepository(HotelDbContext context)
        : base(context)
    {
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public void Remove(BookingRequest request) => MarkForRemoval(request);

    /// <inheritdoc />
    public async Task<BookingRequest?> FindByEmailKeyAsync(string emailKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailKey);

        return await Context.BookingRequests
            .SingleOrDefaultAsync(request => request.EmailKey == emailKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// One statement deletes the row and returns it, so of two presses of a link at once one gets the request and the
    /// other gets none (ADR-0052 decision 5): PostgreSQL lets the second delete wait for the first and then finds no row.
    /// The statement is not composed into a query, and the request is not tracked: it is gone from the database.
    /// </remarks>
    public async Task<BookingRequest?> ClaimOpenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        List<BookingRequest> claimed = await Context.BookingRequests
            .FromSql($"DELETE FROM booking_request WHERE token_hash = {tokenHash} AND expires_at > {now} RETURNING *, xmin")
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return claimed.SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task<int> DeleteLapsedAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        await Context.BookingRequests
            .Where(request => request.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

    #endregion Public methods
}
