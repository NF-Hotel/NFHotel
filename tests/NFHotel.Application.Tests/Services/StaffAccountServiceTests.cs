using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Creating staff accounts and the first Manager (ADR-0005).</summary>
public sealed class StaffAccountServiceTests
{
    private const string Password = "correct horse battery staple";

    #region Create

    /// <summary>Positive: a valid account is stored active, with the hash of the password and never the password itself.</summary>
    [Fact]
    public async Task CreateAsync_ValidRequest_StoresTheHashAndSaves()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = Build(world);

        // Act
        var result = await service.CreateAsync(new CreateStaffRequest("Rita", Password, StaffRole.Receptionist), TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.Staff.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal("rita", stored.UserName);
        Assert.Equal(FakePasswordHasher.Prefix + Password, stored.PasswordHash);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Boundary: a password of twelve characters is accepted, eleven is refused.</summary>
    [Fact]
    public async Task CreateAsync_PasswordAtTheMinimum_IsAcceptedAndOneLessIsRefused()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = Build(world);

        // Act
        var accepted = await service.CreateAsync(new CreateStaffRequest("a", new string('x', StaffAccountService.MinPasswordLength), StaffRole.Manager), TestContext.Current.CancellationToken);
        var refused = await service.CreateAsync(new CreateStaffRequest("b", new string('x', StaffAccountService.MinPasswordLength - 1), StaffRole.Manager), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(accepted.IsSuccess);
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, refused.FirstError?.Code);
        Assert.Single(world.Staff.Items);
    }

    /// <summary>Boundary: a password above the longest length is refused.</summary>
    [Fact]
    public async Task CreateAsync_PasswordOverTheMaximum_IsRefused()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await Build(world).CreateAsync(
            new CreateStaffRequest("a", new string('x', StaffAuthService.MaxPasswordLength + 1), StaffRole.Manager),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, result.FirstError?.Code);
    }

    /// <summary>Negative: a blank name and an unknown role are refused with the domain codes, and nothing is stored.</summary>
    [Fact]
    public async Task CreateAsync_BlankNameAndUnknownRole_ReturnsTheDomainCodes()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var blank = await Build(world).CreateAsync(new CreateStaffRequest("  ", Password, StaffRole.Manager), TestContext.Current.CancellationToken);
        var unknown = await Build(world).CreateAsync(new CreateStaffRequest("rita", Password, (StaffRole)0), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.UserNameRequired, blank.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.RoleInvalid, unknown.FirstError?.Code);
        Assert.Empty(world.Staff.Items);
    }

    /// <summary>Negative: a name that exists is refused in any case, and the first account stays as it was.</summary>
    [Fact]
    public async Task CreateAsync_NameTakenInOtherCase_ReturnsTaken()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = Build(world);
        await service.CreateAsync(new CreateStaffRequest("rita", Password, StaffRole.Manager), TestContext.Current.CancellationToken);

        // Act
        var result = await service.CreateAsync(new CreateStaffRequest("RITA", Password, StaffRole.Housekeeping), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.UserNameTaken, result.FirstError?.Code);
        Assert.Equal(StaffRole.Manager, Assert.Single(world.Staff.Items).Role);
    }

    /// <summary>Race: when the database index rejects the name after the check passed, the answer is still "taken".</summary>
    [Fact]
    public async Task CreateAsync_UniqueIndexFiresAfterTheCheck_ReturnsTaken()
    {
        // Arrange
        var world = new ServiceWorld();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate", "ux_staff_member_user_name");

        // Act
        var result = await Build(world).CreateAsync(new CreateStaffRequest("rita", Password, StaffRole.Manager), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.UserNameTaken, result.FirstError?.Code);
    }

    #endregion

    #region First Manager

    /// <summary>Positive: with no account the first Manager is created.</summary>
    [Fact]
    public async Task EnsureFirstManagerAsync_NoAccounts_CreatesAManager()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await Build(world).EnsureFirstManagerAsync("boss", Password, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StaffRole.Manager, Assert.Single(world.Staff.Items).Role);
    }

    /// <summary>Idempotence: a second call, or a call when any account exists, changes nothing.</summary>
    [Fact]
    public async Task EnsureFirstManagerAsync_AccountsExist_ChangesNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        world.Staff.Add(StaffMember.Create("existing", "hash", StaffRole.Housekeeping));
        var service = Build(world);

        // Act
        var first = await service.EnsureFirstManagerAsync("boss", Password, TestContext.Current.CancellationToken);
        var second = await service.EnsureFirstManagerAsync("boss", Password, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal("existing", Assert.Single(world.Staff.Items).UserName);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a short password for the first Manager is refused, so a weak account is never created from the environment file.</summary>
    [Fact]
    public async Task EnsureFirstManagerAsync_ShortPassword_ReturnsTheCodeAndStoresNothing()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await Build(world).EnsureFirstManagerAsync("boss", "short", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, result.FirstError?.Code);
        Assert.Empty(world.Staff.Items);
    }

    /// <summary>Race: when another start created the account first, the answer is success.</summary>
    [Fact]
    public async Task EnsureFirstManagerAsync_AnotherStartWonTheRace_ReturnsSuccess()
    {
        // Arrange
        var world = new ServiceWorld();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate", "ux_staff_member_user_name");

        // Act
        var result = await Build(world).EnsureFirstManagerAsync("boss", Password, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Helpers

    private static StaffAccountService Build(ServiceWorld world) => new(world.Staff, new FakePasswordHasher(), world.UnitOfWork);

    #endregion
}
