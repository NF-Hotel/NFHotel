using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.Web.Common;

/// <summary>
/// Translates the error codes of the Application layer to Danish user texts.
/// </summary>
/// <remarks>
/// A-09: the code is the contract, the text is the responsibility of the UI. Therefore the translation exists in exactly
/// one place — here — and not spread out in the components. A later API maps the same codes
/// to HTTP statuses without inheriting a single Danish string.
/// <para>
/// Unknown codes (and any unexpected exception) become <see cref="Unexpected"/>. We never
/// show a raw error code or an exception text to the user, cf. Conventions.md's
/// "Show the user neutral error messages".
/// </para>
/// </remarks>
public static class ErrorMessages
{
    /// <summary>
    /// The neutral message shown when something went wrong without a known error code.
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
            [ErrorCodes.Booking.OverviewPeriodInvalid] = "The end of the period can't be before its start.",
            [ErrorCodes.Booking.GuestSelectionInvalid] = "Pick an existing guest or create a new one — not both.",
            [ErrorCodes.Booking.ConcurrencyConflict] = "Someone else changed this booking. Reload the page and start over.",

            // ----------------------------------------------------------
            // ROOM
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
            // GUEST
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
            // Configuration and key errors are an operations fault: the user cannot fix them, so
            // the text is neutral. The detail is in the log (HolidayApiClient).
            [ErrorCodes.Api.Unavailable] = "We couldn't reach the hotel system. Please try again in a moment.",
            [ErrorCodes.Api.Unauthorized] = "The hotel system refused this application. Please tell the administrator.",
            [ErrorCodes.Api.UnexpectedResponse] = "The hotel system answered something we couldn't use. Please try again.",
            [ErrorCodes.Api.StaffUnauthorized] = "Your session has ended. Sign in again.",
            [ErrorCodes.Api.Forbidden] = "Your role isn't allowed to do that.",
            [ErrorCodes.Staff.InvalidCredentials] = "The user name or the password is wrong.",
            [ErrorCodes.Staff.TooManyAttempts] = "Too many failed attempts. Wait fifteen minutes and try again.",
            [ErrorCodes.Benchmark.NotFound] = "The benchmark run was not found.",
            [ErrorCodes.Benchmark.SchemaVersionUnsupported] = "The result uses a schema version that is not supported.",
            [ErrorCodes.Benchmark.BodyInvalid] = "The benchmark result is incomplete.",
            [ErrorCodes.Staff.PasswordInvalid] = "The password must be between 12 and 256 characters.",
            [ErrorCodes.Staff.UserNameTaken] = "That user name is already in use.",
            [ErrorCodes.Staff.UserNameRequired] = "Enter a user name.",
            [ErrorCodes.Staff.UserNameTooLong] = "The user name is too long.",
            [ErrorCodes.Staff.RoleInvalid] = "Select a role.",
            [ErrorCodes.Holidays.NotConfigured] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unauthorized] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unavailable] = "We couldn't reach the holiday service. Please try again later.",
            [ErrorCodes.Holidays.InvalidRequest] = "The selected years aren't valid.",
            [ErrorCodes.Holidays.UnexpectedResponse] = "Holiday data isn't available right now.",
            [ErrorCodes.Guest.HasBookings] = "This guest can't be deleted because they still have bookings.",
            [ErrorCodes.Guest.ConcurrencyConflict] = "Someone else changed this guest. Reload the page and start over."
        };

    /// <summary>
    /// Translates one error code to Danish.
    /// </summary>
    /// <param name="error">The error from the Application layer.</param>
    /// <returns>The Danish text, or <see cref="Unexpected"/> if the code is unknown.</returns>
    public static string Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Translations.TryGetValue(error.Code, out var text) ? text : Unexpected;
    }

    /// <summary>
    /// Translates all errors in a result to Danish texts.
    /// </summary>
    /// <param name="result">A failed result from the Application layer.</param>
    /// <returns>One text per error. Empty if the result succeeded.</returns>
    /// <remarks>
    /// Several errors at once is the intention, not a fault: BR-60 and BR-96 require that a form
    /// can show all validation errors at the same time.
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
