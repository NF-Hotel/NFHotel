using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Errors;
using NFHotel.Application.Ports;

using Npgsql;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// Transaktionsgrænsen for en use case (<see cref="IUnitOfWork"/>).
/// </summary>
/// <remarks>
/// Ud over at gemme har typen ét ansvar mere: at oversætte databasens fejl til
/// Application-lagets undtagelsestyper. Uden den oversættelse ville
/// <c>PostgresException</c> lække op i services, og afhængighedsretningen ville være brudt
/// i praksis selv om den holdt på papiret.
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly HotelDbContext _context;

    /// <summary>
    /// Opretter transaktionsgrænsen.
    /// </summary>
    /// <param name="context">Konteksten. Skal være <b>samme</b> instans som repositories bruger.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="context"/> er <c>null</c>.</exception>
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
                "Rækken blev ændret af en anden mellem læsning og skrivning (xmin-token matchede ikke).",
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
    /// Oversætter en PostgreSQL-fejl til Application-lagets typer.
    /// </summary>
    /// <param name="postgres">Den underliggende PostgreSQL-fejl.</param>
    /// <param name="original">Fejlen der skal bevares som <c>InnerException</c> til loggen.</param>
    /// <returns>Den oversatte fejl, eller <paramref name="original"/> hvis koden er ukendt.</returns>
    private static Exception Translate(PostgresException postgres, Exception original) =>
        postgres.SqlState switch
        {
            // 23P01: exclusion constrainten ex_booking_room_period afviste en dobbeltbooking.
            // Servicen har allerede spurgt domænet; det her er kapløbet mellem to samtidige
            // oprettelser der begge så et ledigt rum (B-03).
            PostgresErrorCodes.ExclusionViolation => new BookingOverlapConflictException(
                $"Exclusion constrainten '{postgres.ConstraintName}' afviste bookingen (SQLSTATE {postgres.SqlState}).",
                original),

            // 23505: fx ux_room_room_number (BR-N-01).
            PostgresErrorCodes.UniqueViolation => new UniqueConstraintViolationException(
                $"Unik-constrainten '{postgres.ConstraintName}' blev brudt (SQLSTATE {postgres.SqlState}).",
                postgres.ConstraintName ?? string.Empty,
                original),

            _ => original
        };
}