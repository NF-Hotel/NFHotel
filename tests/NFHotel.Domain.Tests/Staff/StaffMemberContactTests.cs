using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Staff;

/// <summary>The name, the e-mail address and the role of a staff account (MIL-059).</summary>
public sealed class StaffMemberContactTests
{
    #region Creation

    /// <summary>Positive: a member created with a name and an address keeps them, trimmed, and the address in lower case.</summary>
    [Fact]
    public void Create_WithNameAndEmail_KeepsThemNormalised()
    {
        // Arrange and act
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, firstName: " Rita ", lastName: "Hansen ", email: " Rita@Hotel.DK ");

        // Assert
        Assert.Equal("Rita", member.FirstName);
        Assert.Equal("Hansen", member.LastName);
        Assert.Equal("rita@hotel.dk", member.Email);
        Assert.True(member.HasName);
    }

    /// <summary>Positive: a member created without a name has none, so the app asks for it at the first sign-in.</summary>
    [Fact]
    public void Create_WithoutName_HasNoName()
    {
        // Arrange and act
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Assert
        Assert.Null(member.FirstName);
        Assert.Null(member.LastName);
        Assert.Null(member.Email);
        Assert.False(member.HasName);
    }

    /// <summary>Negative: one name without the other is refused with the code of the missing one.</summary>
    [Theory]
    [InlineData("Rita", null, StaffErrors.LastNameRequired)]
    [InlineData(null, "Hansen", StaffErrors.FirstNameRequired)]
    [InlineData("Rita", "  ", StaffErrors.LastNameRequired)]
    public void Create_OnlyOneName_ThrowsTheCodeOfTheMissingName(string? first, string? last, string code)
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, firstName: first, lastName: last));

        // Assert
        Assert.Equal([code], error.Codes);
    }

    /// <summary>Negative: an address without an at sign is refused.</summary>
    [Fact]
    public void Create_InvalidEmail_ThrowsTheEmailInvalidCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, email: "rita.hotel.dk"));

        // Assert
        Assert.Contains(StaffErrors.EmailInvalid, error.Codes);
    }

    #endregion

    #region Rename

    /// <summary>Positive: a rename sets both names, trimmed, and does not end the sessions.</summary>
    [Fact]
    public void Rename_ValidNames_SetsBothAndKeepsTheStamp()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Act
        member.Rename(" Rita ", " Hansen");

        // Assert
        Assert.Equal("Rita", member.FirstName);
        Assert.Equal("Hansen", member.LastName);
        Assert.Equal(1, member.SecurityStamp);
    }

    /// <summary>Negative: both empty names are refused with both codes, and the old names stay.</summary>
    [Fact]
    public void Rename_EmptyNames_ThrowsBothCodesAndKeepsTheOldNames()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, firstName: "Rita", lastName: "Hansen");

        // Act
        var error = Assert.Throws<DomainException>(() => member.Rename(" ", null));

        // Assert
        Assert.Equal([StaffErrors.FirstNameRequired, StaffErrors.LastNameRequired], error.Codes);
        Assert.Equal("Rita", member.FirstName);
        Assert.Equal("Hansen", member.LastName);
    }

    /// <summary>Boundary: a name of exactly the longest length is accepted, one character more is refused and rolled back.</summary>
    [Fact]
    public void Rename_NameAtAndAboveTheLimit_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);
        var longest = new string('a', StaffMember.MaxNameLength);

        // Act
        member.Rename(longest, "Hansen");
        var error = Assert.Throws<DomainException>(() => member.Rename(longest + "a", "Hansen"));

        // Assert
        Assert.Contains(StaffErrors.FirstNameTooLong, error.Codes);
        Assert.Equal(longest, member.FirstName);
    }

    #endregion

    #region E-mail

    /// <summary>State transition: an address is set in lower case, and an empty one clears it.</summary>
    [Fact]
    public void ChangeEmail_SetThenEmpty_SetsThenClears()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Act
        member.ChangeEmail("Rita@Hotel.dk");
        var set = member.Email;
        member.ChangeEmail("  ");

        // Assert
        Assert.Equal("rita@hotel.dk", set);
        Assert.Null(member.Email);
    }

    /// <summary>Negative: an invalid address is refused and the old one stays.</summary>
    [Fact]
    public void ChangeEmail_Invalid_ThrowsAndKeepsTheOldAddress()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist, email: "rita@hotel.dk");

        // Act
        var error = Assert.Throws<DomainException>(() => member.ChangeEmail("not an address"));

        // Assert
        Assert.Contains(StaffErrors.EmailInvalid, error.Codes);
        Assert.Equal("rita@hotel.dk", member.Email);
    }

    #endregion

    #region Role

    /// <summary>State transition: another role is set and ends the sessions; the same role again changes nothing.</summary>
    [Fact]
    public void ChangeRole_OtherThenSame_RaisesTheStampOnce()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Act
        member.ChangeRole(StaffRole.Manager);
        member.ChangeRole(StaffRole.Manager);

        // Assert
        Assert.Equal(StaffRole.Manager, member.Role);
        Assert.Equal(2, member.SecurityStamp);
    }

    /// <summary>Negative: an unknown role is refused and the role stays.</summary>
    [Fact]
    public void ChangeRole_UnknownRole_ThrowsAndKeepsTheRole()
    {
        // Arrange
        var member = StaffMember.Create("rita", "hash-1", StaffRole.Receptionist);

        // Act
        var error = Assert.Throws<DomainException>(() => member.ChangeRole((StaffRole)99));

        // Assert
        Assert.Equal([StaffErrors.RoleInvalid], error.Codes);
        Assert.Equal(StaffRole.Receptionist, member.Role);
        Assert.Equal(1, member.SecurityStamp);
    }

    #endregion
}
