using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Tests.Persistence;

/// <summary>
/// Holder reglen fra ADR-0001 fast: nøglekolonnen hedder <c>&lt;tabel&gt;_id</c>, og en fremmednøgle
/// hedder som den refererede tabels nøgle. Testen læser kun EF-modellen og bruger ingen database.
/// </summary>
public sealed class KeyColumnNamingTests
{
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<HotelDbContext>();
        DependencyInjection.ConfigureHotelDbContext(options, "Host=localhost;Database=model-only");
        var encryptor = new AesGcmStringEncryptor(
            new EncryptionOptions { PassportKey = Convert.ToBase64String(new byte[32]) });

        using var context = new HotelDbContext(options.Options, encryptor);
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static string ColumnOf(IReadOnlyProperty property, string table) =>
        property.GetColumnName(StoreObjectIdentifier.Table(table))
        ?? throw new InvalidOperationException($"{property.Name} has no column in {table}.");

    [Fact]
    public void PrimaryKey_EveryEntity_IsNamedAfterItsTable()
    {
        var model = BuildModel();

        var entityTypes = model.GetEntityTypes().ToList();
        Assert.NotEmpty(entityTypes);
        foreach (var entityType in entityTypes)
        {
            var table = entityType.GetTableName() ?? throw new InvalidOperationException("Entity has no table.");
            var key = entityType.FindPrimaryKey();

            Assert.NotNull(key);
            var property = Assert.Single(key.Properties);
            Assert.Equal($"{table}_id", ColumnOf(property, table));
        }
    }

    [Fact]
    public void ForeignKey_EveryRelation_IsNamedAfterTheReferencedTable()
    {
        var model = BuildModel();

        var foreignKeys = model.GetEntityTypes().SelectMany(entityType => entityType.GetForeignKeys()).ToList();
        Assert.NotEmpty(foreignKeys);
        foreach (var foreignKey in foreignKeys)
        {
            var table = foreignKey.DeclaringEntityType.GetTableName() ?? throw new InvalidOperationException("Entity has no table.");
            var principalTable = foreignKey.PrincipalEntityType.GetTableName() ?? throw new InvalidOperationException("Entity has no table.");

            var property = Assert.Single(foreignKey.Properties);
            Assert.Equal($"{principalTable}_id", ColumnOf(property, table));
        }
    }
}