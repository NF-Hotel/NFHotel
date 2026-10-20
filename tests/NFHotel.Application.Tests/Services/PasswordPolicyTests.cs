using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Application.Tests.Services;

/// <summary>The one rule for a new staff password (ADR-0015): length, user name, current password, common passwords.</summary>
public sealed class PasswordPolicyTests
{
    private const string Good = "correct horse battery staple";

    #region Accepted

    /// <summary>Positive: a long phrase that is none of the refused kinds passes.</summary>
    [Fact]
    public void Check_GoodPassword_ReturnsNull() =>
        Assert.Null(PasswordPolicy.Check(Good, "rita", "another current one"));

    /// <summary>Boundary: exactly the shortest and exactly the longest length pass.</summary>
    [Theory]
    [InlineData(PasswordPolicy.MinLength)]
    [InlineData(PasswordPolicy.MaxLength)]
    public void Check_AtTheLimits_ReturnsNull(int length) =>
        Assert.Null(PasswordPolicy.Check(new string('a', length - 1) + "b", "rita"));

    #endregion

    #region Refused

    /// <summary>Boundary: one character too few or too many is refused with the length code; a missing password too.</summary>
    [Theory]
    [InlineData(PasswordPolicy.MinLength - 1)]
    [InlineData(PasswordPolicy.MaxLength + 1)]
    [InlineData(1)]
    public void Check_OutsideTheLimits_ReturnsTheLengthCode(int length) =>
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, PasswordPolicy.Check(new string('a', length - 1) + "b", "rita"));

    /// <summary>Negative: an empty password is refused with the length code.</summary>
    [Fact]
    public void Check_EmptyPassword_ReturnsTheLengthCode() =>
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, PasswordPolicy.Check(string.Empty, "rita"));

    /// <summary>Negative: a null password is refused with the length code.</summary>
    [Fact]
    public void Check_NullPassword_ReturnsTheLengthCode() =>
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, PasswordPolicy.Check(null, "rita"));

    /// <summary>Equivalence: the user name is refused in any case and with spaces around it.</summary>
    [Theory]
    [InlineData("rita.hansen.1")]
    [InlineData("RITA.HANSEN.1")]
    public void Check_SameAsTheUserName_ReturnsTheUserNameCode(string typed) =>
        Assert.Equal(ErrorCodes.Staff.PasswordSameAsUserName, PasswordPolicy.Check(typed, "  Rita.Hansen.1 "));

    /// <summary>Negative: the current password is refused as the new one, but is compared exactly.</summary>
    [Fact]
    public void Check_SameAsTheCurrentPassword_ReturnsTheCurrentCode()
    {
        Assert.Equal(ErrorCodes.Staff.PasswordSameAsCurrent, PasswordPolicy.Check(Good, "rita", Good));
        Assert.Null(PasswordPolicy.Check(Good, "rita", Good.ToUpperInvariant()));
    }

    /// <summary>Equivalence: a listed password is refused in any case; so is one repeated character.</summary>
    [Theory]
    [InlineData("password1234")]
    [InlineData("PassWord1234")]
    [InlineData("qwertyuiop12")]
    [InlineData("aaaaaaaaaaaaaaaa")]
    [InlineData("111111111111")]
    public void Check_CommonPassword_ReturnsTheCommonCode(string typed) =>
        Assert.Equal(ErrorCodes.Staff.PasswordTooCommon, PasswordPolicy.Check(typed, "rita"));

    /// <summary>The rules are checked in order: length first, then the user name, then the current password.</summary>
    [Fact]
    public void Check_BreaksSeveralRules_ReturnsTheFirst() =>
        Assert.Equal(ErrorCodes.Staff.PasswordInvalid, PasswordPolicy.Check("rita", "rita", "rita"));

    #endregion
}
