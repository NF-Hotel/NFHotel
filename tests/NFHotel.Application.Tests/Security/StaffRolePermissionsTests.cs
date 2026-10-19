using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// The back-office permissions of ADR-0005 inside the one table of <see cref="RolePermissions"/>: the expected matrix is
/// written out here, role by role, so a changed table fails a test.
/// </summary>
public sealed class StaffRolePermissionsTests
{
    private static readonly string[] BackOffice =
    [
        Permissions.BookingsView,
        Permissions.EditOtherBookings,
        Permissions.ViewOtherGuestData,
        Permissions.GuestsManage,
        Permissions.RoomsView,
        Permissions.RoomsManage,
        Permissions.UpdateHousekeepingStatus,
        Permissions.SalesView,
        Permissions.BenchmarkView,
        Permissions.BenchmarkRun,
        Permissions.BenchmarkDelete,
    ];

    private static readonly Dictionary<StaffRole, string[]> Expected = new()
    {
        [StaffRole.Receptionist] =
        [
            Permissions.BookingsView,
            Permissions.EditOtherBookings,
            Permissions.ViewOtherGuestData,
            Permissions.GuestsManage,
            Permissions.RoomsView,
        ],
        [StaffRole.Manager] = BackOffice,
        [StaffRole.Housekeeping] =
        [
            Permissions.RoomsView,
            Permissions.UpdateHousekeepingStatus,
        ],
    };

    /// <summary>For every staff role and back-office permission, the table says what the matrix says.</summary>
    [Fact]
    public void HasPermission_EveryStaffRoleAndPermission_MatchesTheMatrix()
    {
        // Arrange
        var mismatches = new List<string>();

        // Act
        foreach (var role in Enum.GetValues<StaffRole>())
        {
            foreach (var permission in BackOffice)
            {
                var expected = Expected[role].Contains(permission);
                if (RolePermissions.HasPermission(role, permission) != expected)
                {
                    mismatches.Add($"{role} {permission}: expected {expected}");
                }
            }
        }

        // Assert
        Assert.True(mismatches.Count == 0, string.Join("; ", mismatches));
    }

    /// <summary>Negative: a staff role that is not in the table holds nothing.</summary>
    [Fact]
    public void HasPermission_UnknownStaffRole_IsFalseForEveryPermission()
    {
        // Arrange
        var unknown = (StaffRole)99;

        // Act
        var any = Permissions.All.Any(permission => RolePermissions.HasPermission(unknown, permission));

        // Assert
        Assert.False(any);
    }

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
}
