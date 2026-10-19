

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// Definerer systemets adgangsrettigheder(permissions ) ud fra RBAC-matrixen.
/// </summary>
public static class Permissions
{

    /// <summary>
    /// Tilladelse til at oprette en booking.
    /// </summary>
    public const string CreateBooking = "Bookings.Create";

    /// <summary>
    /// Tilladelse til at ændre sin egen booking.
    /// </summary>
    public const string EditOwnBooking = "Bookings.EditOwn";

    /// <summary>
    /// Tilladelse til at ændre andre gæsters bookinger.
    /// </summary>
    public const string EditOtherBookings = "Bookings.EditOthers";

    /// <summary>
    /// Tilladelse til at se egne gæsteoplysninger.
    /// </summary>
    public const string ViewOwnGuestData = "Guests.ViewOwn";

    /// <summary>
    /// Tilladelse til at se andre gæsters oplysninger.
    /// </summary>
    public const string ViewOtherGuestData = "Guests.ViewOthers";

    /// <summary>
    /// Tilladelse til at se egne passportdata.
    /// </summary>
    public const string ViewOwnPassportData = "Guests.Passport.ViewOwn";

    /// <summary>
    /// Tilladelse til at se hotelgæsters pasdata som autoriseret personale.
    /// </summary>
    public const string ViewGuestPassportData = "Guests.Passport.ViewGuest";


    /// <summary>
    /// Tilladelse til at ændre priser og rabatter.
    /// </summary>
    public const string ManagePrices = "Prices.Manage";

    /// <summary>
    /// Tilladelse til at se housekeeping-information.
    /// </summary>
    public const string ViewHousekeeping = "Housekeeping.View";

    /// <summary>
    /// Tilladelse til at ændre housekeeping-status.
    /// </summary>
    public const string UpdateHousekeepingStatus = "Housekeeping.UpdateStatus";

    /// <summary>
    /// Tilladelse til at se bookinger i back office (ADR-0005).
    /// </summary>
    public const string BookingsView = "Bookings.View";

    /// <summary>
    /// Tilladelse til at ændre og slette gæster i back office (ADR-0005).
    /// </summary>
    public const string GuestsManage = "Guests.Manage";

    /// <summary>
    /// Tilladelse til at se værelser og deres status (ADR-0005).
    /// </summary>
    public const string RoomsView = "Rooms.View";

    /// <summary>
    /// Tilladelse til at oprette, ændre og slette værelser (ADR-0005).
    /// </summary>
    public const string RoomsManage = "Rooms.Manage";

    /// <summary>
    /// Tilladelse til at se omsætningen (ADR-0005).
    /// </summary>
    public const string SalesView = "Sales.View";

    /// <summary>
    /// Tilladelse til at se de gemte benchmark-kørsler i back office (ADR-0008).
    /// </summary>
    public const string BenchmarkView = "Benchmark.View";

    /// <summary>
    /// Tilladelse til at starte en benchmark-kørsel fra back office (ADR-0008, MIL-020).
    /// </summary>
    public const string BenchmarkRun = "Benchmark.Run";

    /// <summary>
    /// Alle permissions, til at bygge én autorisationspolitik pr. permission.
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
    ];
}