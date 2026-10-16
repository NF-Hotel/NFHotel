namespace NFHotel.Application.Errors;

/// <summary>
/// The row was changed by someone else between read and write (optimistic concurrency).
/// </summary>
/// <remarks>
/// Translated from the concurrency error of the data access layer. The service returns an error code, so
/// the user can fetch again and start over — the old system ignored
/// <c>rowsAffected</c> and failed silently.
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">Technical message for the log.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error with an underlying database error.</summary>
    /// <param name="message">Technical message for the log.</param>
    /// <param name="innerException">The underlying error from the data access layer.</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}