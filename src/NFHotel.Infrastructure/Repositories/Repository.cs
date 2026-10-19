using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Common EF Core implementation of <see cref="IRepository{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <remarks>
/// This is what the three repositories had in common: fetch by key and mark changes. Each
/// repository only adds its own lookups (ADR-0001). No <c>SaveChanges</c> — that is done by
/// <c>IUnitOfWork</c>.
/// </remarks>
public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
    protected Repository(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Context = context;
    }

    /// <summary>The context all lookups go through.</summary>
    protected HotelDbContext Context { get; }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await Context.Set<TEntity>()
            .FindAsync([id], cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Context.Set<TEntity>().Add(entity);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Context.Set<TEntity>().Update(entity);
    }

    /// <summary>
    /// Marks an entity for deletion. Protected, so only the repositories whose entity may be
    /// deleted (guest, room) show it in their interface; a booking is never deleted (BR-13).
    /// </summary>
    /// <param name="entity">Entiteten.</param>
    protected void MarkForRemoval(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Context.Set<TEntity>().Remove(entity);
    }
}
