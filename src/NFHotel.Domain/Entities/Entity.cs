namespace NFHotel.Domain.Entities;

/// <summary>
/// Common base type for all entities: carries the primary key <see cref="Id"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key. <c>int</c> when the database generates a compact key.</typeparam>
/// <remarks>
/// The column name of the key (<c>&lt;table&gt;_id</c>) and whether the database generates the value are stated only in
/// Infrastructure (<c>EntityConfiguration</c>), never as attributes here (ADR-0001).
/// The setter is protected: only EF Core and test tools assign the key.
/// </remarks>
public abstract class Entity<TKey>
    where TKey : notnull
{
    /// <summary>Primary key. The default value until the entity is persisted (A-15).</summary>
    // default! is justified: EF Core or the factory method assigns the key before first use.
    public TKey Id { get; protected set; } = default!;
}