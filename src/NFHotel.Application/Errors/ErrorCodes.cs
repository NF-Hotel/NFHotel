using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Errors;

/// <summary>
/// Combined catalogue of the error codes the Application layer can return (A-09).
/// </summary>
/// <remarks>
/// <b>The domain owns its own codes.</b> The constants below <i>reuse</i> them
/// (<c>= BookingErrors.StartDateInPast</c>) rather than inventing parallel codes — so the
/// two layers cannot drift apart, and a search for the code hits both the rule and its
/// translation.
/// <para>
/// The codes that end in <c>.invalid_state</c> are the ones
/// <see cref="InvalidStateTransitionException"/> generates from the entity name and the method name.
/// They are listed here so the UI and Phase 3's ledger audit can enumerate them without guessing the form.
/// </para>
/// <para>
/// Only the codes that start with something the domain does not know — "does not exist", "room is
/// occupied", "concurrency conflict" — are defined here from scratch. They belong to
/// the use case, not to the invariant.
/// </para>
/// </remarks>
public static class ErrorCodes
{
    /// <summary>Error codes for bookings.</summary>
    public static class Booking
    {
        /// <summary>The booking does not exist (BR-115).</summary>
        public const string NotFound = "booking.not_found";

        /// <summary>The arrival date is not filled in (BR-101).</summary>
        public const string StartDateRequired = DateRange.StartRequired;

        /// <summary>The departure date is not filled in (BR-102).</summary>
        public const string EndDateRequired = DateRange.EndRequired;

        /// <summary>The departure date is not after the arrival date (BR-38, BR-43, BR-88, BR-103).</summary>
        public const string EndNotAfterStart = DateRange.EndNotAfterStart;

        /// <summary>The period starts before today (BR-44, BR-104, BR-108).</summary>
        public const string StartDateInPast = BookingErrors.StartDateInPast;

        /// <summary>The booking has no room reference (BR-45, BR-105).</summary>
        public const string RoomRequired = BookingErrors.RoomRequired;

        /// <summary>The booking has no guest reference (BR-106).</summary>
        public const string GuestRequired = BookingErrors.GuestRequired;

        /// <summary>The check-out time is not after the check-in time (BR-107).</summary>
        public const string CheckOutBeforeCheckIn = BookingErrors.CheckOutBeforeCheckIn;

        /// <summary>The check-out date is before the arrival date.</summary>
        public const string CheckOutDateBeforeStartDate = BookingErrors.CheckOutDateBeforeStartDate;

        /// <summary>
        /// The check-out date is after today.
        /// </summary>
        /// <remarks>
        /// A date in the future would push the effective end date of the booking forward and block
        /// the room until then (A-01) — and the typing mistake cannot be corrected afterwards, because a
        /// <c>CheckedOut</c> booking can be neither rescheduled nor cancelled.
        /// </remarks>
        public const string CheckOutDateInFuture = BookingErrors.CheckOutDateInFuture;

        /// <summary>The booking may not be confirmed from its current status (BR-02, BR-121).</summary>
        public const string ConfirmNotAllowed = "booking.confirm.invalid_state";

        /// <summary>The booking may not be checked in now (BR-06, BR-07).</summary>
        public const string CheckInNotAllowed = "booking.check_in.invalid_state";

        /// <summary>The booking may not be checked out (BR-09, BR-10).</summary>
        public const string CheckOutNotAllowed = "booking.check_out.invalid_state";

        /// <summary>The booking may not be cancelled (BR-11, BR-122).</summary>
        public const string CancelNotAllowed = "booking.cancel.invalid_state";

        /// <summary>The booking may not be moved (BR-14, BR-123).</summary>
        public const string RescheduleNotAllowed = "booking.reschedule.invalid_state";

        /// <summary>The room is already occupied in the desired period (BR-19, B-03).</summary>
        /// <remarks>
        /// The same code whether the service check or the database exclusion constraint
        /// stopped it — the difference is visible only in the log.
        /// </remarks>
        public const string RoomNotAvailable = "booking.room_not_available";

        /// <summary>The room cannot be let: it is out of service or blocked (BR-110).</summary>
        public const string RoomNotBookable = "booking.room_not_bookable";

        /// <summary>The end of the overview period is before its start (MIL-013). A period of one day is allowed.</summary>
        public const string OverviewPeriodInvalid = "booking.overview_period_invalid";

        /// <summary>Neither an existing guest was chosen nor a new one given — or both (BR-47).</summary>
        public const string GuestSelectionInvalid = "booking.guest_selection_invalid";

        /// <summary>The booking was changed by someone else in the meantime. Fetch again and start over.</summary>
        public const string ConcurrencyConflict = "booking.concurrency_conflict";
    }

    /// <summary>Error codes for rooms.</summary>
    public static class Room
    {
        /// <summary>The room does not exist (BR-115).</summary>
        public const string NotFound = "room.not_found";

        /// <summary>Room number is missing (BR-97).</summary>
        public const string RoomNumberRequired = RoomErrors.RoomNumberRequired;

        /// <summary>The room number is too long (A-10, BR-N-03).</summary>
        public const string RoomNumberTooLong = RoomErrors.RoomNumberTooLong;

        /// <summary>The floor is not greater than 0 (BR-98).</summary>
        public const string FloorInvalid = RoomErrors.FloorInvalid;

        /// <summary>The room type is not a defined value (BR-99).</summary>
        public const string SizeInvalid = RoomErrors.SizeInvalid;

        /// <summary>The capacity is not greater than 0 (BR-100).</summary>
        public const string CapacityInvalid = RoomErrors.CapacityInvalid;

        /// <summary>Another room already has that room number (BR-N-01).</summary>
        public const string RoomNumberAlreadyExists = "room.room_number_already_exists";

        /// <summary>The room cannot be deleted, because there are bookings for it (BR-78, BR-112).</summary>
        public const string HasBookings = "room.has_bookings";

        /// <summary>The room cannot be set to daily cleaning from its current state (BR-N-06).</summary>
        public const string MarkDailyCleaningDueNotAllowed = "room.mark_daily_cleaning_due.invalid_state";

        /// <summary>The room cannot be set to departure cleaning from its current state (BR-N-06).</summary>
        public const string MarkDepartureCleaningDueNotAllowed = "room.mark_departure_cleaning_due.invalid_state";

        /// <summary>The cleaning cannot be started: the room is not waiting for cleaning (BR-N-06).</summary>
        public const string StartCleaningNotAllowed = "room.start_cleaning.invalid_state";

        /// <summary>The cleaning cannot be completed: it is not in progress (BR-N-06).</summary>
        public const string CompleteCleaningNotAllowed = "room.complete_cleaning.invalid_state";

        /// <summary>Service cannot be reported: it is already in progress (BR-N-06).</summary>
        public const string ReportServiceNeededNotAllowed = "room.report_service_needed.invalid_state";

        /// <summary>The service cannot be started: the room is not waiting for service (BR-N-06).</summary>
        public const string StartServiceNotAllowed = "room.start_service.invalid_state";

        /// <summary>The service cannot be completed: it is not in progress (BR-N-06).</summary>
        public const string CompleteServiceNotAllowed = "room.complete_service.invalid_state";

        /// <summary>The room was changed by someone else in the meantime. Fetch again and start over.</summary>
        public const string ConcurrencyConflict = "room.concurrency_conflict";
    }

    /// <summary>Error codes of the connection from the web application to the database API (ADR-0004).</summary>
    public static class Api
    {
        /// <summary>The API could not be reached, did not answer in time or failed (network, timeout, HTTP 5xx).</summary>
        public const string Unavailable = "api.unavailable";

        /// <summary>The API refused the token of this application (HTTP 401).</summary>
        public const string Unauthorized = "api.unauthorized";

        /// <summary>The API answered something this application cannot use (unreadable content, a missing value).</summary>
        public const string UnexpectedResponse = "api.unexpected_response";

        /// <summary>The call needs a valid staff token and had none (ADR-0005).</summary>
        public const string StaffUnauthorized = "api.staff_unauthorized";

        /// <summary>The role of the signed-in member of staff lacks the permission (ADR-0005).</summary>
        public const string Forbidden = "api.forbidden";
    }

    /// <summary>Codes of the staff sign-in and accounts (ADR-0005).</summary>
    public static class Staff
    {
        /// <summary>The user name or the password is wrong, or the account is inactive; the cases are not told apart.</summary>
        public const string InvalidCredentials = "staff.invalid_credentials";

        /// <summary>The user name has used up its attempts in the window.</summary>
        public const string TooManyAttempts = "staff.too_many_attempts";

        /// <summary>The password is shorter or longer than allowed.</summary>
        public const string PasswordInvalid = "staff.password_invalid";

        /// <summary>The user name exists already.</summary>
        public const string UserNameTaken = "staff.user_name_taken";

        /// <summary>The user name is empty.</summary>
        public const string UserNameRequired = StaffErrors.UserNameRequired;

        /// <summary>The user name is too long.</summary>
        public const string UserNameTooLong = StaffErrors.UserNameTooLong;

        /// <summary>The role is not a known role.</summary>
        public const string RoleInvalid = StaffErrors.RoleInvalid;
    }

    /// <summary>Error codes for the holiday calendar of the analysis service.</summary>
    /// <remarks>
    /// The codes describe what the caller can do about the error, not the HTTP details:
    /// <see cref="NotConfigured"/> and <see cref="Unauthorized"/> are an operations fault,
    /// <see cref="Unavailable"/> is worth trying again later.
    /// </remarks>
    public static class Holidays
    {
        /// <summary>The address or API key of the service is not set in the configuration.</summary>
        public const string NotConfigured = "holidays.not_configured";

        /// <summary>The service rejected the API key (HTTP 401/403).</summary>
        public const string Unauthorized = "holidays.unauthorized";

        /// <summary>The service could not be reached, did not answer in time or failed (network, timeout, HTTP 5xx).</summary>
        public const string Unavailable = "holidays.unavailable";

        /// <summary>The years are invalid (for example first year after last year), or the service rejected them (HTTP 400/422).</summary>
        public const string InvalidRequest = "holidays.invalid_request";

        /// <summary>The service answered something we cannot use (unexpected status or unreadable content).</summary>
        public const string UnexpectedResponse = "holidays.unexpected_response";
    }

    /// <summary>Error codes for guests.</summary>
    public static class Guest
    {
        /// <summary>The guest does not exist (BR-115).</summary>
        public const string NotFound = "guest.not_found";

        /// <summary>First name is missing (BR-52, BR-91).</summary>
        public const string FirstNameRequired = GuestErrors.FirstNameRequired;

        /// <summary>The first name is too long (A-10, BR-N-03).</summary>
        public const string FirstNameTooLong = GuestErrors.FirstNameTooLong;

        /// <summary>Last name is missing (BR-53, BR-92).</summary>
        public const string LastNameRequired = GuestErrors.LastNameRequired;

        /// <summary>The last name is too long (A-10, BR-N-03).</summary>
        public const string LastNameTooLong = GuestErrors.LastNameTooLong;

        /// <summary>Email is missing (BR-93).</summary>
        public const string EmailRequired = GuestErrors.EmailRequired;

        /// <summary>The email lacks '@' or '.' (BR-55, BR-93).</summary>
        public const string EmailInvalid = GuestErrors.EmailInvalid;

        /// <summary>The email is too long (A-10, BR-N-03).</summary>
        public const string EmailTooLong = GuestErrors.EmailTooLong;

        /// <summary>Phone number is missing (BR-54, BR-94).</summary>
        public const string PhoneNumberRequired = GuestErrors.PhoneNumberRequired;

        /// <summary>The phone number is too long (A-10, BR-N-03).</summary>
        public const string PhoneNumberTooLong = GuestErrors.PhoneNumberTooLong;

        /// <summary>Country is missing (BR-56, BR-95).</summary>
        public const string CountryRequired = GuestErrors.CountryRequired;

        /// <summary>The country is too long (A-10, BR-N-03).</summary>
        public const string CountryTooLong = GuestErrors.CountryTooLong;

        /// <summary>The passport number is too long (A-10, BR-N-03).</summary>
        public const string PassportNumberTooLong = GuestErrors.PassportNumberTooLong;

        /// <summary>The guest cannot be deleted, because there are bookings for the guest (BR-N-02).</summary>
        public const string HasBookings = "guest.has_bookings";

        /// <summary>The guest was changed by someone else in the meantime. Fetch again and start over.</summary>
        public const string ConcurrencyConflict = "guest.concurrency_conflict";
    }

    /// <summary>Codes of the stored benchmark runs (ADR-0008). The field codes are those of the domain.</summary>
    public static class Benchmark
    {
        /// <summary>No run or request has that key.</summary>
        public const string NotFound = "benchmark.not_found";

        /// <summary>The result schema version is not the one that this API reads.</summary>
        public const string SchemaVersionUnsupported = "benchmark.schema_version_unsupported";

        /// <summary>The body is missing, or a part of it is missing.</summary>
        public const string BodyInvalid = "benchmark.body_invalid";
    }

}