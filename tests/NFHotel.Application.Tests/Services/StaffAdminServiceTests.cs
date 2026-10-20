using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>What a Manager does with staff accounts and the audit trail (UC-006, ADR-0015).</summary>
public sealed class StaffAdminServiceTests
{
    private const string Temporary = "temporary passphrase 7";

    #region Create

    /// <summary>Positive: a created account is active, can be told to change its password, and is recorded with the actor.</summary>
    [Fact]
    public async Task CreateAsync_ByAManager_RecordsTheCreationInTheSameSave()
    {
        // Arrange
        var (service, world, manager) = Build();

        // Act
        var result = await service.CreateAsync(new CreateStaffRequest("Lena", Temporary, StaffRole.Receptionist, MustChangePassword: true), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var created = world.Staff.Items.Single(member => member.UserName == "lena");
        Assert.True(created.MustChangePassword);
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.AccountCreated, recorded.Action);
        Assert.Equal(manager.Id, recorded.ActorId);
        Assert.Equal("lena", recorded.TargetName);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a password that breaks the policy creates nothing and records nothing.</summary>
    [Fact]
    public async Task CreateAsync_CommonPassword_CreatesNothing()
    {
        // Arrange
        var (service, world, _) = Build();

        // Act
        var result = await service.CreateAsync(new CreateStaffRequest("lena", "password1234", StaffRole.Receptionist), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.PasswordTooCommon, result.FirstError?.Code);
        Assert.Single(world.Staff.Items);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>State transition: the command on the server has no caller, so the actor is named <c>system</c>.</summary>
    [Fact]
    public async Task CreateAsync_WithoutACaller_RecordsTheSystemAsActor()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.StaffAccountService();

        // Act
        await service.CreateAsync(new CreateStaffRequest("boss", Temporary, StaffRole.Manager), TestContext.Current.CancellationToken);

        // Assert
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal("system", recorded.ActorName);
        Assert.Null(recorded.ActorId);
    }

    /// <summary>Positive: the first Manager asks for a change by default, and not when told otherwise.</summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task EnsureFirstManagerAsync_MustChangeSetting_IsKept(bool? mustChange, bool expected)
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.StaffAccountService();

        // Act
        var result = mustChange is null
            ? await service.EnsureFirstManagerAsync("boss", Temporary, TestContext.Current.CancellationToken)
            : await service.EnsureFirstManagerAsync("boss", Temporary, mustChange.Value, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, world.Staff.Items.Single().MustChangePassword);
    }

    #endregion

    #region Disable and enable

    /// <summary>Positive: disabling ends the sessions (the stamp rises), is recorded, and the account can no longer sign in.</summary>
    [Fact]
    public async Task DisableAsync_OtherAccount_DisablesRaisesTheStampAndRecords()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.DisableAsync(rita.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(rita.IsActive);
        Assert.Equal(2, rita.SecurityStamp);
        Assert.Equal(StaffAuditAction.AccountDisabled, world.StaffAudit.Events.Single().Action);
    }

    /// <summary>Negative: a Manager cannot disable their own account.</summary>
    [Fact]
    public async Task DisableAsync_OwnAccount_IsRefused()
    {
        // Arrange
        var (service, world, manager) = Build();
        Add(world, "second", StaffRole.Manager);

        // Act
        var result = await service.DisableAsync(manager.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CannotDisableSelf, result.FirstError?.Code);
        Assert.True(manager.IsActive);
    }

    /// <summary>Negative: the last active Manager cannot be disabled, but one of two can.</summary>
    [Fact]
    public async Task DisableAsync_TheLastActiveManager_IsRefusedButNotWhenAnotherRemains()
    {
        // Arrange
        var (service, world, _) = Build(callerRole: StaffRole.Receptionist);
        var only = Add(world, "boss", StaffRole.Manager);

        // Act
        var refused = await service.DisableAsync(only.Id, TestContext.Current.CancellationToken);
        var other = Add(world, "boss2", StaffRole.Manager);
        var allowed = await service.DisableAsync(only.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.LastManager, refused.FirstError?.Code);
        Assert.True(allowed.IsSuccess);
        Assert.True(other.IsActive);
    }

    /// <summary>Negative: an unknown account is not found.</summary>
    [Fact]
    public async Task DisableAsync_UnknownAccount_ReturnsNotFound()
    {
        var (service, _, _) = Build();
        var result = await service.DisableAsync(999, TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCodes.Staff.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: enabling needs a new password, clears the limits of the account, and is recorded.</summary>
    [Fact]
    public async Task EnableAsync_DisabledAccount_RequiresAChangeClearsLimitsAndRecords()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        var (service, world, _) = Build(throttle: throttle);
        var rita = Add(world, "rita", StaffRole.Receptionist);
        rita.Deactivate();

        // Act
        var result = await service.EnableAsync(rita.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(rita.IsActive);
        Assert.True(rita.MustChangePassword);
        throttle.Received(1).Reset("rita");
        throttle.Received(1).Reset($"pwd:{rita.Id}");
        Assert.Equal(StaffAuditAction.AccountEnabled, world.StaffAudit.Events.Single().Action);
    }

    #endregion

    #region Reset

    /// <summary>Positive: a reset sets the temporary hash, requires a change, ends sessions, clears limits and records.</summary>
    [Fact]
    public async Task ResetPasswordAsync_OtherAccount_SetsTheTemporaryPassword()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        var (service, world, _) = Build(throttle: throttle);
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.ResetPasswordAsync(rita.Id, new ResetPasswordRequest(Temporary), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new FakePasswordHasher().Hash(Temporary), rita.PasswordHash);
        Assert.True(rita.MustChangePassword);
        Assert.Equal(2, rita.SecurityStamp);
        throttle.Received(1).Reset("rita");
        Assert.Equal(StaffAuditAction.PasswordReset, world.StaffAudit.Events.Single().Action);
    }

    /// <summary>Negative: a reset of the own account is refused, and a weak temporary password changes nothing.</summary>
    [Fact]
    public async Task ResetPasswordAsync_OwnAccountOrWeakPassword_IsRefused()
    {
        // Arrange
        var (service, world, manager) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var own = await service.ResetPasswordAsync(manager.Id, new ResetPasswordRequest(Temporary), TestContext.Current.CancellationToken);
        var weak = await service.ResetPasswordAsync(rita.Id, new ResetPasswordRequest("rita"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CannotResetSelf, own.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, weak.FirstError?.Code);
        Assert.Equal(1, rita.SecurityStamp);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Positive: the command on the server resets by name, in any case; an unknown name is not found.</summary>
    [Fact]
    public async Task ResetPasswordByNameAsync_KnownAndUnknownName_ResetsOnlyTheKnownOne()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);

        // Act
        var known = await service.ResetPasswordByNameAsync(" RITA ", Temporary, TestContext.Current.CancellationToken);
        var unknown = await service.ResetPasswordByNameAsync("nobody", Temporary, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(known.IsSuccess);
        Assert.True(rita.MustChangePassword);
        Assert.Equal(ErrorCodes.Staff.NotFound, unknown.FirstError?.Code);
    }

    #endregion

    #region Read

    /// <summary>Positive: the list shows every account by name with its state and never a hash.</summary>
    [Fact]
    public async Task ListAsync_Accounts_AreOrderedByNameWithTheirState()
    {
        // Arrange
        var (service, world, _) = Build();
        var rita = Add(world, "rita", StaffRole.Receptionist);
        rita.Deactivate();

        // Act
        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["manager", "rita"], result.Value.Select(item => item.UserName));
        Assert.False(result.Value.Single(item => item.UserName == "rita").IsActive);
    }

    /// <summary>Boundary: the audit page is newest first, the page size is capped, and a page below one is page one.</summary>
    [Fact]
    public async Task ListAuditAsync_Pages_AreNewestFirstAndCapped()
    {
        // Arrange
        var (service, world, _) = Build();
        for (var number = 1; number <= 5; number++)
        {
            world.StaffAudit.Add(StaffAuditEvent.Create(DateTimeOffset.UnixEpoch.AddMinutes(number), StaffAuditAction.SignedIn, "Success", targetName: $"user{number}"));
        }

        // Act
        var first = await service.ListAuditAsync(1, 2, TestContext.Current.CancellationToken);
        var third = await service.ListAuditAsync(3, 2, TestContext.Current.CancellationToken);
        var capped = await service.ListAuditAsync(0, 10_000, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["user5", "user4"], first.Value.Items.Select(item => item.TargetName));
        Assert.Equal(5, first.Value.Total);
        Assert.Equal(["user1"], third.Value.Items.Select(item => item.TargetName));
        Assert.Equal(1, capped.Value.Page);
        Assert.Equal(StaffAccountService.MaxAuditPageSize, capped.Value.PageSize);
    }

    #endregion

    #region Helpers

    private static (StaffAccountService Service, ServiceWorld World, StaffMember Caller) Build(
        ILoginThrottle? throttle = null,
        StaffRole callerRole = StaffRole.Manager)
    {
        var world = new ServiceWorld();
        var caller = Add(world, "manager", callerRole);
        world.CallContext.StaffId = caller.Id;
        world.CallContext.StaffName = caller.UserName;
        return (world.StaffAccountService(throttle), world, caller);
    }

    private static StaffMember Add(ServiceWorld world, string name, StaffRole role)
    {
        var member = StaffMember.Create(name, new FakePasswordHasher().Hash("correct horse battery staple"), role);
        world.Staff.Add(member);
        return member;
    }

    #endregion
}
