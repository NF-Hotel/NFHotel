using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Web.Security;

/// <summary>The names of the authorization policies, one per <see cref="Permission"/> (ADR-0005), for the page attributes.</summary>
public static class StaffPolicies
{
    /// <summary>See bookings.</summary>
    public const string BookingsView = nameof(Permission.BookingsView);

    /// <summary>Change bookings.</summary>
    public const string BookingsManage = nameof(Permission.BookingsManage);

    /// <summary>See guests.</summary>
    public const string GuestsView = nameof(Permission.GuestsView);

    /// <summary>Change guests.</summary>
    public const string GuestsManage = nameof(Permission.GuestsManage);

    /// <summary>See rooms.</summary>
    public const string RoomsView = nameof(Permission.RoomsView);

    /// <summary>Change rooms and their bookability.</summary>
    public const string RoomsManage = nameof(Permission.RoomsManage);

    /// <summary>Set cleaning and service status.</summary>
    public const string HousekeepingUpdate = nameof(Permission.HousekeepingUpdate);

    /// <summary>See revenue.</summary>
    public const string SalesView = nameof(Permission.SalesView);
}