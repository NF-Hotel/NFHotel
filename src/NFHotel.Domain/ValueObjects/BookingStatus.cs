namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The life cycle of the booking.
/// </summary>
/// <remarks>
/// The ordinal values 0-4 are kept from the old system, because they already exist as int
/// in existing data (B-07). The values must never be renumbered without a data migration —
/// the database exclusion constraint refers directly to <c>status &lt;&gt; 4</c>.
/// </remarks>
public enum BookingStatus
{
    /// <summary>Created, but payment not guaranteed. The start status of every booking (BR-01).</summary>
    Pending = 0,

    /// <summary>
    /// Payment guaranteed. Is NOT a prerequisite for check-in — a walk-in goes directly
    /// from <see cref="Pending"/> to <see cref="CheckedIn"/> (B-01).
    /// </summary>
    Confirmed = 1,

    /// <summary>The guest has been checked in.</summary>
    CheckedIn = 2,

    /// <summary>
    /// The guest has been checked out. Blocks the room until the effective end date of the booking,
    /// not until the original <c>EndDate</c> (A-01).
    /// </summary>
    CheckedOut = 3,

    /// <summary>
    /// Cancelled. Soft delete — the row is never deleted (BR-13).
    /// The only status that releases the room completely (A-01).
    /// </summary>
    Cancelled = 4
}