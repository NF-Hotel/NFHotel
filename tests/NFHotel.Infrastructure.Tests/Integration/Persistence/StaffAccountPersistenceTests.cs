using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The new columns of the staff table, the list, the last-Manager query and the audit table against a real PostgreSQL
/// (ADR-0015).
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class StaffAccountPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public StaffAccountPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Columns

    /// <summary>Integration: a changed password, the flag and the stamp survive a round trip.</summary>
    [Fact]
    public async Task SaveChangesAsync_ChangedPassword_KeepsTheHashTheFlagAndTheStamp()
    {
        // Arrange
        await SeedAsync("rita", StaffRole.Receptionist, mustChange: true);
        await using (var context = _fixture.CreateContext())
        {
            var repository = new StaffRepository(context);
            var member = await repository.FindByUserNameAsync("rita", TestContext.Current.CancellationToken);
            member!.ChangePassword("hash-2");
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(reader).FindByUserNameAsync("rita", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("hash-2", found!.PasswordHash);
        Assert.False(found.MustChangePassword);
        Assert.Equal(2, found.SecurityStamp);
    }

    /// <summary>Integration: a new member starts at stamp 1 and keeps the must-change flag it was created with.</summary>
    [Fact]
    public async Task FindByUserNameAsync_NewMember_StartsAtStampOneWithItsFlag()
    {
        // Arrange
        await SeedAsync("boss", StaffRole.Manager, mustChange: true);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new StaffRepository(context).FindByUserNameAsync("boss", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, found!.SecurityStamp);
        Assert.True(found.MustChangePassword);
    }

    #endregion

    #region Queries

    /// <summary>Positive: the list holds every member ordered by name.</summary>
    [Fact]
    public async Task ListAsync_Members_AreOrderedByUserName()
    {
        // Arrange
        await SeedAsync("zoe", StaffRole.Housekeeping);
        await SeedAsync("anna", StaffRole.Manager);
        await using var context = _fixture.CreateContext();

        // Act
        var list = await new StaffRepository(context).ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["anna", "zoe"], list.Select(member => member.UserName));
    }

    /// <summary>Boundary: another active Manager is found only when one exists besides the given account and is active.</summary>
    [Fact]
    public async Task AnotherActiveManagerExistsAsync_OneTwoAndDisabledManagers_AnswersEachCase()
    {
        // Arrange
        await SeedAsync("boss1", StaffRole.Manager);
        await SeedAsync("rita", StaffRole.Receptionist);
        await using var context = _fixture.CreateContext();
        var repository = new StaffRepository(context);
        var first = (await repository.FindByUserNameAsync("boss1", TestContext.Current.CancellationToken))!;

        // Act
        var alone = await repository.AnotherActiveManagerExistsAsync(first.Id, TestContext.Current.CancellationToken);
        await SeedAsync("boss2", StaffRole.Manager);
        var withAnother = await repository.AnotherActiveManagerExistsAsync(first.Id, TestContext.Current.CancellationToken);
        await using var changing = _fixture.CreateContext();
        var second = (await new StaffRepository(changing).FindByUserNameAsync("boss2", TestContext.Current.CancellationToken))!;
        second.Deactivate();
        await new UnitOfWork(changing).SaveChangesAsync(TestContext.Current.CancellationToken);
        var whenTheOtherIsDisabled = await repository.AnotherActiveManagerExistsAsync(first.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(alone);
        Assert.True(withAnother);
        Assert.False(whenTheOtherIsDisabled);
    }

    #endregion

    #region Audit

    /// <summary>Integration: audit records are saved with the change in one transaction, read newest first, and counted.</summary>
    [Fact]
    public async Task StaffAuditRepository_AddedRecords_AreReadNewestFirstAndCounted()
    {
        // Arrange
        await using (var context = _fixture.CreateContext())
        {
            var repository = new StaffAuditRepository(context);
            repository.Add(StaffAuditEvent.Create(new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero), StaffAuditAction.SignedIn, "Success", 1, "boss", 1, "boss", "203.0.113.7"));
            repository.Add(StaffAuditEvent.Create(new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero), StaffAuditAction.PasswordReset, "Success", 1, "boss", 2, "rita"));
            repository.Add(StaffAuditEvent.Create(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero), StaffAuditAction.SignInFailed, "staff.invalid_credentials", targetName: "rita"));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();
        var audit = new StaffAuditRepository(reader);

        // Act
        var page = await audit.ListAsync(0, 2, TestContext.Current.CancellationToken);
        var rest = await audit.ListAsync(2, 2, TestContext.Current.CancellationToken);
        var count = await audit.CountAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([StaffAuditAction.SignInFailed, StaffAuditAction.PasswordReset], page.Select(item => item.Action));
        Assert.Equal(StaffAuditAction.SignedIn, Assert.Single(rest).Action);
        Assert.Equal("203.0.113.7", rest[0].RemoteAddress);
        Assert.Equal(3, count);
    }

    /// <summary>Boundary: a text above the column length is cut when the record is made, so the save cannot fail on it.</summary>
    [Fact]
    public async Task StaffAuditRepository_LongTexts_AreCutToTheColumnLength()
    {
        // Arrange
        var long100 = new string('n', 150);
        await using (var context = _fixture.CreateContext())
        {
            new StaffAuditRepository(context).Add(StaffAuditEvent.Create(new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero), StaffAuditAction.SignedIn, long100, actorName: long100, remoteAddress: long100));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var reader = _fixture.CreateContext();

        // Act
        var stored = Assert.Single(await new StaffAuditRepository(reader).ListAsync(0, 10, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(StaffAuditEvent.MaxTextLength, stored.Outcome.Length);
        Assert.Equal(StaffAuditEvent.MaxTextLength, stored.ActorName!.Length);
    }

    #endregion

    #region Helpers

    private async Task SeedAsync(string name, StaffRole role, bool mustChange = false)
    {
        await using var context = _fixture.CreateContext();
        new StaffRepository(context).Add(StaffMember.Create(name, "hash-1", role, mustChange));
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion
}
