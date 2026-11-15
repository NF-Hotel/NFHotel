using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// The ownership rule of the guest sign-in (MIL-018, ADR-0047 decision 5): a signed-in guest reaches only the booking
/// in the guest token, and only with a permission that the Guest role holds in the one permission table
/// (<see cref="RolePermissions"/>). The API checks the role at the door; this rule is the check in the use case.
/// </summary>
public static class GuestOwnership
{
    #region Public methods

    /// <summary>Tells whether the calling guest may use a permission on a booking.</summary>
    /// <param name="guest">The guest who is calling, if any.</param>
    /// <param name="bookingId">The key of the booking that the call is about.</param>
    /// <param name="permission">The permission that the call needs, from <see cref="Permissions"/>.</param>
    /// <returns>True when the call carries a guest token for exactly this booking and the Guest role holds the permission.</returns>
    public static bool Allows(IGuestCallContext guest, int bookingId, string permission)
    {
        ArgumentNullException.ThrowIfNull(guest);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return guest.BookingId is int own
            && own == bookingId
            && RolePermissions.HasPermission(Roles.Guest, permission);
    }

    #endregion Public methods
}
