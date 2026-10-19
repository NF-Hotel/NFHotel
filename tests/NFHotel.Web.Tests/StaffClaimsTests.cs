using System.Security.Claims;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Security;

namespace NFHotel.Web.Tests;

/// <summary>The claims of a signed-in member of staff in the web application (ADR-0005).</summary>
public sealed class StaffClaimsTests
{
    private static readonly DateTimeOffset Later = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    #region The principal

    /// <summary>Positive: the principal carries the name, the role and the staff token, and is authenticated.</summary>
    [Fact]
    public void CreatePrincipal_Session_CarriesNameRoleAndToken()
    {
        // Arrange
        var session = new StaffSessionDto("the-token", Later, "rita", StaffRole.Receptionist);

        // Act
        var principal = StaffClaims.CreatePrincipal(session);

        // Assert
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal("rita", principal.Identity?.Name);
        Assert.True(principal.IsInRole("Receptionist"));
        Assert.Equal("the-token", principal.FindFirst(StaffClaims.TokenClaim)?.Value);
    }

    /// <summary>Negative: a missing session is a programming error.</summary>
    [Fact]
    public void CreatePrincipal_NullSession_Throws()
    {
        // Arrange and act
        var thrown = Record.Exception(() => StaffClaims.CreatePrincipal(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    #endregion

    #region The role

    /// <summary>Equivalence: the role is read back from the principal for every role.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Housekeeping)]
    public void RoleOf_SignedInUser_ReturnsTheRole(StaffRole role)
    {
        // Arrange
        var principal = StaffClaims.CreatePrincipal(new StaffSessionDto("t", Later, "x", role));

        // Act
        var read = StaffClaims.RoleOf(principal);

        // Assert
        Assert.Equal(role, read);
    }

    /// <summary>Negative: nobody signed in, a role claim without an authenticated identity, and a claim that is not a role name hold no role.</summary>
    [Fact]
    public void RoleOf_AnonymousOrUnknownRole_ReturnsNull()
    {
        // Arrange
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var notAuthenticated = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Manager")]));
        var unknown = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Director")], "test"));
        var number = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "2")], "test"));

        // Act and assert
        Assert.Null(StaffClaims.RoleOf(anonymous));
        Assert.Null(StaffClaims.RoleOf(notAuthenticated));
        Assert.Null(StaffClaims.RoleOf(unknown));
        Assert.Null(StaffClaims.RoleOf(number));
    }

    #endregion

    #region Permissions

    /// <summary>Equivalence: a user holds a permission exactly when the table of the role says so.</summary>
    [Theory]
    [InlineData(StaffRole.Manager, Permissions.SalesView, true)]
    [InlineData(StaffRole.Receptionist, Permissions.SalesView, false)]
    [InlineData(StaffRole.Housekeeping, Permissions.RoomsView, true)]
    [InlineData(StaffRole.Housekeeping, Permissions.ViewOtherGuestData, false)]
    public void Holds_RoleAndPermission_FollowsTheTable(StaffRole role, string permission, bool expected)
    {
        // Arrange
        var principal = StaffClaims.CreatePrincipal(new StaffSessionDto("t", Later, "x", role));

        // Act
        var holds = StaffClaims.Holds(principal, permission);

        // Assert
        Assert.Equal(expected, holds);
    }

    /// <summary>Negative: an anonymous visitor holds no permission.</summary>
    [Fact]
    public void Holds_Anonymous_IsFalseForEveryPermission()
    {
        // Arrange
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var any = Permissions.All.Any(permission => StaffClaims.Holds(anonymous, permission));

        // Assert
        Assert.False(any);
    }

    #endregion
}
