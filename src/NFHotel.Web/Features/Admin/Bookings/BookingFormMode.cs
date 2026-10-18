namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// What the booking form has been opened for.
/// </summary>
/// <remarks>
/// The two modes share date choice and room search, but each hits its own use case:
/// <c>CreateAsync</c> and <c>RescheduleAsync</c>. They are deliberately not merged — a
/// reschedule must not be able to change the guest, and a creation has no booking to leave out of
/// the availability lookup.
/// </remarks>
public enum BookingFormMode
{
    /// <summary>Create a new booking (BR-01).</summary>
    Create = 0,

    /// <summary>Move an existing booking to new dates or a new room (BR-14).</summary>
    Reschedule = 1
}

/// <summary>
/// Whether the booking is to be linked to an existing guest or to one created now.
/// </summary>
/// <remarks>
/// Corresponds to BR-47: exactly one of <c>GuestId</c> and <c>NewGuest</c> may be filled in on
/// <c>CreateBookingRequest</c>. The choice here decides which of the two is sent along, so
/// the form can never send both.
/// </remarks>
public enum BookingGuestMode
{
    /// <summary>Choose a guest that already exists.</summary>
    Existing = 0,

    /// <summary>Create the guest as part of the booking.</summary>
    New = 1
}