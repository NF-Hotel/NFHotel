

namespace NFHotel.Application.Security;

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

}
