using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The staff table against a real PostgreSQL: the migration, the lookup by name and the unique index (ADR-0005).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class StaffRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public StaffRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Lookup

    /// <summary>Integration and positive: a stored member comes back by name with the role, the hash and the active flag.</summary>
    [Fact]
    public async Task FindByUserNameAsync_StoredMember_ReturnsItWithItsFields()
    {
        // Arrange
        await SeedAsync("rita", "hash-1", StaffRole.Receptionist);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(context).FindByUserNameAsync("rita", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("hash-1", found.PasswordHash);
        Assert.Equal(StaffRole.Receptionist, found.Role);
        Assert.True(found.IsActive);
    }

    /// <summary>Equivalence: the lookup does not depend on case or on spaces around the name.</summary>
    [Fact]
    public async Task FindByUserNameAsync_NameInOtherCase_ReturnsTheMember()
    {
        // Arrange
        await SeedAsync("rita", "hash-1", StaffRole.Manager);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(context).FindByUserNameAsync("  RiTa ", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
    }

    /// <summary>Negative: an unknown name gives no member.</summary>
    [Fact]
    public async Task FindByUserNameAsync_UnknownName_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(context).FindByUserNameAsync("nobody", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(found);
    }

    /// <summary>Integration: the table is empty until a member is stored, and then it is not.</summary>
    [Fact]
    public async Task AnyAsync_EmptyAndFilledTable_AnswerFalseAndTrue()
    {
        // Arrange
        await using var emptyContext = _fixture.CreateContext();
        var empty = await new StaffRepository(emptyContext).AnyAsync(TestContext.Current.CancellationToken);
        await SeedAsync("rita", "hash-1", StaffRole.Manager);
        await using var context = _fixture.CreateContext();

        // Act
        var filled = await new StaffRepository(context).AnyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(empty);
        Assert.True(filled);
    }

    #endregion

    #region Uniqueness and changes

    /// <summary>Race: the unique index refuses a second member with the same name, and the unit of work reports it as a unique violation.</summary>
    [Fact]
    public async Task SaveChangesAsync_SameNameTwice_ThrowsAUniqueViolation()
    {
        // Arrange
        await SeedAsync("rita", "hash-1", StaffRole.Manager);
        await using var context = _fixture.CreateContext();
        new StaffRepository(context).Add(StaffMember.Create("RITA", "hash-2", StaffRole.Housekeeping));

        // Act
        var error = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("ux_staff_member_user_name", error.ConstraintName);
    }

    /// <summary>State transition: a deactivated member is stored as inactive.</summary>
    [Fact]
    public async Task SaveChangesAsync_DeactivatedMember_IsStoredInactive()
    {
        // Arrange
        await SeedAsync("rita", "hash-1", StaffRole.Manager);
        await using (var context = _fixture.CreateContext())
        {
            var repository = new StaffRepository(context);
            var member = await repository.FindByUserNameAsync("rita", TestContext.Current.CancellationToken);
            member!.Deactivate();
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(reader).FindByUserNameAsync("rita", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(found!.IsActive);
    }

    #endregion

    #region Helpers

    private async Task SeedAsync(string name, string hash, StaffRole role)
    {
        await using var context = _fixture.CreateContext();
        new StaffRepository(context).Add(StaffMember.Create(name, hash, role));
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion
}