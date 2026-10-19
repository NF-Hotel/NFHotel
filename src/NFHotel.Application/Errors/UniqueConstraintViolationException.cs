namespace NFHotel.Application.Errors;

/// <summary>
/// A unique constraint was broken — for example two rooms with the same room number (BR-N-01).
/// </summary>
/// <remarks>
/// Translated from <c>SQLSTATE 23505</c> in Infrastructure. The service checks explicitly before
/// it writes; this error is the race safeguard behind that check.
/// </remarks>
public sealed class UniqueConstraintViolationException : Exception
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">Technical message for the log.</param>
    /// <param name="constraintName">The name of the broken constraint. Used as a discriminator.</param>
    public UniqueConstraintViolationException(string message, string constraintName)
        : base(message)
    {
        ConstraintName = constraintName;
    }

    /// <summary>Creates the error with an underlying database error.</summary>
    /// <param name="message">Technical message for the log.</param>
    /// <param name="constraintName">The name of the broken constraint.</param>
    /// <param name="innerException">The underlying error from the data access layer.</param>
    public UniqueConstraintViolationException(string message, string constraintName, Exception innerException)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }

    /// <summary>The name of the constraint that was broken, for example <c>ux_room_room_number</c>.</summary>
    public string ConstraintName { get; }
}
