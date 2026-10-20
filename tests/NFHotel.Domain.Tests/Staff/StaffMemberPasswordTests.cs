using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Staff;

/// <summary>The password, the "must change" flag and the security stamp of a staff account (ADR-0015).</summary>
public sealed class StaffMemberPasswordTests
{
    #region Creation

    /// <summary>Positive: a new account starts at stamp 1, and asks for a change only when it is told to.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_MustChangeFlag_IsKeptAndTheStampStartsAtOne(bool mustChange)
    {
        // Arrange and act
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, mustChange);

        // Assert
        Assert.Equal(mustChange, member.MustChangePassword);
        Assert.Equal(1, member.SecurityStamp);
    }

    #endregion

    #region Change and reset

    /// <summary>State transition: a change stores the new hash, clears the duty to change and raises the stamp.</summary>
    [Fact]
    public void ChangePassword_ValidHash_StoresItClearsTheFlagAndRaisesTheStamp()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager, mustChangePassword: true);

        // Act
        member.ChangePassword("hash-2");

        // Assert
        Assert.Equal("hash-2", member.PasswordHash);
        Assert.False(member.MustChangePassword);
        Assert.Equal(2, member.SecurityStamp);
    }

    /// <summary>State transition: a reset stores the temporary hash, requires a change and raises the stamp.</summary>
    [Fact]
    public void ResetPassword_ValidHash_StoresItRequiresAChangeAndRaisesTheStamp()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);

        // Act
        member.ResetPassword("temporary");

        // Assert
        Assert.Equal("temporary", member.PasswordHash);
        Assert.True(member.MustChangePassword);
        Assert.Equal(2, member.SecurityStamp);
    }

    /// <summary>Negative: an empty hash is refused and nothing changes, not the hash and not the stamp.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ChangePassword_EmptyHash_ThrowsAndChangesNothing(string? hash)
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);

        // Act
        var exception = Assert.Throws<DomainException>(() => member.ChangePassword(hash!));

        // Assert
        Assert.Contains(StaffErrors.PasswordHashRequired, exception.Codes);
        Assert.Equal("hash-1", member.PasswordHash);
        Assert.Equal(1, member.SecurityStamp);
    }

    /// <summary>Negative: a hash above the column length is refused and the old hash stays.</summary>
    [Fact]
    public void ResetPassword_HashTooLong_ThrowsAndKeepsTheOldHash()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);

        // Act
        Assert.Throws<DomainException>(() => member.ResetPassword(new string('h', StaffMember.MaxPasswordHashLength + 1)));

        // Assert
        Assert.Equal("hash-1", member.PasswordHash);
        Assert.False(member.MustChangePassword);
    }

    #endregion

    #region Disable and enable

    /// <summary>State transition: disabling stops the sign-in and raises the stamp, once.</summary>
    [Fact]
    public void Deactivate_ActiveMember_RaisesTheStampOnlyOnce()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);

        // Act
        member.Deactivate();
        member.Deactivate();

        // Assert
        Assert.False(member.IsActive);
        Assert.Equal(2, member.SecurityStamp);
    }

    /// <summary>State transition: enabling a disabled member requires a new password and raises the stamp, once.</summary>
    [Fact]
    public void Activate_DisabledMember_RequiresAChangeAndRaisesTheStampOnlyOnce()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);
        member.Deactivate();

        // Act
        member.Activate();
        member.Activate();

        // Assert
        Assert.True(member.IsActive);
        Assert.True(member.MustChangePassword);
        Assert.Equal(3, member.SecurityStamp);
    }

    #endregion
}
