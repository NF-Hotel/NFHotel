using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Fælles kontrakt for et aggregats repository: hent på nøgle og markér ændringer.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">Nøglens type.</typeparam>
/// <remarks>
/// Skriv-metoderne markerer og persisterer ikke — det gør <c>IUnitOfWork</c>, derfor er de
/// synkrone. Sletning er ikke med her, fordi en booking aldrig slettes (BR-13); entiteter der
/// kan slettes, tilføjer <c>Remove</c> i deres egen interface.
/// </remarks>
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>Henter entiteten, så domænemetoderne kan kaldes på den.</summary>
    /// <param name="id">Entitetens nøgle.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Entiteten, eller <c>null</c> hvis den ikke findes (BR-115).</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Markerer en ny entitet til indsættelse. Persisterer ikke.</summary>
    /// <param name="entity">Entiteten. Nøglen sættes når der gemmes.</param>
    void Add(TEntity entity);

    /// <summary>Markerer en ændret entitet til opdatering. Persisterer ikke.</summary>
    /// <param name="entity">Entiteten.</param>
    void Update(TEntity entity);
}