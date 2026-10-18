using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// The permission table equals the Access Control Matrix of <c>docs/refactor/Access-Control-Matrix.md</c> as ADR-0005
/// extends it: the expected matrix is written out here, role by role, so a changed table fails a test.
/// </summary>
public sealed class RolePermissionsTests
{
    private static readonly Dictionary<StaffRole, Permission[]> Expected = new()
    {
        [StaffRole.Receptionist] =
        [
            Permission.BookingsView,
            Permission.BookingsManage,
            Permission.GuestsView,
            Permission.GuestsManage,
            Permission.RoomsView,
        ],
        [StaffRole.Manager] =
        [
            Permission.BookingsView,
            Permission.BookingsManage,
            Permission.GuestsView,
            Permission.GuestsManage,
            Permission.RoomsView,
            Permission.RoomsManage,
            Permission.HousekeepingUpdate,
            Permission.SalesView,
        ],
        [StaffRole.Housekeeping] =
        [
            Permission.RoomsView,
            Permission.HousekeepingUpdate,
        ],
    };

    #region The matrix

    /// <summary>Every role holds exactly the permissions the matrix gives it, and no more.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Housekeeping)]
    public void For_EachRole_HoldsExactlyTheMatrix(StaffRole role)
    {
        // Arrange and act
        var held = RolePermissions.For(role);

        // Assert
        Assert.Equal(Expected[role].Order(), held.Order());
    }

    /// <summary>For every role and every permission, Allows says what the matrix says (allowed or refused).</summary>
    [Fact]
    public void Allows_EveryRoleAndPermission_MatchesTheMatrix()
    {
        // Arrange
        var mismatches = new List<string>();

        // Act
        foreach (var role in Enum.GetValues<StaffRole>())
        {
            foreach (var permission in Enum.GetValues<Permission>())
            {
                var expected = Expected[role].Contains(permission);
                if (RolePermissions.Allows(role, permission) != expected)
                {
                    mismatches.Add($"{role} {permission}: expected {expected}");
                }
            }
        }

        // Assert
        Assert.True(mismatches.Count == 0, string.Join("; ", mismatches));
    }

    /// <summary>Least privilege: Housekeeping holds nothing about bookings, guests or revenue, and so no passport number.</summary>
    [Theory]
    [InlineData(Permission.BookingsView)]
    [InlineData(Permission.BookingsManage)]
    [InlineData(Permission.GuestsView)]
    [InlineData(Permission.GuestsManage)]
    [InlineData(Permission.SalesView)]
    [InlineData(Permission.RoomsManage)]
    public void Allows_HousekeepingAndAGuestOrBookingPermission_IsFalse(Permission permission)
    {
        // Arrange and act
        var allowed = RolePermissions.Allows(StaffRole.Housekeeping, permission);

        // Assert
        Assert.False(allowed);
    }

    /// <summary>Least privilege: a Receptionist cannot change rooms, set cleaning status or see revenue.</summary>
    [Theory]
    [InlineData(Permission.RoomsManage)]
    [InlineData(Permission.HousekeepingUpdate)]
    [InlineData(Permission.SalesView)]
    public void Allows_ReceptionistAndARoomOrRevenuePermission_IsFalse(Permission permission)
    {
        // Arrange and act
        var allowed = RolePermissions.Allows(StaffRole.Receptionist, permission);

        // Assert
        Assert.False(allowed);
    }

    /// <summary>Negative: a role that is not in the table holds nothing.</summary>
    [Fact]
    public void Allows_UnknownRole_IsFalseForEveryPermission()
    {
        // Arrange
        var unknown = (StaffRole)99;

        // Act
        var any = Enum.GetValues<Permission>().Any(permission => RolePermissions.Allows(unknown, permission));

        // Assert
        Assert.False(any);
        Assert.Empty(RolePermissions.For(unknown));
    }

    #endregion

    #region Landing page

    /// <summary>Equivalence: roles that may see bookings start on the dashboard, Housekeeping on the rooms.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist, "/admin")]
    [InlineData(StaffRole.Manager, "/admin")]
    [InlineData(StaffRole.Housekeeping, "/admin/rooms")]
    public void LandingPath_EachRole_IsAPageTheRoleMayOpen(StaffRole role, string expected)
    {
        // Arrange and act
        var path = RolePermissions.LandingPath(role);

        // Assert
        Assert.Equal(expected, path);
    }

    #endregion
}