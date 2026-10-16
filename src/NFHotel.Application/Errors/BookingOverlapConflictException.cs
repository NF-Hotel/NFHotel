namespace NFHotel.Application.Errors;

/// <summary>
/// Databasens sidste værn mod dobbeltbooking slog til (B-03).
/// </summary>
/// <remarks>
/// Overlap afvises to steder, og de gør ikke det samme: <c>BookingService</c> spørger
/// domænet (den normale vej, pæn fejlkode, testbar uden database), mens PostgreSQLs
/// exclusion constraint fanger <i>kapløbet</i> mellem to samtidige oprettelser der begge
/// så et ledigt rum.
/// <para>
/// Infrastructure oversætter <c>SQLSTATE 23P01</c> til denne type, fordi Application ikke
/// må kende <c>PostgresException</c>. Servicen fanger den og returnerer
/// <see cref="ErrorCodes.Booking.RoomNotAvailable"/> — samme fejlkode som servicetjekket,
/// så brugeren ser det samme uanset hvilket værn der stoppede hende.
/// </para>
/// </remarks>
public sealed class BookingOverlapConflictException : Exception
{
    /// <summary>Opretter fejlen.</summary>
    /// <param name="message">Teknisk besked til loggen. Vises aldrig til brugeren.</param>
    public BookingOverlapConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Opretter fejlen med en underliggende databasefejl.</summary>
    /// <param name="message">Teknisk besked til loggen.</param>
    /// <param name="innerException">Den underliggende fejl fra dataadgangslaget.</param>
    public BookingOverlapConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}