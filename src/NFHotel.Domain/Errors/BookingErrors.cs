
namespace NFHotel.Domain.Errors;

/// <summary>
/// Fejlkoderne for brud på booking-invarianterne. Koden er kontrakten (A-09); Application oversætter den til tekst.
/// </summary>
public static class BookingErrors
{
    /// <summary>Fejlkode: perioden starter før i dag (BR-44, BR-104, BR-108).</summary>
    public const string StartDateInPast = "booking.start_date_in_past";

    /// <summary>Fejlkode: bookingen mangler en rumreference (BR-45, BR-105).</summary>
    public const string RoomRequired = "booking.room_required";

    /// <summary>Fejlkode: bookingen mangler en gæstereference (BR-106).</summary>
    public const string GuestRequired = "booking.guest_required";

    /// <summary>Fejlkode: udtjekningstidspunktet er ikke efter indtjekningstidspunktet (BR-107).</summary>
    public const string CheckOutBeforeCheckIn = "booking.check_out_before_check_in";

    /// <summary>Fejlkode: udtjekningsdatoen ligger før ankomstdatoen.</summary>
    public const string CheckOutDateBeforeStartDate = "booking.check_out_date_before_start_date";

    /// <summary>
    /// Fejlkode: udtjekningsdatoen ligger efter i dag. En dato i fremtiden ville spærre
    /// rummet frem til den (A-01) og kunne ikke rettes bagefter.
    /// </summary>
    public const string CheckOutDateInFuture = "booking.check_out_date_in_future";
}