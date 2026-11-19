
namespace NFHotel.Domain.Errors;

/// <summary>
/// The error codes for breaches of the booking invariants. The code is the contract (A-09); Application translates it to text.
/// </summary>
public static class BookingErrors
{
    /// <summary>Error code: the period starts before today (BR-44, BR-104, BR-108).</summary>
    public const string StartDateInPast = "booking.start_date_in_past";

    /// <summary>Error code: the stay ends more than five years after today (BR-N-08, MIL-086, ADR-0050).</summary>
    public const string EndDateTooFarAhead = "booking.end_date_too_far_ahead";

    /// <summary>Error code: a stay booked or moved on the public site has more than 30 nights (BR-C-07, MIL-086).</summary>
    public const string StayTooLong = "booking.stay_too_long";

    /// <summary>Error code: the party of a public booking is not 1 to 4 guests (BR-C-07, MIL-086).</summary>
    public const string GuestsOutOfRange = "booking.guests_out_of_range";

    /// <summary>Error code: the booking has no room reference (BR-45, BR-105).</summary>
    public const string RoomRequired = "booking.room_required";

    /// <summary>Error code: the booking has no guest reference (BR-106).</summary>
    public const string GuestRequired = "booking.guest_required";

    /// <summary>Error code: the check-out time is not after the check-in time (BR-107).</summary>
    public const string CheckOutBeforeCheckIn = "booking.check_out_before_check_in";

    /// <summary>Error code: the check-out date is before the arrival date.</summary>
    public const string CheckOutDateBeforeStartDate = "booking.check_out_date_before_start_date";

    /// <summary>
    /// Error code: the check-out date is after today. A date in the future would block
    /// the room until then (A-01) and could not be corrected afterwards.
    /// </summary>
    public const string CheckOutDateInFuture = "booking.check_out_date_in_future";
}
