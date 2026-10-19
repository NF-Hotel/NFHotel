using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Web.Security;

/// <summary>The names of the authorization policies, one per <see cref="Permissions"/> (ADR-0005), for the page attributes.</summary>
public static class StaffPolicies
{
    /// <summary>See bookings.</summary>
    public const string BookingsView = Permissions.BookingsView;

    /// <summary>Change bookings.</summary>
    public const string BookingsManage = Permissions.EditOtherBookings;

    /// <summary>See guests.</summary>
    public const string GuestsView = Permissions.ViewOtherGuestData;

    /// <summary>Change guests.</summary>
    public const string GuestsManage = Permissions.GuestsManage;

    /// <summary>See rooms.</summary>
    public const string RoomsView = Permissions.RoomsView;

    /// <summary>Change rooms and their bookability.</summary>
    public const string RoomsManage = Permissions.RoomsManage;

    /// <summary>Set cleaning and service status.</summary>
    public const string HousekeepingUpdate = Permissions.UpdateHousekeepingStatus;

    /// <summary>See revenue.</summary>
    public const string SalesView = Permissions.SalesView;

    /// <summary>See the stored benchmark runs.</summary>
    public const string BenchmarkView = Permissions.BenchmarkView;

    /// <summary>Start a benchmark run.</summary>
    public const string BenchmarkRun = Permissions.BenchmarkRun;
}
