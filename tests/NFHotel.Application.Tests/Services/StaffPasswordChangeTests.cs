using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>The change of the own password and the audit of the sign-in (UC-005, ADR-0015).</summary>
public sealed class StaffPasswordChangeTests
{
    private const string Current = "correct horse battery staple";
    private const string Fresh = "another long passphrase 42";

    #region Change

    /// <summary>Positive: the right current password and a good new one store the hash, raise the stamp and give a new session.</summary>
    [Fact]
    public async Task ChangePasswordAsync_Valid_StoresTheHashAndReturnsANewSession()
    {
        // Arrange
        var (service, world, member) = Build(mustChange: true);

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest(Current, Fresh), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.MustChangePassword);
        Assert.Equal(new FakePasswordHasher().Hash(Fresh), member.PasswordHash);
        Assert.False(member.MustChangePassword);
        Assert.Equal(2, member.SecurityStamp);
        Assert.Contains(member, world.Staff.Updated);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a wrong current password changes nothing, counts a failure and is recorded.</summary>
    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_RefusesCountsAndRecords()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        var (service, world, member) = Build(throttle: throttle);

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest("not the password", Fresh), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CurrentPasswordInvalid, result.FirstError?.Code);
        Assert.Equal(new FakePasswordHasher().Hash(Current), member.PasswordHash);
        Assert.Equal(1, member.SecurityStamp);
        throttle.Received(1).RecordFailure($"pwd:{member.Id}");
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.PasswordChangeFailed, recorded.Action);
        Assert.Equal(ErrorCodes.Staff.CurrentPasswordInvalid, recorded.Outcome);
    }

    /// <summary>Negative: a blocked account is refused before the password is checked.</summary>
    [Fact]
    public async Task ChangePasswordAsync_BlockedAccount_ReturnsTooManyAttemptsWithoutCheckingThePassword()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        var hasher = Substitute.For<IPasswordHasher>();
        var (service, _, member) = Build(throttle: throttle, hasher: hasher);
        throttle.IsBlocked($"pwd:{member.Id}").Returns(true);

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest(Current, Fresh), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.TooManyAttempts, result.FirstError?.Code);
        hasher.DidNotReceiveWithAnyArgs().Verify(default, default!);
    }

    /// <summary>Equivalence: each rule of the policy refuses the new password and changes nothing.</summary>
    [Theory]
    [InlineData("short", ErrorCodes.Staff.PasswordInvalid)]
    [InlineData("rita", ErrorCodes.Staff.PasswordInvalid)]
    [InlineData(Current, ErrorCodes.Staff.PasswordSameAsCurrent)]
    [InlineData("password1234", ErrorCodes.Staff.PasswordTooCommon)]
    public async Task ChangePasswordAsync_NewPasswordBreaksARule_ReturnsTheCode(string fresh, string code)
    {
        // Arrange
        var (service, _, member) = Build();

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest(Current, fresh), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
        Assert.Equal(1, member.SecurityStamp);
    }

    /// <summary>Negative: nobody signed in, an unknown account and a disabled account all get the staff-unauthorized code.</summary>
    [Fact]
    public async Task ChangePasswordAsync_NoUsableCaller_ReturnsStaffUnauthorized()
    {
        // Arrange
        var (service, world, member) = Build();
        var request = new ChangePasswordRequest(Current, Fresh);

        // Act and assert
        world.CallContext.StaffId = null;
        Assert.Equal(ErrorCodes.Api.StaffUnauthorized, (await service.ChangePasswordAsync(request, TestContext.Current.CancellationToken)).FirstError?.Code);
        world.CallContext.StaffId = 999;
        Assert.Equal(ErrorCodes.Api.StaffUnauthorized, (await service.ChangePasswordAsync(request, TestContext.Current.CancellationToken)).FirstError?.Code);
        world.CallContext.StaffId = member.Id;
        member.Deactivate();
        Assert.Equal(ErrorCodes.Api.StaffUnauthorized, (await service.ChangePasswordAsync(request, TestContext.Current.CancellationToken)).FirstError?.Code);
    }

    /// <summary>Boundary: a current password above the longest length is refused without hashing it.</summary>
    [Fact]
    public async Task ChangePasswordAsync_CurrentPasswordOverTheLimit_IsRefusedWithoutHashing()
    {
        // Arrange
        var hasher = Substitute.For<IPasswordHasher>();
        var (service, _, _) = Build(hasher: hasher);

        // Act
        var result = await service.ChangePasswordAsync(new ChangePasswordRequest(new string('x', 257), Fresh), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.CurrentPasswordInvalid, result.FirstError?.Code);
        hasher.DidNotReceiveWithAnyArgs().Verify(default, default!);
    }

    /// <summary>Negative: a null request is a programming error.</summary>
    [Fact]
    public async Task ChangePasswordAsync_NullRequest_Throws()
    {
        var (service, _, _) = Build();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ChangePasswordAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Audit of the sign-in

    /// <summary>Positive: a sign-in is recorded with the account as actor and target, and the address of the call.</summary>
    [Fact]
    public async Task SignInAsync_Success_RecordsTheSignIn()
    {
        // Arrange
        var (service, world, member) = Build();
        world.CallContext.RemoteAddress = "203.0.113.7";

        // Act
        await service.SignInAsync(new SignInRequest("rita", Current), TestContext.Current.CancellationToken);

        // Assert
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.SignedIn, recorded.Action);
        Assert.Equal(member.Id, recorded.ActorId);
        Assert.Equal("rita", recorded.ActorName);
        Assert.Equal("203.0.113.7", recorded.RemoteAddress);
        Assert.Equal("Success", recorded.Outcome);
    }

    /// <summary>Negative: a wrong password for an existing account is recorded; for an unknown name nothing is recorded.</summary>
    [Fact]
    public async Task SignInAsync_Failures_AreRecordedOnlyForExistingAccounts()
    {
        // Arrange
        var (service, world, _) = Build();

        // Act
        await service.SignInAsync(new SignInRequest("rita", "wrong"), TestContext.Current.CancellationToken);
        await service.SignInAsync(new SignInRequest("nobody", "wrong"), TestContext.Current.CancellationToken);

        // Assert
        var recorded = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.SignInFailed, recorded.Action);
        Assert.Equal("rita", recorded.TargetName);
    }

    /// <summary>State transition: the failure that uses up the attempts also records the lockout.</summary>
    [Fact]
    public async Task SignInAsync_TheLastAllowedFailure_RecordsTheLockout()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        throttle.IsBlocked("rita").Returns(false, true);
        var (service, world, _) = Build(throttle: throttle);

        // Act
        await service.SignInAsync(new SignInRequest("rita", "wrong"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([StaffAuditAction.SignInFailed, StaffAuditAction.LockedOut], world.StaffAudit.Events.Select(item => item.Action));
    }

    /// <summary>Negative: a refused attempt on a blocked name writes nothing, so a flood cannot fill the trail.</summary>
    [Fact]
    public async Task SignInAsync_BlockedName_RecordsNothing()
    {
        // Arrange
        var throttle = Substitute.For<ILoginThrottle>();
        throttle.IsBlocked("rita").Returns(true);
        var (service, world, _) = Build(throttle: throttle);

        // Act
        await service.SignInAsync(new SignInRequest("rita", Current), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Security: no record ever holds a password, a hash or a token.</summary>
    [Fact]
    public async Task Audit_AfterSignInAndChange_HoldsNoSecret()
    {
        // Arrange
        var (service, world, _) = Build();

        // Act
        await service.SignInAsync(new SignInRequest("rita", "wrong password here"), TestContext.Current.CancellationToken);
        var session = await service.SignInAsync(new SignInRequest("rita", Current), TestContext.Current.CancellationToken);
        await service.ChangePasswordAsync(new ChangePasswordRequest(Current, Fresh), TestContext.Current.CancellationToken);

        // Assert
        var text = string.Join('|', world.StaffAudit.Events.Select(item =>
            $"{item.ActorName}{item.TargetName}{item.Outcome}{item.RemoteAddress}"));
        Assert.DoesNotContain(Current, text, StringComparison.Ordinal);
        Assert.DoesNotContain(Fresh, text, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong password here", text, StringComparison.Ordinal);
        Assert.DoesNotContain(FakePasswordHasher.Prefix, text, StringComparison.Ordinal);
        Assert.DoesNotContain(session.Value.Token, text, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static (NFHotel.Application.UseCases.Staff.StaffAuthService Service, ServiceWorld World, StaffMember Member) Build(
        bool mustChange = false,
        ILoginThrottle? throttle = null,
        IPasswordHasher? hasher = null)
    {
        var world = new ServiceWorld();
        var member = StaffMember.Create("rita", new FakePasswordHasher().Hash(Current), StaffRole.Receptionist, mustChange);
        world.Staff.Add(member);
        world.CallContext.StaffId = member.Id;
        world.CallContext.StaffName = member.UserName;
        var service = world.StaffAuthService(hasher ?? new FakePasswordHasher(), throttle: throttle);
        return (service, world, member);
    }

    #endregion
}
