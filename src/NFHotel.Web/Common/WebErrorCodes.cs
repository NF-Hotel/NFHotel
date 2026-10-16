namespace NFHotel.Web.Common;

/// <summary>
/// Fejlkoder der opstår i Web-laget selv.
/// </summary>
/// <remarks>
/// Application ejer alle forretningsfejlkoder (<c>ErrorCodes</c>). Denne klasse rummer kun
/// de koder Web selv kan frembringe, og der er i dag præcis én: den uventede fejl som
/// <see cref="UseCaseRunner{TService}"/> laver ud af en exception, så skærmene kan behandle
/// "databasen er nede" med den samme <c>Result</c>-kode de behandler alt andet med.
/// </remarks>
public static class WebErrorCodes
{
    /// <summary>
    /// Et use case kastede en uventet exception. Den tekniske detalje er logget; brugeren
    /// får en neutral besked.
    /// </summary>
    public const string Unexpected = "web.unexpected_error";
}