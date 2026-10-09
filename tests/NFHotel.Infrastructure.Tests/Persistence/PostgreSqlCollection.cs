

namespace NFHotel.Infrastructure.Tests.Persistence;


/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// Definerer en xUnit test-collection med navnet "PostgreSQL".
// Testklasser i denne collection kan dele den samme PostgreSqlFixture
/// </summary>
[CollectionDefinition(Name)]

// Opretter collection-klassen.
// ICollectionFixture<PostgreSqlFixture> fortæller xUnit,
// at denne collection skal bruge PostgreSqlFixture som fælles fixture.
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
