

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// Defines the permissions of the system from the RBAC matrix.
/// </summary>
public static class Permissions
{

    /// <summary>
    /// Permission to create a booking.
    /// </summary>
    public const string CreateBooking = "Bookings.Create";

    /// <summary>
    /// Permission to change one's own booking.
    /// </summary>
    public const string EditOwnBooking = "Bookings.EditOwn";

    /// <summary>
    /// Permission to change the bookings of other guests.
    /// </summary>
    public const string EditOtherBookings = "Bookings.EditOthers";

    /// <summary>
    /// Permission to see one's own guest details.
    /// </summary>
    public const string ViewOwnGuestData = "Guests.ViewOwn";

    /// <summary>
    /// Permission to see the details of other guests.
    /// </summary>
    public const string ViewOtherGuestData = "Guests.ViewOthers";

    /// <summary>
    /// Permission to see one's own passport data.
    /// </summary>
    public const string ViewOwnPassportData = "Guests.Passport.ViewOwn";

    /// <summary>
    /// Permission to see the passport data of hotel guests as authorised staff.
    /// </summary>
    public const string ViewGuestPassportData = "Guests.Passport.ViewGuest";


    /// <summary>
    /// Permission to change prices and discounts.
    /// </summary>
    public const string ManagePrices = "Prices.Manage";

    /// <summary>
    /// Permission to see housekeeping information.
    /// </summary>
    public const string ViewHousekeeping = "Housekeeping.View";

    /// <summary>
    /// Permission to change the housekeeping status.
    /// </summary>
    public const string UpdateHousekeepingStatus = "Housekeeping.UpdateStatus";

    /// <summary>
    /// Permission to see bookings in the back office (ADR-0005).
    /// </summary>
    public const string BookingsView = "Bookings.View";

    /// <summary>
    /// Permission to change and delete guests in the back office (ADR-0005).
    /// </summary>
    public const string GuestsManage = "Guests.Manage";

    /// <summary>
    /// Permission to see rooms and their status (ADR-0005).
    /// </summary>
    public const string RoomsView = "Rooms.View";

    /// <summary>
    /// Permission to create, change and delete rooms (ADR-0005).
    /// </summary>
    public const string RoomsManage = "Rooms.Manage";

    /// <summary>
    /// Permission to see the revenue (ADR-0005).
    /// </summary>
    public const string SalesView = "Sales.View";

    /// <summary>
    /// Permission to see the stored benchmark runs in the back office (ADR-0013).
    /// </summary>
    public const string BenchmarkView = "Benchmark.View";

    /// <summary>
    /// Permission to start a benchmark run from the back office (ADR-0013, MIL-020).
    /// </summary>
    public const string BenchmarkRun = "Benchmark.Run";

    /// <summary>
    /// Permission to delete a stored benchmark run for good (ADR-0013).
    /// </summary>
    public const string BenchmarkDelete = "Benchmark.Delete";

    /// <summary>
    /// All permissions, to build one authorisation policy per permission.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        CreateBooking,
        EditOwnBooking,
        EditOtherBookings,
        ViewOwnGuestData,
        ViewOtherGuestData,
        ViewOwnPassportData,
        ViewGuestPassportData,
        ManagePrices,
        ViewHousekeeping,
        UpdateHousekeepingStatus,
        BookingsView,
        GuestsManage,
        RoomsView,
        RoomsManage,
        SalesView,
        BenchmarkView,
        BenchmarkRun,
        BenchmarkDelete,
    ];
}
