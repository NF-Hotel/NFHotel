using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fælles EF Core-mapping for entiteter med <see cref="Entity{TKey}"/>: tabelnavn og nøglekolonne.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">Nøglens type.</typeparam>
/// <remarks>
/// Nøglekolonnen hedder altid <c>&lt;tabel&gt;_id</c> og databasen genererer værdien. Reglen står
/// kun her (ADR-0001) og er ikke længere et tilfældigt resultat af snake_case-konventionen:
/// da nøglen hedder <c>Id</c> på alle entiteter, ville konventionen ellers give <c>id</c>.
/// En fremmednøgle hedder som den refererede tabels nøgle (<c>room_id</c>). En test over
/// modellen holder begge regler fast.
/// </remarks>
public abstract class EntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>Tabellens navn. Nøglekolonnen udledes af det.</summary>
    protected abstract string TableName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName, ConfigureTable);

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName($"{TableName}_id")
            .ValueGeneratedOnAdd();

        ConfigureEntity(builder);
    }

    /// <summary>Tabellens egne databaseværn, fx check constraints. Standard: ingen.</summary>
    /// <param name="table">Tabelbyggeren.</param>
    protected virtual void ConfigureTable(TableBuilder<TEntity> table)
    {
    }

    /// <summary>Entitetens egne kolonner, indekser og relationer.</summary>
    /// <param name="builder">Entitetsbyggeren.</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}