using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Dataadgang for gæster — entitetsdelen. Implementeres i Infrastructure.
/// </summary>
/// <remarks>
/// Skrivemetoderne markerer og persisterer ikke — det gør <c>IUnitOfWork</c>. Der findes
/// bevidst ingen søgning og ingen sortering på pasnummer: feltet er krypteret med en
/// randomiseret ciffer (B-09), så selv et lighedsopslag ville ikke virke. Projektioner ligger
/// i <c>IGuestQueries</c> (Application).
/// </remarks>
public interface IGuestRepository : IRepository<Guest, int>
{
    /// <summary>
    /// Afgør om gæsten findes (BR-114).
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Sand hvis gæsten findes. Henter ikke hele grafen.</returns>
    Task<bool> ExistsAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Afgør om der findes bookinger på gæsten.
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Sand hvis mindst én booking peger på gæsten.</returns>
    /// <remarks>Guard for sletning (BR-N-02) og spor for D-05's persondatasletning.</remarks>
    Task<bool> HasAnyBookingsAsync(int guestId, CancellationToken cancellationToken = default);


    /// <summary>Markerer en gæst til sletning (BR-N-02). Persisterer ikke.</summary>
    /// <param name="guest">Gæsten.</param>
    void Remove(Guest guest);
}