namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Rules that belong to <see cref="BookingStatus"/>.
/// </summary>
public static class BookingStatusExtensions
{
    /// <summary>
    /// Decides whether a booking with the given status occupies the room.
    /// </summary>
    /// <param name="status">The status of the booking.</param>
    /// <returns>True for everything except <see cref="BookingStatus.Cancelled"/>.</returns>
    /// <remarks>
    /// Only cancellation releases the room completely. A checked-out booking still blocks — but
    /// only until its effective end date, so early check-out still releases the
    /// remaining nights (A-01). Corresponds to the database <c>WHERE (status &lt;&gt; 4)</c>.
    /// </remarks>
    public static bool BlocksRoom(this BookingStatus status) => status != BookingStatus.Cancelled;

    /// <summary>
    /// Decides whether a booking with the given status is still open: it may be cancelled and moved (BR-11, BR-14).
    /// </summary>
    /// <param name="status">The status of the booking.</param>
    /// <returns>True for <see cref="BookingStatus.Pending"/> and <see cref="BookingStatus.Confirmed"/>.</returns>
    /// <remarks>
    /// The guest page reads what it may offer from the status of the receipt alone (ADR-0049 decision 7), so the rule
    /// lives here once, beside the booking that applies it.
    /// </remarks>
    public static bool IsOpen(this BookingStatus status) => status is BookingStatus.Pending or BookingStatus.Confirmed;
}
