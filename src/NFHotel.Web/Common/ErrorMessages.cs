using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// Oversætter Application-lagets fejlkoder til danske brugertekster.
/// </summary>
/// <remarks>
/// A-09: koden er kontrakten, teksten er UI'ets ansvar. Derfor findes oversættelsen præcis
/// ét sted — her — og ikke spredt ud i komponenterne. Et senere API mapper de samme koder
/// til HTTP-statusser uden at arve en eneste dansk streng.
/// <para>
/// Ukendte koder (og enhver uventet exception) bliver til <see cref="Unexpected"/>. Vi viser
/// aldrig en rå fejlkode eller en exception-tekst til brugeren, jf. Conventions.md's
/// "Vis brugeren neutrale fejlbeskeder".
/// </para>
/// </remarks>
public static class ErrorMessages
{
    /// <summary>
    /// Den neutrale besked der vises når noget gik galt uden en kendt fejlkode.
    /// </summary>
    public const string Unexpected = "Something went wrong. Please try again.";

    private static readonly IReadOnlyDictionary<string, string> Translations =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ----------------------------------------------------------
            // WEB
            // ----------------------------------------------------------
            [WebErrorCodes.Unexpected] = Unexpected,

            // ----------------------------------------------------------
            // BOOKING
            // ----------------------------------------------------------
            [ErrorCodes.Booking.NotFound] = "We couldn't find that booking.",
            [ErrorCodes.Booking.StartDateRequired] = "Enter a check-in date.",
            [ErrorCodes.Booking.EndDateRequired] = "Enter a check-out date.",
            [ErrorCodes.Booking.EndNotAfterStart] = "The check-out date must be after the check-in date.",
            [ErrorCodes.Booking.StartDateInPast] = "The check-in date can't be in the past.",
            [ErrorCodes.Booking.RoomRequired] = "Select a room.",
            [ErrorCodes.Booking.GuestRequired] = "Select a guest.",
            [ErrorCodes.Booking.CheckOutBeforeCheckIn] = "The guest can't check out before they check in.",
            [ErrorCodes.Booking.CheckOutDateBeforeStartDate] = "The check-out date can't be before the check-in date.",
            [ErrorCodes.Booking.CheckOutDateInFuture] = "The check-out date can't be in the future.",
            [ErrorCodes.Booking.ConfirmNotAllowed] = "This booking can't be confirmed in its current state.",
            [ErrorCodes.Booking.CheckInNotAllowed] = "The guest can't be checked in while the booking is in its current state.",
            [ErrorCodes.Booking.CheckOutNotAllowed] = "The guest can't be checked out while the booking is in its current state.",
            [ErrorCodes.Booking.CancelNotAllowed] = "This booking can't be cancelled in its current state.",
            [ErrorCodes.Booking.RescheduleNotAllowed] = "This booking can't be moved in its current state.",
            [ErrorCodes.Booking.RoomNotAvailable] = "That room is already taken for the selected dates.",
            [ErrorCodes.Booking.RoomNotBookable] = "That room can't be booked right now.",
            [ErrorCodes.Booking.GuestSelectionInvalid] = "Pick an existing guest or create a new one — not both.",
            [ErrorCodes.Booking.ConcurrencyConflict] = "Someone else changed this booking. Reload the page and start over.",

            // ----------------------------------------------------------
            // RUM
            // ----------------------------------------------------------
            [ErrorCodes.Room.NotFound] = "We couldn't find that room.",
            [ErrorCodes.Room.RoomNumberRequired] = "Enter a room number.",
            [ErrorCodes.Room.RoomNumberTooLong] = "That room number is too long.",
            [ErrorCodes.Room.FloorInvalid] = "The floor must be a positive number.",
            [ErrorCodes.Room.SizeInvalid] = "Pick a valid room size.",
            [ErrorCodes.Room.CapacityInvalid] = "The capacity must be a positive number.",
            [ErrorCodes.Room.RoomNumberAlreadyExists] = "A room with that number already exists.",
            [ErrorCodes.Room.HasBookings] = "This room can't be deleted because it still has bookings on it.",
            [ErrorCodes.Room.MarkDailyCleaningDueNotAllowed] = "This room can't be marked for daily cleaning in its current state.",
            [ErrorCodes.Room.MarkDepartureCleaningDueNotAllowed] = "This room can't be marked for departure cleaning in its current state.",
            [ErrorCodes.Room.StartCleaningNotAllowed] = "Cleaning can't be started while the room is in its current state.",
            [ErrorCodes.Room.CompleteCleaningNotAllowed] = "Cleaning can't be completed while the room is in its current state.",
            [ErrorCodes.Room.ReportServiceNeededNotAllowed] = "Service can't be reported while the room is in its current state.",
            [ErrorCodes.Room.StartServiceNotAllowed] = "Service can't be started while the room is in its current state.",
            [ErrorCodes.Room.CompleteServiceNotAllowed] = "Service can't be completed while the room is in its current state.",
            [ErrorCodes.Room.ConcurrencyConflict] = "Someone else changed this room. Reload the page and start over.",

            // ----------------------------------------------------------
            // GÆST
            // ----------------------------------------------------------
            [ErrorCodes.Guest.NotFound] = "We couldn't find that guest.",
            [ErrorCodes.Guest.FirstNameRequired] = "Enter a first name.",
            [ErrorCodes.Guest.FirstNameTooLong] = "That first name is too long.",
            [ErrorCodes.Guest.LastNameRequired] = "Enter a last name.",
            [ErrorCodes.Guest.LastNameTooLong] = "That last name is too long.",
            [ErrorCodes.Guest.EmailRequired] = "Enter an email address.",
            [ErrorCodes.Guest.EmailInvalid] = "The email address must contain both '@' and '.'.",
            [ErrorCodes.Guest.EmailTooLong] = "That email address is too long.",
            [ErrorCodes.Guest.PhoneNumberRequired] = "Enter a phone number.",
            [ErrorCodes.Guest.PhoneNumberTooLong] = "That phone number is too long.",
            [ErrorCodes.Guest.CountryRequired] = "Enter a country.",
            [ErrorCodes.Guest.CountryTooLong] = "That country name is too long.",
            [ErrorCodes.Guest.PassportNumberTooLong] = "That passport number is too long.",

            // ----------------------------------------------------------
            // HELLIGDAGE (analysetjenesten)
            // ----------------------------------------------------------
            // Konfigurations- og nøglefejl er en driftsfejl: brugeren kan ikke rette dem, så
            // teksten er neutral. Detaljen står i loggen (HolidayApiClient).
            [ErrorCodes.Holidays.NotConfigured] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unauthorized] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unavailable] = "We couldn't reach the holiday service. Please try again later.",
            [ErrorCodes.Holidays.InvalidRequest] = "The selected years aren't valid.",
            [ErrorCodes.Holidays.UnexpectedResponse] = "Holiday data isn't available right now.",
            [ErrorCodes.Guest.HasBookings] = "This guest can't be deleted because they still have bookings.",
            [ErrorCodes.Guest.ConcurrencyConflict] = "Someone else changed this guest. Reload the page and start over."
        };

    /// <summary>
    /// Oversætter én fejlkode til dansk.
    /// </summary>
    /// <param name="error">Fejlen fra Application-laget.</param>
    /// <returns>Den danske tekst, eller <see cref="Unexpected"/> hvis koden er ukendt.</returns>
    public static string Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Translations.TryGetValue(error.Code, out var text) ? text : Unexpected;
    }

    /// <summary>
    /// Oversætter alle fejl i et resultat til danske tekster.
    /// </summary>
    /// <param name="result">Et fejlet resultat fra Application-laget.</param>
    /// <returns>Én tekst pr. fejl. Tom hvis resultatet lykkedes.</returns>
    /// <remarks>
    /// Flere fejl på én gang er meningen, ikke en fejl: BR-60 og BR-96 kræver at en formular
    /// kan vise alle valideringsfejl samtidig.
    /// </remarks>
    public static IReadOnlyList<string> Describe(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return Array.Empty<string>();
        }

        var texts = new string[result.Errors.Count];

        for (var i = 0; i < result.Errors.Count; i++)
        {
            texts[i] = Describe(result.Errors[i]);
        }

        return texts;
    }
}