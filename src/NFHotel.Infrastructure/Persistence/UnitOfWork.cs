using Microsoft.EntityFrameworkCore;

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
                $"Exclusion constrainten '{postgres.ConstraintName}' afviste bookingen (SQLSTATE {postgres.SqlState}).",
                original),

            // 23505: for example ux_room_room_number (BR-N-01).
            PostgresErrorCodes.UniqueViolation => new UniqueConstraintViolationException(
                $"Unik-constrainten '{postgres.ConstraintName}' blev brudt (SQLSTATE {postgres.SqlState}).",
                postgres.ConstraintName ?? string.Empty,
                original),

            _ => original
        };
}
