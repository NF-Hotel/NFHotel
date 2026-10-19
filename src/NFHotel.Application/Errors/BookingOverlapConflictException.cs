namespace NFHotel.Application.Errors;

/// <summary>
/// The last safeguard of the database against double booking kicked in (B-03).
/// </summary>
/// <remarks>
/// Overlap is rejected in two places, and they do not do the same: <c>BookingService</c> asks
/// the domain (the normal path, a nice error code, testable without a database), while PostgreSQL's
/// exclusion constraint catches the <i>race</i> between two simultaneous creations that both
/// saw a free room.
/// <para>
/// Infrastructure translates <c>SQLSTATE 23P01</c> to this type, because Application must not
/// know <c>PostgresException</c>. The service catches it and returns
/// <see cref="ErrorCodes.Booking.RoomNotAvailable"/> — the same error code as the service check,
/// so the user sees the same thing whichever safeguard stopped her.
/// </para>
/// </remarks>
public sealed class BookingOverlapConflictException : Exception
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">Technical message for the log. Never shown to the user.</param>
    public BookingOverlapConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error with an underlying database error.</summary>
    /// <param name="message">Technical message for the log.</param>
    /// <param name="innerException">The underlying error from the data access layer.</param>
    public BookingOverlapConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
