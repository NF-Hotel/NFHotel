using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// Common EF Core mapping for entities with <see cref="Entity{TKey}"/>: table name and key column.
/// </summary>
/// <typeparam name="TEntity">Entiteten.</typeparam>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <remarks>
/// The key column is always called <c>&lt;table&gt;_id</c> and the database generates the value. The rule is stated
/// only here (ADR-0001) and is no longer an accidental result of the snake_case convention:
/// since the key is called <c>Id</c> on all entities, the convention would otherwise give <c>id</c>.
/// A foreign key is named like the key of the referenced table (<c>room_id</c>). A test over
/// the model pins both rules.
/// </remarks>
public abstract class EntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    /// <summary>The name of the table. The key column is derived from it.</summary>
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

    /// <summary>The table's own database safeguards, for example check constraints. Default: none.</summary>
    /// <param name="table">Tabelbyggeren.</param>
    protected virtual void ConfigureTable(TableBuilder<TEntity> table)
    {
    }

    /// <summary>The entity's own columns, indexes and relations.</summary>
    /// <param name="builder">Entitetsbyggeren.</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}