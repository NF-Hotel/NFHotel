using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// What a Manager changes on an account besides its password and its state, and the own name at the first sign-in
/// (MIL-059): the name, the e-mail address, the role, and the deletion.
/// </summary>
public sealed class StaffAccountChangesTests
{
    private const string Password = "correct horse battery staple";

    #region Create

    /// <summary>Positive: a new account keeps the name and the address it was created with.</summary>
    [Fact]
    public async Task CreateAsync_WithNameAndEmail_StoresThem()
    {
        // Arrange
        var (service, world, _) = Build();

        // Act
        var result = await service.CreateAsync(
            new CreateStaffRequest("lena", "temporary passphrase 7", StaffRole.Housekeeping, true, "Lena", "Berg", "Lena@Hotel.dk"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var created = world.Staff.Items.Single(member => member.UserName == "lena");
        Assert.Equal(("Lena", "Berg", "lena@hotel.dk"), (created.FirstName, created.LastName, created.Email));
    }

    /// <summary>Negative: an address that another account has creates nothing.</summary>
    [Fact]
    public async Task CreateAsync_TakenEmail_ReturnsEmailTakenAndCreatesNothing()
    {
        // Arrange
        var (service, world, _) = Build();
        Add(world, "rita", StaffRole.Receptionist, email: "front@hotel.dk");

        // Act
        var result = await service.CreateAsync(
            new CreateStaffRequest("lena", "temporary passphrase 7", StaffRole.Housekeeping, Email: "FRONT@hotel.dk"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.EmailTaken, result.FirstError?.Code);
        Assert.DoesNotContain(world.Staff.Items, member => member.UserName == "lena");
    }

    #endregion

    #region Rename

    /// <summary>Positive: a rename stores the names, is recorded, and the list shows them.</summary>
    [Fact]
    public async Task RenameAsync_ValidNames_StoresRecordsAndLists()
    {
        // Arrange
        var (service, world, manager) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.RenameAsync(rita.Id, new RenameStaffRequest("Rita", "Hansen"), TestContext.Current.CancellationToken);
        var listed = await service.ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal((StaffAuditAction.NameChanged, manager.Id, "rita"), (recorded.Action, recorded.ActorId, recorded.TargetName));
        var row = listed.Value.Single(account => account.Id == rita.Id);
        Assert.Equal(("Rita", "Hansen"), (row.FirstName, row.LastName));
    }

    /// <summary>Negative: empty names are refused with their codes and nothing is saved.</summary>
    [Fact]
    public async Task RenameAsync_EmptyNames_ReturnsTheCodesAndSavesNothing()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.RenameAsync(rita.Id, new RenameStaffRequest("", " "), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Staff.FirstNameRequired, ErrorCodes.Staff.LastNameRequired], result.Errors.Select(error => error.Code));
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a Receptionist cannot rename an account, and an unknown account is not found.</summary>
    [Fact]
    public async Task RenameAsync_WithoutPermissionOrUnknown_IsRefused()
    {
        // Arrange
        var (receptionistService, _, _) = Build(callerRole: StaffRole.Receptionist);
        var (managerService, _, _) = Build();

        // Act
        var forbidden = await receptionistService.RenameAsync(1, new RenameStaffRequest("A", "B"), TestContext.Current.CancellationToken);
        var missing = await managerService.RenameAsync(999, new RenameStaffRequest("A", "B"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, forbidden.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.NotFound, missing.FirstError?.Code);
    }

    #endregion

    #region E-mail

    /// <summary>Positive: an address is stored and recorded without the address itself in the trail.</summary>
    [Fact]
    public async Task ChangeEmailAsync_FreeAddress_StoresAndRecordsWithoutTheAddress()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.ChangeEmailAsync(rita.Id, new ChangeEmailRequest("Rita@Hotel.dk"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("rita@hotel.dk", rita.Email);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.EmailChanged, recorded.Action);
        Assert.Null(recorded.Detail);
    }

    /// <summary>Equivalence: the own address again is not taken; another account's address is, in any case.</summary>
    [Fact]
    public async Task ChangeEmailAsync_OwnAddressAndOtherAddress_AcceptsTheOwnRefusesTheOther()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist, email: "rita@hotel.dk");
        Add(world, "lena", StaffRole.Housekeeping, email: "lena@hotel.dk");

        // Act
        var own = await service.ChangeEmailAsync(rita.Id, new ChangeEmailRequest("RITA@hotel.dk"), TestContext.Current.CancellationToken);
        var other = await service.ChangeEmailAsync(rita.Id, new ChangeEmailRequest("Lena@Hotel.dk"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(own.IsSuccess);
        Assert.Equal(ErrorCodes.Staff.EmailTaken, other.FirstError?.Code);
        Assert.Equal("rita@hotel.dk", rita.Email);
    }

    /// <summary>Positive: an empty address clears it, so several accounts can be without one.</summary>
    [Fact]
    public async Task ChangeEmailAsync_Empty_ClearsTheAddress()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist, email: "rita@hotel.dk");

        // Act
        var result = await service.ChangeEmailAsync(rita.Id, new ChangeEmailRequest(null), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(rita.Email);
    }

    #endregion

    #region Role

    /// <summary>Positive: another role is set, ends the sessions, and is recorded with the old and the new role.</summary>
    [Fact]
    public async Task ChangeRoleAsync_OtherAccount_SetsRaisesTheStampAndRecordsBothRoles()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.ChangeRoleAsync(rita.Id, new ChangeRoleRequest(StaffRole.Manager), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StaffRole.Manager, rita.Role);
        Assert.Equal(2, rita.SecurityStamp);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal((StaffAuditAction.RoleChanged, "Receptionist to Manager"), (recorded.Action, recorded.Detail));
    }

    /// <summary>Edge: the same role again succeeds and records nothing.</summary>
    [Fact]
    public async Task ChangeRoleAsync_SameRole_SucceedsWithoutARecord()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.ChangeRoleAsync(rita.Id, new ChangeRoleRequest(StaffRole.Receptionist), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(1, rita.SecurityStamp);
    }

    /// <summary>Negative: a Manager cannot change the own role.</summary>
    [Fact]
    public async Task ChangeRoleAsync_OwnAccount_ReturnsCannotChangeOwnRole()
    {
        // Arrange
        var (service, _, manager) = Build();

        // Act
        var result = await service.ChangeRoleAsync(manager.Id, new ChangeRoleRequest(StaffRole.Housekeeping), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CannotChangeOwnRole, result.FirstError?.Code);
        Assert.Equal(StaffRole.Manager, manager.Role);
    }

    /// <summary>Negative: the last active Manager cannot lose the role; with a second Manager it can.</summary>
    [Fact]
    public async Task ChangeRoleAsync_LastActiveManager_IsRefusedUntilAnotherExists()
    {
        // Arrange: the caller is no Manager in the store, so the rule is reached; the permission is allowed outright.
        var (service, world, _) = Build(callerRole: StaffRole.Receptionist, allowAll: true);
        var boss = Add(world, "boss", StaffRole.Manager);

        // Act
        var refused = await service.ChangeRoleAsync(boss.Id, new ChangeRoleRequest(StaffRole.Receptionist), TestContext.Current.CancellationToken);
        Add(world, "deputy", StaffRole.Manager);
        var accepted = await service.ChangeRoleAsync(boss.Id, new ChangeRoleRequest(StaffRole.Receptionist), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.LastManager, refused.FirstError?.Code);
        Assert.True(accepted.IsSuccess);
    }

    #endregion

    #region Delete

    /// <summary>Positive: a deletion removes the account and is recorded with its user name.</summary>
    [Fact]
    public async Task DeleteAsync_OtherAccount_RemovesAndRecords()
    {
        // Arrange
        var (service, world, manager) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.DeleteAsync(rita.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(rita, world.Staff.Removed);
        Assert.DoesNotContain(rita, world.Staff.Items);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal((StaffAuditAction.AccountDeleted, manager.Id, "rita"), (recorded.Action, recorded.ActorId, recorded.TargetName));
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a Manager cannot delete the own account, and an unknown account is not found.</summary>
    [Fact]
    public async Task DeleteAsync_OwnOrUnknownAccount_IsRefused()
    {
        // Arrange
        var (service, world, manager) = Build();

        // Act
        var own = await service.DeleteAsync(manager.Id, TestContext.Current.CancellationToken);
        var missing = await service.DeleteAsync(999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CannotDeleteSelf, own.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.NotFound, missing.FirstError?.Code);
        Assert.Empty(world.Staff.Removed);
    }

    /// <summary>Negative: the last active Manager cannot be deleted; a disabled Manager can.</summary>
    [Fact]
    public async Task DeleteAsync_LastActiveManager_IsRefusedButADisabledManagerIsNot()
    {
        // Arrange: the caller is no Manager in the store, so the rule is reached; the permission is allowed outright.
        var (service, world, _) = Build(callerRole: StaffRole.Receptionist, allowAll: true);
        var boss = Add(world, "boss", StaffRole.Manager);
        var former = Add(world, "former", StaffRole.Manager);
        former.Deactivate();

        // Act
        var refused = await service.DeleteAsync(boss.Id, TestContext.Current.CancellationToken);
        var accepted = await service.DeleteAsync(former.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.LastManager, refused.FirstError?.Code);
        Assert.True(accepted.IsSuccess);
    }

    /// <summary>Negative: a Housekeeping member cannot delete an account.</summary>
    [Fact]
    public async Task DeleteAsync_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        var (service, world, _) = Build(callerRole: StaffRole.Housekeeping);
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.DeleteAsync(rita.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Forbidden, result.FirstError?.Code);
        Assert.Contains(rita, world.Staff.Items);
    }

    #endregion

    #region The own name

    /// <summary>Positive: an account without a name gets one, is recorded as its own act, and the session carries it.</summary>
    [Fact]
    public async Task SetOwnNameAsync_WithoutAName_SetsItAndReturnsTheSession()
    {
        // Arrange
        var (service, world, member) = BuildSelf();

        // Act
        var result = await service.SetOwnNameAsync(new RenameStaffRequest("Rita", "Hansen"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("Rita", "Hansen"), (result.Value.FirstName, result.Value.LastName));
        Assert.True(member.HasName);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal((StaffAuditAction.NameChanged, member.Id, member.Id), (recorded.Action, recorded.ActorId, recorded.TargetId));
    }

    /// <summary>Negative: once there is a name, the member cannot change it alone.</summary>
    [Fact]
    public async Task SetOwnNameAsync_NameAlreadySet_ReturnsNameAlreadySetAndKeepsIt()
    {
        // Arrange
        var (service, _, member) = BuildSelf();
        member.Rename("Rita", "Hansen");

        // Act
        var result = await service.SetOwnNameAsync(new RenameStaffRequest("Other", "Name"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.NameAlreadySet, result.FirstError?.Code);
        Assert.Equal("Rita", member.FirstName);
    }

    /// <summary>Negative: nobody signed in is unauthorised.</summary>
    [Fact]
    public async Task SetOwnNameAsync_NoCaller_ReturnsStaffUnauthorized()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.StaffAuthService(new FakePasswordHasher());

        // Act
        var result = await service.SetOwnNameAsync(new RenameStaffRequest("Rita", "Hansen"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.StaffUnauthorized, result.FirstError?.Code);
    }

    /// <summary>Positive: the sign-in tells whether the account has a name.</summary>
    [Fact]
    public async Task SignInAsync_NamedAccount_ReturnsTheNames()
    {
        // Arrange
        var world = new ServiceWorld();
        Add(world, "rita", StaffRole.Receptionist, firstName: "Rita", lastName: "Hansen");
        var service = world.StaffAuthService(new FakePasswordHasher());

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", Password), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(("Rita", "Hansen"), (result.Value.FirstName, result.Value.LastName));
        Assert.Equal(world.Staff.Items.Single().Id, result.Value.StaffId);
    }

    #endregion

    #region Helpers

    private static (StaffAccountService Service, ServiceWorld World, StaffMember Caller) Build(
        StaffRole callerRole = StaffRole.Manager,
        bool allowAll = false)
    {
        var world = new ServiceWorld();
        var caller = Add(world, "manager", callerRole);
        world.CallContext.StaffId = caller.Id;
        world.CallContext.StaffName = caller.UserName;
        IStaffAuthorizationService authorization = new StaffAuthorizationService(world.CallContext, world.Staff);
        if (allowAll)
        {
            authorization = Substitute.For<IStaffAuthorizationService>();
            authorization.HasPermissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        }

        return (world.StaffAccountService(authorization: authorization), world, caller);
    }

    private static (StaffAuthService Service, ServiceWorld World, StaffMember Member) BuildSelf()
    {
        var world = new ServiceWorld();
        var member = Add(world, "rita", StaffRole.Housekeeping);
        world.CallContext.StaffId = member.Id;
        world.CallContext.StaffName = member.UserName;
        return (world.StaffAuthService(new FakePasswordHasher()), world, member);
    }

    private static StaffMember Add(
        ServiceWorld world,
        string name,
        StaffRole role,
        string? firstName = null,
        string? lastName = null,
        string? email = null)
    {
        var member = StaffMember.Create(name, new FakePasswordHasher().Hash(Password), role, firstName: firstName, lastName: lastName, email: email);
        world.Staff.Add(member);
        return member;
    }

    #endregion
}
