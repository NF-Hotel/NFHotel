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
}
