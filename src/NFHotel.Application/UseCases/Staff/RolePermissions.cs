using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// The one table that says what each role may do (Access-Control-Matrix.md, ADR-0005). Least privilege: a role
/// holds only what its work needs, and a role that is not in the table holds nothing.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<StaffRole, IReadOnlySet<Permission>> Table =
        new Dictionary<StaffRole, IReadOnlySet<Permission>>
        {
            [StaffRole.Receptionist] = new HashSet<Permission>
            {
                Permission.BookingsView,
                Permission.BookingsManage,
                Permission.GuestsView,
                Permission.GuestsManage,
                Permission.RoomsView,
            },
            [StaffRole.Manager] = new HashSet<Permission>(Enum.GetValues<Permission>()),
            [StaffRole.Housekeeping] = new HashSet<Permission>
            {
                Permission.RoomsView,
                Permission.HousekeepingUpdate,
            },
        };

    /// <summary>Tells whether a role holds a permission.</summary>
    /// <param name="role">The role.</param>
    /// <param name="permission">The permission.</param>
    /// <returns>True when the role may.</returns>
    public static bool Allows(StaffRole role, Permission permission) =>
        Table.TryGetValue(role, out var held) && held.Contains(permission);

    /// <summary>Lists what a role holds.</summary>
    /// <param name="role">The role.</param>
    /// <returns>The permissions, empty for an unknown role.</returns>
    public static IReadOnlySet<Permission> For(StaffRole role) =>
        Table.TryGetValue(role, out var held) ? held : new HashSet<Permission>();

    /// <summary>The page that a role starts on after signing in: the dashboard if it may see bookings, else the rooms.</summary>
    /// <param name="role">The role.</param>
    /// <returns>A path of the back office.</returns>
    public static string LandingPath(StaffRole role) =>
        Allows(role, Permission.BookingsView) ? "/admin" : "/admin/rooms";
}