namespace NFHotel.Application.Errors;

/// <summary>
/// Rækken blev ændret af en anden mellem læsning og skrivning (optimistisk samtidighed).
/// </summary>
/// <remarks>
/// Oversat fra dataadgangslagets samtidighedsfejl. Servicen returnerer en fejlkode, så
/// brugeren kan hente igen og prøve forfra — det gamle system ignorerede
/// <c>rowsAffected</c> og fejlede lydløst.
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>Opretter fejlen.</summary>
    /// <param name="message">Teknisk besked til loggen.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Opretter fejlen med en underliggende databasefejl.</summary>
    /// <param name="message">Teknisk besked til loggen.</param>
    /// <param name="innerException">Den underliggende fejl fra dataadgangslaget.</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}