using NFHotel.Domain.ValueObjects;


namespace NFHotel.Application.UseCases.Staff;


/// <summary>
/// Defines which permissions the different roles have
/// from the approved RBAC matrix.
/// </summary>
public static class RolePermissions
{
    /// <summary>
    /// Creates a Dictionary that links each role to a set of permissions.
    /// string = the role, for example "Guest" or "Manager".
    /// HashSet of string = a set of permissions without duplicates.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> PermissionsByRole = new()
    {
        [Roles.Guest] =
        [
            Permissions.CreateBooking,
            Permissions.EditOwnBooking,
            Permissions.ViewOwnGuestData,
        ],

        [Roles.Receptionist] =
        [
            Permissions.CreateBooking,
            Permissions.EditOwnBooking,
            Permissions.EditOtherBookings,
            Permissions.ViewOwnGuestData,
            Permissions.ViewOtherGuestData,
            Permissions.ViewGuestPassportData,
            Permissions.ViewHousekeeping,
            Permissions.BookingsView,
            Permissions.GuestsManage,
            Permissions.RoomsView,
        ],

        [Roles.Manager] =
        [
            Permissions.CreateBooking,
            Permissions.EditOwnBooking,
            Permissions.EditOtherBookings,
            Permissions.ViewOwnGuestData,
            Permissions.ViewOtherGuestData,
            Permissions.ViewGuestPassportData,
            Permissions.ManagePrices,
            Permissions.ViewHousekeeping,
            Permissions.UpdateHousekeepingStatus,
            Permissions.BookingsView,
            Permissions.GuestsManage,
            Permissions.RoomsView,
            Permissions.RoomsManage,
            Permissions.SalesView,
            Permissions.BenchmarkView,
            Permissions.BenchmarkRun,
            Permissions.BenchmarkDelete,
            Permissions.AiDefaultsManage
        ],

        [Roles.Housekeeping] =
        [
            Permissions.ViewHousekeeping,
            Permissions.UpdateHousekeepingStatus,
            Permissions.RoomsView
        ]
    };

    /// <summary>
    /// Checks whether a role has a given permission.
    /// </summary>
    public static bool HasPermission(string role, string permission)
    {
        // TryGetValue tries to find the role in the Dictionary.
        // If the role exists, its permissions are stored in the variable permissions.
        // Contains checks whether the wanted permission is in the set.
        return PermissionsByRole.TryGetValue(role, out var permissions)
            && permissions.Contains(permission);
    }

    /// <summary>
    /// Kontrollerer om en medarbejderrolle har en bestemt permission.
    /// </summary>
    public static bool HasPermission(StaffRole role, string permission) =>
        HasPermission(role.ToString(), permission);

    /// <summary>
    /// The page that a role starts on after signing in: the dashboard if it may see bookings, otherwise the rooms.
    /// </summary>
    public static string LandingPath(StaffRole role) =>
        HasPermission(role, Permissions.BookingsView) ? "/admin" : "/admin/rooms";
}
