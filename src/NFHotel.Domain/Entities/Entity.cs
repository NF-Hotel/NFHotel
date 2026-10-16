namespace NFHotel.Domain.Entities;

/// <summary>
/// Fælles basetype for alle entiteter: bærer primærnøglen <see cref="Id"/>.
/// </summary>
/// <typeparam name="TKey">Nøglens type. <c>int</c> når databasen genererer en kompakt nøgle.</typeparam>
/// <remarks>
/// Nøglens kolonnenavn (<c>&lt;tabel&gt;_id</c>) og om databasen genererer værdien står kun i
/// Infrastructure (<c>EntityConfiguration</c>), aldrig som attributter her (ADR-0001).
/// Setteren er beskyttet: kun EF Core og testværktøjer tildeler nøglen.
/// </remarks>
public abstract class Entity<TKey>
    where TKey : notnull
{
    /// <summary>Primærnøgle. Standardværdien indtil entiteten er persisteret (A-15).</summary>
    // default! is justified: EF Core or the factory method assigns the key before first use.
    public TKey Id { get; protected set; } = default!;
}