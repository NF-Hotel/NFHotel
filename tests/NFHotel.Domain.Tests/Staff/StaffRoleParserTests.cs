using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Staff;

/// <summary>A role is read from a claim only by its exact name (ADR-0005), never by number or by a different spelling.</summary>
public sealed class StaffRoleParserTests
{
    #region Names

    /// <summary>Equivalence: the exact name of each role is accepted.</summary>
    [Theory]
    [InlineData("Receptionist", StaffRole.Receptionist)]
    [InlineData("Manager", StaffRole.Manager)]
    [InlineData("Housekeeping", StaffRole.Housekeeping)]
    public void TryParse_ExactName_ReturnsTheRole(string text, StaffRole expected)
    {
        // Arrange and act
        var parsed = StaffRoleParser.TryParse(text, out var role);

        // Assert
        Assert.True(parsed);
        Assert.Equal(expected, role);
    }

    /// <summary>Negative: a number, another case, a list, white space, an empty text and nothing are all refused.</summary>
    [Theory]
    [InlineData("2")]
    [InlineData("manager")]
    [InlineData("Manager,Housekeeping")]
    [InlineData(" Manager")]
    [InlineData("Director")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_AnythingElse_IsRefused(string? text)
    {
        // Arrange and act
        var parsed = StaffRoleParser.TryParse(text, out _);

        // Assert
        Assert.False(parsed);
    }

    #endregion
}