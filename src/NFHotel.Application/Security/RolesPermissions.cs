using System;


namespace NFHotel.Application.Security;


/// <summary>
/// Definerer hvilke permissions de forskellige roller har
/// ud fra den godkendte RBAC-matrix.
/// </summary>
public static class RolePermissions
{
    /// <summary>
    /// Opretter en Dictionary der kobler hver rolle sammen med et sæt permissions (tilladelser).
    /// string = rollen, fx "Guest" eller "Manager".
    /// HashSet af string = et sæt tilladelser uden dubletter.
    /// </summary>
    private static readonly Dictionary<string,HashSet<string>> PermissionsByRole = new()
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
            Permissions.UpdateHousekeepingStatus
        ],

        [Roles.Housekeeping] =
        [
            Permissions.ViewHousekeeping,
            Permissions.UpdateHousekeepingStatus
        ]
    };

    /// <summary>
    /// Kontrollerer om en rolle har en bestemt permission.
    /// </summary>
    public static bool HasPermission(string role, string permission)
    {
        // TryGetValue forsøger at finde rollen i Dictionary.
        // Hvis rollen findes, gemmes dens permissions i variablen permissions.
        // Contains kontrollerer om den ønskede permission findes i sættet.
        return PermissionsByRole.TryGetValue(role, out var permissions)
            && permissions.Contains(permission);
    }
}
