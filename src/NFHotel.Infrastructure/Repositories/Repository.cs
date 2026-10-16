using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Fælles EF Core-implementering af <see cref="IRepository{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">Nøglens type.</typeparam>
/// <remarks>
/// Her ligger det de tre repositories havde ens: hent på nøgle og markér ændringer. Hvert
/// repository tilføjer kun sine egne opslag (ADR-0001). Ingen <c>SaveChanges</c> — det gør
/// <c>IUnitOfWork</c>.
/// </remarks>
public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Opretter repositoryet.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="context"/> er <c>null</c>.</exception>
    protected Repository(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Context = context;
    }

    /// <summary>Konteksten alle opslag går gennem.</summary>
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
    /// Markerer en entitet til sletning. Beskyttet, så kun de repositories hvis entitet må
    /// slettes (gæst, rum) viser det i deres interface; en booking slettes aldrig (BR-13).
    /// </summary>
    /// <param name="entity">Entiteten.</param>
    protected void MarkForRemoval(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Context.Set<TEntity>().Remove(entity);
    }
}