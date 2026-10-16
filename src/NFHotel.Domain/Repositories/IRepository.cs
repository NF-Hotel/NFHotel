using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Common contract for the repository of an aggregate: fetch by key and mark changes.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <remarks>
/// The write methods mark and do not persist — <c>IUnitOfWork</c> does, which is why they are
/// synchronous. Deletion is not included here, because a booking is never deleted (BR-13); entities that
/// can be deleted add <c>Remove</c> in their own interface.
/// </remarks>
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>Fetches the entity so the domain methods can be called on it.</summary>
    /// <param name="id">The key of the entity.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The entity, or <c>null</c> if it does not exist (BR-115).</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Marks a new entity for insertion. Does not persist.</summary>
    /// <param name="entity">The entity. The key is set when it is saved.</param>
    void Add(TEntity entity);

    /// <summary>Marks a changed entity for update. Does not persist.</summary>
    /// <param name="entity">Entiteten.</param>
    void Update(TEntity entity);
}