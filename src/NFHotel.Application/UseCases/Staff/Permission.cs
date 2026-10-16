namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// What a member of staff may do in the back office. The API checks one permission per staff endpoint and the web
/// application builds its page policies and its menu from the same names (ADR-0005).
/// </summary>
public enum Permission
{
    /// <summary>See the bookings of a period.</summary>
    BookingsView,

    /// <summary>Confirm, check in, check out, cancel and reschedule bookings.</summary>
    BookingsManage,

    /// <summary>See guests, including the passport number.</summary>
    GuestsView,

    /// <summary>Change and delete guests.</summary>
    GuestsManage,

    /// <summary>See rooms and their status.</summary>
    RoomsView,

    /// <summary>Create, change and delete rooms and set their bookability.</summary>
    RoomsManage,

    /// <summary>Set the cleaning and service status of rooms.</summary>
    HousekeepingUpdate,

    /// <summary>See the revenue.</summary>
    SalesView
}