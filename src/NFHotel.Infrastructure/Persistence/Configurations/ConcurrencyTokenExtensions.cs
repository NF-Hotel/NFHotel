using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// Optimistisk samtidighed via PostgreSQLs systemkolonne <c>xmin</c>.
/// </summary>
/// <remarks>
/// <c>xmin</c> er transaktions-id'et der sidst skrev rækken. PostgreSQL vedligeholder den
/// selv, så tokenet koster hverken en kolonne, en trigger eller en ekstra skrivning — og
/// Npgsql-provideren udelader systemkolonnen fra migrationer.
/// <para>
/// Uden tokenet ville to samtidige redigeringer af samme booking begge lykkes, og den ene
/// ville forsvinde uden spor. Det er nøjagtig det hul det gamle systems <c>Update</c> havde,
/// hvor <c>rowsAffected</c> blev ignoreret.
/// </para>
/// <para>
/// Npgsqls tidligere <c>UseXminAsConcurrencyToken()</c> findes ikke længere i EF Core 9;
/// denne extension er den anbefalede erstatning og holder de fire linjer ét sted.
/// </para>
/// </remarks>
public static class ConcurrencyTokenExtensions
{
    /// <summary>Navnet på skyggeegenskaben og på systemkolonnen.</summary>
    public const string XminPropertyName = "xmin";

    /// <summary>
    /// Gør <c>xmin</c> til entitetens samtidighedstoken.
    /// </summary>
    /// <typeparam name="TEntity">Entitetstypen.</typeparam>
    /// <param name="builder">Entitetsbuilderen.</param>
    /// <returns>Samme builder, så kald kan kædes.</returns>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="builder"/> er <c>null</c>.</exception>
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