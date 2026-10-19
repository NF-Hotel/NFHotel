

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;


/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// Defines an xUnit test collection named "PostgreSQL".
// Test classes in this collection can share the same PostgreSqlFixture
/// </summary>
[CollectionDefinition(Name)]

// Creates the collection class.
// ICollectionFixture<PostgreSqlFixture> tells xUnit
// that this collection must use PostgreSqlFixture as the shared fixture.
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
