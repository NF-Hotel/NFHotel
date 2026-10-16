namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// The status transitions the booking overview can perform on one booking.
/// </summary>
/// <remarks>
/// Corresponds one-to-one to the five fields of <c>BookingActionsDto</c> minus <c>CanReschedule</c>,
/// which requires a form with dates and room and therefore belongs to the reschedule screen.
/// <para>
/// An enum and not a string: a button that points to a transition that does not exist must be a
/// compiler error, not a silent nothing.
/// </para>
/// </remarks>
public enum BookingAction
{
    /// <summary>Confirm the booking (BR-02).</summary>
    Confirm = 0,

    /// <summary>Check the guest in (BR-06).</summary>
    CheckIn = 1,

    /// <summary>Check the guest out (BR-09).</summary>
    CheckOut = 2,

    /// <summary>Cancel the booking (BR-11). Requires a confirmation first (BR-04).</summary>
    Cancel = 3
}