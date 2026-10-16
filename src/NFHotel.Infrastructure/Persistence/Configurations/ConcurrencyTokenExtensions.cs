using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// Optimistic concurrency via PostgreSQL's system column <c>xmin</c>.
/// </summary>
/// <remarks>
/// <c>xmin</c> is the transaction id that last wrote the row. PostgreSQL maintains it
/// itself, so the token costs neither a column, a trigger nor an extra write — and
/// the Npgsql provider leaves the system column out of migrations.
/// <para>
/// Without the token two simultaneous edits of the same booking would both succeed, and one
/// would disappear without trace. That is exactly the gap the old system's <c>Update</c> had,
/// where <c>rowsAffected</c> was ignored.
/// </para>
/// <para>
/// Npgsql's earlier <c>UseXminAsConcurrencyToken()</c> no longer exists in EF Core 9;
/// this extension is the recommended replacement and keeps the four lines in one place.
/// </para>
/// </remarks>
public static class ConcurrencyTokenExtensions
{
    /// <summary>The name of the shadow property and of the system column.</summary>
    public const string XminPropertyName = "xmin";

    /// <summary>
    /// Makes <c>xmin</c> the concurrency token of the entity.
    /// </summary>
    /// <typeparam name="TEntity">Entitetstypen.</typeparam>
    /// <param name="builder">Entitetsbuilderen.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
    public static EntityTypeBuilder<TEntity> UseXminAsConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property<uint>(XminPropertyName)
            .HasColumnName(XminPropertyName)
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return builder;
    }
}