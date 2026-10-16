using NFHotel.Application.Errors;

namespace NFHotel.Application.Ports;

/// <summary>
/// Transaktionsgrænsen for en use case. Application-lagets eneste vej til at persistere.
/// </summary>
/// <remarks>
/// Repositories markerer ændringer (<c>Add</c>, <c>Update</c>, <c>Remove</c>) men gemmer
/// aldrig selv. Selv-gemmende repositories ville dele den samme underliggende session og
/// dermed committe hinandens ventende ændringer usynligt — og BR-47 ("opret booking for
/// ny gæst") kræver at to entiteter skrives under ét.
/// <para>
/// Interfacet holdes bevidst minimalt: ingen <c>BeginTransaction</c>, ingen
/// <c>Rollback</c>, ingen repository-samling. En eksplicit transaktions-API tilføjes den
/// dag en use case skriver i to omgange — se <c>BookingService.CreateAsync</c>, der i dag
/// er den eneste der gemmer to gange, og hvor konsekvensen er dokumenteret.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persisterer alle ventende ændringer i én transaktion.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Antal berørte rækker.</returns>
    /// <exception cref="BookingOverlapConflictException">
    /// Kastes af Infrastructure når databasens exclusion constraint afviser en
    /// dobbeltbooking (B-03).
    /// </exception>
    /// <exception cref="UniqueConstraintViolationException">
    /// Kastes af Infrastructure ved brud på en unik-constraint, fx rumnummer (BR-N-01).
    /// </exception>
    /// <exception cref="ConcurrencyConflictException">
    /// Kastes af Infrastructure når rækken er ændret af en anden siden den blev læst.
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}