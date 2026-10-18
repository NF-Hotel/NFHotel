using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Staff;

/// <summary>The rules of a staff account (ADR-0005): a name, a hash, a known role, and a name that does not depend on case.</summary>
public sealed class StaffMemberTests
{
    #region Creation

    /// <summary>Positive: a valid member is active and keeps its role and hash.</summary>
    [Fact]
    public void Create_ValidFields_ReturnsAnActiveMember()
    {
        // Arrange and act
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Assert
        Assert.Equal("rita", member.UserName);
        Assert.Equal("hash-1", member.PasswordHash);
        Assert.Equal(StaffRole.Receptionist, member.Role);
        Assert.True(member.IsActive);
    }

    /// <summary>Equivalence: the name is trimmed and lower-cased, so the sign-in does not depend on case.</summary>
    [Theory]
    [InlineData("Rita", "rita")]
    [InlineData("  RITA  ", "rita")]
    [InlineData("Rita.Hansen", "rita.hansen")]
    public void Create_NameInAnyCase_StoresTheNormalisedName(string typed, string stored)
    {
        // Arrange and act
        var member = StaffMember.Create(typed, "hash-1", StaffRole.Manager);

        // Assert
        Assert.Equal(stored, member.UserName);
    }

    #endregion

    #region Invalid fields

    /// <summary>Negative: an empty or blank name is refused with its code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsTheNameRequiredCode(string name)
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => StaffMember.Create(name, "hash-1", StaffRole.Manager));

        // Assert
        Assert.Contains(StaffErrors.UserNameRequired, error.Codes);
    }

    /// <summary>Boundary: a name of exactly the longest length is accepted, one character more is refused.</summary>
    [Fact]
    public void Create_NameAtAndAboveTheLimit_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange
        var atLimit = new string('a', StaffMember.MaxUserNameLength);
        var overLimit = new string('a', StaffMember.MaxUserNameLength + 1);

        // Act
        var accepted = StaffMember.Create(atLimit, "hash-1", StaffRole.Manager);
        var error = Assert.Throws<DomainException>(() => StaffMember.Create(overLimit, "hash-1", StaffRole.Manager));

        // Assert
        Assert.Equal(atLimit, accepted.UserName);
        Assert.Contains(StaffErrors.UserNameTooLong, error.Codes);
    }

    /// <summary>Negative: a member without a password hash is refused, so an account can never be open to any password.</summary>
    [Fact]
    public void Create_EmptyHash_ThrowsTheHashRequiredCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => StaffMember.Create("rita", string.Empty, StaffRole.Manager));

        // Assert
        Assert.Contains(StaffErrors.PasswordHashRequired, error.Codes);
    }

    /// <summary>Negative: a number that is not a role is refused.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Create_UnknownRole_ThrowsTheRoleInvalidCode(int role)
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => StaffMember.Create("rita", "hash-1", (StaffRole)role));

        // Assert
        Assert.Contains(StaffErrors.RoleInvalid, error.Codes);
    }

    #endregion

    #region Deactivation

    /// <summary>State transition: a deactivated member is inactive, and deactivating again changes nothing.</summary>
    [Fact]
    public void Deactivate_CalledTwice_LeavesTheMemberInactive()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Manager);

        // Act
        member.Deactivate();
        member.Deactivate();

        // Assert
        Assert.False(member.IsActive);
    }

    #endregion
}